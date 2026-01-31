@echo off

:: Use powershell to get version number
FOR /F "tokens=*" %%i IN ('powershell -ExecutionPolicy Bypass -Command "(Get-Item 'publish/win-x64/UFO 50 Mod Loader.exe').VersionInfo.ProductVersion"') DO (
    SET "version=%%i"
)
if "%version%"=="" (
    echo publish exe file not found. Did you run PUBLISH.bat?
    pause
    exit /b 1
)

:: Get Github token from GITHUB_TOKEN file (do not commit this file!!!)
set /p token=<GITHUB_TOKEN
if "%token%"=="" (
    echo GITHUB_TOKEN file not found.
    pause
    exit /b 1
)

vpk upload github --repoUrl https://github.com/phil-macrocheira/UFO-50-MOD-LOADER --publish --releaseName "UFO 50 Mod Loader v%version%" --tag v%version% --channel win --token %token%
vpk upload github --repoUrl https://github.com/phil-macrocheira/UFO-50-MOD-LOADER --publish --releaseName "UFO 50 Mod Loader v%version%" --tag v%version% --channel linux --merge --token %token%

pause
