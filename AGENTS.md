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
| `shared/world/realik_collision.bin` | Land/water walkability (`REAL` v1) |
| `shared/world/realik_elevation.bin` | Per-cell elevation (`ELEV` v2) |

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

## Tiled maps (dungeons, interiors, hand-authored rooms)

**Use Tiled for instanced maps** — not the Realik overworld (that stays PNG → binary → autotile).

Authoritative manifest: [`client/Deathborn.Client/Content/Maps/manifest.json`](client/Deathborn.Client/Content/Maps/manifest.json)

### Quick facts

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
| `Maps/TiledMapPreview.cs` | F11 dev overlay |
| `scripts/register_tiled_maps.py` | Adds map entries to `Content/Maps/manifest.json` |

### Overworld vs Tiled

| | Realik overworld | Tiled instanced maps |
|--|------------------|----------------------|
| Authoring | Paint `realik_reference.png` | Tiled editor |
| Data | `realik_*.bin` | `.tmx` → content pipeline |
| Rendering | Elevation autotile | Painted tile GIDs |
| Use for | Continent, coastlines | Dungeons, houses, arenas |
