#!/usr/bin/env bash
# Install Deathborn for the current user (no sudo). Works on most glibc distros.
set -euo pipefail

GAME_NAME="Deathborn"
INSTALL_DIR="${DEATHBORN_INSTALL_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/deathborn}"
BIN_DIR="${XDG_BIN_HOME:-$HOME/.local/bin}"
DESKTOP_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
SOURCE_DIR="$(cd "$(dirname "$0")" && pwd)"

mkdir -p "$INSTALL_DIR" "$BIN_DIR" "$DESKTOP_DIR"
rm -rf "${INSTALL_DIR:?}/"*
cp -a "$SOURCE_DIR/." "$INSTALL_DIR/"
find "$INSTALL_DIR" -name install.sh -delete 2>/dev/null || true

ln -sfn "$INSTALL_DIR/Deathborn.Client" "$BIN_DIR/deathborn"

cat >"$DESKTOP_DIR/deathborn.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=${GAME_NAME}
Comment=You are born to die. Only skill decides when.
Exec=${INSTALL_DIR}/Deathborn.Client
Icon=${INSTALL_DIR}/Deathborn.Client
Terminal=false
Categories=Game;
EOF

chmod +x "$INSTALL_DIR/Deathborn.Client" "$BIN_DIR/deathborn"

echo "${GAME_NAME} installed to: $INSTALL_DIR"
echo "Run: deathborn   (or launch ${GAME_NAME} from your app menu)"
echo "Uninstall: rm -rf '$INSTALL_DIR' '$BIN_DIR/deathborn' '$DESKTOP_DIR/deathborn.desktop'"
