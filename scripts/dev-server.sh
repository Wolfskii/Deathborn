#!/usr/bin/env bash
# Ensure Air is on PATH (installs if missing), then live-reload the Go server.
# Used by: task dev:server
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SERVER_DIR="${ROOT}/server"
WORLD_WATCH_INTERVAL="${DEV_WATCH_INTERVAL:-0.75}"
WORLD_WATCH_PID=""

# Platform overrides ([build.windows]) need Air >= 1.64.
AIR_MIN_VERSION="1.64.0"

air_version() {
  air -v 2>&1 | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -1
}

version_lt() {
  # Returns 0 if $1 < $2 (semver major.minor.patch).
  printf '%s\n%s\n' "$1" "$2" | sort -V | head -1 | grep -qx "$1" && [[ "$1" != "$2" ]]
}

ensure_air() {
  local gobin
  gobin="$(go env GOPATH)/bin"
  export PATH="${gobin}:${PATH}"

  local need_install=1
  if command -v air >/dev/null 2>&1; then
    local ver
    ver="$(air_version || true)"
    if [[ -n "$ver" ]] && ! version_lt "$ver" "$AIR_MIN_VERSION"; then
      need_install=0
    elif [[ -n "$ver" ]]; then
      echo "Air ${ver} is too old (need >= ${AIR_MIN_VERSION}); upgrading..."
    fi
  fi

  if [[ "$need_install" -eq 1 ]]; then
    echo "Installing air (Go live reload)..."
    go install github.com/air-verse/air@latest
    export PATH="${gobin}:${PATH}"
  fi

  if ! command -v air >/dev/null 2>&1; then
    echo "air not found after install. Add ${gobin} to your PATH." >&2
    exit 1
  fi
}

watch_world_data() {
  while true; do
    if ! PYTHONDONTWRITEBYTECODE=1 WORLD_SYNC_QUIET=1 \
      python "$ROOT/scripts/sync_world_if_changed.py"; then
      echo "World data regeneration failed — retrying..." >&2
    fi
    sleep "$WORLD_WATCH_INTERVAL"
  done
}

cleanup() {
  if [ -n "$WORLD_WATCH_PID" ]; then
    kill "$WORLD_WATCH_PID" 2>/dev/null || true
    wait "$WORLD_WATCH_PID" 2>/dev/null || true
  fi
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
ensure_air
cd "$SERVER_DIR"
watch_world_data &
WORLD_WATCH_PID=$!
air -c .air.toml
