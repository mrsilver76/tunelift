/*
 * GitHubVersionChecker.cs
 * Version 1.0.4
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 *  
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

/*
 * Usage:
 * -------
 * This class provides a simple way to check GitHub for the latest release of your application
 * and determine whether an update is available.
 *
 * 1. Add a reference to ini-parser (NuGet: ini-parser) to handle caching in an INI file.
 *
 * 2. Call `GitHubVersionChecker.CheckLatestRelease` from your code:
 *
 *      var currentVersion = new Version(1, 0, 2, 0);  // Your app's current version
 *      var repo = "owner/repo";                       // GitHub repository in "owner/repo" format
 *      var iniPath = "path/to/cache.ini";             // File path to store cached version info
 *
 *      var result = GitHubVersionChecker.CheckLatestRelease(currentVersion, repo, iniPath);
 *
 * 3. The returned `VersionCheckResult` contains:
 *      - UpdateAvailable (bool): true if a newer release exists
 *      - LatestVersion (Version?): the latest version found, null if unavailable
 *
 * 4. The method automatically caches the last check in the provided INI file to avoid
 *    unnecessary network calls. It will re-check GitHub if more than 7 days have passed.
 *    If a check fails (no network, timeout, bad response), it is not retried for 24 hours.
 *
 * Notes:
 * -------
 * - The method does not throw. A damaged or read-only INI file is ignored, and a request
 *   that gets no answer from GitHub within 5 seconds is treated as a failed check.
 * - The version string in GitHub releases should follow "major.minor.revision" (e.g., "1.0.2").
 * - The check respects semantic versioning but converts the revision to the fourth segment
 *   of System.Version (major.minor.0.revision) for comparison.
 * - The HTTP request uses a simple User-Agent header derived from the repository and current
 *   version. Without one, GitHub will reject the request.
 */

using IniParser;
using IniParser.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TuneLift
{
    /// <summary>
    /// Checks GitHub for a newer release. The result is cached in an ini file, so GitHub is asked at most about once a week,
    /// and a failed check is not retried for 24 hours.
    /// </summary>
    internal static partial class GitHubVersionChecker
    {
        /// <summary>
        /// Result of a version check. Contains whether an update is available and the latest version found.
        /// </summary>
        internal readonly record struct VersionCheckResult(
            bool UpdateAvailable,  // Indicates if a newer version is available
            Version? LatestVersion  // The latest version available, or null if not found
        );

        /// <summary>How long to wait for GitHub to answer before giving up.</summary>
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        /// <summary>How long to wait before checking again after a failed check.</summary>
        private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromHours(24);

        /// <summary>Number of days to keep the saved latest version before asking GitHub again.</summary>
        private const int RecheckDays = 7;

        /// <summary>
        /// Returns whether GitHub has a newer release, and its version. The result is cached in the INI file to avoid repeating network requests,
        /// and the GitHub release is fetched only when the cache says a check is due. This method does not throw.
        /// </summary>
        /// <param name="currentVersion">The current version of the software.</param>
        /// <param name="gitHubRepo">The GitHub repository, as "owner/repo".</param>
        /// <param name="iniPath">The file path to the INI configuration file used to cache version check data.</param>
        /// <returns>Whether an update is available, and the latest version found (null if there is none).</returns>
        public static VersionCheckResult CheckLatestRelease(Version currentVersion, string gitHubRepo, string iniPath)
        {
            var parser = new FileIniDataParser();
            IniData ini = new();

            if (File.Exists(iniPath))
            {
                try
                {
                    ini = parser.ReadFile(iniPath);
                }
                catch
                {
                    // Damaged cache: start from an empty one, which makes the check due and rewrites the file below.
                    Logger.Write($"Cannot read '{iniPath}', so it will be recreated.", true);
                }
            }

            if (NeedsCheck(ini, out Version? cachedVersion))
            {
                string? latest = TryFetchLatestVersion(gitHubRepo, currentVersion);
                string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                if (latest != null)
                {
                    ini["Version"]["LatestReleaseChecked"] = now;
                    ini["Version"]["LatestReleaseVersion"] = latest;
                    ini["Version"].RemoveKey("LastCheckFailed");
                    cachedVersion = ParseSemanticVersion(latest);
                }
                else
                {
                    // Cache the failure so an unreachable GitHub is not retried on every run
                    ini["Version"]["LastCheckFailed"] = now;
                }

                try
                {
                    parser.WriteFile(iniPath, ini);
                }
                catch
                {
                    // Read-only file or missing folder: the check will repeat next time
                }
            }

            bool updateAvailable = cachedVersion != null && cachedVersion > currentVersion;

            return new VersionCheckResult(updateAvailable, cachedVersion);
        }

        /// <summary>
        /// Parses a semantic version string "major.minor.revision" into a Version object "major.minor.0.revision".
        /// Returns null if parsing fails.
        /// </summary>
        /// <param name="versionString">The version text, for example "1.6.0".</param>
        /// <returns>The version, or null if the text is not three non-negative whole numbers separated by dots.</returns>
        private static Version? ParseSemanticVersion(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return null;

            var parts = versionString.Split('.');
            if (parts.Length != 3)
                return null;

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int major))
                return null;

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minor))
                return null;

            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int revision))
                return null;

            // Version does not accept negative numbers
            if (major < 0 || minor < 0 || revision < 0)
                return null;

            return new Version(major, minor, 0, revision);
        }

        /// <summary>
        /// Returns whether a new check is due.
        /// </summary>
        /// <param name="ini">The cached check data.</param>
        /// <param name="cachedVersion">The latest version stored in the cache, or null if there is none.</param>
        /// <returns>True if the last check is a week old or more and a failed check is not being waited out.</returns>
        private static bool NeedsCheck(IniData ini, out Version? cachedVersion)
        {
            string dateStr = ini["Version"]["LatestReleaseChecked"];
            string versionStr = ini["Version"]["LatestReleaseVersion"];
            string failedStr = ini["Version"]["LastCheckFailed"];

            // The dates are stored as UTC in this fixed format (see CheckLatestRelease)
            const string StoredFormat = "yyyy-MM-dd HH:mm:ss";
            const DateTimeStyles StoredStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

            bool hasTimestamp = DateTime.TryParseExact(dateStr, StoredFormat, CultureInfo.InvariantCulture, StoredStyles, out DateTime lastChecked);
            bool hasFailure = DateTime.TryParseExact(failedStr, StoredFormat, CultureInfo.InvariantCulture, StoredStyles, out DateTime lastFailed);

            bool isExpired = !hasTimestamp || (DateTime.UtcNow - lastChecked).TotalDays >= RecheckDays;
            bool backingOff = hasFailure && (DateTime.UtcNow - lastFailed) < RetryAfterFailure;

            cachedVersion = ParseSemanticVersion(versionStr);

            return isExpired && !backingOff;
        }

        /// <summary>
        /// Fetches the latest release version from GitHub.
        /// Returns null if the fetch fails for any reason (timeout, no network, bad response).
        /// </summary>
        /// <param name="repo">The GitHub repository, in the format "owner/repo".</param>
        /// <param name="currentVersion">The running version, sent to GitHub in the User-Agent header.</param>
        /// <returns>The release tag without a leading "v", or null if it could not be fetched.</returns>
        private static string? TryFetchLatestVersion(string repo, Version currentVersion)
        {
            try
            {
                string url = $"https://api.github.com/repos/{repo}/releases/latest";
                using var client = new HttpClient { Timeout = RequestTimeout };

                string ua = $"{repo.Replace('/', '.')}/{currentVersion}";
                client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);

                using var response = client.GetAsync(url).GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    return null;

                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var match = TagName().Match(json);

                if (!match.Success)
                    return null;

                return match.Groups[1].Value.TrimStart('v', 'V');
            }
            catch
            {
                // Any failure (timeout, no network, bad response) is treated as a failed check and cached for 24 hours
                return null;
            }
        }

        /// <summary>Matches the "tag_name" field in the GitHub release JSON.</summary>
        [GeneratedRegex("\"tag_name\"\\s*:\\s*\"([^\"]+)\"")]
        private static partial Regex TagName();
    }
}
