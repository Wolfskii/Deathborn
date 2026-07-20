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
    parser.add_argument("--ground-layer", default="Ground")
    parser.add_argument("--water-layer", default="Water")
    args = parser.parse_args()

    if not args.tmx.exists():
        raise SystemExit(f"TMX not found: {args.tmx}")

    src_tw, src_th, layers = parse_tile_layers(args.tmx)
    if args.ground_layer not in layers:
        raise SystemExit(f"Layer '{args.ground_layer}' not found in {args.tmx}")

    ground = layers[args.ground_layer]
    bbox = content_bbox(ground)
    if bbox is None:
        raise SystemExit("Ground layer is empty — nothing to center.")

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
    if args.water_layer in layers:
        for row in layers[args.water_layer]:
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
        {args.water_layer: new_water, args.ground_layer: new_ground},
        layouts,
        layer_order=(args.water_layer, args.ground_layer),
    )

    print(f"Layout {args.tmx.name}: {src_tw}x{src_th} -> {out_tw}x{out_th}")
    print(f"  land crop {max_x - min_x + 1}x{max_y - min_y + 1} @ ({min_x},{min_y})")
    print(f"  scaled {scaled_w}x{scaled_h}, placed at ({offset_x},{offset_y})")
    print("Run: py -3 scripts/import_tiled_overworld.py")


if __name__ == "__main__":
    main()
