@echo off

:: Clean up old builds and releases
rmdir /s /q "%~dp0publish"
rmdir /s /q "%~dp0Releases"

:: Publish
dotnet publish "UFO 50 Mod Loader\UFO 50 Mod Loader.csproj" -c Release -r win-x64 -o "publish\win-x64"
dotnet publish "UFO 50 Mod Loader\UFO 50 Mod Loader.csproj" -c Release -r linux-x64 -o "publish\linux-x64"

:: Delete createdump executable
del "%~dp0publish\win-x64\UFO 50 Mod Loader\createdump.exe" 2>nul
del "%~dp0publish\linux-x64\UFO 50 Mod Loader\createdump" 2>nul

:: Use powershell to get version number
FOR /F "tokens=*" %%i IN ('powershell -ExecutionPolicy Bypass -Command "(Get-Item 'publish/win-x64/UFO 50 Mod Loader.exe').VersionInfo.ProductVersion"') DO (
    SET "version=%%i"
)

:: Download latest releases from github
set "VPK_REPO_URL=https://github.com/phil-macrocheira/UFO-50-MOD-LOADER"
set "VPK_PRE=true"
vpk download github --repoUrl %VPK_REPO_URL% --channel win
vpk download github --repoUrl %VPK_REPO_URL% --channel linux

:: Pack
set "VPK_PACK_ID=UFO-50-Mod-Loader"
set "VPK_PACK_TITLE=UFO 50 Mod Loader"
set "VPK_PACK_VERSION=%version%"
set "VPK_PACK_AUTHORS=Phil"
vpk pack --packId %VPK_PACK_ID% --packVersion %VPK_PACK_VERSION% --packDir "publish\win-x64" --mainExe "UFO 50 Mod Loader.exe" --runtime win-x64
vpk [linux] pack --packId %VPK_PACK_ID% --packVersion %VPK_PACK_VERSION% --packDir "publish\linux-x64" --mainExe "UFO 50 Mod Loader" --runtime linux-x64

:: Delete legacy RELEASES files
del "Releases\RELEASES"
del "Releases\RELEASES-linux"

pause