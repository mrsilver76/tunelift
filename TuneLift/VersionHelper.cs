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
    /// Helper functions for formatting .NET Version objects
    /// </summary>
    internal static class VersionHelper
    {
        /// <summary>
        /// Given a .NET Version object, outputs the version in a semantic version format.
        /// If the build number is greater than 0, it appends (dev build X) to the version string.
        /// </summary>
        /// <param name="netVersion">Version object to format</param>
        /// <returns>Formatted version string</returns>
        public static string ToSemanticString(Version? netVersion)
        {
            if (netVersion == null)
                return "0.0.0";

            int major = netVersion.Major;
            int minor = netVersion.Minor;
            int revision = netVersion.Revision >= 0 ? netVersion.Revision : 0;

            string result = $"{major}.{minor}.{revision}";

            if (netVersion.Build > 0)
                result += $" (dev build {netVersion.Build})";

            return result;
        }
    }
}
