"""Center and resize authored Realik overworld layers in Tiled.

Usage:
  py -3 scripts/layout_realik_tmx.py
  py -3 scripts/layout_realik_tmx.py --size 1024 --scale 2
"""

from __future__ import annotations

import argparse
from pathlib import Path

from tiled_overworld_io import (
    OVERWORLD,
    OVERWORLD_TILESETS,
    OVERWORLD_TMX_NAME,
    blank_layer,
    blit_layer,
    content_bbox,
    crop_gids,
    ensure_farmrpg_tiles,
    layout_tilesets,
    parse_classified_tile_layers,
    parse_tile_layers,
    scale_gids_nearest,
    write_overworld_tmx_layers,
    write_tsx,
)

ROOT = Path(__file__).resolve().parents[1]
TMX = OVERWORLD / OVERWORLD_TMX_NAME


def main() -> None:
    parser = argparse.ArgumentParser(description="Center land and resize Realik TMX")
    parser.add_argument("--tmx", type=Path, default=TMX)
    parser.add_argument("--size", type=int, default=1024, help="Output map width/height in tiles")
    parser.add_argument("--scale", type=int, default=2, help="Nearest-neighbor upscale for land content")
    parser.add_argument("--ground-layer", default="", help="Override ground layer name (default: first ground_*)")
    parser.add_argument("--water-layer", default="", help="Override water layer name (default: first water_*)")
    args = parser.parse_args()

    if not args.tmx.exists():
        raise SystemExit(f"TMX not found: {args.tmx}")

    src_tw, src_th, classified = parse_classified_tile_layers(args.tmx)
    ground_name = args.ground_layer
    water_name = args.water_layer
    ground_class = "ground_1"
    water_class = "water_1"

    if classified:
        grounds = [layer for layer in classified if layer.kind == "ground"]
        waters = [layer for layer in classified if layer.kind == "water"]
        if not ground_name:
            if not grounds:
                raise SystemExit("No ground_N layer found — set --ground-layer")
            ground_name = grounds[0].name
            ground_class = grounds[0].class_name
        else:
            match = next((layer for layer in grounds if layer.name == ground_name), None)
            if match:
                ground_class = match.class_name
        if not water_name:
            if waters:
                water_name = waters[0].name
                water_class = waters[0].class_name
            else:
                water_name = "Sea"
        else:
            match = next((layer for layer in waters if layer.name == water_name), None)
            if match:
                water_class = match.class_name
        layer_map = {layer.name: layer.gids for layer in classified}
    else:
        src_tw, src_th, layer_map = parse_tile_layers(args.tmx)
        ground_name = ground_name or ("Land" if "Land" in layer_map else "Ground")
        water_name = water_name or ("Sea" if "Sea" in layer_map else "Water")

    if ground_name not in layer_map:
        raise SystemExit(f"Layer '{ground_name}' not found in {args.tmx}")

    ground = layer_map[ground_name]
    bbox = content_bbox(ground)
    if bbox is None:
        raise SystemExit(f"Layer '{ground_name}' is empty — nothing to center.")

    min_x, min_y, max_x, max_y = bbox
    cropped = crop_gids(ground, min_x, min_y, max_x, max_y)
    scaled = scale_gids_nearest(cropped, args.scale)
    scaled_h = len(scaled)
    scaled_w = len(scaled[0])

    out_tw = out_th = args.size
    if scaled_w > out_tw or scaled_h > out_th:
        raise SystemExit(
            f"Scaled land ({scaled_w}x{scaled_h}) does not fit in {out_tw}x{out_th}. "
            f"Increase --size or lower --scale."
        )

    offset_x = (out_tw - scaled_w) // 2
    offset_y = (out_th - scaled_h) // 2

    water_gid = 0
    if water_name in layer_map:
        for row in layer_map[water_name]:
            for gid in row:
                if gid:
                    water_gid = gid
                    break
            if water_gid:
                break
    if water_gid == 0:
        layouts = layout_tilesets(OVERWORLD_TILESETS)
        water_gid = layouts[0].gid(0, 0)

    new_water = blank_layer(out_tw, out_th, water_gid)
    new_ground = blank_layer(out_tw, out_th, 0)
    blit_layer(new_ground, scaled, offset_x, offset_y)

    ensure_farmrpg_tiles()
    layouts = layout_tilesets(OVERWORLD_TILESETS)
    OVERWORLD.mkdir(parents=True, exist_ok=True)
    for layout in layouts:
        write_tsx(OVERWORLD, layout)

    write_overworld_tmx_layers(
        args.tmx,
        out_tw,
        out_th,
        {water_name: new_water, ground_name: new_ground},
        layouts,
        layer_order=(water_name, ground_name),
        layer_classes={water_name: water_class, ground_name: ground_class},
        layer_locked={water_name: True},
    )

    print(f"Layout {args.tmx.name}: {src_tw}x{src_th} -> {out_tw}x{out_th}")
    print(f"  land crop {max_x - min_x + 1}x{max_y - min_y + 1} @ ({min_x},{min_y})")
    print(f"  scaled {scaled_w}x{scaled_h}, placed at ({offset_x},{offset_y})")
    print(f"  layers: {water_name} ({water_class}), {ground_name} ({ground_class})")
    print("Run: py -3 scripts/import_tiled_overworld.py")


if __name__ == "__main__":
    main()
