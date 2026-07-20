# Deathborn — Agent Guide

## Player character sprites (Farm RPG)

**Active player pipeline** — modular Farm RPG layers only. Legacy Swordsman V1/V2 systems have been removed.

Authoritative manifest: [`client/Deathborn.Client/Content/Characters/FarmRpg/manifest.json`](client/Deathborn.Client/Content/Characters/FarmRpg/manifest.json)

### Quick facts

- **Body type id:** `farm_rpg` (`CharacterAnimationCatalog.FarmRpg`)
- **Source pack:** `Content/Characters/Farm RPG - Tiny Asset Pack - (All in One)/` (vendor art — not loaded at runtime)
- **Runtime layers:** `Content/Characters/FarmRpg/layers/<layer-id>/<clip>.png`
- **Cell size:** 32×32 px; 4 directions × N frames in one horizontal strip per layer
- **Direction order:** down, up, right, left (`FacingDirection` / `FarmRpgAnimationSpecs.DirectionIndex`)
- **Facing:** diagonals snap to the dominant axis (4 directions only)

### Clips

| Clip file | Game clip |
|-----------|-----------|
| `idle` | Idle, Cast |
| `walk` | Walk |
| `run` | Run, Roll |
| `attack` | Attack |
| `hurt` | Hurt |
| `death` | Death |

### Install / refresh layers

```bash
python scripts/install_farm_rpg_player.py
```

Copies modular layers from the vendor pack into `FarmRpg/layers/` and registers PNGs in `Content.mgcb`.

### Client code map

| File | Role |
|------|------|
| `Rendering/Characters/FarmRpgCharacterSprites.cs` | Loads skin/eyes/equipment layer sheets |
| `Rendering/Characters/FarmRpgAnimationSpecs.cs` | Frame counts, durations, direction mapping |
| `Rendering/Characters/CharacterRenderer.cs` | Layer compositor |
| `Rendering/Characters/SpriteAssembler.cs` | Resolves layer stack per clip |
| `Rendering/Characters/CharacterLayerCatalog.cs` | Equipment item id → Farm layer mapping |
| `Rendering/Facing.cs` | 4-way cardinal facing (down, up, right, left) |

**Draw order (bottom → top):** Body (skin) → Eyes → Chest → Hair → Weapon.

**Appearance:** `CharacterAppearance` selects skin/eye/hair layer ids; `CharacterEquipment` selects outfit and weapon ids (`farm-*` prefix in catalog).

### When adding a new layer

1. Add the layer to `scripts/install_farm_rpg_player.py` `LAYERS` dict (or copy manually from vendor pack).
2. Run `python scripts/install_farm_rpg_player.py`.
3. Add item id to `CharacterLayerCatalog.cs` and `FarmRpg/manifest.json` if new equipment slot.

Missing layer PNGs are skipped gracefully — partial fallback until art is installed.

### World NPCs (Tiny RPG)

**Active NPC pipeline** — horizontal-strip sprites from the Tiny RPG Character pack.

NPC sprites live under `Content/Characters/Rpg/` and load via `TinyRpgCharacterSprites.cs` — separate from the player Farm RPG system.

---

## Farm RPG terrain (elevated land & water)

Authoritative reference: [`.tile_debug/farmrpg_guide.json`](.tile_debug/farmrpg_guide.json)

### Quick facts

- **Tile size:** 16×16 px art (world collision grid uses **32 px** cells; renderer scales 2×).
- **Vendor pack:** `Content/Characters/Farm RPG - Tiny Asset Pack - (All in One)/Tileset/`
- **Runtime tiles:** `Content/Tiles/FarmRpg/` (installed via `scripts/install_farm_rpg_terrain.py`)
- **Elevation levels:** `-1` water, `0` sea shoreline, `1` base land, `2–4` plateaus (spring → summer → fall → deep forest palettes).
- **Grass autotile origin:** column 5, row 1; plain fill cell (9, 2). Do **not** use (4, 14) — that is tilled soil.
- **Shoreline:** grass↔water transitions from `grass_water.png` land origin (4, 8).
- **Water fill:** solid `water_fill.png` (16×16 cyan tile).

### Render layer order (bottom → top)

1. Water autotile + animated open-water frames
2. For each elevation `0 … max`:
   - Shadows under tier (elevation ≥ 1, offset one tile south)
   - Grass tops (autotiled; shoreline uses grass-water sheet on elev 0)
   - South-facing cliff lips + bases (cols 16–19, rows 17–18 on grass sheet)
3. Props / foliage (Y-sorted)

### Autotile mask (4-neighbour)

`mask = (N_same << 3) | (E_same << 2) | (S_same << 1) | W_same`

- **Same** = neighbor elevation ≥ cell elevation (plateaus) or adjacent land/water for shoreline tiles.

### World data files

| File | Purpose |
|------|---------|
| `shared/world/swarovia_mainland_collision.bin` | Land/water walkability (`REAL` v1) |
| `shared/world/swarovia_mainland_elevation.bin` | Per-cell elevation (`ELEV` v2) |

Regenerate:

```bash
python scripts/generate_world_collision.py
python scripts/generate_world_elevation.py
```

### Client code map

| File | Role |
|------|------|
| `Rendering/FarmRpgTerrain.cs` | Multi-elevation draw (grass, cliffs, shadows) |
| `Rendering/WaterTiles.cs` | Water fill + animated open water |
| `Rendering/TerrainLandTiles.cs` | Flat fallback when no elevation data |
| `Game/WorldMap.cs` | Loads collision + elevation, orchestrates draw passes |
| `Rendering/WorldFoliage.cs` | Farm RPG trees, bushes, rocks on elevation ≥ 1 land |

### Foliage (Farm RPG)

Runtime copies under `Content/Decorations/FarmRpg/` — pine/maple trees, deep-forest bushes, seasonal stones, water props.

Install / refresh:

```bash
python scripts/install_farm_rpg_terrain.py
```

### When editing terrain

1. Read `.tile_debug/farmrpg_guide.json` for autotile origins and cliff rows.
2. Keep south drops to **one** elevation step for land-to-land.
3. After changing vendor art or elevation data, rerun the install script and rebuild content.

---

## Performance pitfalls (FPS / movement)

Dev log: `client/Deathborn.Client/bin/Debug/net8.0/logs/fps-dips.log` (phase marks: `poll`, `move`, `clouds`, …).

### Never full-scan the 1024×1024 elevation / ramp grid on the move hot path

**Bug (fixed):** `RampTreadAtWorld`, `RampEngagedAtWorld`, and `RampTreadCellId` (client `Game/WorldMap.cs`; server `server/internal/worldmap/elevation.go`) used to iterate **every** ramp/elevation cell. Walking called them several times per `ResolveMove` → ~55–75ms `move=` dips and ~15 FPS while idle frames were ~3ms.

**Rule:** Stair/ramp queries must be **O(1)** or a tiny neighborhood (±2 tiles around the feet). A tread belongs only to the cell underfoot or the ramp one tile south — never `for` over `TileWidth × TileHeight`.

Keep client and server ramp logic in sync when changing engagement rules.

### Other move-path gotchas already hit

| Mistake | Symptom | Fix location |
|---------|---------|--------------|
| `File.GetLastWriteTimeUtc` every `SwaroviaMainland` access | Update stalls | `WorldMap` — cache after load; use `ReloadFromDisk()` for manual reload |
| Rebuild full tiled overworld RT every water anim tick (~0.2s) | Draw/update spikes | `TiledOverworldRenderer` — cache Ground; draw Water live |
| Sync `AutoFlush` / IDE-watched FPS log on game thread | Hitch storms from logging itself | `DevPerfLog` — async queue writer |

### Ellipse vs tiles / props (corners)

Player collider is an **upward ellipse** (rx=12, ry=16), not a circle. F12: red = land/water tile edges; green = player ellipse + prop stems.

**Do not** use 5-point axial samples for terrain walkability — diagonal body can sit past a convex red corner while center ± tips stay on land. Test **ellipse vs each blocked tile AABB** in the ellipse’s tile neighborhood (`EllipseClearOfBlockedTiles` / server `ellipseClearOfBlockedTiles`).

**Do not** use Euclidean clamp for `EllipseOverlapsRect` when `rx ≠ ry` — transform into unit-circle space first (`÷ rx/ry`, then closest-point). Wrong tests cause tree-stem corner clips and inconsistent push-out.

**ResolveMove:** try **both** axis slide orders (X→Y and Y→X), pick farther result; if still jammed, binary-clamp along the intended path. Keep client (`WorldMap` / `WorldFoliage`) and server (`map.go` / `foliage.go`) in sync.

**Thin foliage stems (trees):** Never accept a move or axis slide from endpoint clearance alone. A thin green stem AABB is shorter than one frame of run speed — `from` north of the trunk and `to` south both look free → player teleports past the tree. Always **sweep the path** (~2px steps) / binary-clamp to first contact (`PathClear` / `pathClear`). Soft depenetrate must be distance-capped and must **not** shift the destination by the same delta.

---

## Tiled maps (dungeons + Swarovia mainland authoring)

Authoritative manifest: [`client/Deathborn.Client/Content/Maps/manifest.json`](client/Deathborn.Client/Content/Maps/manifest.json)

### Swarovia mainland in Tiled (painted tiles — WYSIWYG)

The overworld is drawn from `Maps/overworld/swarovia_mainland.tmx` at runtime. What you paint in Tiled is what appears in-game — **no elevation autotile**.

**Prerequisite:** Farm RPG terrain tiles must be installed:

```bash
python scripts/install_farm_rpg_terrain.py
```

**One-time export (seeds Ground layer from current elevation bins):**

```bash
python scripts/export_realik_to_tiled.py
# or: task world:export-tiled
```

Opens: `client/Deathborn.Client/Content/Maps/overworld/swarovia_mainland.tmx` (1024×1024, 32 px cells). Use **Water** + **Ground** tile layers. Layer data is stored as **base64+gzip** (Tiled’s native format for large maps).

Export seeds **uniform plain grass + water** from collision (no elevation shore rings or town icon circles). Pass `--reference` only if you want the art overlay with town icons for tracing.

Paint on the **Ground** layer using the Farm RPG grass, water, cliff, and shoreline tilesets. The export only seeds plain grass/water fills — you hand-paint cliffs, shores, and palette changes.

**Tile properties (optional, for walkability on import):**

| Property | Type | Meaning |
|----------|------|---------|
| `walkable` | bool | Whether players can stand here |
| `elevation` | int | Gameplay elevation (-1 water, 0 shore, 1+ land) |

**After editing in Tiled:**

```bash
python scripts/import_tiled_overworld.py
# or: task world:import-tiled
```

This writes `shared/world/swarovia_mainland_collision.bin` + `swarovia_mainland_elevation.bin` from tile properties and syncs server copies. **Restart the client** to reload the map.

### Dungeons / instanced rooms

- **Editor:** [Tiled Map Editor](https://www.mapeditor.org/) (`.tmx` + `.tsx` + tileset `.png`)
- **Runtime:** MonoGame.Extended 6.x loads `.tmx` at runtime via `TiledTmxParser` (no MGCB rebuild when editing maps)
- **Tile size:** 32×32 px world cells for new dungeon maps (match `Config.WorldTileSize`)
- **Starter map:** `Content/Maps/dungeons/starter_room.tmx`

### Layer conventions

| Layer / object | Purpose |
|----------------|---------|
| `Ground`, `Walls`, … | Tile layers (draw order = Tiled layer order) |
| `Collision` | Object layer — rectangles block movement |
| `Objects` | Point/object markers — name `Spawn` or class `spawn` for player spawn |

Optional map property: `collision_radius` (float, default 12).

### Add or update a map

1. Create/edit `.tmx` under `Content/Maps/` in Tiled (keep relative paths to `.tsx` and `.png`).
2. Register in the manifest:
   ```bash
   python scripts/register_tiled_maps.py
   # or one map:
   python scripts/register_tiled_maps.py --map Maps/dungeons/my_room.tmx --id my_room --title "My Room"
   ```
3. Restart the client (maps are loaded from `Content/Maps/` on disk).
4. In-game dev preview: press **F11** in the world to toggle the active Tiled map centered on your character.

### File layout

Copy `.tmx`, `.tsx`, and referenced tileset `.png` files under `Content/Maps/` (keep relative paths). The client copies `.tmx`/`.tsx` to the output folder automatically; tileset PNGs already in `Content/Tiles/` are resolved via relative paths in the `.tsx`.

### Optional: MGCB content pipeline

`MonoGame.Extended.Content.Pipeline` is included for future baked maps. It requires aligning MonoGame to 3.8.4+ and adding the pipeline DLL reference in MGCB. **Runtime loading is the default workflow today.**

### Client code map

| File | Role |
|------|------|
| `Maps/TiledMapCatalog.cs` | Loads maps from manifest via runtime `TiledTmxParser` |
| `Maps/TiledMapInstance.cs` | Renderer wrapper + collision helpers |
| `Maps/TiledMapMetadata.cs` | Parses `Collision` / `Objects` layers |
| `Maps/TiledOverworldRenderer.cs` | Draws Swarovia mainland from Tiled (replaces autotile) |
| `Maps/TiledMapPreview.cs` | F11 dev overlay |
| `Game/WorldBackgroundRenderer.cs` | Overworld background — Tiled map or autotile fallback |
| `scripts/export_realik_to_tiled.py` | Export overworld bins → painted `swarovia_mainland.tmx` |
| `scripts/import_tiled_overworld.py` | Import Ground layer → walkability + elevation bins |
| `scripts/register_tiled_maps.py` | Adds dungeon map entries to manifest |

### Overworld vs Tiled

| | Swarovia mainland | Tiled instanced maps |
|--|-------------------|----------------------|
| Authoring | **Tiled** `Maps/overworld/swarovia_mainland.tmx` (export/import) or legacy `realik_reference.png` | Tiled editor |
| Data | `swarovia_mainland_*.bin` (walkability) + `swarovia_mainland.tmx` (visuals) | `.tmx` only |
| Rendering | **Painted tiles** from `swarovia_mainland.tmx` (autotile fallback if map missing) | Painted tiles |
| Use for | Continent terrain tiers | Dungeons, house layouts, arenas |
