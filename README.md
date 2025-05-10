# TuneLift
_A Windows command line tool to export iTunes audio playlists as standard or extended .m3u files. It can also adjust file paths for compatibility with other computers, operating systems (like Linux), NAS devices, and embedded systems._

> [!NOTE]
> This program is a complete rewrite of [iTunes Playlist exporter](https://github.com/mrsilver76/itunes_playlist_exporter) and does not upload playlists to Plex. If you wish to upload
> playlists to Plex Media Server then please look at [Plex Playlist Uploder](https://github.com/mrsilver76/plex_playlist_uploader).

## Features
* 🔗 Connects directly to iTunes using the offical Apple SDK, rather than parsing XML files.
* 💾 Export playlists in basic or extended M3U formats.
* 🧠 Export only smart (dynamic rule-based) playlists, regular (manual) playlists or all playlists.
* 🚫 Exclude exporting any playlist whose name starts with specified text.
* 🐧 Convert paths (forward slashes and LF endings) for Linux.
* 🔁 Rewrite paths to make exported playlists portable.
* 🧹 Delete existing exports before saving new ones.

## Download

Get the latest version from https://github.com/mrsilver76/tunelift/releases. If you don't want the source code then you should download the exe file. 

You may need to install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime) first. 

This program has been tested extensively on Windows 11, but should also work on Windows 10.

## Quick start guide

Below are a couple of command scenarios for using TuneLift:

```
TuneLift.exe -d "c:\temp\playlists"

TuneLift.exe --delete-existing "c:\temp\playlists"
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

TuneLift.exe \\raspberry\pi\playlists --delete-existing --linux --find "C:/Users/MrSilver/Music/iTunes/iTunes Media/Music" --replace "/home/pi/music"
```
* Export all playlists from iTunes and save them into the shared network folder `\\raspberry\pi\playlists`.
* Delete any playlists already in the folder before starting.
* Write the playlist files with Linux paths and file endings.
* Replace `C:\Users\MrSilver\Music\iTunes\iTunes Media\Music` with `/home/pi/music`.

> [!IMPORTANT]
> Using `-l` will cause the any backslashes (`\`) in the filename and path to be replaced with with forward slashes (`/`) **before any search and replace is performed**. This is why the search string is written as `C:/Users/MrSilver/Music/iTunes/iTunes Media/Music`.

## Command line options

```
TuneLift.exe [options] <destination folder>
```

If `<destination folder>` doesn't exist then it will be created.

`[options]` can be 1 or more of the following:

### 🎵 Playlist Selection

- **`-ns`, `--no-smart`**  
  Skips smart (dynamic rule-based) playlists and exports only regular (manual) ones.

- **`-np`, `--no-playlist`**  
  Skips regular (manual) playlists and only exports smart (dynamic rule-based) playlists.

- **`-i <text>`, `--ignore <text>`**  
  Excludes playlists whose names start with `<text>`. This is case-insensitive, so `--ignore temp` will ignore playlists with titles such as "TEMPMIX" and "Temp - Chill".

### 📁 Output Format

- **`-8`, `--append-8`**  
  Exports playlists with the `.m3u8` extension instead of `.m3u`.

> [!NOTE]
> **Playlist files are always encoded in UTF-8.** This ensures broad compatibility with file paths and file names that contain non-ASCII characters (eg. accented letters, umlauts or non-Latin symbols). The `--append-8` flag simply changes the extension to `.m3u8`.

- **`-ne`, `--not-extended`**  
  Outputs basic `.m3u` files without extended metadata (track duration or title). This is useful when trying to use the playlists with simpler or legacy media players.

- **`-l`, `--linux`**  
  Converts Windows-style paths (backslashes) to Linux-style (forward slashes) and uses LF line endings. This is useful when exporting playlist files that will be used on Linux based machines - such as NASes, media servers or embedded systems.

> [!TIP]
> If you plan to use `--linux` then you may need to manipulate the path so that it correctly points to the songs. See [Path Rewriting](#-path-rewriting).

### 🔀 Path Rewriting

- **`-f <text>`, `--find <text>`**  
  Searches for a specific substring in each file path. This is intended for use with `--replace` to modify paths for different devices or OSes. Searches are case-insensitive and you can only find one substring.

> [!IMPORTANT]
> When also using `--linux`, backslashes in the path will be replaced with forward slashes **before the search and replace is performed**. As a result, searches containing `/` will always fail unless they are manually replaced with `\`.

- **`-r <text>`, `--replace <text>`**  
  Replaces matched text from `--find` with this new value. If `--find` is used and there is no `--replace` value, then it will be assumed to be blank and the matching string will be removed.

### 🧹 File Management

- **`-d`, `--delete-existing`**  
  Deletes existing playlist files in the destination folder before exporting new ones.

### 📖 Help

- **`/?`, `-h`, `--help`**  
  Displays the full help text with all available options.

## Common questions

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

## Questions/problems?

Please raise an issue at https://github.com/mrsilver76/tunelift/issues.

## Future improvements

Possible future improvements can be found at https://github.com/mrsilver76/tunelift/labels/enhancement. Unless there is significant interest, it's doubtful I'll implement many of them as the program in its current form seems to suit me just fine.

## Version history

### 0.0.1 (xx November 2024)
- Initial release.





