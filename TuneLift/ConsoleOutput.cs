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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace TuneLift
{
    /// <summary>
    /// Writes the usage text, the run header and the update notice to the console.
    /// </summary>
    internal sealed class ConsoleOutput
    {
        /// <summary>
        /// Displays the usage information to the user and optional error message. This method never returns.
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        [DoesNotReturn]
        public static void ShowUsage(string errorMessage = "")
        {
            Console.WriteLine($"Usage: {System.Diagnostics.Process.GetCurrentProcess().ProcessName} [options] <destination folder>\n" +
                              $"Export iTunes audio playlists as standard or extended .m3u files.\n");

            if (string.IsNullOrEmpty(errorMessage))
                Console.WriteLine($"This is version {VersionHelper.OutputVersion(Globals.ProgramVersion)}, copyright © 2020-{DateTime.Now.Year} Richard Lawrence.\n" +
                                  $"Forklift icon by nawicon - Flaticon (https://www.flaticon.com/free-icons/forklift)\n");

            PrintOptionsWithDescriptions();

            Console.WriteLine($"Logs are written to {Path.Combine(Globals.AppDataPath, "Logs")}");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine();
                Console.WriteLine($"Error: {errorMessage}");
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Prints each section of command line options with its descriptions. Descriptions are wrapped to the
        /// console width, with wrapped lines indented.
        /// </summary>
        private static void PrintOptionsWithDescriptions()
        {
            // Define sections and their options + descriptions
            Dictionary<string, (string Option, string Description)[]> sections = new()
            {
                ["Mandatory"] =
                [
                    ("<destination folder>", "The folder to export the playlists to.")
                ],
                ["Playlist Source"] =
                [
                    ("-x <file>, --xml <file>", "Read playlists from an iTunes XML library file instead of from iTunes.")
                ],
                ["Playlist Selection"] =
                [
                    ("-ns, --no-smart", "Skip exporting smart playlists."),
                    ("-np, --no-playlist", "Skip exporting regular (non-smart) playlists."),
                    ("-i <text>, --ignore <text>", "Exclude playlists with names starting <text>.")
                ],
                ["Output Format"] =
                [
                    ("-8, --append-8", "Use .m3u8 file extension."),
                    ("-ne, --not-extended", "Export using basic .m3u format, with no extended playlist/song titles and duration information."),
                    ("-u, --unix", "Use Unix-style paths and LF line endings.")
                ],
                ["File Path Adjustments"] =
                [
                    ("-f <text>, --find <text>", "Match <text> in file path for substitution."),
                    ("-r <text>, --replace <text>", "Replace matched text with <text>."),
                    ("-b <path>, --base-path <path>", "Remove leading <path> from file path.")
                ],
                ["File Management"] =
                [
                    ("-d, --delete", "Remove existing playlist files from destination.")
                ],
                ["Other Options"] =
                [
                    ("-c, --close", "Close iTunes after export (won't close if already running)."),
                    ("-nc, --no-check", "Do not check GitHub for later versions."),
                    ("/?, -h, --help", "Show this help message.")
                ]
            };

            // Determine max line width (at least 50 chars, or console width - 5).
            // Console.WindowWidth throws when output is redirected, so use 80 columns then.
            int consoleWidth = 80;
            if (!Console.IsOutputRedirected)
                consoleWidth = Console.WindowWidth;

            int maxLineWidth = Math.Max(consoleWidth, 50) - 5;

            // Find max option length across all sections for consistent column width
            int firstColWidth = 0;

            foreach (var section in sections.Values)
                foreach (var (option, _) in section)
                    firstColWidth = Math.Max(firstColWidth, option.Length);

            firstColWidth += 2;  // 2-character gap

            // Print each section followed by the options
            foreach (var (header, options) in sections)
            {
                // Section header
                Console.WriteLine($"{header}:");

                // Each option + description
                foreach (var (option, description) in options)
                {
                    // Wrap description
                    var wrapped = WrapText(description, maxLineWidth - firstColWidth - 1);  // -1 for extra indent
                    bool firstLine = true;

                    foreach (var line in wrapped)
                    {
                        if (firstLine)
                        {
                            // First line: 1-character indent, then the option, then the description.
                            Console.WriteLine($" {option.PadRight(firstColWidth)}{line}");
                            firstLine = false;
                        }
                        else
                        {
                            // Wrapped lines: 1-character indent, then firstColWidth spaces, 1 space and the text.
                            Console.WriteLine(new string(' ', firstColWidth + 2) + line);
                        }
                    }
                }

                Console.WriteLine();  // Blank line between sections.
            }
        }

        /// <summary>
        /// Displays the header information for the application, including version, copyright, and settings.
        /// </summary>
        public static void ShowHeader()
        {
            string line = new('─', 70);

            Console.WriteLine(line);
            Console.WriteLine($"TuneLift {VersionHelper.OutputVersion(Globals.ProgramVersion)}");
            Console.WriteLine($"Copyright © 2020-{DateTime.Now.Year} Richard Lawrence");
            Console.WriteLine("https://github.com/mrsilver76/tunelift");
            Console.WriteLine();
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY. This is free software,");
            Console.WriteLine("and you are welcome to redistribute it under certain conditions; see");
            Console.WriteLine("the documentation for details.");
            Console.WriteLine();

            // Prepare titles + content
            List<(string Title, string Value)> items = [];

            if (string.IsNullOrEmpty(Globals.XmlLibrary))
                items.Add(("Source:", "iTunes"));
            else
                items.Add(("Source:", Globals.XmlLibrary));

            items.Add(("Export folder:", Globals.ExportFolder));
            items.Add(("Playlists:", DescribePlaylistSelection()));

            if (!string.IsNullOrEmpty(Globals.IgnorePrefix))
                items.Add(("Ignoring prefix:", Globals.IgnorePrefix));

            items.Add(("Format:", DescribeFormat()));

            if (!string.IsNullOrEmpty(Globals.BasePath))
                items.Add(("Base path:", Globals.BasePath));
            if (!string.IsNullOrEmpty(Globals.FindText))
            {
                items.Add(("Find:", Globals.FindText));
                items.Add(("Replace with:", Globals.ReplaceText));
            }

            if (Globals.DeleteExisting)
                items.Add(("Existing files:", "Delete before export"));

            // Find longest title length
            int pad = items.Max(i => i.Title.Length) + 2;

            // Print everything
            foreach (var (title, value) in items)
                Console.WriteLine($"{title.PadRight(pad)}{value}");

            Console.WriteLine(line);
            Console.WriteLine();

            // Log environment info
            LogEnvironmentInfo();
        }

        /// <summary>
        /// Tells the user that TuneLift could not connect to iTunes, why that may be, and how to use an
        /// XML library instead.
        /// </summary>
        public static void ShowComFailure()
        {
            string xmlLibrary = CommandLineParser.FindXmlLibrary();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" ⚠️ Warning: TuneLift could not connect to iTunes!");
            Console.ResetColor();
            Console.WriteLine("    iTunes may not be installed, or you may be running a version of");
            Console.WriteLine("    iTunes (such as the one from the Microsoft Store) that no longer");
            Console.WriteLine("    supports this method of connecting.");
            Console.WriteLine();

            if (xmlLibrary.Length == 0)
            {
                Console.WriteLine("    You can still export your playlists by appending -x followed by");
                Console.WriteLine("    the path to your iTunes XML library file.");
            }
            else
            {
                Console.WriteLine("    You can still export your playlists by appending the following:");
                Console.WriteLine($"      -x \"{xmlLibrary}\"");
            }

            Console.WriteLine();
            Console.WriteLine("    For more information, please read the FAQ:");
            Console.WriteLine("      https://github.com/mrsilver76/tunelift/blob/main/FAQ.md#com");
            Console.WriteLine();
        }

        /// <summary>
        /// Describes which kinds of playlist will be exported.
        /// </summary>
        /// <returns>For example "Regular and smart".</returns>
        private static string DescribePlaylistSelection()
        {
            if (Globals.IgnoreSmartPlaylists)
                return "Regular only";

            if (Globals.IgnorePlaylists)
                return "Smart only";

            return "Regular and smart";
        }

        /// <summary>
        /// Describes the kind of playlist file that will be written.
        /// </summary>
        /// <returns>For example ".m3u8, extended, Unix paths".</returns>
        private static string DescribeFormat()
        {
            string extension = ".m3u";
            if (Globals.AppendEight)
                extension = ".m3u8";

            string extended = "extended";
            if (Globals.NotExtended)
                extended = "basic";

            string paths = "Windows paths";
            if (Globals.UseUnixPaths)
                paths = "Unix paths";

            return $"{extension}, {extended}, {paths}";
        }

        /// <summary>
        /// Output to the logs the environment information, such as .NET version, OS and architecture.
        /// Also includes the full command line.
        /// </summary>
        private static void LogEnvironmentInfo()
        {
            string dotnet = RuntimeInformation.FrameworkDescription;
            string os = RuntimeInformation.OSDescription.Trim();

            string archName = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

            Logger.Write($"Running {VersionHelper.OutputVersion(Globals.ProgramVersion)} on {dotnet} ({os}, {archName})", true);

            Logger.Write($"Command line: {Environment.CommandLine}", true);
        }

        /// <summary>
        /// Checks if there is a later release of the application on GitHub and notifies the user.
        /// </summary>
        public static void CheckLatestRelease()
        {
            // Skip if disabled
            if (!Globals.CheckForUpdates)
                return;

            string gitRepo = "mrsilver76/tunelift";
            var result = GitHubVersionChecker.CheckLatestRelease(Globals.ProgramVersion, gitRepo, Path.Combine(Globals.AppDataPath, "versionCheck.ini"));

            if (result.UpdateAvailable)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"  ℹ️ A new version ({VersionHelper.OutputVersion(result.LatestVersion)}) is available!");
                Console.ResetColor();
                Console.WriteLine($" You are using {VersionHelper.OutputVersion(Globals.ProgramVersion)}");
                Console.WriteLine($"     Get it from https://www.github.com/{gitRepo}/");
            }
        }

        /// <summary>
        /// Given a block of text and a maximum line width, yields lines of text wrapped at word boundaries.
        /// </summary>
        /// <param name="text">The text to wrap.</param>
        /// <param name="maxWidth">The longest line allowed, in characters.</param>
        /// <returns>The wrapped lines, without line breaks.</returns>
        private static IEnumerable<string> WrapText(string text, int maxWidth)
        {
            var words = text.Split(' ');
            var line = new StringBuilder();

            foreach (var word in words)
            {
                int separatorWidth = 0;
                if (line.Length > 0)
                    separatorWidth = 1;

                if (line.Length + word.Length + separatorWidth > maxWidth)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0)
                    line.Append(' ');

                line.Append(word);
            }

            if (line.Length > 0)
                yield return line.ToString();
        }
    }
}
