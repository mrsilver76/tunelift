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

using System.Reflection;

namespace TuneLift
{
    /// <summary>
    /// All global variables used by the application.
    /// </summary>
    internal static class Globals
    {
        // Variables that can be set by the user via command line arguments
        #region Export settings

        /// <summary>Flag for verbose output</summary>
        public static bool VerboseMode { get; set; }

        /// <summary>Folder to export playlists to</summary>
        public static string ExportFolder { get; set; } = "";

        /// <summary>Ignore smart playlists</summary>
        public static bool IgnoreSmartPlaylists { get; set; }

        /// <summary>Ignore regular playlists</summary>
        public static bool IgnorePlaylists { get; set; }

        /// <summary>Ignore playlists prefixed with this string</summary>
        public static string IgnorePrefix { get; set; } = "";

        /// <summary>Use Unix-style line endings and path format</summary>
        public static bool UseUnixPaths { get; set; }

        /// <summary>Text to find in file paths</summary>
        public static string FindText { get; set; } = "";

        /// <summary>Text to replace found text with in file paths</summary>
        public static string ReplaceText { get; set; } = "";

        /// <summary>Append .8 to the end of the file extension (for .m3u8 files)</summary>
        public static bool AppendEight { get; set; }

        /// <summary>Export as standard .m3u files instead of extended .m3u files</summary>
        public static bool NotExtended { get; set; }

        /// <summary>Delete existing .m3u files in the export folder before exporting new ones</summary>
        public static bool DeleteExisting { get; set; }

        /// <summary>Base path to remove from file paths before exporting</summary>
        public static string BasePath { get; set; } = "";

        /// <summary>Check for updates on GitHub when the program finishes</summary>
        public static bool CheckForUpdates { get; set; } = true;

        /// <summary>Close iTunes after exporting playlists. This will only work if iTunes was launched by TuneLift.</summary>
        public static bool CloseiTunesAfterExport { get; set; }

        /// <summary>Path of an iTunes XML library to read instead of using iTunes</summary>
        public static string XmlLibrary { get; set; } = "";

        #endregion

        // Variables used internally by the application
        #region Internal variables

        /// <summary>Version of the application, set from the assembly version</summary>
        public static Version ProgramVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version!;

        /// <summary>Path to the application data folder for storing settings and logs</summary>
        public static string AppDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "TuneLift");

        /// <summary>Was iTunes launched by TuneLift? If so, we can close it when done if the user requested that.</summary>
        public static bool ITunesLaunchedByUs { get; set; }

        /// <summary>Names (without the folder) of the playlist files written in this run, so two playlists never share one</summary>
        public static HashSet<string> WrittenFileNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        #endregion

        /// <summary>
        /// One playlist to export, with its audio tracks once they have been read.
        /// </summary>
        internal sealed class Playlist
        {
            /// <summary>Name of the playlist</summary>
            public required string Name { get; init; }

            /// <summary>Is this a smart playlist?</summary>
            public required bool IsSmart { get; init; }

            /// <summary>Number of tracks of any kind in the playlist, as reported by the source</summary>
            public required int TrackCount { get; init; }

            /// <summary>The audio tracks to export, in playlist order. Empty until they have been read.</summary>
            public List<Track> Tracks { get; } = [];
        }

        /// <summary>
        /// One audio track in a playlist.
        /// </summary>
        internal sealed class Track
        {
            /// <summary>Full path of the audio file</summary>
            public required string Location { get; init; }

            /// <summary>Length in seconds</summary>
            public required int Duration { get; init; }

            /// <summary>Artist name</summary>
            public required string Artist { get; init; }

            /// <summary>Track title</summary>
            public required string Name { get; init; }
        }
    }
}
