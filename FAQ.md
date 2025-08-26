# TuneLift Frequently Asked Questions

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

---

## Can I just double-click on this program from Windows Explorer and it run?

The programs expects at least one command line argument to run, so double-clicking on it in Explorer will not work.

However you can enable this with a couple of steps:

1. Place `TuneLift.exe` wherever you would like to store it.
2. Right-click on `TuneLift.exe`, select "Show more options" and then "Create shortcut".
3. Right-click on the newly created `TuneLift.exe - Shortcut` and select "Properties"
4. In the text box labelled "Target" add the following to the end of the string: `-d "%USERPROFILE%\Documents\Exported iTunes Playlists"`. This tells TuneLift to export all the playlists to a folder called `Exported iTunes Playlists` within your `Documents` folder. The `-d` tells TuneLift to delete any existing playlists in there before exporting. You can change these arguments to anything you want as documented [here](#command-line-options).
5. Click on "OK"
6. To run, double-click on `TuneLift.exe - Shortcut`. You can rename this to something more useful and move it elsewhere if you'd like.
7. Once TuneLift has finished running, the pop-up window will close automatically.

## Does this work with the Apple Music app in the Windows Store?

No. TuneLift requires the classic iTunes application for Windows. The new Apple Music app (available from the Microsoft Store) does not expose a COM interface or support local music library access in the same way. Apple has not provided an alternative API or integration point for third-party tools. If you’ve already migrated your iTunes library to the Apple Music app, you’ll need to reinstall iTunes and revert the migration to use TuneLift.

## Will this automatically sync new playlists or changes from iTunes?
No. TuneLift is a manual export tool. It runs once and generates playlist files based on the current state of your iTunes library. If your playlists change later, you'll need to run the export again.

## What happens if a playlist already exists in the destination folder?
By default, existing playlist files will be overwritten. If you’d prefer to clean out the folder first, you can use the `-d` option to delete all `.m3u` or `.m3u8` files in the destination before exporting.

## Can I export playlists to a network drive or shared folder?
Yes, as long as the drive or shared path is accessible and writeable from your system. Make sure the destination is mounted or mapped correctly (e.g., `\\NAS\Music` or a mapped drive like `Z:\`). UNC paths are fully supported.

## Are tracks copied or moved, or is only the playlist file exported?
Only the playlist file is exported. TuneLift does not move, copy or modify any of your music files. It simply generates `.m3u` or `.m3u8` files containing references to the existing file locations.

## How are smart playlists handled differently from normal playlists?
Smart playlists are evaluated in iTunes at runtime and exported as regular playlists containing fixed track lists. You can choose to exclude smart playlists with the `-ns` flag if needed.

## Should I generate standard m3u or extended m3u files?
It depends on the level of detail you need in your playlist:

* Standard `.m3u` files are simple, listing only the file paths of the tracks. They’re widely compatible with most music players and devices.
* Extended `.m3u` files include additional metadata like track lengths, the playlist title and song titles. This is particularly useful for preserving a playlist title that contains characters not allowed in filenames (e.g., slashes or colons) on Windows or Linux, as the title is explicitly included in the file rather than being inferred from the filename.

> [!TIP]
> **TuneLift generates extended `.m3u` files encoded in UTF-8 format by default.** This ensures correct preservation of special characters in filenames and paths.

## Why do you encode basic m3u files with UTF-8?

Modern operating systems support filenames with a wide range of characters, including non-Latin scripts, accented letters, and symbols. Older `m3u` files often relied on limited codepages (like ASCII or ISO-8859-1), which can't accurately represent these characters. As a result, applications that attempt to read the playlist may fail to locate the referenced files, since misencoded characters in the path make the filenames invalid or unrecognisable.

TuneLift uses UTF-8 encoding to ensure all filenames, regardless of language or special symbols, are preserved correctly. 

## Can I use this for non-English filenames or folders?
Yes. All playlist files are encoded in UTF-8 by default, which ensures that characters like `ü`, `é`, `ß`, or `ñ` are correctly preserved in paths and filenames.

## I'm using `--unix`, why isn't `--find` matching?
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

**Correct Usage:** Use forward slashes in the `--find` string to match the slash transformation:
```
--unix --find "D:/Music" --replace "/mnt/media"
```

This will correctly transform the path to `/mnt/media/Pop/track.mp3`
