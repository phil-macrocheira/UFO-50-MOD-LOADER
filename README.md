# UFO 50 Mod Loader

An Avalonia App for installing and downloading UFO 50 mods

Installs mods by copying files and repackaging the Game Maker data.win with a modified version of [GMLoader](https://github.com/Senjay-id/GMLoader)

UFO 50 mods are found on [GameBanana](https://gamebanana.com/games/23000)

## Features

### UFO 50 Mods Made Easy

UFO 50 Mod Loader makes playing mods with UFO 50 easy. The program finds your unmodded game files and copies them for you -- all you have to do is download mods from the built-in Mod Downloader, select the mods you want to use, and click 'Load Mods and Launch Game'.

### Windows and Linux Support

UFO 50 Mod Loader is available for Windows and Linux. That includes support for Steam Deck!

### Leave Game Install Files Untouched

By default, the user's UFO 50 game files are copied into the 'UFO 50 Modded Copy' folder. When 'Load Mods and Launch Game' is clicked, the selected mods are installed to the modded copy and the game is automatically launched from this copy as well. To launch this modded copy again without reloading the mods, you can select 'Launch Game' from the UFO 50 dropdown menu.

Be aware that this modded copy uses the same save file as your unmodded game! Use a mod like the [Save To Different File Mod](https://gamebanana.com/mods/607207) to use a different save file for modding.

The user's Steam overlay remains enabled despite running separately from Steam. The original Steam copy of the game remains unedited! For users who prefer to overwrite their game files, there is a setting in the Settings dropdown called 'Overwrite Installed Files'.

Due to some permission quirks with SteamOS, this feature is disabled for SteamOS and the game install files must be overwritten.

### Mod Downloader

UFO 50 Mod Loader includes a built-in Mod Downloader which connects to GameBanana to allow users to browse and download over 100+ UFO 50 mods. All mods downloaded with the Mod Downloader will be highlighted orange in the Mod Downloader when a new update is available for that mod. You can update mods by simply redownloading them in the Mod Downloader.

### Mod Lists

You can save lists of different combinations of mods for later so that you can quickly select that list of mods later. Basically, you can save and load which mods are checked before installing.

### Conflict Checking

UFO 50 Mod Loader checks if mods conflict and prevents installing them together.

### Automatic Self-Updating

UFO 50 Mod Loader is able to update itself when a new version of the program releases.

## Wiki Guide

For more information, see the [Wiki Guide to Modding UFO 50](https://ufo50.miraheze.org/wiki/Guide_to_Modding_UFO_50)

## CLI Headless Install/Launch Mode

### Example
```
"UFO 50 Mod Loader.exe" --headless --mod ./packs/my-adventure --launch --json
```

### CLI surface
```
"UFO 50 Mod Loader.exe" --headless

&#x20;   \[--version]

&#x20;   --mod <path>  \[--mod <path> ...]

&#x20;   \[--launch | -L]

&#x20;   \[--game-path <path>]

&#x20;   \[--ignore-warnings]

&#x20;   \[--json]

```
| Flag | Meaning |
|---|---|
| `--headless` | Required gate. Without it, Mod Loader boots Avalonia normally. |
| `--version` | Prints `<semver>  headless ✓` and exits 0. Used by clients to probe for support. |
| `--mod <path>` | Mod folder path, same shape as `my mods/<x>`. Repeatable. |
| `--launch` / `-L` | After install, launch UFO 50 via `LaunchGameService`. |
| `--game-path <path>` | Override `SettingsService.Settings.GamePath`. |
| `--ignore-warnings` | Treat patch warnings as non-fatal. Blocking conflicts are always fatal. |
| `--json` | Emit one JSON object per line to stdout. |

### Exit codes

| Code | Meaning |
|---|---|
| `0` | Success. Install completed; game launched if `--launch` was given. |
| `10` | Bad args / unknown flag / no `--mod` given. |
| `11` | Game path missing or invalid. |
| `12` | Vanilla copy not yet seeded — run the GUI once first. |
| `20` | Blocking mod conflict (or patch warning without `--ignore-warnings`). |
| `30` | GMLoader install failed. See `log.txt`. |
| `40` | Launch failed (install succeeded). |