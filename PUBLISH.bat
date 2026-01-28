@echo off

dotnet publish "UFO 50 Mod Loader/UFO 50 Mod Loader.csproj" -c Release -r win-x64 -o "publish/win-x64"
robocopy "UFO 50 Mod Loader/bin/Debug/net9.0/win-x64/Data" "publish\win-x64\Data" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "UFO 50 Mod Loader/bin/Debug/net9.0/win-x64/GMLoader" "publish\win-x64\GMLoader" /E /XD mods /XF data.win /NJH /NJS /NDL /NC /NS /NP >nul

dotnet publish "UFO 50 Mod Loader/UFO 50 Mod Loader.csproj" -c Release -r linux-x64 -o "publish/linux-x64"
robocopy "UFO 50 Mod Loader/bin/Debug/net9.0/win-x64/Data" "publish\linux-x64\Data" /E /NJH /NJS /NDL /NC /NS /NP >nul
robocopy "UFO 50 Mod Loader/bin/Debug/net9.0/win-x64/GMLoader" "publish\linux-x64\GMLoader" /E /XD mods /XF data.win /NJH /NJS /NDL /NC /NS /NP >nul

pause