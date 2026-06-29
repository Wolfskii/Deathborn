#!/usr/bin/env bash
# Start Go server in background, then two MonoGame clients for local multiplayer testing.
# Used by: task dev / task dev:all
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SERVER_DIR="${ROOT}/server"
CLIENT_DIR="${ROOT}/client"
CLIENT_PROJECT="$CLIENT_DIR/Deathborn.Client/Deathborn.Client.csproj"
CLIENT_DLL="$CLIENT_DIR/Deathborn.Client/bin/Debug/net8.0/Deathborn.Client.dll"
CLIENT_EXE="${CLIENT_DLL%.dll}"
DEV_PORT="${PORT:-8080}"

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) CLIENT_EXE="${CLIENT_EXE}.exe" ;;
esac

export DATABASE_URL="${DATABASE_URL:-postgres://deathborn:deathborn@localhost:5432/deathborn?sslmode=disable}"
export JWT_SECRET="${JWT_SECRET:-dev-secret-change-me}"
export PORT="${DEV_PORT}"
export LISTEN_HOST="${LISTEN_HOST:-127.0.0.1}"

SERVER_BIN="${ROOT}/bin/deathborn"
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) SERVER_BIN="${SERVER_BIN}.exe" ;;
esac

SERVER_PID=""
CLIENT1_PID=""
CLIENT2_PID=""
WATCH_PID=""

file_mtime() {
  if [ -f "$1" ]; then
    stat -c %Y "$1" 2>/dev/null || stat -f %m "$1"
  fi
}

stop_clients() {
  for pid in "$CLIENT1_PID" "$CLIENT2_PID"; do
    if [ -n "$pid" ]; then
      kill "$pid" 2>/dev/null || true
      wait "$pid" 2>/dev/null || true
    fi
  done
  CLIENT1_PID=""
  CLIENT2_PID=""
}

start_clients() {
  DEATHBORN_INSTANCE=1 "$CLIENT_EXE" &
  CLIENT1_PID=$!
  DEATHBORN_INSTANCE=2 "$CLIENT_EXE" &
  CLIENT2_PID=$!
}

cleanup() {
  if [ -n "$WATCH_PID" ]; then
    kill "$WATCH_PID" 2>/dev/null || true
    wait "$WATCH_PID" 2>/dev/null || true
  fi
  stop_clients
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

echo "Starting Go server on ${LISTEN_HOST}:${DEV_PORT}..."
mkdir -p "${ROOT}/bin"
(
  cd "$SERVER_DIR"
  go build -o "$SERVER_BIN" ./cmd/deathborn
  exec "$SERVER_BIN"
) &
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

echo "Building MonoGame client..."
dotnet build "$CLIENT_PROJECT" --configuration Debug

echo "Starting two MonoGame clients..."
start_clients

echo "Watching for client changes (rebuild restarts both game windows)..."
dotnet watch build --project "$CLIENT_PROJECT" --configuration Debug &
WATCH_PID=$!

last_mtime="$(file_mtime "$CLIENT_DLL")"
while kill -0 "$WATCH_PID" 2>/dev/null; do
  if [ -n "$CLIENT1_PID" ] && ! kill -0 "$CLIENT1_PID" 2>/dev/null \
     && [ -n "$CLIENT2_PID" ] && ! kill -0 "$CLIENT2_PID" 2>/dev/null; then
    echo "Both clients closed."
    break
  fi

  sleep 1
  current_mtime="$(file_mtime "$CLIENT_DLL")"
  if [ -n "$current_mtime" ] && [ "$current_mtime" != "$last_mtime" ]; then
    echo "Client rebuilt — restarting both game windows..."
    stop_clients
    start_clients
    last_mtime="$current_mtime"
  fi
done
