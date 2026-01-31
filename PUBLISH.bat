@echo off

:: Delete publish folder
rmdir /s /q "publish"

:: Publish windows
dotnet publish "UFO 50 Mod Loader\UFO 50 Mod Loader.csproj" -c Release -r win-x64 -o "publish\win-x64"

:: Public Linux
dotnet publish "UFO 50 Mod Loader\UFO 50 Mod Loader.csproj" -c Release -r linux-x64 -o "publish\linux-x64"

:: Rearrange Windows publish folders
robocopy "%~dp0publish\win-x64" "%~dp0publish\win-x64\UFO 50 Mod Loader" /E /MOVE /XD "UFO 50 Mod Loader"
rmdir "%~dp0publish\win-x64" 2>nul

:: Rearrange Linux publish folders
ren "%~dp0publish\linux-x64\UFO 50 Mod Loader" "UFO-50-Mod-Loader"
robocopy "%~dp0publish\linux-x64" "%~dp0publish\linux-x64\UFO 50 Mod Loader" /E /MOVE /XD "UFO 50 Mod Loader"
ren "%~dp0publish\linux-x64\UFO 50 Mod Loader\UFO-50-Mod-Loader" "UFO 50 Mod Loader"
rmdir "%~dp0publish\linux-x64" 2>nul

:: Copy my mods
robocopy "my mods" "publish\win-x64\my mods" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "my mods" "publish\linux-x64\my mods" /E /NJH /NJS /NDL /NC /NS /NP >nul

:: Copy downloaded mods
robocopy "downloaded mods" "publish\win-x64\downloaded mods" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "downloaded mods" "publish\linux-x64\downloaded mods" /E /NJH /NJS /NDL /NC /NS /NP >nul

:: Copy shortcut scripts
copy "Shortcut Scripts\SHORTCUT MAKER.bat" "publish\win-x64\SHORTCUT MAKER.bat"
copy "Shortcut Scripts\SHORTCUT MAKER.sh" "publish\linux-x64\SHORTCUT MAKER.sh"

:: Delete createdump.exe
del "%~dp0publish\win-x64\UFO 50 Mod Loader\createdump.exe" 2>nul
del "%~dp0publish\linux-x64\UFO 50 Mod Loader\createdump" 2>nul

pause