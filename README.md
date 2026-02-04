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