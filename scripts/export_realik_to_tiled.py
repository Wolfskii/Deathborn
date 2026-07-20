"""Export Realik overworld binaries to a painted Tiled map (Farm RPG tiles).

Reads collision bins and seeds the Ground layer with uniform plain grass / water.
You paint cliffs, shores, and palettes directly in Tiled — the game draws those
tiles at runtime (no elevation autotile).

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
    ensure_farmrpg_tiles,
    layout_tilesets,
    seed_gid_for_elevation,
    write_overworld_tmx,
    write_tsx,
)

ROOT = Path(__file__).resolve().parents[1]
TMX = OVERWORLD / OVERWORLD_TMX_NAME
MANIFEST = ROOT / "client" / "Deathborn.Client" / "Content" / "Maps" / "manifest.json"


def load_walkable_grid() -> tuple[int, int, list[list[bool]]]:
    tw, th, _, walkable = load_collision(COLLISION)
    return tw, th, walkable


def build_ground_gids(tw: int, th: int, walkable: list[list[bool]], layouts) -> list[list[int]]:
    """Flat terrain seed: water + one plain grass tile (no shore/plateau circles)."""
    water_gid = seed_gid_for_elevation(layouts, -1)
    grass_gid = seed_gid_for_elevation(layouts, 1)
    return [
        [grass_gid if walkable[ty][tx] else water_gid for tx in range(tw)]
        for ty in range(th)
    ]


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
    gids = build_ground_gids(tw, th, walkable, layouts)
    write_overworld_tmx(TMX, tw, th, gids, layouts, include_reference=args.reference)
    update_manifest()
    print(f"Exported Realik {tw}x{th} -> {TMX}")
    print("Layer format: base64+gzip (opens reliably in Tiled 1.11+).")
    if args.reference:
        print("Reference image layer included (shared/world/realik_reference.png).")
    print("Paint on the Ground layer. Restart the client after editing.")


if __name__ == "__main__":
    main()
