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

namespace TuneLift
{
    /// <summary>
    /// Parses the command line arguments and sets the corresponding global variables.
    /// </summary>
    internal static class CommandLineParser
    {
        /// <summary>Command line options that must be followed by a value</summary>
        private static readonly HashSet<string> OptionsWithValue =
        [
            "-i", "--ignore", "/i",
            "-f", "--find", "/f",
            "-r", "--replace", "/r",
            "-b", "--base-path", "/b",
            "-x", "--xml", "/x"
        ];

        /// <summary>
        /// Parses command line arguments and updates global variables accordingly.
        /// </summary>
        /// <param name="args">Array of command line arguments</param>
        public static void ParseArguments(string[] args)
        {
            if (args.Length == 0)
                ConsoleOutput.ShowUsage();

            for (int i = 0; i < args.Length; i++)
            {
                string lowerArg = args[i].ToLower(CultureInfo.InvariantCulture);

                // Every option that takes a value must have one following it

                if (OptionsWithValue.Contains(lowerArg) && i + 1 >= args.Length)
                    ConsoleOutput.ShowUsage($"Option '{args[i]}' requires a value.");

                if (lowerArg == "-h" || lowerArg == "--help" || lowerArg == "/?")
                    ConsoleOutput.ShowUsage();
                else if (lowerArg == "-ns" || lowerArg == "--no-smart" || lowerArg == "/ns")
                    Globals.IgnoreSmartPlaylists = true;
                else if (lowerArg == "-np" || lowerArg == "--no-playlist" || lowerArg == "--no-playlists" || lowerArg == "/np")
                    Globals.IgnorePlaylists = true;
                else if (lowerArg == "-i" || lowerArg == "--ignore" || lowerArg == "/i")
                {
                    Globals.IgnorePrefix = args[i + 1];
                    i++;  // Skip the next argument as it's the value
                }
                else if (lowerArg == "-u" || lowerArg == "--unix" || lowerArg == "/u" || lowerArg == "-l" || lowerArg == "--linux" || lowerArg == "/l")
                    Globals.UseUnixPaths = true;
                else if (lowerArg == "-f" || lowerArg == "--find" || lowerArg == "/f")
                {
                    Globals.FindText = args[i + 1];
                    i++;  // Skip the next argument as it's the value
                }
                else if (lowerArg == "-r" || lowerArg == "--replace" || lowerArg == "/r")
                {
                    Globals.ReplaceText = args[i + 1];
                    i++;  // Skip the next argument as it's the value
                }
                else if (lowerArg == "-8" || lowerArg == "--append-8" || lowerArg == "/8")
                    Globals.AppendEight = true;
                else if (lowerArg == "-ne" || lowerArg == "--not-extended" || lowerArg == "/ne" || lowerArg == "/n")
                    Globals.NotExtended = true;
                else if (lowerArg == "-d" || lowerArg == "--delete" || lowerArg == "/d")
                    Globals.DeleteExisting = true;
                else if (lowerArg == "-b" || lowerArg == "--base-path" || lowerArg == "/b")
                {
                    Globals.BasePath = args[i + 1];
                    i++;  // Skip the next argument as it's the value
                }
                else if (lowerArg == "-nc" || lowerArg == "--no-check" || lowerArg == "/nc")
                    Globals.CheckForUpdates = false;
                else if (lowerArg == "-c" || lowerArg == "--close" || lowerArg == "/c")
                    Globals.CloseiTunesAfterExport = true;
                else if (lowerArg == "-x" || lowerArg == "--xml" || lowerArg == "/x")
                {
                    // A value that looks like another option means the filename was left out. This does mean you
                    // cannot use a filename that starts with a dash or slash, but that's pretty unlikely.
                    if (args[i + 1].StartsWith('-') || args[i + 1].StartsWith('/'))
                        ConsoleOutput.ShowUsage($"Option '{args[i]}' requires the filename of an iTunes XML library.{SuggestXmlLibrary()}");

                    Globals.XmlLibrary = args[i + 1];
                    i++;  // Skip the next argument as it's the value
                }
                else if (lowerArg.StartsWith('-') || lowerArg.StartsWith('/'))
                    ConsoleOutput.ShowUsage($"Unrecognised argument: {args[i]}");
                else
                {
                    if (!string.IsNullOrEmpty(Globals.ExportFolder))
                        ConsoleOutput.ShowUsage($"Directory already provided, '{args[i]}' is redundant.");

                    Globals.ExportFolder = args[i];
                }
            }

            // Validate the arguments to ensure nothing silly is being done
            ValidateArguments();
        }

        /// <summary>
        /// Checks that the arguments make sense together. Shows the usage and ends the program if they don't.
        /// </summary>
        private static void ValidateArguments()
        {
            if (Globals.IgnorePlaylists && Globals.IgnoreSmartPlaylists)
                ConsoleOutput.ShowUsage("Ignoring both playlists and smart playlists means nothing to do.");

            if (string.IsNullOrEmpty(Globals.FindText) && !string.IsNullOrEmpty(Globals.ReplaceText))
                ConsoleOutput.ShowUsage($"No text to find defined for replacement text ('{Globals.ReplaceText}')");

            if (string.IsNullOrEmpty(Globals.ExportFolder))
                ConsoleOutput.ShowUsage("Missing destination folder.");

            if (string.IsNullOrEmpty(Globals.XmlLibrary))
                return;

            if (!File.Exists(Globals.XmlLibrary))
                ConsoleOutput.ShowUsage($"XML library not found: '{Globals.XmlLibrary}'.{SuggestXmlLibrary()}");

            // A quick look at the start of the file. iTunes always begins a library with a plist
            // element whose first key is "Major Version". Anything unreadable counts as not being a library.
            bool looksLikeLibrary = false;

            try
            {
                using StreamReader reader = File.OpenText(Globals.XmlLibrary);
                char[] buffer = new char[4096];
                int length = reader.ReadBlock(buffer, 0, buffer.Length);
                string start = new(buffer, 0, length);

                looksLikeLibrary = start.Contains("<plist", StringComparison.Ordinal) && start.Contains("<key>Major Version</key>", StringComparison.Ordinal);
            }
            catch (Exception)
            {
                // Leave looksLikeLibrary false, so the message below is shown
            }

            if (!looksLikeLibrary)
                ConsoleOutput.ShowUsage($"'{Globals.XmlLibrary}' does not look like an iTunes XML library.{SuggestXmlLibrary()}");

            if (Globals.CloseiTunesAfterExport)
                Logger.Write("Ignoring '--close' option when reading an XML library.", true);
        }

        /// <summary>
        /// Looks for an iTunes XML library in its usual place in the user's Music folder.
        /// </summary>
        /// <returns>The full path of the first library found, or an empty string if there is none.</returns>
        internal static string FindXmlLibrary()
        {
            string iTunesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music", "iTunes");

            foreach (string fileName in new[] { "iTunes Music Library.xml", "iTunes Library.xml" })
            {
                string path = Path.Combine(iTunesFolder, fileName);

                if (File.Exists(path))
                    return path;
            }

            // Nothing found
            return "";
        }

        /// <summary>
        /// Suggests the iTunes XML library found by <see cref="FindXmlLibrary"/>.
        /// </summary>
        /// <returns>A sentence suggesting the first library found, with a leading space, or an empty string if there is none.</returns>
        private static string SuggestXmlLibrary()
        {
            string path = FindXmlLibrary();

            if (path.Length == 0)
                return "";

            return $" Did you mean '{path}'?";
        }
    }
}

