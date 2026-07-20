#!/usr/bin/env bash
# Ensure Air is on PATH (installs if missing), then live-reload the Go server.
# Used by: task dev:server
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SERVER_DIR="${ROOT}/server"

ensure_air() {
  if command -v air >/dev/null 2>&1; then
    return 0
  fi

  local gobin
  gobin="$(go env GOPATH)/bin"
  export PATH="${gobin}:${PATH}"

  if command -v air >/dev/null 2>&1; then
    return 0
  fi

  echo "Installing air (Go live reload)..."
  go install github.com/air-verse/air@latest
  export PATH="$(go env GOPATH)/bin:${PATH}"

  if ! command -v air >/dev/null 2>&1; then
    echo "air not found after install. Add $(go env GOPATH)/bin to your PATH." >&2
    exit 1
  fi
}

ensure_air
cd "$SERVER_DIR"
exec air -c .air.toml
