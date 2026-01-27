@echo off

dotnet publish "UFO 50 Mod Loader/UFO 50 Mod Loader.csproj" -c Release -r win-x64 -o "publish/win-x64"
robocopy "Data" "publish\win-x64\Data" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "GMLoader" "publish\win-x64\GMLoader" /E /XD mods /XF data.win /NJH /NJS /NDL /NC /NS /NP >nul

dotnet publish "UFO 50 Mod Loader/UFO 50 Mod Loader.csproj" -c Release -r linux-x64 -o "publish/linux-x64"
robocopy "Data" "publish\linux-x64\Data" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "GMLoader" "publish\linux-x64\GMLoader" /E /XD mods /XF data.win /NJH /NJS /NDL /NC /NS /NP >nul

pause