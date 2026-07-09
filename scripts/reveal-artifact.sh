#!/usr/bin/env bash
# Open the OS file manager with a built artifact selected (when supported).
set -euo pipefail

TARGET="${1:?usage: reveal-artifact.sh <path-to-file>}"

if [[ ! -f "$TARGET" && -f "${TARGET}.exe" ]]; then
  TARGET="${TARGET}.exe"
fi

if [[ ! -f "$TARGET" ]]; then
  echo "Artifact not found: $TARGET" >&2
  exit 1
fi

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*)
    if command -v explorer.exe >/dev/null 2>&1; then
      WIN="$(cygpath -w "$TARGET" 2>/dev/null || printf '%s' "$TARGET")"
      explorer.exe "/select,${WIN}" >/dev/null 2>&1 || true
    fi
    ;;
  Darwin)
    open -R "$TARGET"
    ;;
  Linux*)
    if command -v xdg-open >/dev/null 2>&1; then
      xdg-open "$(dirname "$TARGET")" >/dev/null 2>&1 || true
    fi
    ;;
esac

echo "Built: $TARGET"
