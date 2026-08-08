#!/usr/bin/env bash
# Ensure Air is on PATH (installs if missing), then live-reload the Go server.
# Used by: task dev:server
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SERVER_DIR="${ROOT}/server"

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

ensure_air
cd "$SERVER_DIR"
exec air -c .air.toml
