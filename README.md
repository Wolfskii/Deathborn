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

Server URL: local `127.0.0.1:8080` when a dev server is running, otherwise production — see [Server URL](#server-url) below and [client/README.md](client/README.md).

See [client/README.md](client/README.md) for client-specific details.

## Shipping the client (Windows)

Build a self-contained release and package it for friends. **You only need the .NET SDK on your machine** — friends do not need .NET installed.

### Prerequisites (your machine)

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Task](https://taskfile.dev)
- **Installer only:** [Inno Setup 6](https://jrsoftware.org/isdl.php)

```bash
winget install JRSoftware.InnoSetup
```

### Option A — Windows installer (recommended)

One file to send; adds Start Menu entry, desktop shortcut, and uninstaller.

```bash
task client:installer
```

Output: `dist/Deathborn-<version>-win-x64-Setup.exe`

Send friends **only that Setup.exe**. They run it, click through the wizard, and launch from the desktop or Start Menu.

The installer bundles the full game (runtime, assets, world data). It is **not** just the `.exe` from `client/publish/`.

### Option B — Zip (no installer)

Portable folder — unzip and run `Deathborn.Client.exe` inside.

```bash
task client:publish
```

Output: `dist/Deathborn-<version>-win-x64.zip`

Send the **whole zip**. Friends must extract everything and keep the files together; copying only the `.exe` will not work.

### What friends need

| Item | Installer | Zip |
| --- | --- | --- |
| Windows x64 | Yes | Yes |
| .NET installed | No | No |
| Your live server | Yes* | Yes* |
| Extract / keep folder together | No (installer handles it) | Yes |

\*By default the client uses `https://api.deathborn.wolfskii.dev` when no local server is running. Ensure production is up, or tell friends to set `DEATHBORN_SERVER_URL` (see [client/README.md](client/README.md)).

### Server URL

The client picks a server automatically ([`ServerEndpoints.cs`](client/Deathborn.Client/Net/ServerEndpoints.cs)):

1. `DEATHBORN_SERVER_URL` environment variable, if set
2. `http://127.0.0.1:8080` if `/health` responds (local dev)
3. `https://api.deathborn.wolfskii.dev` (production)

To point a installed build at another host without rebuilding:

```bat
set DEATHBORN_SERVER_URL=https://your-api.example.com
"C:\Program Files\Deathborn\Deathborn.Client.exe"
```

### Installer internals

Scripts live under [`installer/`](installer/):

- `Deathborn.iss` — Inno Setup wizard (logo, swordsman run animation, flavor text)
- `prepare_assets.py` — generates wizard bitmaps from game art
- `build-installer.sh` — called by `task client:installer`

Re-run `task client:installer` after any client or content change you want friends to receive.

## Common tasks

This repo uses [Task](https://taskfile.dev) (`Taskfile.yml`). Run `task` to list everything.

| Command | What it does |
| --- | --- |
| `task dev` | **Local dev:** Postgres (Docker) + Go server + MonoGame client on host |
| `task dev:server` | Backend only — Go server on host |
| `task dev:client` | Frontend only — MonoGame client (server must be running) |
| `task up` / `task stop` | Full stack in Docker only (no local client) |
| `task client:build` | Release compile only (CI / quick check) |
| `task client:publish` | Self-contained client zip in `dist/` |
| `task client:installer` | Windows setup wizard in `dist/` (needs Inno Setup 6) |
| `task check` | Server fmt + vet + test + client build |
| `task release -- v0.1.0` | Tag release (triggers GitHub Actions) |

## CI & releases

- **CI** — Go server checks + Docker image build + **C# client build**
- **Release** — cross-platform server binaries + Docker image to GHCR

Cut a release: `task release -- v0.1.0`
