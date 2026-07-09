#!/usr/bin/env bash
# Start Go server in background, then N MonoGame clients for local multiplayer testing.
# Used by: task dev / task dev:all [-- CLIENT_COUNT]
set -e

CLIENT_COUNT="${1:-1}"
if ! [[ "$CLIENT_COUNT" =~ ^[0-9]+$ ]] || [ "$CLIENT_COUNT" -lt 1 ]; then
  echo "Usage: task dev:all [-- CLIENT_COUNT]  (CLIENT_COUNT must be a positive integer, default 1)"
  exit 1
fi

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
export DEATHBORN_SKIP_UPDATE="${DEATHBORN_SKIP_UPDATE:-1}"

SERVER_BIN="${ROOT}/bin/deathborn"
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) SERVER_BIN="${SERVER_BIN}.exe" ;;
esac

SERVER_PID=""
CLIENT_PIDS=()
WATCH_PID=""

file_mtime() {
  if [ -f "$1" ]; then
    stat -c %Y "$1" 2>/dev/null || stat -f %m "$1"
  fi
}

stop_clients() {
  for pid in "${CLIENT_PIDS[@]}"; do
    if [ -n "$pid" ]; then
      kill "$pid" 2>/dev/null || true
      wait "$pid" 2>/dev/null || true
    fi
  done
  CLIENT_PIDS=()
}

start_clients() {
  local i
  for ((i = 1; i <= CLIENT_COUNT; i++)); do
    DEATHBORN_INSTANCE="$i" DEATHBORN_SKIP_UPDATE="${DEATHBORN_SKIP_UPDATE:-1}" "$CLIENT_EXE" &
    CLIENT_PIDS+=("$!")
  done
}

all_clients_closed() {
  local pid
  if [ "${#CLIENT_PIDS[@]}" -eq 0 ]; then
    return 0
  fi
  for pid in "${CLIENT_PIDS[@]}"; do
    if [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null; then
      return 1
    fi
  done
  return 0
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

if [ "$CLIENT_COUNT" -eq 1 ]; then
  echo "Starting 1 MonoGame client..."
else
  echo "Starting ${CLIENT_COUNT} MonoGame clients..."
fi
start_clients

echo "Watching for client changes (rebuild restarts all game windows)..."
dotnet watch build --project "$CLIENT_PROJECT" --configuration Debug &
WATCH_PID=$!

last_mtime="$(file_mtime "$CLIENT_DLL")"
while kill -0 "$WATCH_PID" 2>/dev/null; do
  if all_clients_closed; then
    echo "All clients closed."
    break
  fi

  sleep 1
  current_mtime="$(file_mtime "$CLIENT_DLL")"
  if [ -n "$current_mtime" ] && [ "$current_mtime" != "$last_mtime" ]; then
    echo "Client rebuilt — restarting all game windows..."
    stop_clients
    start_clients
    last_mtime="$current_mtime"
  fi
done
