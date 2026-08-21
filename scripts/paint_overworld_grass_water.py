"""Replace plain overworld land with the authored grass/water terrain tiles.

The Land layer already contains the authoritative land mask. This script uses
that mask to choose a grass_water tile whose Wang edges match each land cell's
8-neighbour coastline pattern. Existing manually painted terrain variants are
therefore normalized to the same result as the rest of the continent.

Usage:

    python scripts/paint_overworld_grass_water.py
    python scripts/import_tiled_overworld.py
"""

from __future__ import annotations

import collections
import xml.etree.ElementTree as ET
from pathlib import Path

from tiled_overworld_io import (
    OVERWORLD,
    OVERWORLD_TILESETS,
    OVERWORLD_TMX_NAME,
    layout_tilesets,
    parse_tile_layers,
    write_overworld_tmx_layers,
)


ROOT = Path(__file__).resolve().parents[1]
TMX = OVERWORLD / OVERWORLD_TMX_NAME
GRASS_WATER_TSX = OVERWORLD / "farmrpg_grass_water.tsx"
GRASS_WATER_PNG = "grass_water.png"
NEIGHBOUR_OFFSETS = (
    (0, -1),
    (1, -1),
    (1, 0),
    (1, 1),
    (0, 1),
    (-1, 1),
    (-1, 0),
    (-1, -1),
)
CARDINAL_INDICES = (0, 2, 4, 6)


def pattern_bits(values: tuple[int, ...]) -> int:
    return sum(value << index for index, value in enumerate(values))


def load_wang_tiles() -> dict[int, list[int]]:
    root = ET.parse(GRASS_WATER_TSX).getroot()
    by_pattern: dict[int, list[int]] = collections.defaultdict(list)
    for element in root.findall(".//wangtile"):
        values = tuple(int(value) for value in element.get("wangid", "").split(","))
        if len(values) == 8:
            by_pattern[pattern_bits(values)].append(int(element.get("tileid", "0")))
    if not by_pattern:
        raise SystemExit(f"No Wang tiles found in {GRASS_WATER_TSX}")
    return dict(by_pattern)


def tileset_first_gid() -> int:
    root = ET.parse(TMX).getroot()
    for element in root.findall("tileset"):
        if Path(element.get("source", "")).name == GRASS_WATER_TSX.name:
            return int(element.get("firstgid", "0"))
    raise SystemExit(f"{GRASS_WATER_TSX.name} is not referenced by {TMX}")


def wang_distance(
    wanted: int,
    candidate: int,
) -> tuple[int, int]:
    """Prefer matching cardinal edges, then matching corners."""
    cardinal_misses = sum(
        ((wanted >> i) & 1) != ((candidate >> i) & 1)
        for i in CARDINAL_INDICES
    )
    corner_misses = sum(
        ((wanted >> i) & 1) != ((candidate >> i) & 1)
        for i in range(8)
        if i not in CARDINAL_INDICES
    )
    return cardinal_misses, corner_misses


def choose_tile(
    wanted: int,
    patterns: dict[int, list[int]],
) -> int:
    exact = patterns.get(wanted)
    if exact:
        return exact[0]

    ranked: list[tuple[tuple[int, int], int]] = []
    for candidate, tile_ids in patterns.items():
        distance = wang_distance(wanted, candidate)
        for tile_id in tile_ids:
            ranked.append((distance, tile_id))
    best_distance = min(distance for distance, _ in ranked)
    best_ids = [tile_id for distance, tile_id in ranked if distance == best_distance]
    return min(best_ids)


def main() -> None:
    if not TMX.exists():
        raise SystemExit(f"TMX not found: {TMX}")

    root = ET.parse(TMX).getroot()
    tw, th, layers = parse_tile_layers(TMX)
    land_name = "Land" if "Land" in layers else "Ground"
    if land_name not in layers:
        raise SystemExit("No Land or Ground tile layer found")

    land = layers[land_name]
    land_mask = [[gid != 0 for gid in row] for row in land]
    patterns = load_wang_tiles()
    first_gid = tileset_first_gid()
    tile_for_pattern = {
        wanted: choose_tile(wanted, patterns)
        for wanted in range(1 << len(NEIGHBOUR_OFFSETS))
    }

    full_land_gid = first_gid + tile_for_pattern[(1 << len(NEIGHBOUR_OFFSETS)) - 1]
    converted = [
        [full_land_gid if land_mask[y][x] else 0 for x in range(tw)]
        for y in range(th)
    ]
    chosen_counts: collections.Counter[int] = collections.Counter()
    for y in range(th):
        for x in range(tw):
            if not land_mask[y][x]:
                continue
            if (
                y > 0
                and y + 1 < th
                and x > 0
                and x + 1 < tw
                and all(
                    land_mask[ny][nx]
                    for ny in range(y - 1, y + 2)
                    for nx in range(x - 1, x + 2)
                )
            ):
                chosen_counts[full_land_gid - first_gid] += 1
                continue
            wanted = 0
            for index, (dx, dy) in enumerate(NEIGHBOUR_OFFSETS):
                if (
                    0 <= x + dx < tw
                    and 0 <= y + dy < th
                    and land_mask[y + dy][x + dx]
                ):
                    wanted |= 1 << index
            local_id = tile_for_pattern[wanted]
            converted[y][x] = first_gid + local_id
            chosen_counts[local_id] += 1

    layers[land_name] = converted
    layer_classes = {
        element.get("name", ""): element.get("class", "")
        for element in root.findall("layer")
        if element.get("class")
    }
    layer_locked = {
        element.get("name", ""): element.get("locked") == "1"
        for element in root.findall("layer")
    }
    write_overworld_tmx_layers(
        TMX,
        tw,
        th,
        layers,
        layout_tilesets(OVERWORLD_TILESETS),
        layer_order=tuple(layers),
        layer_classes=layer_classes,
        layer_locked=layer_locked,
        include_reference=(OVERWORLD / "realik_reference.png").exists(),
    )

    print(f"Painted {sum(land_mask[y][x] for y in range(th) for x in range(tw))} land cells")
    print(
        "Chosen grass_water local IDs:",
        dict(sorted(chosen_counts.items())),
    )


if __name__ == "__main__":
    main()
