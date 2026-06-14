# DEATHBORN

> You are born to die. Only skill decides when.

A top-down 2D real-time sandbox MMORPG: permadeath, full-loot, PvP-driven survival.
The server is fully authoritative; the client only sends input and renders state.

This repository currently contains the **Walking Skeleton (M0)** — the thinnest
end-to-end vertical slice. See [docs/MVP_ROADMAP.md](docs/MVP_ROADMAP.md) for the
full phased plan toward the MVP.

## What works today (M0)

- Register / log in with email + password (HTTP, returns a JWT).
- Create a character (one active character per account).
- Connect to the world over WebSocket and move freely (velocity-based).
- See other connected players move in real time.
- Server-authoritative position: the client sends only an input direction; the
  server integrates positions on a fixed tick and broadcasts snapshots.

## Tech stack

- **Client:** Godot 4 (GDScript), 2D top-down.
- **Server:** Go monolith, WebSocket (gorilla/websocket), fixed-tick game loop.
- **Database:** PostgreSQL (pgx).
- **Infra:** Docker + Docker Compose.

## Layout

```
server/                 Go monolith
  cmd/deathborn/        entrypoint
  internal/config/      env config
  internal/db/          pgx pool, migration runner, queries
  internal/auth/        bcrypt + JWT + HTTP handlers
  internal/net/         WebSocket hub, client, JSON protocol
  internal/game/        world, player, tick loop
  migrations/           SQL schema (embedded, run on startup)
client/                 Godot 4 project
  autoload/Net.gd       network singleton (HTTP + WebSocket)
  scenes/               Login, CharacterCreate, World
  scripts/Player.gd     interpolated player node
docs/MVP_ROADMAP.md     phased roadmap toward full MVP
```

## Running the server

### With Docker (recommended)

```bash
docker compose up --build
```

This starts PostgreSQL and the Go server. Migrations run automatically on
server startup. The server listens on `http://localhost:8080`.

### Locally (server only, bring your own Postgres)

```bash
# Start just the database via compose:
docker compose up -d db

cd server
export DATABASE_URL="postgres://deathborn:deathborn@localhost:5432/deathborn?sslmode=disable"
export JWT_SECRET="dev-secret-change-me"
go run ./cmd/deathborn
```

### Smoke-test the auth endpoints

```bash
# Register (returns {"token":"..."})
curl -s -X POST localhost:8080/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"a@b.com","password":"hunter2"}'

# Login
curl -s -X POST localhost:8080/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"a@b.com","password":"hunter2"}'
```

## Running the client

1. Open `client/` in Godot 4.
2. Press Play (F5). The main scene is `scenes/Login.tscn`.
3. Register or log in, create a character, and you are in the world.
4. To test multiplayer, run a second instance (Godot: *Debug > Run Multiple
   Instances*, or export and launch a second copy) and log in with a different
   account. Each client sees the other move.

Movement: arrow keys / WASD (the default Godot `ui_*` actions).

The client points at `127.0.0.1:8080` by default; change `HTTP_BASE` / `WS_BASE`
in [client/autoload/Net.gd](client/autoload/Net.gd) to target a remote server.

## Common tasks

This repo uses [Task](https://taskfile.dev) (`Taskfile.yml`). Run `task` to list
everything. Highlights:

| Command | What it does |
| --- | --- |
| `task start` | Build + run the full stack (Postgres + server) in the foreground |
| `task up` / `task stop` | Start in background / tear down |
| `task logs` | Follow server logs |
| `task dev` | Run the server on the host (`go run`) against the Dockerized DB |
| `task db` | Start only PostgreSQL |
| `task build` | Build a version-stamped binary into `./bin` |
| `task build:image` | Build the server Docker image |
| `task check` | gofmt check + `go vet` + tests (mirrors CI) |
| `task release -- v0.1.0` | Tag and push a release (triggers the Release workflow) |

## CI & releases

GitHub Actions live in [.github/workflows](.github/workflows):

- **CI** (`ci.yml`) runs on pushes to `main`/`develop` and on PRs: gofmt check,
  `go vet`, build, tests, and a no-push Docker image build.
- **Release** (`release.yml`) runs when a `v*` tag is pushed:
  - builds cross-platform server binaries (linux/macOS/windows, amd64/arm64),
    version-stamped via `-ldflags "-X main.version=<tag>"`, with checksums, and
    publishes a GitHub Release with auto-generated notes;
  - builds and pushes the server image to GHCR
    (`ghcr.io/<owner>/<repo>`) tagged with the semver version and `latest`.

Cut a release with `task release -- v0.1.0` (the tag must be `vX.Y.Z`). The
running server logs its version on startup.
