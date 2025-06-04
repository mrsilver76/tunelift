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

using System.Text.RegularExpressions;
using IniParser;
using IniParser.Model;
using static TuneLift.Program;

namespace TuneLift
{
    public static class Helpers
    {
        /// <summary>
        /// Parses the command line arguments
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void ParseCommandLineArguments(string[] args)
        {
            if (args.Length == 0)
                DisplayUsage();

            for (int i = 0; i < args.Length; i++)
            {
                string lowerArg = args[i].ToLower();

                if (lowerArg == "-h" || lowerArg == "--help" || lowerArg == "/?")
                    DisplayUsage();
                else if (lowerArg == "-ns" || lowerArg == "--no-smart" || lowerArg == "/ns")
                    ignoreSmartPlaylists = true;
                else if (lowerArg == "-np" || lowerArg == "--no-playlist" || lowerArg == "--no-playlists" || lowerArg == "/np")
                    ignorePlaylists = true;
                else if (lowerArg == "-i" || lowerArg == "--ignore" || lowerArg == "/i" && i + 1 < args.Length)
                {
                    ignorePrefix = args[i + 1];
                    i++;
                }
                else if (lowerArg == "-u" || lowerArg == "--unix" || lowerArg == "/u" || lowerArg == "-l" || lowerArg == "--linux" || lowerArg == "/l")  // Includes Linux as an alias for Unix
                {
                    useUnixPaths = true;
                }
                else if (lowerArg == "-f" || lowerArg == "--find" || lowerArg == "/f" && i + 1 < args.Length)
                {
                    findText = args[i + 1];
                    i++;
                }
                else if (lowerArg == "-r" || lowerArg == "--replace" || lowerArg == "/r" && i + 1 < args.Length)
                {
                    replaceText = args[i + 1];
                    i++;
                }
                else if (lowerArg == "-8" || lowerArg == "--append-8" || lowerArg == "/8")
                    appendEight = true;
                else if (lowerArg == "-ne" || lowerArg == "--not-extended" || lowerArg == "/n")
                    notExtended = true;
                else if (lowerArg == "-d" || lowerArg == "--delete" || lowerArg == "/d")
                    deleteExisting = true;
                else if (lowerArg == "-b" || lowerArg == "--base-path" || lowerArg == "/b" && i + 1 < args.Length)
                {
                    basePath = args[i + 1];
                    i++;
                }
                else if (lowerArg.StartsWith("-") || lowerArg.StartsWith("--") || lowerArg.StartsWith("/"))
                    DisplayUsage($"Unrecognised argument: {args[i]}");
                else // probably the folder
                {
                    if (!string.IsNullOrEmpty(exportFolder))
                        DisplayUsage($"Directory already provided, '{args[i]}' is redundant.");

                    exportFolder = args[i];
                }
            }

            // Validate any arguments

            if (ignorePlaylists && ignoreSmartPlaylists)
                DisplayUsage("Ignoring both playlists and smart playlists means nothing to do.");

            if (string.IsNullOrEmpty(findText) && !string.IsNullOrEmpty(replaceText))
                DisplayUsage($"No text to find defined for replacement text ('{replaceText}')");

            if (string.IsNullOrEmpty(exportFolder))
                DisplayUsage("Missing destination folder.");
        }

        /// <summary>
        /// Displays the usage information for the application, including command line options and version information. If an error message
        /// is provided, it will be displayed and the program will exit with an error status.
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        public static void DisplayUsage(string errorMessage = "")
        {
            Console.WriteLine($"Usage: {System.Diagnostics.Process.GetCurrentProcess().ProcessName} [options] <destination folder>\n" +
                                "Export iTunes audio playlists as standard or extended .m3u files.\n");

            if (String.IsNullOrEmpty(errorMessage))
                Console.WriteLine( $"This is version {OutputVersion(version)}, copyright © 2020-{DateTime.Now.Year} Richard Lawrence.\n" +
                                    "Forklift icon by nawicon - Flaticon (https://www.flaticon.com/free-icons/forklift)\n");

            Console.WriteLine(  "Mandatory Arguments:\n" +
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
                                "Help:\n" +
                                "  /?, -h, --help                 Show this help message.");

            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine();
                Console.WriteLine($"Error: {errorMessage}");
                Environment.Exit(-1);
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Pluralises a string based on the number provided.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="singular"></param>
        /// <param name="plural"></param>
        /// <returns></returns>
        public static string Pluralise(int number, string singular, string plural)
        {
            return number == 1 ? $"{number} {singular}" : $"{number:N0} {plural}";
        }

        /// <summary>
        /// Defines the location for logs and deletes any old log files
        /// </summary>
        public static void InitialiseLogger()
        {
            // Set the path for the application data folder
            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TuneLift");

            // Set the log folder path to be inside the application data folder
            string logFolderPath = Path.Combine(appDataPath, "Logs");

            // Create the folders if they don't exist
            Directory.CreateDirectory(logFolderPath);

            // Delete log files older than 14 days
            var logFiles = Directory.GetFiles(logFolderPath, "*.log");
            foreach (var file in logFiles)
            {
                DateTime lastModified = File.GetLastWriteTime(file);
                if ((DateTime.Now - lastModified).TotalDays > 14)
                    File.Delete(file);
            }
        }

        /// <summary>
        /// Writes a message to the log file for debugging.
        /// </summary>
        /// <param name="message">Message to output</param>
        /// 
        public static void Logger(string message)
        {
            // Define the path and filename for this log
            string logFile = DateTime.Now.ToString("yyyy-MM-dd");
            logFile = Path.Combine(appDataPath, "Logs", $"log-{logFile}.log");

            string tsDate = DateTime.Now.ToString("yyyy-MM-dd");
            string tsTime = DateTime.Now.ToString("HH:mm:ss");

            // Write to file
            File.AppendAllText(logFile, $"[{tsDate} {tsTime}] {message}{Environment.NewLine}");
            // Output to screen
            Console.WriteLine($"[{tsTime}] {message}");
        }

        /// <summary>
        /// Checks if there is a later release of the application on GitHub and notifies the user.
        /// </summary>
        public static void CheckLatestRelease()
        {
            string gitHubRepo = "mrsilver76/tunelift";
            string iniPath = Path.Combine(appDataPath, "versionCheck.ini");

            var parser = new FileIniDataParser();
            IniData ini = File.Exists(iniPath) ? parser.ReadFile(iniPath) : new IniData();

            if (NeedsCheck(ini, out Version? cachedVersion))
            {
                var latest = TryFetchLatestVersion(gitHubRepo);
                if (latest != null)
                {
                    ini["Version"]["LatestReleaseChecked"] = latest.Value.Timestamp;

                    if (!string.IsNullOrEmpty(latest.Value.Version))
                    {
                        ini["Version"]["LatestReleaseVersion"] = latest.Value.Version;
                        cachedVersion = Version.Parse(latest.Value.Version);
                    }

                    parser.WriteFile(iniPath, ini); // Always write if we got any response at all
                }
            }

            if (cachedVersion != null && cachedVersion > version)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(      $"  ℹ️ A new version ({OutputVersion(cachedVersion)}) is available!");
                Console.ResetColor();
                Console.WriteLine($" You are using {OutputVersion(version)}");
                Console.WriteLine(  $"     Get it from https://www.github.com/{gitHubRepo}/");
            }
        }

        /// <summary>
        /// Takes a semantic version string in the format "major.minor.revision" and returns a Version object in
        /// the format "major.minor.0.revision"
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        public static Version? ParseSemanticVersion(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return null;

            var parts = versionString.Split('.');
            if (parts.Length != 3)
                return null;

            if (int.TryParse(parts[0], out int major) &&
                int.TryParse(parts[1], out int minor) &&
                int.TryParse(parts[2], out int revision))
            {
                try
                {
                    return new Version(major, minor, 0, revision);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Compares the last checked date and version in the INI file to determine if a check is needed.
        /// </summary>
        /// <param name="ini"></param>
        /// <param name="cachedVersion"></param>
        /// <returns></returns>
        private static bool NeedsCheck(IniData ini, out Version? cachedVersion)
        {
            cachedVersion = null;

            string dateStr = ini["Version"]["LatestReleaseChecked"];
            string versionStr = ini["Version"]["LatestReleaseVersion"];

            bool hasTimestamp = DateTime.TryParse(dateStr, out DateTime lastChecked);
            bool isExpired = !hasTimestamp || (DateTime.UtcNow - lastChecked.ToUniversalTime()).TotalDays >= 7;

            cachedVersion = ParseSemanticVersion(versionStr);

            return isExpired;
        }

        /// <summary>
        /// Fetches the latest version from the GitHub repo by looking at the releases/latest page.
        /// </summary>
        /// <param name="repo">The name of the repo</param>
        /// <returns>Version and today's date and time</returns>
        private static (string? Version, string Timestamp)? TryFetchLatestVersion(string repo)
        {
            string url = $"https://api.github.com/repos/{repo}/releases/latest";
            using var client = new HttpClient();

            string ua = repo.Replace('/', '.') + "/" + OutputVersion(version);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);

            try
            {
                var response = client.GetAsync(url).GetAwaiter().GetResult();
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                if (!response.IsSuccessStatusCode)
                {
                    // Received response, but it's a client or server error (e.g., 404, 500)
                    return (null, timestamp);  // Still update "last checked"
                }

                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var match = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                if (!match.Success)
                {
                    return (null, timestamp);  // Response body not as expected
                }

                string version = match.Groups[1].Value.TrimStart('v', 'V');
                return (version, timestamp);
            }
            catch
            {
                // This means we truly couldn't reach GitHub at all
                return null;
            }
        }

        /// <summary>
        /// Given a .NET Version object, outputs the version in a semantic version format.
        /// If the build number is greater than 0, it appends `-preX` to the version string.
        /// </summary>
        /// <returns></returns>
        public static string OutputVersion(Version? netVersion)
        {
            if (netVersion == null)
                return "0.0.0";

            // Use major.minor.revision from version, defaulting patch to 0 if missing
            int major = netVersion.Major;
            int minor = netVersion.Minor;
            int revision = netVersion.Revision >= 0 ? netVersion.Revision : 0;

            // Build the base semantic version string
            string result = $"{major}.{minor}.{revision}";

            // Append `-preX` if build is greater than 0
            if (netVersion.Build > 0)
            {
                result += $"-pre{netVersion.Build}";
            }

            return result;
        }
    }
}
