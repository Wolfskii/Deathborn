#!/usr/bin/env bash
# Compile installer/Deathborn.iss with Inno Setup 6 (ISCC.exe).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ISS="$ROOT/installer/Deathborn.iss"
VERSION="${1:-dev}"
PUBLISH_DIR="$ROOT/client/publish"

if [[ ! -f "$PUBLISH_DIR/Deathborn.Client.exe" ]]; then
  echo "Missing publish build: $PUBLISH_DIR/Deathborn.Client.exe" >&2
  echo "Run: task client:publish" >&2
  exit 1
fi

find_iscc() {
  local candidate
  for candidate in \
    "${ISCC:-}" \
    "$LOCALAPPDATA/Programs/Inno Setup 6/ISCC.exe" \
    "/c/Program Files (x86)/Inno Setup 6/ISCC.exe" \
    "/c/Program Files/Inno Setup 6/ISCC.exe" \
    "$(command -v ISCC.exe 2>/dev/null || true)" \
    "$(command -v iscc 2>/dev/null || true)"
  do
    if [[ -n "$candidate" && -f "$candidate" ]]; then
      echo "$candidate"
      return 0
    fi
  done
  return 1
}

ISCC_BIN="$(find_iscc || true)"
if [[ -z "$ISCC_BIN" ]]; then
  echo "Inno Setup 6 not found (ISCC.exe)." >&2
  echo "Install: winget install --id JRSoftware.InnoSetup -e" >&2
  echo "Or: https://jrsoftware.org/isdl.php" >&2
  exit 1
fi

python "$ROOT/installer/prepare_assets.py"

cd "$ROOT/installer"
# Use -D not /D: Git Bash mangles /D* into a Windows path and ISCC sees two "scripts".
"$ISCC_BIN" "-DMyAppVersion=$VERSION" "Deathborn.iss"

SETUP_EXE="$ROOT/dist/Deathborn-${VERSION}-win-x64-Setup.exe"
echo "Installer: $SETUP_EXE"
bash "$ROOT/scripts/reveal-artifact.sh" "$SETUP_EXE"
