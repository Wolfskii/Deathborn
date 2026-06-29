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

## Project layout

```
Deathborn.Client/
  Program.cs              entry point
  DeathbornGame.cs        MonoGame Game class, content loading
  Config.cs               server URL, viewport, tuning constants
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

Default: `http://127.0.0.1:8080` — change in `Config.cs`.
