using Microsoft.Win32;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;
using IniParser;
using IniParser.Model;
using static TuneLift.Program;
using System;

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
                else if (lowerArg == "-l" || lowerArg == "--linux" || lowerArg == "/l")
                {
                    useLinuxPaths = true;
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

        public static void DisplayUsage(string errorMessage = "")
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version!;

            Console.WriteLine($"Usage: {System.Diagnostics.Process.GetCurrentProcess().ProcessName} [options] <destination folder>\n" +
                                "Export iTunes audio playlists as standard or extended .m3u files.\n");

            if (String.IsNullOrEmpty(errorMessage))
                Console.WriteLine( $"This is version v{version.Major}.{version.Minor}.{version.Revision}, copyright © 2020-{DateTime.Now.Year} Richard Lawrence.\n" +
                                    "Forklift icon by nawicon - Flaticon (https://www.flaticon.com/free-icons/forklift)\n");

            Console.WriteLine(  "Playlist Selection:\n" +
                                "  -ns, --no-smart                Skip exporting smart playlists.\n" +
                                "  -np, --no-playlist             Skip exporting regular (non-smart) playlists.\n" +
                                "  -i <text>, --ignore <text>     Exclude playlists with names starting with <text>.\n" +
                                "\n" +
                                "Output Format:\n" +
                                "  -8, --append-8                 Use .m3u8 file extension.\n" +
                                "  -ne, --not-extended            Export using basic .m3u format, with no extended info.\n" +
                                "  -l, --linux                    Use Linux-style paths and LF line endings.\n" +
                                "\n" +
                                "File Path Adjustments:\n" +
                                "  -f <text>, --find <text>       Match <text> in file path for substitution.\n" +
                                "  -r <text>, --replace <text>    Replace matched text with <text>.\n" +
                                "\n" +
                                "File Management:\n" +
                                "  -d, --delete                   Remove existing playlist files from the destination.\n" +
                                "\n" +
                                "Help:\n" +
                                "  -h, --help                     Show this help message.\n");

            
            if (!string.IsNullOrEmpty(errorMessage))
            { 
                Console.WriteLine($"Error: {errorMessage}");
                Environment.Exit(-1);
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Given a number, returns the number and either the singular or plural
        /// version of that description
        /// </summary>
        /// <param name="num">Number</param>
        /// <param name="single">Word if singular</param>
        /// <param name="plural">Word if plural</param>
        /// <returns></returns>
        public static string Pluralise(int num, string single, string plural)
        {
            string ret = num.ToString() + " ";

            if (num == 1)
                ret += single;
            else
                ret += plural;

            return (ret);
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
                    ini["Version"]["LatestReleaseVersion"] = latest.Value.Version;
                    ini["Version"]["LatestReleaseChecked"] = latest.Value.Timestamp;
                    parser.WriteFile(iniPath, ini);
                    cachedVersion = Version.Parse(latest.Value.Version);
                }
            }

            var localVersion = Assembly.GetExecutingAssembly().GetName().Version!;
            if (cachedVersion != null && cachedVersion > localVersion)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"   A new version ({cachedVersion}) is available!");
                Console.ResetColor();
                Console.WriteLine($" You are using {localVersion.Major}.{localVersion.Minor}.{localVersion.Revision}");
                Console.WriteLine($"    Get it from https://www.github.com/{gitHubRepo}/");
            }
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

            bool parseSuccess = DateTime.TryParse(dateStr, out DateTime lastChecked);
            bool isExpired = !parseSuccess || (DateTime.UtcNow - lastChecked.ToUniversalTime()).TotalDays >= 7;
            bool hasVersion = Version.TryParse(versionStr ?? "", out Version? parsed);

            if (hasVersion)
                cachedVersion = parsed;

            return isExpired || !hasVersion;
        }

        /// <summary>
        /// Fetches the latest version from the GitHub repo by looking at the releases/latest page.
        /// </summary>
        /// <param name="repo"></param>
        /// <returns></returns>
        private static (string Version, string Timestamp)? TryFetchLatestVersion(string repo)
        {
            string url = $"https://api.github.com/repos/{repo}/releases/latest";
            using var client = new HttpClient();

            Version? localVersion = Assembly.GetExecutingAssembly().GetName().Version!;
            string ua = repo.Replace('/', '.') + "/" + localVersion;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);

            try
            {
                string json = client.GetStringAsync(url).GetAwaiter().GetResult();
                var match = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                if (!match.Success) return null;

                string version = match.Groups[1].Value.TrimStart('v', 'V');
                return (version, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if the console supports Unicode characters.
        /// </summary>
        /// <returns>True if the console does</returns>
        static bool ConsoleSupportsUnicode()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                return Console.OutputEncoding.WebName == "utf-8";
            }
            catch
            {
                return false;
            }
        }
    }
}
