/*
 * TuneLift - Export iTunes audio playlists as standard or extended .m3u files.
 * Copyright (C) 2020 Richard Lawrence
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see
 * <https://www.gnu.org/licenses/>.
 */

using System.Runtime.InteropServices;

namespace TuneLift
{
    /// <summary>
    /// Talks to the classic iTunes app through its COM interface: connects, finds the playlists
    /// to export, reads their tracks and disconnects. The calls are late-bound (dynamic), so every
    /// property name used here must match the iTunes COM API exactly.
    /// </summary>
    internal sealed class ITunesCOM
    {
        /// <summary>
        /// Connects to iTunes, exports every wanted playlist, then disconnects.
        /// </summary>
        /// <returns>True if iTunes was reached and the playlists were exported, false if not.</returns>
        public static bool ExportPlaylists()
        {
            dynamic? iTunes = Connect();
            if (iTunes == null)
                return false;

            try
            {
                return ExportPlaylists(iTunes);
            }
            finally
            {
                Disconnect(iTunes);
            }
        }

        /// <summary>
        /// Starts or attaches to iTunes.
        /// </summary>
        /// <returns>The iTunes application object, or null if it could not be reached.</returns>
        private static dynamic? Connect()
        {
            Type? iTunesAppType = Type.GetTypeFromProgID("iTunes.Application");
            if (iTunesAppType == null)
            {
                Logger.Write("iTunes does not appear to be installed on this computer.");
                ConsoleOutput.ShowComFailure();
                return null;
            }

            Logger.Write("Connecting to iTunes...");

            // Since we cannot tell from the COM interface if iTunes is running or not, we will cheat by
            // checking the process list and assuming that if it is running, it will not be launched by TuneLift.
            Globals.ITunesLaunchedByUs = System.Diagnostics.Process.GetProcessesByName("iTunes").Length == 0;

            try
            {
                return Activator.CreateInstance(iTunesAppType);
            }
            catch (Exception ex)
            {
                Logger.Write($"Unable to connect to iTunes: {ex.Message}");
                ConsoleOutput.ShowComFailure();
                return null;
            }
        }

        /// <summary>
        /// Closes iTunes if the user asked for that and TuneLift started it, then releases the COM object.
        /// </summary>
        /// <param name="iTunes">The iTunes application object.</param>
        private static void Disconnect(dynamic iTunes)
        {
            // This is wrapped in a try because even if we can't or don't want to close iTunes,
            // we still need to release the COM object.
            try
            {
                // Did the user want us to close iTunes?
                if (!Globals.CloseiTunesAfterExport)
                    return;

                // If we didn't launch iTunes, we should not close it
                if (!Globals.ITunesLaunchedByUs)
                {
                    Logger.Write("iTunes was not started by TuneLift; leaving it running.");
                    return;
                }

                // We need to now close iTunes
                Logger.Write("Attempting to close iTunes...");
                iTunes.Quit();
            }
            catch (Exception ex)
            {
                Logger.Write($"Unable to close iTunes: {ex.Message}");
            }
            finally
            {
                // Release the COM object whether or not iTunes was closed above
                Marshal.ReleaseComObject(iTunes);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Finds the playlists the user wants, then reads and writes each one.
        /// </summary>
        /// <param name="iTunes">The iTunes application object.</param>
        /// <returns>True if the playlists were exported, false if iTunes' library could not be read.</returns>
        private static bool ExportPlaylists(dynamic iTunes)
        {
            Logger.Write("Getting playlist details...");

            dynamic? library = iTunes.LibraryPlaylist;
            if (library == null)
            {
                Logger.Write("Unable to access iTunes library.");
                return false;
            }

            dynamic? playlistsCollection = iTunes.LibrarySource?.Playlists;
            if (playlistsCollection == null)
            {
                Logger.Write("Unable to access iTunes playlists.");
                return false;
            }

            // Work out which playlists we want to export and how many tracks they contain
            List<(dynamic ComPlaylist, Globals.Playlist Playlist)> wanted = [];
            int totalTracks = 0;

            foreach (dynamic comPlaylist in playlistsCollection)
            {
                if (comPlaylist == null)
                    continue;  // Skip null playlists

                Globals.Playlist? playlist = ReadPlaylist(comPlaylist, true);
                if (playlist == null)
                    continue;

                wanted.Add((comPlaylist, playlist));
                totalTracks += playlist.TrackCount;
            }

            Logger.Write($"Found {GrammarHelper.Pluralise(wanted.Count, "playlist", "playlists")} (totalling {GrammarHelper.Pluralise(totalTracks, "track", "tracks")}) to export.");

            // Don't delete the previous export unless there is something to replace it with
            if (wanted.Count == 0)
            {
                Logger.Write("Nothing to export, existing playlists left in place.");
                return true;
            }

            if (Globals.DeleteExisting && !FileOperations.DeleteExistingFiles())
                return false;

            // Now read and export the tracks of each playlist we want
            int playlistCount = 0;
            int failedCount = 0;

            foreach (var (comPlaylist, playlist) in wanted)
            {
                playlistCount++;
                Logger.Write($"Exporting {playlistCount}/{wanted.Count}: {playlist.Name} ({GrammarHelper.Pluralise(playlist.TrackCount, "track", "tracks")})");

                try
                {
                    ReadTracks(comPlaylist, playlist);
                    M3uWriter.WritePlaylist(playlist);
                }
                catch (Exception ex)
                {
                    Logger.Write($"Failed to export playlist {playlist.Name}: {ex.Message}");
                    failedCount++;
                }
            }

            // Each failure has already been logged, so just return true or false
            if (failedCount == 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Reads the details of an iTunes playlist, if it is one we might export.
        /// </summary>
        /// <param name="comPlaylist">iTunes playlist.</param>
        /// <param name="showNotification">Output to the user if this playlist has been skipped.</param>
        /// <returns>The playlist without its tracks, or null if it should not be exported.</returns>
        private static Globals.Playlist? ReadPlaylist(dynamic comPlaylist, bool showNotification)
        {
            // Needs to be a user playlist (ITPlaylistKindUser = 2)
            if ((int)comPlaylist.Kind != 2)
                return null;

            // ...and needs to be visible
            if (!(bool)comPlaylist.Visible)
                return null;

            // ...and not special (ITUserPlaylistSpecialKindNone = 0)
            if ((int)comPlaylist.SpecialKind != 0)
                return null;

            // Set the details of the playlist now
            Globals.Playlist playlist = new()
            {
                Name = (string)comPlaylist.Name,
                IsSmart = (bool)comPlaylist.Smart,
                TrackCount = (int)comPlaylist.Tracks.Count
            };

            // ...but we might not want to export it depending on the users preferences
            if (!M3uWriter.IsWantedPlaylist(playlist, showNotification))
                return null;

            return playlist;
        }

        /// <summary>
        /// Reads the audio tracks of an iTunes playlist into the playlist.
        /// </summary>
        /// <param name="comPlaylist">iTunes playlist.</param>
        /// <param name="playlist">The playlist to add the tracks to.</param>
        private static void ReadTracks(dynamic comPlaylist, Globals.Playlist playlist)
        {
            // Sort the tracks by PlayOrderIndex. iTunes does not expose column sorts (such as Artist or
            // Date Added) or smart playlist sorting rules through COM, so the default order of
            // playlist.Tracks is unreliable. PlayOrderIndex does not always match the order on screen
            // either, but it is the closest available (see GitHub issues #5 and #8).

            // Read each PlayOrderIndex once (every COM call is slow), then sort on the saved numbers.
            // OrderBy is stable, so tracks with the same index keep iTunes' own order.

            List<(int PlayOrder, dynamic Track)> readTracks = [];
            foreach (dynamic track in comPlaylist.Tracks)
                readTracks.Add(((int)track.PlayOrderIndex, track));

            // Now iterate through the tracks and keep the audio files

            foreach (var (_, track) in readTracks.OrderBy(t => t.PlayOrder))
            {
                if (track == null)
                    continue;  // Skip null tracks

                // Check track kind (ITTrackKindFile = 1) and file type
                if ((int)track.Kind != 1)
                    continue;

                string? location = (string?)track.Location;
                if (location == null || !M3uWriter.IsAudioFile(location))
                    continue;

                playlist.Tracks.Add(new Globals.Track
                {
                    Location = location,
                    Duration = (int)track.Duration,
                    Artist = (string)track.Artist,
                    Name = (string)track.Name
                });
            }
        }
    }
}
