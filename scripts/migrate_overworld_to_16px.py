"""Expand the authored overworld from 32px cells to 16px cells.

The existing map covers a fixed physical area. Each old 32px cell therefore
becomes a 2x2 block of 16px cells. GIDs are repeated rather than regenerated,
so all Tiled painting is preserved while the map and gameplay grid double in
each dimension.

Run once after changing the shared tile-size constants:

    python scripts/migrate_overworld_to_16px.py
    python scripts/import_tiled_overworld.py
"""

from __future__ import annotations

import xml.etree.ElementTree as ET
from pathlib import Path

from tiled_overworld_io import (
    OVERWORLD,
    OVERWORLD_TILESETS,
    OVERWORLD_TMX_NAME,
    layout_tilesets,
    parse_classified_tile_layers,
    scale_gids_nearest,
    write_overworld_tmx_layers,
)


ROOT = Path(__file__).resolve().parents[1]
TMX = OVERWORLD / OVERWORLD_TMX_NAME
SCALE = 2


def main() -> None:
    if not TMX.exists():
        raise SystemExit(f"TMX not found: {TMX}")

    root = ET.parse(TMX).getroot()
    old_width = int(root.get("width", "0"))
    old_height = int(root.get("height", "0"))
    if old_width <= 0 or old_height <= 0:
        raise SystemExit(f"Invalid map dimensions in {TMX}")

    classified = parse_classified_tile_layers(TMX)[2]
    if not classified:
        raise SystemExit("No classified ground_N / water_N tile layers found")

    layers = {
        layer.name: scale_gids_nearest(layer.gids, SCALE)
        for layer in classified
    }
    layer_classes = {layer.name: layer.class_name for layer in classified}
    layer_locked = {
        layer.get("name", ""): layer.get("locked") == "1"
        for layer in root.findall("layer")
    }

    layouts = layout_tilesets(OVERWORLD_TILESETS)
    write_overworld_tmx_layers(
        TMX,
        old_width * SCALE,
        old_height * SCALE,
        layers,
        layouts,
        layer_order=tuple(layer.name for layer in classified),
        layer_classes=layer_classes,
        layer_locked=layer_locked,
        include_reference=(OVERWORLD / "realik_reference.png").exists(),
    )

    print(
        f"Migrated {old_width}x{old_height} -> "
        f"{old_width * SCALE}x{old_height * SCALE}: {TMX}"
    )
    print("Preserved classified layers by repeating each cell into a 2x2 block.")


if __name__ == "__main__":
    main()
