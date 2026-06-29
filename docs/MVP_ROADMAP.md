# DEATHBORN — MVP Roadmap

> You are born to die. Only skill decides when.

This document is the ordered plan from the current **Walking Skeleton** to the
full MVP. Each milestone is a self-contained, shippable slice and lists the
server packages, protocol messages, DB tables, and client code it touches so a
future plan can pick it up cleanly.

## Guiding principles (do not violate)

- **Server-authoritative.** The client only sends input and renders state. The
  server alone decides position, damage, loot, XP, character state, and death.
- **Simplicity over complexity.** Monolithic Go server, modular internal
  packages, JSON over WebSocket, PostgreSQL for all permanent data.
- **Working over perfect.** Each milestone must run end-to-end before the next.
- Avoid microservices, premature optimization, and heavy abstraction.

## Architecture baseline (established in M0)

- **Client:** MonoGame 3.8 (**C#**, .NET 8, DesktopGL). `GameClient` owns HTTP
  auth + the world WebSocket. Screens: `LoginScreen`, `CharacterCreateScreen`,
  `WorldScreen` (code-only UI, no scene editor).
- **Server:** Go monolith. Packages: `config`, `db`, `auth`, `net` (WS hub +
  JSON protocol), `game` (world + fixed tick loop). Entry: `cmd/deathborn`.
- **Wire protocol:** JSON envelope `{ "type": string, "data": {...} }`.
- **Tick:** 20 Hz authoritative simulation; full snapshot broadcast each tick.
- **DB:** `accounts`, `oauth_identities`, `characters`, `character_history`,
  `highscores` (the last three exist now; most columns unused until later).

---

## M0 — Walking Skeleton  ✅ (current)

Thinnest end-to-end vertical slice.

- [x] Register / login with email + password (HTTP, returns JWT).
- [x] JWT-authenticated WebSocket connection (`/ws?token=`).
- [x] Create a character (one living character per account, DB-enforced).
- [x] Enter the world; free velocity-based movement (server-integrated).
- [x] See other players move in real time via tick snapshots.
- [x] Position persisted on disconnect; resume on reconnect.
- [x] Hotbar, interactables (E / click), dev quick-login (Debug builds).

**Touches:** all baseline server packages; client `Net/`, `Screens/`, `Game/`.
**Protocol:** `input`, `create_character`, `interact` (C→S); `welcome`,
`need_character`, `snapshot`, `error` (S→C).

---

## M1 — Global chat

First social system; exercises broadcast in both directions.

- Client (C#): chat input + scrolling log in `WorldScreen`.
- Protocol: `chat_send {text}` (C→S); `chat {from, text}` (S→C broadcast).
- Server: `net` validates/trims/length-limits text, rate-limits per client,
  broadcasts via hub. No persistence required for MVP.
- DB: none.

---

## M2 — World zones + safe zones

Give the world structure and the first rule that makes PvP meaningful.

- Define static zones: Starter Town (safe), Market (safe), Bank (safe), Forest,
  Mine, Lake, Wilderness (full PvP). Data-driven (a zones config: rect/AABB +
  `safe` flag + name).
- Server: `game` gains a `zones` concept; `PlayerState`/snapshot may include the
  current zone for the client HUD. Spawn point = Starter Town.
- Client (C#): `WorldScreen` — zone backgrounds/labels; HUD shows zone + safe/danger.
- DB: none (zones are static config); later, persist player zone if needed.
- Protocol: extend `snapshot`/`welcome` with `zone`.

---

## M3 — Resource gathering + usage-based skills

The progression backbone.

- Resource nodes (trees, rocks, fishing spots) placed per zone (static config).
- Protocol: `gather_start {nodeId}` / `gather_stop` (C→S); `inventory {items}`,
  `skills {levels, xp}`, `gather_result` (S→C).
- Server: `game` adds `ResourceNode`, gather timers on the tick loop, an
  `inventory` per character, and a `skills` system (XP curve, level-up on
  action). New package `internal/skills` (data-driven skill table:
  Woodcutting, Mining, Fishing, Cooking, Smithing, Attack, Strength, Defense,
  Hitpoints).
- DB: add `inventories` (or JSONB column on `characters`) and persist
  `skills` JSONB + `total_xp` on `characters`.
- Client (C#): inventory + skills panels in `WorldScreen` or overlay screens.

---

## M4 — Real-time combat

The core risk.

- Combat skills consume the M3 skill system (Attack/Strength/Defense/Hitpoints).
- Protocol: `attack {targetId}` (C→S); `combat_event {attacker, target, damage,
  hp}` and `hp` updates folded into `snapshot` (S→C).
- Server: `game`/new `internal/combat` resolves hits on the tick (range check,
  hit/damage formula, HP, aggro). PvP allowed only outside safe zones (uses M2
  zone flags). PvE mobs optional here or split to M4.5.
- DB: persist HP on `characters`.
- Client (C#): health bars, hit feedback in `WorldScreen`.

---

## M5 — Permadeath

The identity of the game.

- On death (HP <= 0): server marks the character dead, removes it from the
  world, and writes a `character_history` snapshot.
- Protocol: `you_died {reason, summary}` (S→C); on next connect the account has
  no living character so the existing `need_character` flow runs.
- Server: `game` death handler; reuses `db` (`character_history` insert +
  set `characters.alive = false`). The partial unique index already allows a new
  living character afterward.
- DB: write `character_history` (name, total_xp, skills, survival_seconds,
  pvp_kills, wealth, died_at, death_reason).
- Client (C#): `CharacterCreateScreen` flow after death message.

---

## M6 — Full-loot drops + looting

Makes death hurt and PvP rewarding.

- On death, all equipped + inventory items drop to the world at the death
  position as a lootable pile.
- Protocol: `loot_pickup {itemId}` (C→S); ground items included in `snapshot`
  (or a `ground_items` message) (S→C).
- Server: `game` ground-item entities; pickup validation (range, capacity).
- DB: ground items can be in-memory (acceptable loss on restart) or persisted in
  a `ground_items` table for durability — decide at implementation time.
- Client (C#): render ground loot; pickup via interact system.

---

## M7 — Character death history surfacing

Turn the M5 data into a legacy view.

- Protocol/HTTP: `GET /history` (JWT) returns the account's past lives.
- Server: `db` query over `character_history`; small HTTP handler in `auth`/new
  `internal/account` package.
- Client (C#): new `LegacyScreen` listing past lives.
- DB: read-only over `character_history` (written in M5).

---

## M8 — Highscores / leaderboards

Long-term retention.

- On death, update the account's `highscores` row (max of each stat).
- HTTP: `GET /highscores` (top N per category) and `GET /highscores/me`.
- Server: `db` upsert into `highscores` on death; query handlers.
- Client (C#): `LeaderboardScreen`.
- DB: `highscores` table (already created).

---

## M9 — Google OAuth login

Account growth; schema is already OAuth-ready.

- HTTP: OAuth redirect/callback endpoints; on callback, find-or-create an
  account and link an `oauth_identities` row, then issue the same JWT used today.
- Server: new `internal/auth` provider code (Google first; provider-agnostic
  interface so Discord/Steam slot in later). `accounts.password_hash` is already
  nullable for OAuth-only accounts.
- Client (C#): "Sign in with Google" on `LoginScreen` (system browser flow).
- DB: `oauth_identities` table (already created).

---

## Cross-cutting (fold in opportunistically)

- **Interest management:** when player counts grow, snapshot only nearby
  entities instead of the whole world (per-zone or grid-based culling).
- **Input sequence + reconciliation:** optional client-side prediction if
  movement feels laggy; keep server authoritative.
- **Rate limiting / anti-abuse:** on chat, auth, and gather/attack actions.
- **Observability:** structured logging and basic metrics (tick duration,
  connected clients).
- **Migrations tooling:** swap the startup runner for `golang-migrate` if schema
  churn grows.
