# Deathborn — Agent Guide

## Tiny Swords terrain (elevated land & hills)

Authoritative reference: [`.tile_debug/tinyswords_guide.json`](.tile_debug/tinyswords_guide.json)

### Quick facts

- **Tile size:** 64×64 px art on a 64×64 grid (world collision grid uses 16 px cells; renderer scales sprites to fit).
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
