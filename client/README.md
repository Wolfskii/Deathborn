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

## Ship to friends (Windows)

### Quick start

```bash
# From repo root — builds publish output + installer
task client:installer
```

Send: `dist/Deathborn-<version>-win-x64-Setup.exe`

### Prerequisites (your machine only)

| Tool | Purpose |
| --- | --- |
| [.NET 8 SDK](https://dotnet.microsoft.com/download) | Build the client |
| [Task](https://taskfile.dev) | `task client:installer` |
| [Inno Setup 6](https://jrsoftware.org/isdl.php) | Compile the wizard |

```bash
winget install JRSoftware.InnoSetup
```

Friends need **none** of the above — the installer is self-contained.

### Publish vs installer

| Command | Output | Send to friends? |
| --- | --- | --- |
| `task client:publish` | `dist/Deathborn-<version>-win-x64.zip` | Yes — they must unzip the **whole** folder |
| `task client:installer` | `dist/Deathborn-<version>-win-x64-Setup.exe` | **Preferred** — single setup file |

`task client:installer` runs `client:publish` first, then packages the result into a Windows wizard (logo, running swordsman during install, shortcuts, uninstaller).

**Do not** send only `Deathborn.Client.exe` — the game needs DLLs, `.xnb` content, and world binaries alongside it.

### After install

Default install path: `C:\Program Files\Deathborn\`

Launch **Deathborn** from the desktop shortcut or Start Menu. Optional post-install checkbox launches the game immediately.

Uninstall via Windows **Settings → Apps → Deathborn**.

### Server URL

Resolution order ([`Net/ServerEndpoints.cs`](Deathborn.Client/Net/ServerEndpoints.cs)):

1. Environment variable `DEATHBORN_SERVER_URL`
2. Local `http://127.0.0.1:8080` if `/health` succeeds
3. Production `https://api.deathborn.wolfskii.dev`

For a custom server without rebuilding:

```bat
set DEATHBORN_SERVER_URL=https://your-api.example.com
Deathborn.Client.exe
```

Or set `DEATHBORN_SERVER_URL` as a Windows user environment variable.

### Manual build (without Task)

```bash
cd client
dotnet tool restore
dotnet publish Deathborn.Client/Deathborn.Client.csproj \
  -c Release -r win-x64 --self-contained true -o publish
```

Then from repo root:

```bash
bash installer/build-installer.sh <version>
```

### Installer files

```
installer/
  Deathborn.iss         Inno Setup script
  prepare_assets.py     Logo + run-frame bitmaps for the wizard
  build-installer.sh    Compile helper (used by Task)
  assets/               Generated BMPs (gitignored; rebuilt each run)
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
