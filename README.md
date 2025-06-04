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

## 📦 Download

Get the latest version from https://github.com/mrsilver76/tunelift/releases. If you don't want the source code then you should download the exe file. 

You may need to install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime) first. 

This program has been tested extensively on Windows 11, but should also work on Windows 10.

## 🚀 Quick start guide

Below are a couple of command scenarios for using TuneLift:

```
TuneLift.exe /d "c:\temp\playlists"

TuneLift.exe --delete "c:\temp\playlists"
```
* Export all playlists from iTunes and save them into `C:\Temp\Playlists`.
* Delete any playlists already in the folder before starting.

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
> If you plan to use `--unix` then you may need to manipulate the path so that it correctly points to the songs. See [Path rewriting](#-path-rewriting).

### Path rewriting

If the playlist will be used by other users, machines, or software, the original file paths may not work for them. For example, a path like `D:\MyMusic` might not be accessible from a NAS or another computer. Even if the files are shared, the other system may require a different path, such as `\\mycomputer\MyMusic`. To address this, you can use the options below to rewrite paths so they match the environment where the playlist will be used:

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

### File Management

- **`/d`, `-d`, `--delete`**  
  Deletes playlist files already in the destination folder before exporting new ones.

### Help

- **`/?`, `-h`, `--help`**  
  Displays the full help text with all available options.

## ❓ Common questions

### Can I just double-click on this program from Windows Explorer and it run?

The programs expects at least one command line argument to run, so double-clicking on it in Explorer will not work.

However you can enable this with a couple of steps:

1. Place `TuneLift.exe` wherever you would like to store it.
2. Right-click on `TuneLift.exe`, select "Show more options" and then "Create shortcut".
3. Right-click on the newly created `TuneLift.exe - Shortcut` and select "Properties"
4. In the text box labelled "Target" add the following to the end of the string: `-d "%USERPROFILE%\Documents\Exported iTunes Playlists"`. This tells TuneLift to export all the playlists to a folder called `Exported iTunes Playlists` within your `Documents` folder. The `-d` tells TuneLift to delete any existing playlists in there before exporting. You can change these arguments to anything you want as documented [here](#command-line-options).
5. Click on "OK"
6. To run, double-click on `TuneLift.exe - Shortcut`. You can rename this to something more useful and move it elsewhere if you'd like.
7. Once TuneLift has finished running, the pop-up window will close automatically.

### Does this work with the Apple Music app in the Windows Store?

No. TuneLift requires the classic iTunes application for Windows. The new Apple Music app (available from the Microsoft Store) does not expose a COM interface or support local music library access in the same way. Apple has not provided an alternative API or integration point for third-party tools. If you’ve already migrated your iTunes library to the Apple Music app, you’ll need to reinstall iTunes and revert the migration to use TuneLift.

### What happens if a playlist already exists in the destination folder?
By default, existing playlist files will be overwritten. If you’d prefer to clean out the folder first, you can use the `-d` option to delete all `.m3u` or `.m3u8` files in the destination before exporting.

### Can I export playlists to a network drive or shared folder?
Yes, as long as the drive or shared path is accessible and writeable from your system. Make sure the destination is mounted or mapped correctly (e.g., `\\NAS\Music` or a mapped drive like `Z:\`). UNC paths are fully supported.

### Are tracks copied or moved, or is only the playlist file exported?
Only the playlist file is exported. TuneLift does not move, copy or modify any of your music files. It simply generates `.m3u` or `.m3u8` files containing references to the existing file locations.

### Can I use this for non-English filenames or folders?
Yes. All playlist files are encoded in UTF-8 by default, which ensures that characters like `ü`, `é`, `ß`, or `ñ` are correctly preserved in paths and filenames.

### How are smart playlists handled differently from normal playlists?
Smart playlists are evaluated in iTunes at runtime and exported as regular playlists containing fixed track lists. You can choose to exclude smart playlists with the `-ns` flag if needed.

### Will this automatically sync new playlists or changes from iTunes?
No. TuneLift is a manual export tool. It runs once and generates playlist files based on the current state of your iTunes library. If your playlists change later, you'll need to run the export again.

### Should I generate standard m3u or extended m3u files?
It depends on the level of detail you need in your playlist:

* Standard `.m3u` files are simple, listing only the file paths of the tracks. They’re widely compatible with most music players and devices.
* Extended `.m3u` files include additional metadata like track lengths, the playlist title and song titles. This is particularly useful for preserving a playlist title that contains characters not allowed in filenames (e.g., slashes or colons) on Windows or Linux, as the title is explicitly included in the file rather than being inferred from the filename.

> [!TIP]
> **TuneLift generates extended `.m3u` files encoded in UTF-8 format by default.** This ensures correct preservation of special characters in filenames and paths.

### Why do you encode basic m3u files with UTF-8?

Modern operating systems support filenames with a wide range of characters, including non-Latin scripts, accented letters, and symbols. Older `m3u` files often relied on limited codepages (like ASCII or ISO-8859-1), which can't accurately represent these characters. As a result, applications that attempt to read the playlist may fail to locate the referenced files, since misencoded characters in the path make the filenames invalid or unrecognisable.

TuneLift uses UTF-8 encoding to ensure all filenames, regardless of language or special symbols, are preserved correctly. 

### I'm using `--unix`, why isn't `--find` matching?
The `--unix` options change all slashes in the song paths before the `--find` and `--replace` logic runs. This means that if your `--find` string uses backslashes then it won’t match the transformed path.

As an example, lets assume your music track is stored in iTunes at:
```
D:\Music\Pop\track.mp3
```

If you run the tool with:
```
--unix --find "D:\Music" --replace "/mnt/media"
```
then after `--unix` is actioned, the revised path is now:
```
D:/Music/Pop/track.mp3
```
Since the `--find` string `"D:\Music"` doesn't match `"D:/Music"` there will be no further transformations.

✅ **Correct Usage**   
Use forward slashes in the `--find` string to match the slash transformation:
```
--unix --find "D:/Music" --replace "/mnt/media"
```

This will correctly transform the path to `/mnt/media/Pop/track.mp3`

## 🛟 Questions/problems?

Please raise an issue at https://github.com/mrsilver76/tunelift/issues.

## 📝 Attribution

- Apple, iTunes, and macOS are trademarks of Apple Inc., registered in the U.S. and other countries. This tool is not affiliated with or endorsed by Apple Inc.
- Forklift icon by nawicon - Flaticon (https://www.flaticon.com/free-icons/forklift)

## Version history

### 1.0.0 (tbc)
- 🏁 Declared as the first stable release.
- Cleaned up version number handling, ensuring consistency and correct handling of pre-releases.
- Added `--base-path` option to remove leading paths in playlists to make them relative.
- Added `-l`, `--linux` and `/l` as an alias for `--unix`
- Cleaned up various pieces of code.
- Improved `Publish.ps1` build script for final executables.

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





