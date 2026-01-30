#!/bin/bash
BASEDIR="$(dirname "$0")"
TARGET="$BASEDIR/UFO 50 Mod Loader/UFO 50 Mod Loader"
DESKTOP="$BASEDIR/UFO 50 Mod Loader.desktop"

if [ ! -f "$TARGET" ]; then
    exit 1
fi

cat > "$DESKTOP" <<EOL
[Desktop Entry]
Type=Application
Name=UFO 50 Mod Loader
Exec="$TARGET"
Path="$BASEDIR/UFO 50 Mod Loader"
Terminal=false
EOL

chmod +x "$DESKTOP"
rm -- "$0"