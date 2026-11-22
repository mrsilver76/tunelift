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
    /// Entry point
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Main entry point for the application. Returns 0 on success, or 1 if the run failed,
        /// so that batch files and scheduled tasks can tell.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The exit code: 0 for success, 1 for any failure.</returns>
        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch (Exception)
            {
                // Ignore it and use the default encoding instead
            }

            // Initialise the logger. If this fails there is no log to write to, so the message can only go to the console.
            string logFolder = Path.Combine(Globals.AppDataPath, "Logs");

            try
            {
                Logger.Initialise(logFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Cannot open the log file in '{logFolder}': {ex.Message}");
                return 1;
            }

            Logger.Write("TuneLift started.", true);

            try
            {
                // Parse the command line arguments
                CommandLineParser.ParseArguments(args);

                // Show the header information
                ConsoleOutput.ShowHeader();

                // Create the export folder, or delete the playlists already in it if asked
                if (!FileOperations.PrepareExportFolder())
                    return 1;

                // Get the playlists from the XML library if one was given, otherwise from iTunes, and export them
                bool exported;
                if (string.IsNullOrEmpty(Globals.XmlLibrary))
                    exported = ITunesCOM.ExportPlaylists();
                else
                    exported = ITunesXML.ExportPlaylists();

                if (!exported)
                    return 1;

                Logger.Write("TuneLift finished.");

                // Check for latest release
                ConsoleOutput.CheckLatestRelease();

                return 0;
            }
            catch (Exception ex)
            {
                // Any failure not handled where it happens ends the run here. The console gets the
                // message, and the log file gets the full exception for troubleshooting.
                Logger.Write($"Unexpected error: {ex.Message} (details are in the log: {Logger.LogFilePath})");
                Logger.Write(ex.ToString(), true);
                return 1;
            }
        }
    }
}
