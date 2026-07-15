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

## Tiny Swords terrain (elevated land & hills)

Authoritative reference: [`.tile_debug/tinyswords_guide.json`](.tile_debug/tinyswords_guide.json)

### Quick facts

- **Tile size:** 64×64 px art on a 64×64 grid (world collision grid uses **32 px** cells; renderer scales sprites to fit).
- **Elevation levels:** `-1` water, `0` sea shoreline (flat ground / foam), `1` base land, `2–4` plateaus (darker green palettes).
- **Tilemap files:** `Tilemap_color1.png` … `Tilemap_color5.png` — same 9×6 layout, different palette per elevation.
- **Sheet layout:** columns 0–3 = flat ground + stairs; column 4 = divider; columns 5–8 = elevated grass + cliffs.

### Render layer order (bottom → top)

1. Water background (`Water Background color.png`)
2. Water foam — **only elevation-0** shoreline tiles (`Water Foam.png`, animated 16×192×192 frames)
3. For each elevation `0 … max`:
   - Shadows under tier (elevation ≥ 1, offset one tile south)
   - Grass tops (autotiled)
   - South-facing cliff bases on the row below lips
4. Props / foliage (Y-sorted)

### Autotile mask (4-neighbour)

`mask = (N_same << 3) | (E_same << 2) | (S_same << 1) | W_same`

- **Same** = neighbor elevation ≥ cell elevation (or another road cell for roads).
- Lookup table: `flatGroundLookup` in the JSON guide.

### Cliffs

- Only **south** neighbours may step down (by 1 to land, or to water).
- Lip pieces: elevated grass ids 7–9, 15 (or 10–12, 16 when north is water).
- Cliff base row at `(x, y+1)`: ids 17–20 on land below, 21–24 on water (includes foam).

### World data files

| File | Purpose |
|------|---------|
| `shared/world/realik_collision.bin` | Land/water walkability (`REAL` v1) |
| `shared/world/realik_elevation.bin` | Per-cell elevation (`ELEV` v1) |

Regenerate:

```bash
python scripts/generate_world_collision.py
python scripts/generate_world_elevation.py
```

### Client code map

| File | Role |
|------|------|
| `Rendering/TinySwordsTerrain.cs` | Multi-elevation draw (grass, cliffs, shadows) |
| `Rendering/WaterTiles.cs` | Water fill + shoreline foam |
| `Rendering/TerrainLandTiles.cs` | Flat fallback when no elevation data |
| `Game/WorldMap.cs` | Loads collision + elevation, orchestrates draw passes |
| `Rendering/WorldFoliage.cs` | Props on elevation ≥ 1 land |

### Map design (default generator)

- Base land = elevation **1**; left-sea shore ring = elevation **0**.
- **4–6 plateaus** at elevation 2 with nested 3/4 cores (see `scripts/generate_world_elevation.py`).
- No N/E/W autopad promotion — plateaus sit flush on lower land at north/east/west.
- Stairs/ramps: not yet placed by generator (future work); renderer supports cliff pieces.

### When editing terrain

1. Read `.tile_debug/tinyswords_guide.json` for piece ids and placement rules.
2. Keep south drops to **one** elevation step for land-to-land.
3. Do not draw foam under elevated cliffs (they use cliff-to-water pieces).
4. After changing elevation data, rebuild client content and restart the game.
