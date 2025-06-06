﻿/*
 * TuneLift - Export iTunes audio playlists to M3U format.
 * Copyright (C) 2020-2025 Richard Lawrence
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

/*
 * This program uses the forklist icon, created by nawicon, from Flaticon.
 * https://www.flaticon.com/free-icons/forklift
 */

using System.Reflection;
using static TuneLift.Helpers;

namespace TuneLift
{
    public class Program
    {
        public static string exportFolder = "";
        public static bool ignoreSmartPlaylists = false;
        public static bool ignorePlaylists = false;
        public static string ignorePrefix = "";
        public static bool useLinuxPaths = false;
        public static string findText = "";
        public static string replaceText = "";
        public static bool appendEight = false;
        public static bool notExtended = false;
        public static bool deleteExisting = false;
        public static string appDataPath = "";
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Version version = Assembly.GetExecutingAssembly().GetName().Version!;

            ParseCommandLineArguments(args);

            Console.WriteLine($"TuneLift v{version.Major}.{version.Minor}.{version.Revision}, Copyright © 2020-{DateTime.Now.Year} Richard Lawrence");
            Console.WriteLine("Export iTunes audio playlists as standard or extended .m3u files.");
            Console.WriteLine($"https://github.com/mrsilver76/tunelift\n");
            Console.WriteLine($"This program comes with ABSOLUTELY NO WARRANTY. This is free software,");
            Console.WriteLine($"and you are welcome to redistribute it under certain conditions; see");
            Console.WriteLine($"the documentation for details.");
            Console.WriteLine();

            InitialiseLogger();
            Logger("Starting TuneLift...");

            if (Directory.Exists(exportFolder))
            {
                // Delete any files existing in destination folder
                if (deleteExisting)
                    DeleteExistingFiles();
                else
                    Logger($"Exporting to folder: {exportFolder}");
            }
            else  // Folder doesn't exist
            {
                try
                {
                    Directory.CreateDirectory(exportFolder);
                    Logger($"Created folder for export: {exportFolder}");
                }
                catch (Exception e)
                {
                    Logger($"Unable to create folder '{exportFolder}': {e.Message}");
                    Environment.Exit(-1);
                }

            }

            // Connect to iTunes
            Type? iTunesAppType = Type.GetTypeFromProgID("iTunes.Application");
            if (iTunesAppType == null)
            {
                Logger("iTunes does not appear to be installed on this computer.");
                Environment.Exit(-1);
            }

            Logger("Connecting to iTunes...");
            dynamic? iTunes = null;
            try
            {
                iTunes = Activator.CreateInstance(iTunesAppType);
            }
            catch (Exception ex)
            {
                Logger($"Unable to connect to iTunes: {ex.Message}");
                Environment.Exit(-1);
            }


            // Work out how many playlists we want to export

            Logger("Getting playlist details...");

            dynamic? library = iTunes?.LibraryPlaylist;
            if (library == null)
            {
                Logger("Unable to access iTunes library.");
                Environment.Exit(-1);
            }
            dynamic? playlistsCollection = iTunes?.LibrarySource?.Playlists;
            if (playlistsCollection == null)
            {
                Logger("Unable to access iTunes playlists.");
                Environment.Exit(-1);
            }

            int wantedPlaylists = 0;
            int totalTracks = 0;

            foreach (dynamic playlist in playlistsCollection)
            {
                if (playlist == null) continue; // Skip null playlists

                // Check if this is a playlist we want to export
                if (IsWantedPlaylist(playlist, true))
                {
                    wantedPlaylists++;
                    totalTracks += (int)playlist.Tracks.Count;
                }
            }
            Logger($"Found {Pluralise(wantedPlaylists, "playlist", "playlists")} (totaling {Pluralise(totalTracks, "track", "tracks")}) to export.");

            // Now export those tracks
            string lineEnding = useLinuxPaths ? "\n" : "\r\n";
            string fileEnding = appendEight ? ".m3u8" : ".m3u";
            int playlistCount = 0;

            // Loop through the playlists and export them
            foreach (dynamic playlist in playlistsCollection)
            {
                if (playlist == null) continue; // Skip null playlists

                // Check if this is a playlist we want to export
                if (IsWantedPlaylist(playlist, false))
                {
                    string playlistTitle = (string)playlist.Name;
                    playlistCount++;
                    Logger($"Exporting {playlistCount}/{wantedPlaylists}: {playlistTitle} ({(int)playlist.Tracks.Count} tracks)");

                    // Sanitize playlist name for a valid filename
                    string sanitizedTitle = string.Join("_", playlistTitle.Split(Path.GetInvalidFileNameChars()));
                    string filePath = Path.Combine(exportFolder, sanitizedTitle + fileEnding);

                    // Store the playlist contents in a string
                    string playlistContents = "";
                    if (!notExtended)
                        playlistContents = "#EXTM3U" + lineEnding + "#PLAYLIST:" + playlistTitle + lineEnding;

                    foreach (dynamic track in playlist.Tracks)
                    {
                        if (track == null) continue; // Skip null tracks

                        // Check track kind (ITTrackKindFile = 1) and file type
                        if ((int)track.Kind == 1)
                        {
                            string? location = (string?)track.Location;
                            if (location != null)
                            {
                                string fileExtension = Path.GetExtension(location)?.ToLower() ?? string.Empty;
                                if (fileExtension == ".mp3" || fileExtension == ".m4a")
                                {
                                    // Write track information
                                    if (notExtended)
                                        playlistContents += RewriteLocation(location) + lineEnding;
                                    else
                                    {
                                        playlistContents += $"#EXTINF:{(int)track.Duration},{(string)track.Artist} - {(string)track.Name}" + lineEnding;
                                        playlistContents += RewriteLocation(location) + lineEnding;
                                    }
                                }
                            }
                        }

                    }

                    // Write out the playlist
                    File.WriteAllText(filePath, playlistContents, System.Text.Encoding.UTF8);
                }
            }

            // Ensure the iTunes object is released (important for COM interop)
            if (iTunes != null)
                System.Runtime.InteropServices.Marshal.ReleaseComObject(iTunes);

            Logger("TuneLift finished.");
            CheckLatestRelease();
            Environment.Exit(0);
        }


        /// <summary>
        /// Takes a path and rewrites it depending on whether useLinuxPaths is configured and if there are any search and replace paramaters defined.
        /// </summary>
        /// <param name="path">Path and filename of a file</param>
        /// <returns>Converted path and filename</returns>
        static string RewriteLocation(string path)
        {
            string newPath = path;

            // If Linux paths are being used then swap \ for /

            if (useLinuxPaths)
                newPath = newPath.Replace('\\', '/');

            // Do other search and repace here
            if (!string.IsNullOrEmpty(findText))
                newPath = newPath.Replace(findText, replaceText, StringComparison.CurrentCultureIgnoreCase);

            return newPath;
        }

        static void DeleteExistingFiles()
        {
            Logger($"Deleting existing playlists from '{exportFolder}'");

            int count = 0;
            try
            {
                foreach (string file in Directory.EnumerateFiles(exportFolder))
                {
                    string extension = Path.GetExtension(file);
                    if (extension.Equals(".m3u", StringComparison.OrdinalIgnoreCase) || extension.Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger($"Unable to delete: {ex.Message}");
                return;
            }
            Logger($"Sucessfully deleted {Pluralise(count, "playlist", "playlists")}");
        }

        /// <summary>
        /// Determines if a playlist should be exported depending on it's properties and user preferences
        /// </summary>
        /// <param name="playlist">iTunes playlist</param>
        /// <param name="showNotification">Output to the user if this playlist has been skipped</param>
        /// <returns></returns>
        static bool IsWantedPlaylist(dynamic playlist, Boolean showNotification)
        {
            // Needs to be a user playlist (ITPlaylistKindUser = 2)
            if ((int)playlist.Kind != 2) return false;

            // ...and needs to be visible
            if ((bool)playlist.Visible == false) return false;

            // ...and not special (0 = ITUserPlaylistSpecialKindNone = 0)
            dynamic upl = playlist;
            if ((int)upl.SpecialKind != 0)
                return false;

            // ...and not empty
            if ((int)playlist.Tracks.Count == 0)
            {
                if (showNotification)
                    Logger($"Ignoring empty playlist: {playlist.Name}");
                return false;
            }

            // ...and not smart (if configured to ignore smart)
            if (ignoreSmartPlaylists && (bool)upl.Smart)
            {
                if (showNotification)
                    Logger($"Ignoring smart playlist: {playlist.Name}");
                return false;
            }

            // ...and not a regular playlist (if configured to ignore those)
            if (ignorePlaylists && !(bool)upl.Smart)
            {
                if (showNotification)
                    Logger($"Ignoring regular playlist: {playlist.Name}");
                return false;
            }

            // ...and doesn't start with something we want to ignore
            if (!string.IsNullOrEmpty(ignorePrefix) && ((string)playlist.Name).StartsWith(ignorePrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (showNotification)
                    Logger($"Ignoring playlist with prefix: {playlist.Name}");
                return false;
            }

            // ...then this is a playlist we want
            return true;
        }
    }


}
