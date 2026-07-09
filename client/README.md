# DEATHBORN Client (MonoGame)

The game client is written in **C#** on **.NET 8** using [MonoGame](https://monogame.net/) (DesktopGL).

Everything is code-driven — screens, UI widgets, rendering, networking, and game logic live in C# source files rather than a visual editor scene tree.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer
- Windows, Linux, or macOS (DesktopGL)

## Run (debug)

With the server running (`task up` from repo root):

```bash
cd client
dotnet tool restore   # first time only (MonoGame content builder)
dotnet run --project Deathborn.Client
```

Or from repo root:

```bash
task client
```

## Ship to friends

See the root [README shipping section](../README.md#shipping-the-client) for full detail. Quick map:

| OS | Build (your machine) | Send friends |
| --- | --- | --- |
| **This OS** | `task client:package` | — |
| Windows | `task client:installer` | `dist/*-win-x64-Setup.exe` |
| Linux | `task client:package:linux` | `dist/*-linux-x64.tar.gz` → `tar -xzf … && ./install.sh` |
| macOS | `task client:package:mac` *(on a Mac)* | `dist/*-osx-*.dmg` → drag app to Applications |

One **linux-x64** build covers Ubuntu, Fedora, Arch, Mint, etc. macOS needs **osx-arm64** (Apple Silicon) or **osx-x64** (Intel) — `task client:publish:mac` picks the right one for your Mac.

From Windows you can also run `task client:publish:linux` or `task client:package:linux` without a Linux VM.

### Server URL

Resolution order ([`Net/ServerEndpoints.cs`](Deathborn.Client/Net/ServerEndpoints.cs)):

1. Environment variable `DEATHBORN_SERVER_URL`
2. Local `http://127.0.0.1:8080` if `/health` succeeds
3. Production `https://api.deathborn.wolfskii.dev`

For a custom server without rebuilding:

```bash
# Linux / macOS
export DEATHBORN_SERVER_URL=https://your-api.example.com
deathborn   # or ./Deathborn.Client
```

```bat
REM Windows
set DEATHBORN_SERVER_URL=https://your-api.example.com
Deathborn.Client.exe
```

### Installer files

```
installer/
  Deathborn.iss           Windows Inno Setup
  build-installer.sh      Windows Setup.exe
  package-linux.sh        Linux tar.gz
  package-macos.sh        macOS .dmg
  linux/install.sh        per-user Linux install script
  macos/Info.plist        macOS app bundle metadata
  prepare_assets.py       Windows wizard art
```

## Project layout

```
Deathborn.Client/
  Program.cs              entry point
  DeathbornGame.cs        MonoGame Game class, content loading
  Config.cs               viewport and tuning constants
  Net/ServerEndpoints.cs  server URL resolution (local vs production)
  Net/GameClient.cs       HTTP auth + WebSocket (JSON protocol)
  Screens/                Login, CharacterCreate, World (screen manager)
  Game/                   players, interactables, hotbar (namespace: Gameplay)
  Ui/                     text fields, buttons (code-only UI)
  Rendering/              primitive drawing helpers
  SavedLogin.cs           optional "remember email and password" on login
  Content/                MonoGame content pipeline (sprite font)
```

## Remember me

On the login screen, check **Remember email and password** to store credentials locally
(`%LocalAppData%/Deathborn/saved_login.cfg`). They are filled in automatically next launch.
Uncheck and log in once to clear the saved data.

## Server URL

See [Ship to friends](#ship-to-friends-windows) above. For local development, start the server (`task up` or `task dev:server`) — the client auto-detects `127.0.0.1:8080`.
