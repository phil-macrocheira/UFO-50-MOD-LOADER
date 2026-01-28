@echo off

:: Publish windows
dotnet publish "UFO 50 Mod Loader\UFO 50 Mod Loader.csproj" -c Release -r win-x64 -o "publish\win-x64"

:: Public Linux
dotnet publish "UFO 50 Mod Loader\UFO 50 Mod Loader.csproj" -c Release -r linux-x64 -o "publish\linux-x64"

:: Rearrange Windows publish folders
mkdir "%~dp0\publish\win-x64\UFO 50 Mod Loader"
move "%~dp0\publish\win-x64\*" "%~dp0\publish\win-x64\UFO 50 Mod Loader"
rmdir "%~dp0\publish\win-x64" 2>nul
robocopy "Data" "publish\win-x64\UFO 50 Mod Loader\Data" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "GMLoader" "publish\win-x64\UFO 50 Mod Loader\GMLoader" /E /XD mods /XF data.win /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "GameSpecificData" "publish\win-x64\UFO 50 Mod Loader\GameSpecificData" /E /NJH /NJS /NDL /NC /NS /NP >nul

:: Rearrange Linux publish folders
ren "%~dp0\publish\linux-x64\UFO 50 Mod Loader" "UFO-50-Mod-Loader"
mkdir "%~dp0\publish\linux-x64\UFO 50 Mod Loader"
move "%~dp0\publish\linux-x64\*" "%~dp0\publish\linux-x64\UFO 50 Mod Loader"
rmdir "%~dp0\publish\linux-x64" 2>nul
robocopy "Data" "publish\linux-x64\UFO 50 Mod Loader\Data" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "GMLoader" "publish\linux-x64\UFO 50 Mod Loader\GMLoader" /E /XD mods /XF data.win /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "GameSpecificData" "publish\linux-x64\UFO 50 Mod Loader\GameSpecificData" /E /NJH /NJS /NDL /NC /NS /NP >nul

:: Copy my mods
robocopy "my mods" "publish\win-x64\my mods" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "my mods" "publish\linux-x64\my mods" /E /NJH /NJS /NDL /NC /NS /NP >nul

:: Delete createdump.exe
del "%~dp0\publish\win-x64\UFO 50 Mod Loader\createdump.exe" 2>nul
del "%~dp0\publish\linux-x64\UFO 50 Mod Loader\createdump" 2>nul

pause