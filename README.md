# TuneLift
_A command line program (for Microsoft Windows) to export your audio playlists stored within iTunes as m3u or extended m3u files._

## Features
* 🔗 Connects directly to iTunes using the offical Apple SDK. No parsing of XML files is required.
* 📄 Export playlists as `.m3u` or `.m3u8`. Save playlists in basic or extended M3U formats.
* 🧠 Export only smart, only regular or all playlists.
* 🚫 Exclude exporting any playlist whose name starts with specified text.
* 🐧 Convert paths (forward slashes and LF endings) for Linux.
* 🔁 Rewrite paths to make exported playlists portable.
* 🧹 Delete existing exports before saving new ones.

## Download

Get the latest version from https://github.com/mrsilver76/tunelift/releases. If you don't want the source code then you should download the exe file for your platform. 

This program has been tested on Windows 11, but should also work on Windows 10. You may need to install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime) first. 

## Quick start guide

```
TuneLift.exe -d "c:\temp\playlists"
```

* Export all playlists from iTunes and save them into `C:\Temp\Playlists`.
* Delete any playlists already in the folder before starting.

```
TuneLift.exe "C:\Temp\PlayLists" -d -l -f "C:/Users/MrSilver/Music/iTunes/iTunes Media/Music" -r "/home/mrsilver/music"
```

* Export all playlists from iTunes and save them into `C:\Temp\Playlists`.
* Delete any playlists already in the folder before starting.
* Write the playlist files with Linux paths and file endings.
* Replace `C:\Users\MrSilver\Music\iTunes\iTunes Media\Music` with `/home/mrsilver/music`.

> [!IMPORTANT]
> Using `-l` for Linux paths will cause the filename and path to be replaced with with `/` **before** any search and replace is performed. As such your search string needs to use `/` otherwise it won't match.

## Command line options

```
TuneLift [options] <destination folder>
```

where `[options]` can be 1 or more of the following:

### 🎵 Playlist Selection

- **`-ns`, `--no-smart`**  
  Skips smart (dynamic rule-based playlists) and exports only regular (manual) ones.

- **`-np`, `--no-playlist`**  
  Skips regular (manual) playlists and only exports smart (dynamic rule-based) playlists.

- **`-i <text>`, `--ignore <text>`**  
  Excludes playlists whose names start with `<text>`. This is case-insensitive, so `--ignore temp` will ignore playlists with titles such as "TEMPMIX" and "Temp - Chill".

### 📁 Output Format

- **`-8`, `--append-8`**  
  Exports playlists with the `.m3u8` extension instead of `.m3u`.

> [!NOTE]
> **Playlist files are always encoded in UTF-8.** This ensures broad compatibility with file paths and file names that contain non-ASCII characters (eg. accented letters, umlauts or non-Latin symbols). The `-8` flag simply changes the extension to `.m3u8`.

- **`-ne`, `--not-extended`**  
  Outputs basic `.m3u` files without extended metadata (like track duration or title comments). This is useful when trying to use the playlists with simpler or legacy media players.

- **`-l`, `--linux`**  
  Converts Windows-style paths (backslashes) to Linux-style (forward slashes) and uses LF line endings. This is useful when exporting playlist files that will be used on Linux based machines - such as NAS, media servers or embedded systems.

> [!TIP]
> If you plan to use `--linux` then you may need to manipulate the path so that it correctly points to the songs. See "Path Rewiring" below.

### 🔀 Path Rewriting

- **`-f <text>`, `--find <text>`**  
  Searches for a specific substring in each file path. This is intended for use with `--replace` to modify paths for different devices or OSes. Searches are case-insensitive.

> [!IMPORTANT]
> When also using `--linux`, backslashes in the path will be replaced with forward slashes **before the search and replace is performed**. As a result, searches containing `/` will always fail unless they are manually replaced with `\`.

- **`-r <text>`, `--replace <text>`**  
  Replaces matched text from `--find` with this new value. If `--find` is used and there is no `--replace` value, then it will be assumed to be blank and the matching string will be removed.

### 🧹 File Management

- **`-d`, `--delete-existing`**  
  Deletes existing playlist files in the destination folder before exporting new ones.

### 📖 Help

- **`-h`, `--help`**  
  Displays the full help text with all available options and usage examples.







