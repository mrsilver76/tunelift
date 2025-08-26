# TuneLift
_A Windows command line tool to export iTunes audio playlists as standard or extended `.m3u` files. It can also adjust file paths for compatibility with other computers, operating systems (like Linux), NAS devices and embedded systems._

>[!TIP]
>Using Plex for music? [ListPorter](https://github.com/mrsilver76/listporter) makes it easy to import your resulting `.m3u` files to Plex Media Server.

## 🧰 Features
* 🔗 Connects directly to iTunes via the exposed COM interface, rather than parsing XML files.
* 💾 Export playlists in basic or extended M3U formats.
* 🧠 Export only smart (dynamic rule-based) playlists, regular (manual) playlists or all playlists.
* 🚫 Exclude exporting any playlist whose name starts with specified text.
* 🐧 Convert paths (forward slashes and LF endings) for Linux.
* 🔁 Rewrite paths to make exported playlists portable.
* 📁 Remove a common base path from file entries to make playlists relative.
* 🧹 Delete existing exports before saving new ones.
* ⏹️ Automatically close iTunes after export if TuneLift started it.

## 📦 Download

Get the latest version from https://github.com/mrsilver76/tunelift/releases. If you don't want the source code then you should download the exe file. 

You may need to install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime) first. 

This program has been tested extensively on Windows 11, but should also work on Windows 10.

## 🚀 Quick start guide

Below are a couple of command scenarios for using TuneLift:

```
TuneLift.exe /d "c:\temp\playlists" -c

TuneLift.exe --delete "c:\temp\playlists" --close
```
* Export all playlists from iTunes and save them into `C:\Temp\Playlists`.
* Delete any playlists already in the folder before starting.
* Close iTunes after exporting (if it was started by TuneLift)

```
TuneLift.exe "C:\Users\MrSilver\Documents\Playlists" -ne -i "run" -ns

TuneLift.exe "C:\Users\MrSilver\Documents\Playlists" --not-extended --ignore "run" --no-smart
```
* Export all playlists from iTunes and save them into the `My Documents\Playlists` folder owned by `MrSilver`.
* Use the basic (not extended) `m3u` file format.
* Don't export any playlists with a title that starts with `run`.
* Don't export smart (dynamic rule-based) playlists.

```
TuneLift.exe \\raspberry\pi\playlists -d -l -f "C:/Users/MrSilver/Music/iTunes/iTunes Media/Music" -r "/home/pi/music"

TuneLift.exe \\raspberry\pi\playlists --delete --unix --find "C:/Users/MrSilver/Music/iTunes/iTunes Media/Music" --replace "/home/pi/music"
```
* Export all playlists from iTunes and save them into the shared network folder `\\raspberry\pi\playlists`.
* Delete any playlists already in the folder before starting.
* Write the playlist files with Unix paths and file endings.
* Replace `C:\Users\MrSilver\Music\iTunes\iTunes Media\Music` with `/home/pi/music`.

> [!IMPORTANT]
> When using `--unix`, path slashes are converted before any `--find` and `--replace` operations. Make sure your `--find` string reflects the adjusted slash style. In the example above, backslashes are converted to `/`, so `--find` must also use forward slashes (`/`.

## 💻 Command line options

```
TuneLift.exe [options] <destination folder>
```

If `<destination folder>` doesn't exist then it will be created.

`[options]` can be 1 or more of the following:

### Playlist selection

- **`/ns`, `-ns`, `--no-smart`**  
  Skips smart (dynamic rule-based) playlists and exports only regular (manual) ones.

- **`/np`, `-np`, `--no-playlist`**  
  Skips regular (manual) playlists and only exports smart (dynamic rule-based) playlists.

- **`/i <text>`, `-i <text>`, `--ignore <text>`**  
  Excludes playlists whose names start with `<text>`. This is case-insensitive, so `--ignore temp` will ignore playlists with titles such as "TEMPMIX" and "Temp - Chill".

### Output format

- **`/8`, `-8`, `--append-8`**  
  Exports playlists with the `.m3u8` extension instead of `.m3u`.

> [!NOTE]
> **Playlist files are always encoded in UTF-8.** This ensures broad compatibility with file paths and file names that contain non-ASCII characters (eg. accented letters, umlauts or non-Latin symbols). The `--append-8` flag simply changes the extension to `.m3u8`.

- **`/ne`, `-ne`, `--not-extended`**  
  Outputs basic `.m3u` files without extended metadata (track duration or title). This is useful when trying to use the playlists with simpler or legacy media players.

- **`/u`, `-u`, `--unix`**  
  Converts Windows-style paths (backslashes) to Unix-style (forward slashes) and uses LF line endings. This is useful when exporting playlist files that will be used on Unix based machines - such as NASes, media servers or embedded systems.

> [!TIP]
> If you plan to use `--unix` then you may need to manipulate the path so that it correctly points to the songs. See [Path rewriting](#path-rewriting).

### Path rewriting

If the playlist will be used by other users, machines, or software, the original file paths may not work for them. For example, a path like `D:\MyMusic` might not be accessible from a NAS or another computer. Even if the files are shared, the other system may require a different path, such as `\\mycomputer\MyMusic`. To address this, you can use the options below to rewrite paths so they match the environment where the playlist will be used:

> [!TIP]
> If you're using [ListPorter](https://github.com/mrsilver76/listporter) to upload your playlists to Plex, you probably don't need to rewrite paths! This is because ListPorter uses fuzzy matching to associate tracks, even when the paths don’t match exactly.

- **`/f <text>`,`-f <text>`, `--find <text>`**  
  Searches for a specific substring in each file path. This is intended for use with `--replace` to modify paths for different devices or OSes. Searches are case-insensitive and you can only find one substring.

> [!IMPORTANT]
> When also using `--unix`, backslashes in the path will be replaced with forward slashes **before the search and replace is performed**. As a result, searches containing `\` will always fail unless they are manually replaced with `/`.

- **`/r <text>`, `-r <text>`, `--replace <text>`**  
  Replaces matched text from `--find` with this new value. If `--find` is used and there is no `--replace` value, then it will be assumed to be blank and the matching string will be removed.

- **`/b <path>`, `-b <path>`, `--base-path <path>`**   
  Removes the specified base path from the beginning of each file path in the exported playlist. This is useful when you want the playlist entries to be relative to a certain directory. The comparison is case-insensitive.

  If your music files are located at `C:\Music\Library\Artist\Song.mp3` and you set `--base-path C:\Music\Library\`, the resulting path in the playlist will be `Artist\Song.mp3`.

> [!NOTE]
> The base path is removed before any other rewriting actions (such as `--find`, `--replace`, or `--unix`) are applied.

> [!IMPORTANT]
> To avoid leaving a leading slash or backslash in the result, make sure your `--base-path` ends with the appropriate path separator (`\` for Windows-style paths or `/` for Linux-style paths).

### File management

- **`/d`, `-d`, `--delete`**  
  Deletes playlist files already in the destination folder before exporting new ones.

### Other options

- **`-c`, `--close`**  
  Automatically closes iTunes after the export completes, but only if TuneLift started it. iTunes will not be closed if it was already running beforehand.

- **`-nc`, `--no-check`**  
  Disables GitHub version checks for TuneLift.

>[!NOTE]
>Version checks occur at most once every 7 days. TuneLift connects only to [this URL](https://api.github.com/repos/mrsilver76/tunelift/releases/latest) to retrieve version information. No data about you or your music library is shared with the author or GitHub - you can verify this yourself by reviewing `GitHubVersionChecker.cs`

- **`/?`, `-h`, `--help`**  
  Displays the full help text with all available options, credits and the location of the log files.

## ❓ Frequently Asked Questions

Running TuneLift

- [Can I just double-click on this program from Windows Explorer and it run?](FAQ.md#can-i-just-double-click-on-this-program-from-windows-explorer-and-it-run)
- [Does this work with the Apple Music app in the Windows Store?](FAQ.md#does-this-work-with-the-apple-music-app-in-the-windows-store)
- [Will this automatically sync new playlists or changes from iTunes?](FAQ.md#will-this-automatically-sync-new-playlists-or-changes-from-itunes)

Export Behavior

- [What happens if a playlist already exists in the destination folder?](FAQ.md#what-happens-if-a-playlist-already-exists-in-the-destination-folder)
- [Can I export playlists to a network drive or shared folder?](FAQ.md#can-i-export-playlists-to-a-network-drive-or-shared-folder)
- [Are tracks copied or moved, or is only the playlist file exported?](FAQ.md#are-tracks-copied-or-moved-or-is-only-the-playlist-file-exported)

Playlist Types

- [How are smart playlists handled differently from normal playlists?](FAQ.md#how-are-smart-playlists-handled-differently-from-normal-playlists)
- [Should I generate standard m3u or extended m3u files?](FAQ.md#should-i-generate-standard-m3u-or-extended-m3u-files)

Encoding and Path Handling

- [Why do you encode basic m3u files with UTF-8?](FAQ.md#why-do-you-encode-basic-m3u-files-with-utf-8)
- [Can I use this for non-English filenames or folders?](FAQ.md#can-i-use-this-for-non-english-filenames-or-folders)
- [I'm using `--unix`, why isn't `--find` matching?](FAQ.md#im-using---unix-why-isnt---find-matching)

## 🛟 Questions/problems?

Please raise an issue at https://github.com/mrsilver76/tunelift/issues.

## 💡 Future development: open but unplanned

TuneLift currently meets the needs it was designed for, and no major new features are planned at this time. However, the project remains open to community suggestions and improvements. If you have ideas or see ways to enhance the tool, please feel free to submit a [feature request](https://github.com/mrsilver76/tunelift/issues).

## 📝 Attribution

- Apple, iTunes, and macOS are trademarks of Apple Inc., registered in the U.S. and other countries. This tool is not affiliated with or endorsed by Apple Inc.
- Forklift icon by nawicon - Flaticon (https://www.flaticon.com/free-icons/forklift)

## Version history

### 1.1.0 (xx August 2025)
- Added `-nc` (`--no-check`) to disable GitHub version checks.
- Added `-c` (`--close`) to close iTunes after export, but only when TuneLift launched it.
- Fixed minor formatting issue when displaying the number of tracks exported.
- Corrected conversion of .NET build numbers to semantic version strings.
- Improved logger performance by keeping log files open instead of repeatedly opening and closing them.
- Split utility functions into separate static classes for clearer structure and easier navigation.
- Resolved all .NET code analysis warnings to standardise style, remove potential pitfalls, and tidy the codebase.
- Relocated FAQs to a dedicated document for a cleaner, more readable README.

### 1.0.0 (23 June 2025)
- 🏁 Declared as the first stable release.
- Cleaned up version number handling, ensuring consistency and correct handling of pre-releases.
- Added `--base-path` option to remove leading paths in playlists to make them relative.
- Added `-l`, `--linux` and `/l` as an alias for `--unix`
- Cleaned up various pieces of code.
- Improved `Publish.ps1` build script for final executable.

### 0.9.1 (21 May 2025)
- Changed `/l`, `-l` and `--linux` to `/u`, `-u` and `--unix` to align with [ListPorter](https://github.com/mrsilver76/listporter) command-line conventions.
- Fixed an issue in the version checker that could cause excessive requests to GitHub under certain conditions.
- Fixed an issue in the version checker caused by a mismatch between .NET assembly versioning and GitHub's semantic versioning.
- Fixed a bug that prevented audiobooks from being included in playlists.
- Fixed a bug where playlists with no audio content were still being written to disk.
- Greatly improved documentation for better clarity and usability.


### 0.9.0 (12 May 2025)
- Initial release, a C# port from [iTunes Playlist Exporter](https://github.com/mrsilver76/itunes_playlist_exporter/).
- Removed Plex importing functionality, now handled by a separate tool called [ListPorter](https://github.com/mrsilver76/listporter/).
- Moved all options to command line, no editing of code required.
- Added automatic version checking with update notifications.
- Logger now writes output to a log file instead of just the console.
- Added `--no-playlist` to skip regular playlists and export only smart ones.
- Added `--append-8` to use `.m3u8` extension instead of `.m3u`.
- Added `--delete` to remove `.m3u` files before exporting (for clean re-exports)





