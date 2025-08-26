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
    /// Parses the command line arguments and sets the corresponding Program static fields.
    /// </summary>
    internal static class CommandLineParser
    {
        /// <summary>
        /// Parses command line arguments and updates Program fields accordingly.
        /// </summary>
        /// <param name="args">Array of command line arguments</param>
        public static void ParseArguments(string[] args)
        {
            if (args.Length == 0)
                UsagePrinter.Show();

            for (int i = 0; i < args.Length; i++)
            {
                string lowerArg = args[i].ToLower(System.Globalization.CultureInfo.CurrentCulture);

                if (lowerArg == "-h" || lowerArg == "--help" || lowerArg == "/?")
                    UsagePrinter.Show();
                else if (lowerArg == "-ns" || lowerArg == "--no-smart" || lowerArg == "/ns")
                    Program.IgnoreSmartPlaylists = true;
                else if (lowerArg == "-np" || lowerArg == "--no-playlist" || lowerArg == "--no-playlists" || lowerArg == "/np")
                    Program.IgnorePlaylists = true;
                else if ((lowerArg == "-i" || lowerArg == "--ignore" || lowerArg == "/i") && i + 1 < args.Length)
                {
                    Program.IgnorePrefix = args[i + 1];
                    i++;
                }
                else if (lowerArg == "-u" || lowerArg == "--unix" || lowerArg == "/u" || lowerArg == "-l" || lowerArg == "--linux" || lowerArg == "/l")
                    Program.UseUnixPaths = true;
                else if ((lowerArg == "-f" || lowerArg == "--find" || lowerArg == "/f") && i + 1 < args.Length)
                {
                    Program.FindText = args[i + 1];
                    i++;
                }
                else if ((lowerArg == "-r" || lowerArg == "--replace" || lowerArg == "/r") && i + 1 < args.Length)
                {
                    Program.ReplaceText = args[i + 1];
                    i++;
                }
                else if (lowerArg == "-8" || lowerArg == "--append-8" || lowerArg == "/8")
                    Program.AppendEight = true;
                else if (lowerArg == "-ne" || lowerArg == "--not-extended" || lowerArg == "/n")
                    Program.NotExtended = true;
                else if (lowerArg == "-d" || lowerArg == "--delete" || lowerArg == "/d")
                    Program.DeleteExisting = true;
                else if ((lowerArg == "-b" || lowerArg == "--base-path" || lowerArg == "/b") && i + 1 < args.Length)
                {
                    Program.BasePath = args[i + 1];
                    i++;
                }
                else if (lowerArg == "-nc" || lowerArg == "--no-check" || lowerArg == "/nc")
                    Program.CheckForUpdates = false;
                else if (lowerArg == "-c" || lowerArg == "--close" || lowerArg == "/c")
                    Program.CloseiTunesAfterExport = true;
                else if (lowerArg.StartsWith('-') || lowerArg.StartsWith("--", StringComparison.CurrentCulture) || lowerArg.StartsWith('/'))
                    UsagePrinter.Show($"Unrecognised argument: {args[i]}");
                else
                {
                    if (!string.IsNullOrEmpty(Program.ExportFolder))
                        UsagePrinter.Show($"Directory already provided, '{args[i]}' is redundant.");

                    Program.ExportFolder = args[i];
                }
            }

            // Validate any arguments
            if (Program.IgnorePlaylists && Program.IgnoreSmartPlaylists)
                UsagePrinter.Show("Ignoring both playlists and smart playlists means nothing to do.");

            if (string.IsNullOrEmpty(Program.FindText) && !string.IsNullOrEmpty(Program.ReplaceText))
                UsagePrinter.Show($"No text to find defined for replacement text ('{Program.ReplaceText}')");

            if (string.IsNullOrEmpty(Program.ExportFolder))
                UsagePrinter.Show("Missing destination folder.");
        }
    }
}

