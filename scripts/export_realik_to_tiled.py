"""Export Realik overworld binaries to a painted Tiled map (Farm RPG tiles).

Reads collision bins and seeds Sea (water_1) + Land (ground_1) layers.
You paint cliffs, shores, and higher tiers (ground_2 / water_2, …) in Tiled —
the game draws those tiles at runtime (no elevation autotile).

Usage:
  py -3 scripts/export_realik_to_tiled.py
  py -3 scripts/export_realik_to_tiled.py --reference   # include art overlay (town icons)
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from swarovia_mainland_world_io import COLLISION, load_collision
from tiled_overworld_io import (
    OVERWORLD,
    OVERWORLD_TILESETS,
    OVERWORLD_TMX_NAME,
    blank_layer,
    ensure_farmrpg_tiles,
    layout_tilesets,
    seed_gid_for_elevation,
    write_overworld_tmx_layers,
    write_tsx,
)

ROOT = Path(__file__).resolve().parents[1]
TMX = OVERWORLD / OVERWORLD_TMX_NAME
MANIFEST = ROOT / "client" / "Deathborn.Client" / "Content" / "Maps" / "manifest.json"


def load_walkable_grid() -> tuple[int, int, list[list[bool]]]:
    tw, th, _, walkable = load_collision(COLLISION)
    return tw, th, walkable


def build_seed_layers(
    tw: int, th: int, walkable: list[list[bool]], layouts
) -> tuple[list[list[int]], list[list[int]]]:
    """Sea filled with water; Land gets plain grass on walkable cells only."""
    water_gid = seed_gid_for_elevation(layouts, -1)
    grass_gid = seed_gid_for_elevation(layouts, 1)
    sea = blank_layer(tw, th, water_gid)
    land = [
        [grass_gid if walkable[ty][tx] else 0 for tx in range(tw)]
        for ty in range(th)
    ]
    return sea, land


def update_manifest() -> None:
    entry = {
        "id": "swarovia_mainland",
        "contentPath": "Maps/overworld/swarovia_mainland",
        "title": "Swarovia Mainland",
        "tags": ["overworld", "authoring"],
    }
    if MANIFEST.exists():
        data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    else:
        data = {"maps": []}
    maps: list[dict] = data.setdefault("maps", [])
    maps[:] = [m for m in maps if m.get("id") != "swarovia_mainland"]
    maps.insert(0, entry)
    MANIFEST.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Export overworld bins to swarovia_mainland.tmx")
    parser.add_argument(
        "--reference",
        action="store_true",
        help="Include locked Reference imagelayer (realik_reference.png with town icons)",
    )
    args = parser.parse_args()

    ensure_farmrpg_tiles()
    layouts = layout_tilesets(OVERWORLD_TILESETS)
    OVERWORLD.mkdir(parents=True, exist_ok=True)
    for layout in layouts:
        write_tsx(OVERWORLD, layout)

    tw, th, walkable = load_walkable_grid()
    sea, land = build_seed_layers(tw, th, walkable, layouts)
    write_overworld_tmx_layers(
        TMX,
        tw,
        th,
        {"Sea": sea, "Land": land},
        layouts,
        layer_order=("Sea", "Land"),
        layer_classes={"Sea": "water_1", "Land": "ground_1"},
        layer_locked={"Sea": True},
        include_reference=args.reference,
    )
    update_manifest()
    print(f"Exported Realik {tw}x{th} -> {TMX}")
    print("Layers: Sea (class water_1) + Land (class ground_1); base64+gzip.")
    if args.reference:
        print("Reference image layer included (shared/world/realik_reference.png).")
    print("Paint Land; add ground_2 / water_2 layers for higher tiers. Then import.")


if __name__ == "__main__":
    main()
