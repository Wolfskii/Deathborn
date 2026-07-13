#!/usr/bin/env bash
# Bundle linux-x64 publish output into dist/Deathborn-<version>-linux-x64.tar.gz
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="${1:-dev}"
RID="${2:-linux-x64}"
PUBLISH_DIR="$ROOT/client/publish/$RID"
STAGE="$ROOT/client/.package-stage/$RID"
OUT="$ROOT/dist/Deathborn-${VERSION}-${RID}.tar.gz"

if [[ ! -x "$PUBLISH_DIR/Deathborn.Client" ]]; then
  echo "Missing Linux build: $PUBLISH_DIR/Deathborn.Client" >&2
  echo "Run: task client:publish:linux" >&2
  exit 1
fi

python "$ROOT/scripts/generate_app_icons.py"

rm -rf "$STAGE"
mkdir -p "$STAGE"
cp -a "$PUBLISH_DIR/." "$STAGE/"
cp "$ROOT/installer/linux/install.sh" "$STAGE/install.sh"
cp "$ROOT/installer/linux/deathborn.png" "$STAGE/deathborn.png"
chmod +x "$STAGE/install.sh" "$STAGE/Deathborn.Client"

mkdir -p "$ROOT/dist"
rm -f "$OUT"
tar -C "$STAGE" -czf "$OUT" .

echo "Linux package: $OUT"
echo "Friends: tar -xzf $(basename "$OUT") && ./install.sh"
