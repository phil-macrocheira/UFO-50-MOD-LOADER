@echo off
setlocal

set BASEDIR=%~dp0
set TARGET=%BASEDIR%UFO 50 Mod Loader\UFO 50 Mod Loader.exe
set SHORTCUT=%BASEDIR%UFO 50 Mod Loader.lnk

if not exist "%TARGET%" exit /b 1

powershell -NoProfile -Command ^
 "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('%SHORTCUT%');" ^
 "$s.TargetPath='%TARGET%';" ^
 "$s.WorkingDirectory='%~dp0UFO 50 Mod Loader';" ^
 "$s.Save()"

del "%~f0"