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

namespace TuneLift
{
    /// <summary>
    /// Displays usage/help information for the application.
    /// </summary>
    internal static class UsagePrinter
    {
        /// <summary>
        /// Displays the usage information for the application, including command line options and version information.
        /// If an error message is provided, it will be displayed and the program will exit with an error status.
        /// </summary>
        /// <param name="errorMessage">Error message to display</param>
        public static void Show(string errorMessage = "")
        {
            Console.WriteLine($"Usage: {System.Diagnostics.Process.GetCurrentProcess().ProcessName} [options] <destination folder>\n" +
                              "Export iTunes audio playlists as standard or extended .m3u files.\n");

            if (String.IsNullOrEmpty(errorMessage))
                Console.WriteLine($"This is version {VersionHelper.OutputVersion(Program.ProgramVersion)}, copyright © 2020-{DateTime.Now.Year} Richard Lawrence.\n" +
                                  "Forklift icon by nawicon - Flaticon (https://www.flaticon.com/free-icons/forklift)\n");

            Console.WriteLine("Mandatory Arguments:\n" +
                              "  <destination folder>           The folder to export the playlists to.\n" +
                              "\n" +
                              "Playlist Selection:\n" +
                              "  -ns, --no-smart                Skip exporting smart playlists.\n" +
                              "  -np, --no-playlist             Skip exporting regular (non-smart) playlists.\n" +
                              "  -i <text>, --ignore <text>     Exclude playlists with names starting <text>.\n" +
                              "\n" +
                              "Output Format:\n" +
                              "  -8, --append-8                 Use .m3u8 file extension.\n" +
                              "  -ne, --not-extended            Export using basic .m3u format, with no extended\n" +
                              "                                 playlist/song titles and duration information.\n" +
                              "  -u, --unix                     Use Unix-style paths and LF line endings.\n" +
                              "\n" +
                              "File Path Adjustments:\n" +
                              "  -f <text>, --find <text>       Match <text> in file path for substitution.\n" +
                              "  -r <text>, --replace <text>    Replace matched text with <text>.\n" +
                              "  -b <path>, --base-path <path>  Remove leading <path> from file path.\n" +
                              "\n" +
                              "File Management:\n" +
                              "  -d, --delete                   Remove existing playlist files from destination.\n" +
                              "\n" +
                              "Other Options:\n" +
                              "  -c, --close                    Close iTunes after export (won't close if already running).\n" +
                              "  -nc, --no-check                Do not check GitHub for later versions.\n" +
                              "  /?, -h, --help                 Show this help message.\n" +
                              "\n" +
                              $"Logs are written to {Path.Combine(Program.AppDataPath, "Logs")}");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine();
                Console.WriteLine($"Error: {errorMessage}");
                Environment.Exit(-1);
            }
            Environment.Exit(0);
        }
    }
}
