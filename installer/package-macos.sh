#!/usr/bin/env bash
# Bundle osx publish output into dist/Deathborn-<version>-<rid>.dmg
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="${1:-dev}"
RID="${2:-osx-arm64}"
PUBLISH_DIR="$ROOT/client/publish/$RID"
APP_NAME="Deathborn.app"
STAGE="$ROOT/client/.package-stage/$RID"
APP_PATH="$STAGE/$APP_NAME"
OUT="$ROOT/dist/Deathborn-${VERSION}-${RID}.dmg"

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "macOS .app/.dmg must be built on a Mac (or use GitHub Actions release)." >&2
  exit 1
fi

if [[ ! -x "$PUBLISH_DIR/Deathborn.Client" ]]; then
  echo "Missing macOS build: $PUBLISH_DIR/Deathborn.Client" >&2
  echo "Run: task client:publish:mac" >&2
  exit 1
fi

python "$ROOT/scripts/generate_app_icons.py"

rm -rf "$STAGE"
mkdir -p "$APP_PATH/Contents/MacOS"
cp -a "$PUBLISH_DIR/." "$APP_PATH/Contents/MacOS/"
cp "$ROOT/installer/macos/Info.plist" "$APP_PATH/Contents/Info.plist"
chmod +x "$APP_PATH/Contents/MacOS/Deathborn.Client"

ICONSET="$ROOT/installer/macos/AppIcon.iconset"
ICNS="$APP_PATH/Contents/Resources/AppIcon.icns"
if [[ -d "$ICONSET" ]]; then
  mkdir -p "$APP_PATH/Contents/Resources"
  iconutil -c icns "$ICONSET" -o "$ICNS"
fi

# Replace version placeholder in Info.plist when possible.
if command -v sed >/dev/null 2>&1; then
  sed -i '' "s/<string>0.1.0<\\/string>/<string>${VERSION}<\\/string>/g" "$APP_PATH/Contents/Info.plist" || true
fi

mkdir -p "$ROOT/dist"
rm -f "$OUT"
hdiutil create \
  -volname "Deathborn" \
  -srcfolder "$APP_PATH" \
  -ov \
  -format UDZO \
  "$OUT" >/dev/null

echo "macOS package: $OUT"
echo "Friends: open the .dmg and drag Deathborn.app to Applications"
