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

using System.Globalization;
using System.Xml.Linq;

namespace TuneLift
{
    /// <summary>
    /// Reads playlists from an iTunes XML library file instead of from iTunes itself,
    /// then hands them to the M3uWriter.
    /// </summary>
    internal sealed class ITunesXML
    {
        /// <summary>
        /// Reads the XML library named in Globals.XmlLibrary and exports every wanted playlist.
        /// </summary>
        /// <returns>True if the playlists were exported, false if the library could not be read.</returns>
        public static bool ExportPlaylists()
        {
            Logger.Write($"Reading {Globals.XmlLibrary}...");

            XDocument document;

            try
            {
                document = XDocument.Load(Globals.XmlLibrary);
            }
            catch (Exception ex)
            {
                Logger.Write($"Unable to read the XML library: {ex.Message}");
                return false;
            }

            // The top level of the file is one dict. Its "Tracks" key holds a dict of every track,
            // and its "Playlists" key holds an array of playlist dicts.
            XElement? rootDict = document.Root?.Element("dict");
            if (rootDict == null)
            {
                Logger.Write("The XML library is not in the format iTunes uses.");
                return false;
            }

            Dictionary<string, XElement> library = ReadDict(rootDict);
            if (!library.TryGetValue("Tracks", out XElement? tracksElement) || !library.TryGetValue("Playlists", out XElement? playlistsElement))
            {
                Logger.Write("The XML library has no tracks or playlists.");
                return false;
            }

            Logger.Write("Getting playlist details...");

            // Read the audio files, keyed by track ID, so playlists can find them
            Dictionary<string, Globals.Track> tracks = [];

            foreach (var (trackID, trackElement) in ReadDict(tracksElement))
            {
                Dictionary<string, XElement> trackDict = ReadDict(trackElement);

                // Streams and tracks that are not on this computer have no local file
                string? location = GetText(trackDict, "Location");
                if (location == null || !Uri.TryCreate(location, UriKind.Absolute, out Uri? uri) || !uri.IsFile)
                    continue;

                // iTunes writes file://localhost/C:/Music/song.mp3, but Uri.LocalPath would read "localhost"
                // as a network server. Decode the path ourselves. A network share comes out as //server/share/song.mp3.
                string path = uri.LocalPath;
                if (uri.Host == "localhost")
                {
                    path = Uri.UnescapeDataString(uri.AbsolutePath).Replace('/', '\\');

                    // A drive letter has a leading slash to remove (\C:\Music), but a network share keeps both (\\server\share)
                    if (path.Length > 2 && path[0] == '\\' && path[2] == ':')
                        path = path[1..];
                }

                // If this isn't an audio file we can export then ignore it
                if (!M3uWriter.IsAudioFile(path))
                    continue;

                // Total Time is in milliseconds
                int duration = 0;
                if (int.TryParse(GetText(trackDict, "Total Time"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int milliseconds))
                    duration = milliseconds / 1000;

                tracks[trackID] = new Globals.Track
                {
                    Location = path,
                    Duration = duration,
                    Artist = GetText(trackDict, "Artist") ?? "",
                    Name = GetText(trackDict, "Name") ?? ""
                };
            }

            // Work out which playlists we want to export and how many tracks they contain
            List<(Dictionary<string, XElement> PlaylistDict, Globals.Playlist Playlist)> wanted = [];
            int totalTracks = 0;

            foreach (XElement playlistElement in playlistsElement.Elements("dict"))
            {
                Dictionary<string, XElement> playlistDict = ReadDict(playlistElement);

                // Skip the library itself, folders and iTunes' own playlists (Music, Movies and so on)
                if (IsTrue(playlistDict, "Master") || IsTrue(playlistDict, "Folder") || playlistDict.ContainsKey("Distinguished Kind"))
                    continue;

                // ...and playlists that iTunes hides
                if (playlistDict.TryGetValue("Visible", out XElement? visible) && visible.Name == "false")
                    continue;

                // ... and ones with no name
                string? name = GetText(playlistDict, "Name");
                if (name == null)
                    continue;

                int trackCount = 0;
                if (playlistDict.TryGetValue("Playlist Items", out XElement? items))
                    trackCount = items.Elements("dict").Count();

                Globals.Playlist playlist = new()
                {
                    Name = name,
                    IsSmart = playlistDict.ContainsKey("Smart Info"),
                    TrackCount = trackCount
                };

                // Check if the user actually wants this playlist
                if (!M3uWriter.IsWantedPlaylist(playlist, true))
                    continue;

                wanted.Add((playlistDict, playlist));
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

            // Now add the audio tracks of each playlist we want, in playlist order, and export it
            int playlistCount = 0;
            int failedCount = 0;

            foreach (var (playlistDict, playlist) in wanted)
            {
                playlistCount++;
                Logger.Write($"Exporting {playlistCount}/{wanted.Count}: {playlist.Name} ({GrammarHelper.Pluralise(playlist.TrackCount, "track", "tracks")})");

                try
                {
                    foreach (XElement item in playlistDict["Playlist Items"].Elements("dict"))
                    {
                        string? trackID = GetText(ReadDict(item), "Track ID");

                        if (trackID != null && tracks.TryGetValue(trackID, out Globals.Track? track))
                            playlist.Tracks.Add(track);
                    }

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
        /// Turns a plist dict into a lookup.
        /// </summary>
        /// <param name="dict">The dict element.</param>
        /// <returns>The value elements, by key. A key that appears twice keeps its last value.</returns>
        private static Dictionary<string, XElement> ReadDict(XElement dict)
        {
            Dictionary<string, XElement> values = [];
            List<XElement> children = [.. dict.Elements()];

            for (int i = 0; i + 1 < children.Count; i += 2)
            {
                if (children[i].Name == "key")
                    values[children[i].Value] = children[i + 1];
            }

            return values;
        }

        /// <summary>
        /// Gets the text of a string or number value from a plist dict.
        /// </summary>
        /// <param name="dict">The dict, as read by ReadDict.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>The text of the value, or null if the key is not there.</returns>
        private static string? GetText(Dictionary<string, XElement> dict, string key)
        {
            if (dict.TryGetValue(key, out XElement? value))
                return value.Value;

            return null;
        }

        /// <summary>
        /// Checks for a true flag in a plist dict. Plists write a flag as an empty true or false element,
        /// and leave out flags that are not set.
        /// </summary>
        /// <param name="dict">The dict, as read by ReadDict.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key is there and its value is true.</returns>
        private static bool IsTrue(Dictionary<string, XElement> dict, string key)
        {
            return dict.TryGetValue(key, out XElement? value) && value.Name == "true";
        }
    }
}
