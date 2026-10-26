# DEATHBORN — MVP Roadmap

> You are born to die. Only skill decides when.

This document tracks the path from the current build to the full MVP. Each
milestone is a self-contained, shippable slice. Status reflects the code on
`develop` as of 2026-10-26. Milestones marked **Partial** list what is still
missing.

## Status at a glance

| Milestone | Status |
| --- | --- |
| M0 Walking skeleton | ✅ Done |
| M1 Global chat | ✅ Done (no persistence, by design) |
| M2 World zones + safe zones | 🟡 Partial |
| M3 Gathering + usage-based skills | 🟡 Partial |
| M4 Real-time combat | 🟡 Largely built, needs balance and polish |
| M5 Permadeath | ✅ Done |
| M6 Full-loot drops + looting | 🟡 Partial |
| M7 Death history (legacy view) | ✅ Done |
| M8 Highscores / leaderboards | ✅ Done |
| M9 Google OAuth login | ⬜ Not started |

Built beyond the original plan: see [Built outside the original plan](#built-outside-the-original-plan).

## Guiding principles (do not violate)

- **Server-authoritative.** The client only sends input and renders state. The
  server alone decides position, damage, loot, XP, character state, and death.
- **Simplicity over complexity.** Monolithic Go server, modular internal
  packages, JSON over WebSocket, PostgreSQL for all permanent data.
- **Working over perfect.** Each milestone must run end-to-end before the next.
- Avoid microservices, premature optimization, and heavy abstraction.

## Architecture baseline

- **Client:** MonoGame 3.8 (**C#**, .NET 8, DesktopGL). `GameClient` owns HTTP
  auth + the world WebSocket. Screens live in `Screens/` (`LoginScreen`,
  `CharacterCreateScreen`, `WorldScreen`, and debug screens). UI and gameplay
  are code-only, with no scene editor.
- **Server:** Go monolith. Packages under `server/internal/`: `auth`, `clientupdate`,
  `config`, `db`, `game` (world, tick loop, and gameplay systems), `net`
  (WebSocket hub and handlers), `protocol`, `skills`, `worldmap`. Entry point:
  `server/cmd/deathborn`.
- **Wire protocol:** JSON envelope `{ "type": string, "data": {...} }`. Shared
  combat and protocol data lives in `shared/` (single source of truth; see
  `AGENTS.md`).
- **Tick:** 20 Hz authoritative simulation.
- **DB:** PostgreSQL. Migrations `0001`–`0012` run at startup.
  - `accounts`, `oauth_identities`, `characters`, `character_history`,
    `highscores` (the last three are schema only; see M7–M9).
  - Gameplay tables added since M0: skills, houses and farms, inventory and
    world item drops, friends, cosmetics, active buffs, vitals, and appearance.

---

## M0 — Walking Skeleton  ✅

- [x] Register / login with email + password (HTTP, returns JWT).
- [x] JWT-authenticated WebSocket connection (`/ws?token=`).
- [x] Create a character (one living character per account, DB-enforced).
- [x] Enter the world; velocity-based movement, integrated on the server.
- [x] See other players move in real time via tick snapshots.
- [x] Position persisted on disconnect; resume on reconnect.
- [x] Hotbar, interactables (E / click), dev quick-login (Debug builds).

---

## M1 — Global chat  ✅

- [x] `chat_message` (C→S) with server-side validation and broadcast (S→C).
- [x] `chat_typing` indicator.
- [x] Chat log and typing indicators in `WorldScreen`.
- [x] Per-client rate limiting on chat (per message type, `net/ratelimit.go`).
- No persistence, as planned.

---

## M2 — World zones + safe zones  🟡

Server-side zones (`server/internal/game/zones.go`) gate PvP and spawning.

- [x] Starter Town, Northhaven, Westmere, Eastwatch, Southport are safe zones.
- [x] PvP is allowed outside safe zones (`PvPAllowed`), and bosses and NPCs respect safe areas.
- [x] House interiors and safe-zone exclusion rules.
- [ ] Market, Bank, and Forest / Mine / Lake / Wilderness as named zones. Only
      safe zones are named today. Everything else is an unnamed PvP area.
- [ ] Zone name and safe / danger flag in the HUD (`welcome` and `snapshot` do not carry a zone yet).
- [ ] Zones are config-driven. They are currently hard-coded in `zones.go`.

---

## M3 — Resource gathering + usage-based skills  🟡

- [x] Usage-based skill XP (`GrantSkillXP`), levels, and `total_xp` persisted on `characters`.
- [x] Skill-based activities: farming, fishing, and cooking (see [Built outside the original plan](#built-outside-the-original-plan)).
- [x] Inventory panel, hotbar, and skills display on the client.
- [ ] Woodcutting and mining resource nodes per zone.
- [ ] Generic `gather_start` / `gather_stop` protocol. Farming and fishing use their own actions instead.
- [ ] Smithing, Attack, Strength, Defense, and Hitpoints skill XP tied to actions. Combat XP
      path to confirm.

---

## M4 — Real-time combat  🟡

- [x] Data-driven abilities (`server/internal/game/abilities/abilities.json`), shared with the client.
- [x] Server-side hit range, damage, HP, and death.
- [x] PvP gated by zones.
- [x] Boss encounters with telegraphed abilities (`bosses.go`).
- [x] Boss health bar and buff bar overlays on the client.
- [ ] Combat balance pass across abilities, bosses, and HP.
- [ ] Aggro and PvE mob behaviour beyond bosses (confirm before marking done).
- [ ] Attack / Strength / Defense / Hitpoints XP from combat (confirm).

---

## M5 — Permadeath  🟡

Death writes a permanent record in one transaction (`db.RecordDeath`).

- [x] On death (HP <= 0) the character is marked dead, removed from the world, and `you_died` is sent.
- [x] Death is broadcast to other clients (`player_death`).
- [x] The account can create a new character after death (`need_character` flow).
- [x] A `character_history` row is written on death (reason, killer, epitaph, skills, XP, wealth).
- [x] `survival_seconds` (wall-clock since creation, so it includes offline time) and `pvp_kills` are recorded.
- [x] Death is handled once even if the victim disconnects at the same moment (`World.ClaimDeath`).

---

## M6 — Full-loot drops + looting  🟡

- [x] On death, the inventory drops as persisted world items at the death position
      (`dropInventoryOnDeath`, migration `0004`).
- [x] Pickup flow (`pickup_item`) with a server-side range check.
- [x] Ground items expire (`PruneExpiredDrops`).
- [ ] Equipped gear drops on death. Only inventory is confirmed.
- [ ] Verify full-loot behaviour end to end, including PvP kills.

---

## M7 — Character death history  ⬜

Turn the M5 data into a legacy view.

- [x] `GET /history` (JWT) returns the account's past lives.
- [x] Server query over `character_history` (`db.ListCharacterHistory`).
- [x] Client `LegacyScreen` ("Past Lives") listing past lives, opened from the character screen.
- [x] The death panel shows the cause of death and a life summary.

---

## M8 — Highscores / leaderboards  ⬜

- [x] Upsert the `highscores` row on death (max of each stat).
- [x] `GET /highscores?category=` (top N per category) and `GET /highscores/me`.
- [x] Client `LeaderboardScreen`, opened from the login and character screens.

---

## M9 — Google OAuth login  ⬜

- [ ] OAuth redirect and callback endpoints.
- [ ] Find-or-create an account, link `oauth_identities`, issue the same JWT.
- [ ] Provider-agnostic interface in `internal/auth`, with Google first.
- [ ] "Sign in with Google" on `LoginScreen` (system browser flow).
- Schema is ready: `accounts.password_hash` is nullable, and `oauth_identities` exists.

---

## Built outside the original plan

These systems shipped without a roadmap entry. Listed so future planning starts from the real state.

- **Farming and homesteads** (Stardew-style): house placement, fences, and farm plots (migration `0011`).
- **Fishing** and **cooking**.
- **Friends:** `friend_add`, `friend_respond`, `friend_remove` (migration `0005`).
- **Cosmetics** (migration `0006`) and **active buffs** (migration `0007`).
- **Terrain and rendering:** elevation, water, foliage occlusion, and Tiled map import and export (dungeons and the Swarovia mainland).
- **Player appearance and modular character layers** (migration `0009`).
- **Live-reload dev server** (Air) and **client self-update** (`clientupdate`).
- **Import inbox** for art and content assets (`import/`, see `AGENTS.md`).

---

## Cross-cutting (fold in opportunistically)

- **Interest management:** when player counts grow, snapshot only nearby
  entities instead of the whole world (per-zone or grid-based culling).
- **Input sequence + reconciliation:** optional client-side prediction if
  movement feels laggy; keep server authoritative.
- **Rate limiting / anti-abuse:** Done for WebSocket messages (per type,
  `server/internal/net/ratelimit.go`), login and register (10 per minute per IP,
  `server/internal/auth/limit.go`), and a 64 KB socket read limit. Still open for
  gather actions (Phase 6 of the plan).
- **Observability:** structured logging and basic metrics (tick duration,
  connected clients). Partial: performance logging exists on the client.
- **Migrations tooling:** swap the startup runner for `golang-migrate` if schema
  churn grows. Churn is high at 11 migrations, so this is worth revisiting.
