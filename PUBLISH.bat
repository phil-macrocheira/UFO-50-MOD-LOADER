@echo off

:: Delete publish folder
rmdir /s /q "publish"

:: Publish Windows
dotnet publish "UFO 50 Mod Loader\UFO 50 Mod Loader.csproj" -c Release -r win-x64 -o "publish\win-x64"

:: Publish Linux
dotnet publish "UFO 50 Mod Loader\UFO 50 Mod Loader.csproj" -c Release -r linux-x64 -o "publish\linux-x64"

:: Copy my mods
robocopy "my mods" "publish\win-x64\my mods" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "my mods" "publish\linux-x64\my mods" /E /NJH /NJS /NDL /NC /NS /NP >nul

:: Copy downloaded mods
robocopy "downloaded mods" "publish\win-x64\downloaded mods" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "downloaded mods" "publish\linux-x64\downloaded mods" /E /NJH /NJS /NDL /NC /NS /NP >nul

:: Copy shortcut scripts
::copy "Shortcut Scripts\SHORTCUT MAKER.bat" "publish\win-x64\SHORTCUT MAKER.bat"
::copy "Shortcut Scripts\SHORTCUT MAKER.sh" "publish\linux-x64\SHORTCUT MAKER.sh"

:: Delete createdump.exe
del "%~dp0publish\win-x64\UFO 50 Mod Loader\createdump.exe" 2>nul
del "%~dp0publish\linux-x64\UFO 50 Mod Loader\createdump" 2>nul

:: Use powershell to get version number
FOR /F "tokens=*" %%i IN ('powershell -ExecutionPolicy Bypass -Command "(Get-Item 'publish/win-x64/UFO 50 Mod Loader.exe').VersionInfo.ProductVersion"') DO (
    SET "version=%%i"
)

vpk download github --repoUrl https://github.com/phil-macrocheira/UFO-50-MOD-LOADER --pre
vpk pack --packId "UFO-50-Mod-Loader" --packVersion %version% --packDir "publish\win-x64" --mainExe "UFO 50 Mod Loader.exe" --runtime win-x64
vpk [linux] pack --packId "UFO-50-Mod-Loader" --packVersion %version% --packDir "publish\linux-x64" --mainExe "UFO 50 Mod Loader" --runtime linux-x64

:: Delete legacy RELEASES files
del "Releases\RELEASES"
del "Releases\RELEASES-linux"

pause