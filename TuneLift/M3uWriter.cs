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

using System.Text;

namespace TuneLift
{
    /// <summary>
    /// Decides which playlists to export and writes them as .m3u files. Knows nothing about where
    /// the playlists came from, so any source can use it.
    /// </summary>
    internal sealed class M3uWriter
    {
        /// <summary>
        /// Is this file one we export? Only .mp3, .m4a and .m4b files are.
        /// </summary>
        /// <param name="location">Path and filename of the track.</param>
        /// <returns>True if the file is a supported audio file.</returns>
        public static bool IsAudioFile(string location)
        {
            string fileExtension = Path.GetExtension(location).ToLowerInvariant();

            return fileExtension == ".mp3" || fileExtension == ".m4a" || fileExtension == ".m4b";
        }

        /// <summary>
        /// Determines if a playlist should be exported depending on the user's preferences.
        /// </summary>
        /// <param name="playlist">The playlist, with its track count and smart flag filled in.</param>
        /// <param name="showNotification">Output to the user if this playlist has been skipped.</param>
        /// <returns>True if the playlist should be exported.</returns>
        public static bool IsWantedPlaylist(Globals.Playlist playlist, bool showNotification)
        {
            // Not empty
            if (playlist.TrackCount == 0)
            {
                if (showNotification)
                    Logger.Write($"Ignoring empty playlist: {playlist.Name}");
                return false;
            }

            // ...and not smart (if configured to ignore those)
            if (Globals.IgnoreSmartPlaylists && playlist.IsSmart)
            {
                if (showNotification)
                    Logger.Write($"Ignoring smart playlist: {playlist.Name}");
                return false;
            }

            // ...and not a regular playlist (if configured to ignore those)
            if (Globals.IgnorePlaylists && !playlist.IsSmart)
            {
                if (showNotification)
                    Logger.Write($"Ignoring regular playlist: {playlist.Name}");
                return false;
            }

            // ...and doesn't start with something we want to ignore
            if (!string.IsNullOrEmpty(Globals.IgnorePrefix) && playlist.Name.StartsWith(Globals.IgnorePrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (showNotification)
                    Logger.Write($"Ignoring playlist with prefix: {playlist.Name}");
                return false;
            }

            // ...then this is a playlist we want
            return true;
        }

        /// <summary>
        /// Writes one playlist to the export folder as an .m3u (or .m3u8) file. Does nothing
        /// except log a message if the playlist has no audio tracks.
        /// </summary>
        /// <param name="playlist">The playlist, with its audio tracks read.</param>
        public static void WritePlaylist(Globals.Playlist playlist)
        {
            if (playlist.Tracks.Count == 0)
            {
                Logger.Write($"No audio content to save for playlist: {playlist.Name}");
                return;
            }

            // Windows programs rely on a BOM to recognise UTF-8, but Unix tools treat it as stray text,
            // so only write it when the paths are for Windows.
            string lineEnding = "\r\n";
            bool writeBom = true;
            if (Globals.UseUnixPaths)
            {
                lineEnding = "\n";
                writeBom = false;
            }

            string fileEnding = ".m3u";
            if (Globals.AppendEight)
                fileEnding = ".m3u8";

            // Sanitise the playlist name for a valid filename
            string sanitisedTitle = string.Join("_", playlist.Name.Split(Path.GetInvalidFileNameChars()));

            // Two playlists can end up with the same file name, so add a number to the later one
            string fileName = sanitisedTitle + fileEnding;
            int copyNumber = 1;
            while (!Globals.WrittenFileNames.Add(fileName))
            {
                copyNumber++;
                fileName = $"{sanitisedTitle} ({copyNumber}){fileEnding}";
            }

            if (copyNumber > 1)
                Logger.Write($"A playlist file named {sanitisedTitle + fileEnding} already exists in this run, using {fileName} instead.");

            string filePath = Path.Combine(Globals.ExportFolder, fileName);

            StringBuilder contents = new();

            // Write the header if this is an extended playlist
            if (!Globals.NotExtended)
                contents.Append("#EXTM3U" + lineEnding + "#PLAYLIST:" + playlist.Name + lineEnding);

            foreach (var track in playlist.Tracks)
            {
                if (!Globals.NotExtended)
                {
                    // Tracks without an artist (audiobooks, podcasts) get just their name
                    string title = track.Name;
                    if (!string.IsNullOrEmpty(track.Artist))
                        title = $"{track.Artist} - {track.Name}";

                    contents.Append($"#EXTINF:{track.Duration},{title}" + lineEnding);
                }

                contents.Append(RewriteLocation(track.Location) + lineEnding);
            }

            // Write to a temporary file first, so that a failed write never leaves a half-written playlist
            string tempPath = filePath + ".tmp";

            try
            {
                File.WriteAllText(tempPath, contents.ToString(), new UTF8Encoding(writeBom));
                File.Move(tempPath, filePath, true);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        /// <summary>
        /// Takes a path and rewrites it depending on whether UseUnixPaths is configured and if there are any search and replace parameters defined.
        /// </summary>
        /// <param name="path">Path and filename of a file.</param>
        /// <returns>Converted path and filename.</returns>
        private static string RewriteLocation(string path)
        {
            string newPath = path;

            // Remove the BasePath if it is set and the path starts with it
            if (!string.IsNullOrEmpty(Globals.BasePath) && newPath.StartsWith(Globals.BasePath, StringComparison.OrdinalIgnoreCase))
                newPath = newPath[Globals.BasePath.Length..];

            // If Unix paths are being used then swap \ for /
            if (Globals.UseUnixPaths)
                newPath = newPath.Replace('\\', '/');

            // Do other search and replace here
            if (!string.IsNullOrEmpty(Globals.FindText))
                newPath = newPath.Replace(Globals.FindText, Globals.ReplaceText, StringComparison.OrdinalIgnoreCase);

            return newPath;
        }
    }
}
