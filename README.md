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
- Hotbar (keys 1–9, 0), interactables (click or E), remember-me login.

## Tech stack

| Layer | Technology | Language |
| --- | --- | --- |
| **Client** | MonoGame 3.8 (DesktopGL), .NET 8 | **C#** |
| **Server** | Go monolith, WebSocket, fixed-tick loop | **Go** |
| **Database** | PostgreSQL (pgx) | SQL |
| **Infra** | Docker + Docker Compose | — |

The **client is C#** — all gameplay UI, rendering, and networking are plain C# classes (no scene editor). The **server is Go**.

## Layout

```
server/                     Go authoritative game server
client/
  Deathborn.Client.sln
  Deathborn.Client/         MonoGame C# client
    Net/GameClient.cs       HTTP auth + WebSocket
    Screens/                Login, CharacterCreate, World
    Game/                   players, interactables, hotbar
docs/MVP_ROADMAP.md         phased roadmap toward full MVP
Taskfile.yml                task runner (server + client commands)
```

## Running the server

### With Docker (recommended)

```bash
task up
# or: docker compose up --build -d
```

PostgreSQL + Go server start on `http://localhost:8080`. Migrations run on startup.

### Locally (server on host, DB in Docker)

```bash
task db
task dev
```

### Smoke-test auth

```bash
curl -s -X POST localhost:8080/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"a@b.com","password":"hunter2"}'
```

## Running the client

**Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).**

1. Start local dev (database + server + client):

```bash
task dev
```

Or run backend and frontend in **separate terminals**:

```bash
task dev:server    # terminal 1
task dev:client    # terminal 2
```

For Docker-only server (no local client): `task up`

3. Register or log in, create a character, enter the world.
4. **WASD** to move, **E** or **click** to interact, **1–9 / 0** for hotbar.
5. Multiplayer: run a second client instance with another account.

Check **Remember email and password** on the login screen to pre-fill credentials on the next launch.

Server URL defaults to `127.0.0.1:8080` — edit [client/Deathborn.Client/Config.cs](client/Deathborn.Client/Config.cs).

See [client/README.md](client/README.md) for client-specific details.

## Common tasks

This repo uses [Task](https://taskfile.dev) (`Taskfile.yml`). Run `task` to list everything.

| Command | What it does |
| --- | --- |
| `task dev` | **Local dev:** Postgres (Docker) + Go server + MonoGame client on host |
| `task dev:server` | Backend only — Go server on host |
| `task dev:client` | Frontend only — MonoGame client (server must be running) |
| `task up` / `task stop` | Full stack in Docker only (no local client) |
| `task client:publish` | Self-contained client zip in `dist/` |
| `task client:installer` | Windows setup wizard in `dist/` (needs [Inno Setup 6](https://jrsoftware.org/isdl.php)) |
| `task check` | Server fmt + vet + test + client build |
| `task release -- v0.1.0` | Tag release (triggers GitHub Actions) |

## CI & releases

- **CI** — Go server checks + Docker image build + **C# client build**
- **Release** — cross-platform server binaries + Docker image to GHCR

Cut a release: `task release -- v0.1.0`
