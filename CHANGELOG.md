# Version history

## 1.2.0 (22 November 2026)
- Exporting from iTunes is now about 2x faster than previously.
- TuneLift can now export playlists from an iTunes XML library file using the `-x` (`--xml`) option.
- If TuneLift can't connect to iTunes, it now says so and suggests using `-x`. See the [FAQ](FAQ.md#why-is-tunelift-unable-to-connect-to-my-itunes).
- TuneLift now needs the .NET 10 Runtime, as support for .NET 8 ended on 10th November 2026.
- Exit codes are now reported correctly. This enables scripts to understand if the task was successful or not.
- `-d` (`--delete`) now only deletes the existing playlists once TuneLift knows there is something to export.
- One playlist failing to export no longer stops the rest from being exported and half-written playlist files aren't left lying around.
- Playlists whose names end up as the same file name no longer overwrite each other. Later ones get ` (2)`, ` (3)` and so on appended.
- Playlists written using `-u` (`--unix`), no longer start with the UTF-8 BOM, which could stop some Linux and NAS players from reading the first line.
- Tracks with no artist no longer get a leading " - " in their extended title.
- A multitude of parsing issues with command line options have been resolved.
- A number of issues with logging have been resolved.
- The update check can no longer crash or hold up the run. It gives up after 5 seconds when GitHub is down, and a damaged update file is ignored.
- Fixed an issue where `--help` and error messages weren't being printed when the output is being piped or redirected.
- `-u` (`--unix`) is now the documented name for Unix paths. `-l` and `--linux` still work.
- Minor output and documentation changes.

## 1.1.1 (06 October 2025)
- Fixed a bug that exported the tracks in a playlist in the wrong order.
- Updated GitHub version checker to latest version.
- Updated logger to latest version.
- Updated publish script to latest version.

## 1.1.0 (26 August 2025)
- Added `-nc` (`--no-check`) to disable GitHub version checks.
- Added `-c` (`--close`) to close iTunes after export, but only when TuneLift launched it.
- Fixed minor formatting issue when displaying the number of tracks exported.
- Corrected conversion of .NET build numbers to semantic version strings.
- Improved logger performance by keeping log files open instead of repeatedly opening and closing them.
- Split utility functions into separate static classes for clearer structure and easier navigation.
- Resolved all .NET code analysis warnings to standardise style, remove potential pitfalls, and tidy the codebase.
- Relocated FAQs to a dedicated document for a cleaner, more readable README.

## 1.0.0 (23 June 2025)
- 🏁 Declared as the first stable release.
- Cleaned up version number handling, ensuring consistency and correct handling of pre-releases.
- Added `--base-path` option to remove leading paths in playlists to make them relative.
- Added `-l`, `--linux` and `/l` as an alias for `--unix`
- Cleaned up various pieces of code.
- Improved `Publish.ps1` build script for final executable.

## 0.9.1 (21 May 2025)
- Changed `/l`, `-l` and `--linux` to `/u`, `-u` and `--unix` to align with [ListPorter](https://github.com/mrsilver76/listporter) command-line conventions.
- Fixed an issue in the version checker that could cause excessive requests to GitHub under certain conditions.
- Fixed an issue in the version checker caused by a mismatch between .NET assembly versioning and GitHub's semantic versioning.
- Fixed a bug that prevented audiobooks from being included in playlists.
- Fixed a bug where playlists with no audio content were still being written to disk.
- Greatly improved documentation for better clarity and usability.


## 0.9.0 (12 May 2025)
- Initial release, a C# port from [iTunes Playlist Exporter](https://github.com/mrsilver76/itunes_playlist_exporter/).
- Removed Plex importing functionality, now handled by a separate tool called [ListPorter](https://github.com/mrsilver76/listporter/).
- Moved all options to command line, no editing of code required.
- Added automatic version checking with update notifications.
- Logger now writes output to a log file instead of just the console.
- Added `--no-playlist` to skip regular playlists and export only smart ones.
- Added `--append-8` to use `.m3u8` extension instead of `.m3u`.
- Added `--delete` to remove `.m3u` files before exporting (for clean re-exports)
