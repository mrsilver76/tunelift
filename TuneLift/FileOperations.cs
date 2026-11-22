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

namespace TuneLift
{
    /// <summary>
    /// Prepares the export folder, creating it or clearing out old playlists.
    /// </summary>
    internal sealed class FileOperations
    {
        /// <summary>
        /// Makes sure the export folder exists. Deleting the playlists already in it is left until
        /// there is something to export, see DeleteExistingFiles.
        /// </summary>
        /// <returns>True if the folder is ready, false if it could not be created.</returns>
        public static bool PrepareExportFolder()
        {
            if (!Directory.Exists(Globals.ExportFolder))
            {
                try
                {
                    Directory.CreateDirectory(Globals.ExportFolder);
                    Logger.Write($"Created folder for export: {Globals.ExportFolder}");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Write($"Unable to create folder '{Globals.ExportFolder}': {ex.Message}");
                    return false;
                }
            }

            Logger.Write($"Exporting to folder: {Globals.ExportFolder}");
            return true;
        }

        /// <summary>
        /// Deletes existing .m3u and .m3u8 files in the export folder. Called once the playlists to
        /// export are known, so that a failed or empty run leaves the previous export in place.
        /// </summary>
        /// <returns>True if the files were deleted, false if a file could not be deleted.</returns>
        public static bool DeleteExistingFiles()
        {
            Logger.Write($"Deleting existing playlists from: {Globals.ExportFolder}");

            int count = 0;

            try
            {
                foreach (string file in Directory.EnumerateFiles(Globals.ExportFolder))
                {
                    string extension = Path.GetExtension(file);
                    if (extension.Equals(".m3u", StringComparison.OrdinalIgnoreCase) || extension.Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write($"Unable to delete: {ex.Message}");
                return false;
            }

            Logger.Write($"Successfully deleted {GrammarHelper.Pluralise(count, "playlist", "playlists")}.");
            return true;
        }
    }
}
