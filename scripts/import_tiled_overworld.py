"""Import hand-painted Swarovia mainland overworld from Tiled into world binaries.

Reads Content/Maps/overworld/swarovia_mainland.tmx and derives walkability + elevation
from layer classes:

  ground_N  — land at height N (N=1 base land; N=2 one tier higher, …)
  water_N   — water at the same height as ground_N (elevated lakes, etc.)

Layer *names* are free (Sea, Land, …); the class attribute is authoritative.

Usage:
  python scripts/import_tiled_overworld.py
  python scripts/import_tiled_overworld.py --tmx path/to/swarovia_mainland.tmx
"""

from __future__ import annotations

import argparse
from pathlib import Path

from swarovia_mainland_world_io import (
    COLLISION,
    ELEVATION,
    load_elevation,
    sync_server_copies,
    write_collision,
    write_elevation,
)
from tiled_overworld_io import (
    gameplay_from_classified_layers,
    gameplay_from_gids,
    load_gid_properties,
    merge_layer_gids,
    parse_classified_tile_layers,
    parse_tile_layer,
    parse_tile_layers,
    OVERWORLD,
    OVERWORLD_TMX_NAME,
)

DEFAULT_TMX = OVERWORLD / OVERWORLD_TMX_NAME


def main() -> None:
    parser = argparse.ArgumentParser(description="Import Swarovia mainland overworld from Tiled TMX")
    parser.add_argument("--tmx", type=Path, default=DEFAULT_TMX, help="Path to swarovia_mainland.tmx")
    args = parser.parse_args()

    if not args.tmx.exists():
        raise SystemExit(f"TMX not found: {args.tmx}\nRun: python scripts/export_realik_to_tiled.py")

    gid_props = load_gid_properties(args.tmx)
    tw, th, classified = parse_classified_tile_layers(args.tmx)

    if classified:
        walkable, elev = gameplay_from_classified_layers(classified, gid_props)
        mode = (
            "classes "
            + ", ".join(f"{layer.name}:{layer.class_name}" for layer in classified)
        )
    else:
        # Legacy fallback: Ground/Land + Water/Sea by name, tile elevation props.
        try:
            tw, th, layer_map = parse_tile_layers(args.tmx)
            if "Ground" in layer_map or "Land" in layer_map:
                gids = merge_layer_gids(layer_map)
            else:
                gids = parse_tile_layer(args.tmx)[2]
        except ValueError:
            tw, th, gids = parse_tile_layer(args.tmx)
        walkable, elev = gameplay_from_gids(gids, gid_props)
        mode = "legacy tile properties (no ground_N / water_N classes)"

    write_collision(COLLISION, tw, th, walkable)
    ramps = [[0] * tw for _ in range(th)]
    write_elevation(ELEVATION, tw, th, elev, ramps)
    sync_server_copies()

    hist: dict[int, int] = {}
    _, _, elev_out, _ = load_elevation(ELEVATION)
    for row in elev_out:
        for v in row:
            hist[v] = hist.get(v, 0) + 1

    walk_count = sum(1 for row in walkable for cell in row if cell)
    print(f"Imported {tw}x{th} from {args.tmx}")
    print(f"  mode: {mode}")
    print(f"  -> {COLLISION}")
    print(f"  -> {ELEVATION}")
    print(f"  walkable tiles: {walk_count}")
    print(f"  elevation histogram: {dict(sorted(hist.items()))}")
    print("Restart the client to see painted tiles.")


if __name__ == "__main__":
    main()
