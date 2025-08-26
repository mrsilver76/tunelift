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

namespace TuneLift
{
    internal static class Program
    {
#region Export settings

        /// <summary>
        /// Folder to export playlists to
        /// </summary>
        public static string ExportFolder { get; set; } = "";

        /// <summary>
        /// Ignore smart playlists
        /// </summary>
        public static bool IgnoreSmartPlaylists { get; set; }

        /// <summary>
        /// Ignore regular playlists
        /// </summary>
        public static bool IgnorePlaylists { get; set; }

        /// <summary>
        /// Ignore playlists prefixed with this string
        /// </summary>
        public static string IgnorePrefix { get; set; } = "";

        /// <summary>
        /// Use Unix-style line endings and path format
        /// </summary>
        public static bool UseUnixPaths { get; set; }

        /// <summary>
        /// Text to find in file paths
        /// </summary>
        public static string FindText { get; set; } = "";

        /// <summary>
        /// Text to replace found text with in file paths
        /// </summary>
        public static string ReplaceText { get; set; } = "";

        /// <summary>
        /// Append .8 to the end of the file extension (for .m3u8 files)
        /// </summary>
        public static bool AppendEight { get; set; }

        /// <summary>
        /// Export as standard .m3u files instead of extended .m3u files
        /// </summary>
        public static bool NotExtended { get; set; }

        /// <summary>
        /// Delete existing .m3u files in the export folder before exporting new ones
        /// </summary>
        public static bool DeleteExisting { get; set; }

        /// <summary>
        /// Base path to remove from file paths before exporting
        /// </summary>
        public static string BasePath { get; set; } = "";

        /// <summary>
        /// Check for updates on GitHub when the program finishes
        /// </summary>
        public static bool CheckForUpdates { get; set; } = true;

        /// <summary>
        /// Close iTunes after exporting playlists. This will only work if iTunes was launched by TuneLift.
        /// </summary>
        public static bool CloseiTunesAfterExport { get; set; }

        #endregion

        #region Internal variables

        /// <summary>
        /// Version of the application, set from the assembly version
        /// </summary>
        public static Version ProgramVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version!;

        /// <summary>
        /// Path to the application data folder for storing settings and logs
        /// </summary>
        public static string AppDataPath { get; set; } = "";

        /// <summary>
        /// Was iTunes launched by TuneLift? If so, we can close it when done if the user requested that.
        /// </summary>
        private static bool ITunesLaunchedByUs { get; set; }

        #endregion

        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TuneLift");
            Logger.Initialise(Path.Combine(AppDataPath, "Logs"));

            CommandLineParser.ParseArguments(args);

            Console.WriteLine($"TuneLift v{VersionHelper.ToSemanticString(ProgramVersion)}, Copyright © 2020-{DateTime.Now.Year} Richard Lawrence");
            Console.WriteLine("Export iTunes audio playlists as standard or extended .m3u files.");
            Console.WriteLine($"https://github.com/mrsilver76/tunelift\n");
            Console.WriteLine($"This program comes with ABSOLUTELY NO WARRANTY. This is free software,");
            Console.WriteLine($"and you are welcome to redistribute it under certain conditions; see");
            Console.WriteLine($"the documentation for details.");
            Console.WriteLine();

            Logger.Write("Starting TuneLift...");

            // We do a lot of things in this function: bootstrapping, connecting to iTunes,
            // enumerating and filtering playlists, exporting playlists to disk and rewriting paths.
            //
            // Although it would make sense to break this up into smaller functions, this program
            // is reasonably feature-complete and has a limited lifespan (given that Apple will
            // eventually retire iTunes, especially the one with the COM support), so for now
            // we will keep it all in one place.

            if (Directory.Exists(ExportFolder))
            {
                // Delete any files existing in destination folder
                if (DeleteExisting)
                    DeleteExistingFiles();
                else
                    Logger.Write($"Exporting to folder: {ExportFolder}");
            }
            else  // Folder doesn't exist
            {
                try
                {
                    Directory.CreateDirectory(ExportFolder);
                    Logger.Write($"Created folder for export: {ExportFolder}");
                }
                catch (Exception e)
                {
                    Logger.Write($"Unable to create folder '{ExportFolder}': {e.Message}");
                    Environment.Exit(-1);
                }

            }

            // Connect to iTunes
            Type? iTunesAppType = Type.GetTypeFromProgID("iTunes.Application");
            if (iTunesAppType == null)
            {
                Logger.Write("iTunes does not appear to be installed on this computer.");
                Environment.Exit(-1);
            }

            Logger.Write("Connecting to iTunes...");

            // Since we cannot tell from the COM interface if iTunes is running or not, we will cheat by
            // checking the process list and assuming that if it is running, it will not be launched by TuneLift.

            if (System.Diagnostics.Process.GetProcessesByName("iTunes").Length == 0)
                ITunesLaunchedByUs = true;
            else
                ITunesLaunchedByUs = false;

            dynamic? iTunes = null;
            try
            {
                iTunes = Activator.CreateInstance(iTunesAppType);
            }
            catch (Exception ex)
            {
                Logger.Write($"Unable to connect to iTunes: {ex.Message}");
                Environment.Exit(-1);
            }

            // Work out how many playlists we want to export

            Logger.Write("Getting playlist details...");

            dynamic? library = iTunes?.LibraryPlaylist;
            if (library == null)
            {
                Logger.Write("Unable to access iTunes library.");
                Environment.Exit(-1);
            }
            dynamic? playlistsCollection = iTunes?.LibrarySource?.Playlists;
            if (playlistsCollection == null)
            {
                Logger.Write("Unable to access iTunes playlists.");
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
            Logger.Write($"Found {TextUtils.Pluralise(wantedPlaylists, "playlist", "playlists")} (totaling {TextUtils.Pluralise(totalTracks, "track", "tracks")}) to export.");

            // Now export those tracks
            string lineEnding = UseUnixPaths ? "\n" : "\r\n";
            string fileEnding = AppendEight ? ".m3u8" : ".m3u";
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
                    Logger.Write($"Exporting {playlistCount}/{wantedPlaylists}: {playlistTitle} ({TextUtils.Pluralise((int)playlist.Tracks.Count, "track", "tracks")})");

                    // Flag to indicate if there is content in this playlist to save
                    bool contentToSave = false;

                    // Sanitize playlist name for a valid filename
                    string sanitizedTitle = string.Join("_", playlistTitle.Split(Path.GetInvalidFileNameChars()));
                    string filePath = Path.Combine(ExportFolder, sanitizedTitle + fileEnding);

                    // Store the playlist contents in a string
                    string playlistContents = "";
                    if (!NotExtended)
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
                                string fileExtension = Path.GetExtension(location)?.ToLowerInvariant() ?? string.Empty;
                                if (fileExtension == ".mp3" || fileExtension == ".m4a" || fileExtension == ".m4b")
                                {
                                    contentToSave = true; // We have something to save
                                    // Write track information
                                    if (NotExtended)
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
                        Logger.Write($"No audio content to save for playlist: {playlistTitle}");
                }
            }

            // We now need to handle disconnecting from iTunes

            if (iTunes != null)
            {
                // Did the user want us to close iTunes?
                if (CloseiTunesAfterExport)
                {
                    // Did we actually launch iTunes?
                    if (ITunesLaunchedByUs)
                    {
                        Logger.Write("Attempting to close iTunes...");
                        try
                        {
                            iTunes.Quit();
                        }
                        catch (Exception ex)
                        {
                            Logger.Write($"Unable to close iTunes: {ex.Message}");
                        }
                    }
                    else
                    {
                        Logger.Write("iTunes was not started by TuneLift; leaving it running.");
                    }
                }

                // Ensure the iTunes object is released (important for COM interop)

                System.Runtime.InteropServices.Marshal.ReleaseComObject(iTunes);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Logger.Write("TuneLift finished.");

            if (CheckForUpdates)
                CheckLatestRelease();

            Environment.Exit(0);
        }

        /// <summary>
        /// Takes a path and rewrites it depending on whether UseUnixPaths is configured and if there are any search and replace paramaters defined.
        /// </summary>
        /// <param name="path">Path and filename of a file</param>
        /// <returns>Converted path and filename</returns>
        private static string RewriteLocation(string path)
        {
            string newPath = path;

            // Remove the BasePath if it is set and the path starts with it

            if (!string.IsNullOrEmpty(BasePath) && newPath.StartsWith(BasePath, StringComparison.OrdinalIgnoreCase))
                newPath = newPath[BasePath.Length..];

            // If Unix paths are being used then swap \ for /

            if (UseUnixPaths)
                newPath = newPath.Replace('\\', '/');

            // Do other search and repace here

            if (!string.IsNullOrEmpty(FindText))
                newPath = newPath.Replace(FindText, ReplaceText, StringComparison.CurrentCultureIgnoreCase);

            return newPath;
        }

        /// <summary>
        /// Deletes existing .m3u and .m3u8 files in the export folder if DeleteExisting is set to true.
        /// </summary>
        private static void DeleteExistingFiles()
        {
            if (!DeleteExisting)
                return;  // Nothing to do

            Logger.Write($"Deleting existing playlists from: {ExportFolder}");

            int count = 0;
            try
            {
                foreach (string file in Directory.EnumerateFiles(ExportFolder))
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
                Logger.Write($"Unable to delete: {ex.Message}");
                return;
            }
            Logger.Write($"Sucessfully deleted {TextUtils.Pluralise(count, "playlist", "playlists")}.");
        }

        /// <summary>
        /// Determines if a playlist should be exported depending on its properties and user preferences
        /// </summary>
        /// <param name="playlist">iTunes playlist</param>
        /// <param name="showNotification">Output to the user if this playlist has been skipped</param>
        /// <returns></returns>
        private static bool IsWantedPlaylist(dynamic playlist, Boolean showNotification)
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
                    Logger.Write($"Ignoring empty playlist: {playlist.Name}");
                return false;
            }

            // ...and not smart (if configured to ignore those)
            if (IgnoreSmartPlaylists && (bool)upl.Smart)
            {
                if (showNotification)
                    Logger.Write($"Ignoring smart playlist: {playlist.Name}");
                return false;
            }

            // ...and not a regular playlist (if configured to ignore those)
            if (IgnorePlaylists && !(bool)upl.Smart)
            {
                if (showNotification)
                    Logger.Write($"Ignoring regular playlist: {playlist.Name}");
                return false;
            }

            // ...and doesn't start with something we want to ignore
            if (!string.IsNullOrEmpty(IgnorePrefix) && ((string)playlist.Name).StartsWith(IgnorePrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (showNotification)
                    Logger.Write($"Ignoring playlist with prefix: {playlist.Name}");
                return false;
            }

            // ...then this is a playlist we want
            return true;
        }

        /// <summary>
        /// Checks GitHub to see if there is a newer release of TuneLift available and notifies the user if so.
        /// </summary>
        private static void CheckLatestRelease()
        {
            var result = GitHubVersionChecker.CheckLatestRelease(ProgramVersion, "mrsilver76/tunelift", Path.Combine(AppDataPath, "versionCheck.ini"));

            if (result.UpdateAvailable)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"  ℹ️ A new version ({result.LatestVersion}) is available!");
                Console.ResetColor();
                Console.WriteLine($" You are using {result.CurrentVersion}");
                Console.WriteLine($"     Get it from https://www.github.com/{result.Repo}/");
            }
        }
    }
}
