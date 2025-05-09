# TuneLift
_A command line program to export your iTunes audio playlists as m3u or extended m3u files_

## Features
* 📄 Export as .m3u or .m3u8. Save playlists in basic or extended M3U formats.
* 🧠 Export only smart, only regular or all playlists.
* 🚫 Exclude exporting any playlist whose name starts with specified text.
* 🐧 Convert paths (forward slashes and LF endings) for Linux.
* 🔁 Rewrite paths to make exported playlists portable.
* 🧹 Delete existing exports before saving new ones.

## Download

blah

## Quick start guide

```
TuneLift.exe -d "c:\temp\playlists"
```

* Export all playlists from iTunes and save them into `C:\Temp\Playlists`.
* Delete any playlists already in the folder before starting.

```
TuneLift.exe "C:\Temp\PlayLists" -d -l -f "C:/Users/MrSilver/Music/iTunes/iTunes Media/Music" -r "/mnt/content/Music"
```

* Export all playlists from iTunes and save them into `C:\Temp\Playlists`.
* Delete any playlists already in the folder before starting.
* Write the playlist files with Linux paths
* Replace `C:\Users\MrSilver\Music\iTunes\iTunes Media\Music` with `/mnt/content/Music`


> [!NOTE]
> Using `-l` for Linux paths will cause the filename and path to be replaced with with `/` **before** any search and replace is performed. As such your search string needs to use `/` otherwise it won't match.

## Options

