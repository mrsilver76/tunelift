/*
 * TuneLift - Export iTunes audio playlists as standard or extended .m3u files.
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

using System.Reflection;
using static TuneLift.Helpers;

namespace TuneLift
{
    public class Program
    {
        public static string exportFolder = "";  // Folder to export playlists to
        public static bool ignoreSmartPlaylists = false;  // Ignore smart playlists
        public static bool ignorePlaylists = false;  // Ignore regular playlists
        public static string ignorePrefix = "";  // Ignore playlists prefixed with this string
        public static bool useUnixPaths = false;  // Use Unix-style line endings and path format
        public static string findText = "";  // Text to find in file paths
        public static string replaceText = "";  // Text to replace found text with in file paths
        public static bool appendEight = false;  // Append .8 to the end of the file extension (for .m3u8 files)
        public static bool notExtended = false;  // Export as standard .m3u files instead of extended .m3u files
        public static bool deleteExisting = false;  // Delete existing .m3u files in the export folder before exporting new ones
        public static string basePath = "";  // Base path to remove from file paths before exporting

        // Internal global variables
        public static Version version = Assembly.GetExecutingAssembly().GetName().Version!;  // Version of the application, set from the assembly version
        public static string appDataPath = "";  // Path to the application data folder for storing settings and logs

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            ParseCommandLineArguments(args);

            Console.WriteLine($"TuneLift v{OutputVersion(version)}, Copyright © 2020-{DateTime.Now.Year} Richard Lawrence");
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
                if (playlist == null)
                    continue; // Skip null playlists

                // Check if this is a playlist we want to export
                if (IsWantedPlaylist(playlist, true))
                {
                    wantedPlaylists++;
                    totalTracks += (int)playlist.Tracks.Count;
                }
            }
            Logger($"Found {Pluralise(wantedPlaylists, "playlist", "playlists")} (totaling {Pluralise(totalTracks, "track", "tracks")}) to export.");

            // Now export those tracks
            string lineEnding = useUnixPaths ? "\n" : "\r\n";
            string fileEnding = appendEight ? ".m3u8" : ".m3u";
            int playlistCount = 0;

            // Loop through the playlists and export them
            foreach (dynamic playlist in playlistsCollection)
            {
                if (playlist == null)
                    continue; // Skip null playlists

                // Check if this is a playlist we want to export
                if (IsWantedPlaylist(playlist, false))
                {
                    string playlistTitle = (string)playlist.Name;
                    playlistCount++;
                    Logger($"Exporting {playlistCount}/{wantedPlaylists}: {playlistTitle} ({(int)playlist.Tracks.Count} tracks)");

                    // Flag to indicate if there is content in this playlist to save
                    bool contentToSave = false;

                    // Sanitize playlist name for a valid filename
                    string sanitizedTitle = string.Join("_", playlistTitle.Split(Path.GetInvalidFileNameChars()));
                    string filePath = Path.Combine(exportFolder, sanitizedTitle + fileEnding);

                    // Store the playlist contents in a string
                    string playlistContents = "";
                    if (!notExtended)
                        playlistContents = "#EXTM3U" + lineEnding + "#PLAYLIST:" + playlistTitle + lineEnding;

                    foreach (dynamic track in playlist.Tracks)
                    {
                        if (track == null)
                            continue; // Skip null tracks

                        // Check track kind (ITTrackKindFile = 1) and file type
                        if ((int)track.Kind == 1)
                        {
                            string? location = (string?)track.Location;
                            if (location != null)
                            {
                                string fileExtension = Path.GetExtension(location)?.ToLower() ?? string.Empty;
                                if (fileExtension == ".mp3" || fileExtension == ".m4a" || fileExtension == ".m4b")
                                {
                                    contentToSave = true; // We have something to save
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
                    if (contentToSave)
                        File.WriteAllText(filePath, playlistContents, System.Text.Encoding.UTF8);
                    else
                        Logger($"No audio content to save for playlist: {playlistTitle}");
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
        /// Takes a path and rewrites it depending on whether useUnixPaths is configured and if there are any search and replace paramaters defined.
        /// </summary>
        /// <param name="path">Path and filename of a file</param>
        /// <returns>Converted path and filename</returns>
        static string RewriteLocation(string path)
        {
            string newPath = path;

            // Remove the basePath if it is set and the path starts with it

            if (!string.IsNullOrEmpty(basePath) && newPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                newPath = newPath.Substring(basePath.Length);

            // If Unix paths are being used then swap \ for /

            if (useUnixPaths)
                newPath = newPath.Replace('\\', '/');

            // Do other search and repace here

            if (!string.IsNullOrEmpty(findText))
                newPath = newPath.Replace(findText, replaceText, StringComparison.CurrentCultureIgnoreCase);

            return newPath;
        }

        /// <summary>
        /// Deletes existing .m3u and .m3u8 files in the export folder if deleteExisting is set to true.
        /// </summary>
        static void DeleteExistingFiles()
        {
            if (!deleteExisting)
                return;  // Nothing to do

            Logger($"Deleting existing playlists from: {exportFolder}");

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
        /// Determines if a playlist should be exported depending on its properties and user preferences
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

            // ...and not special (ITUserPlaylistSpecialKindNone = 0)
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

            // ...and not smart (if configured to ignore those)
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
