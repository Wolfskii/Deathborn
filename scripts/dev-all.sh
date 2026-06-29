#!/usr/bin/env bash
# Start Go server in background, then two MonoGame clients for local multiplayer testing.
# Used by: task dev / task dev:all
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SERVER_DIR="${ROOT}/server"
CLIENT_DIR="${ROOT}/client"
DEV_PORT="${PORT:-8080}"

export DATABASE_URL="${DATABASE_URL:-postgres://deathborn:deathborn@localhost:5432/deathborn?sslmode=disable}"
export JWT_SECRET="${JWT_SECRET:-dev-secret-change-me}"
export PORT="${DEV_PORT}"

SERVER_PID=""
CLIENT1_PID=""
CLIENT2_PID=""

cleanup() {
  for pid in "$CLIENT1_PID" "$CLIENT2_PID"; do
    if [ -n "$pid" ]; then
      kill "$pid" 2>/dev/null || true
      wait "$pid" 2>/dev/null || true
    fi
  done
  if [ -n "$SERVER_PID" ]; then
    echo "Stopping server (pid $SERVER_PID)..."
    kill "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
  fi
}

# Git Bash / MSYS on Windows only supports EXIT in trap (not INT/TERM).
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) trap cleanup EXIT ;;
  *) trap cleanup EXIT INT TERM ;;
esac

echo "Starting Go server on :${DEV_PORT}..."
(cd "$SERVER_DIR" && go run ./cmd/deathborn) &
SERVER_PID=$!

echo "Waiting for server health check..."
ready=0
for i in $(seq 1 30); do
  if curl -sf "http://127.0.0.1:${DEV_PORT}/health" >/dev/null 2>&1; then
    echo "Server is ready."
    ready=1
    break
  fi
  sleep 1
done
if [ "$ready" -ne 1 ]; then
  echo "Server did not become ready in time."
  exit 1
fi

cd "$CLIENT_DIR"
dotnet tool restore

client_watch() {
  DEATHBORN_INSTANCE="$1" dotnet watch run \
    --project Deathborn.Client \
    --configuration Debug \
    --no-hot-reload
}

echo "Starting two MonoGame clients (dotnet watch — file changes restart both windows)..."
client_watch 1 &
CLIENT1_PID=$!
client_watch 2 &
CLIENT2_PID=$!

wait "$CLIENT1_PID" "$CLIENT2_PID"
