"""Standardize and slice Deathborn icon sprite sheets (no AI).

Resizes incoming sheets to the game's atlas size with nearest-neighbor sampling,
optionally removes a chroma-key background, slices into named cells, and can
install the processed sheet into Content/Icons/.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEBUG_ROOT = ROOT / ".tile_debug" / "sheets"
ATLASES_PATH = Path(__file__).with_name("sprite_sheet_atlases.json")
DEFAULT_CHROMA_KEY = (0, 255, 0)


def rel_to_root(path: Path) -> str:
    try:
        return path.resolve().relative_to(ROOT.resolve()).as_posix()
    except ValueError:
        return path.resolve().as_posix()


def load_atlas(name: str) -> dict:
    data = json.loads(ATLASES_PATH.read_text(encoding="utf-8"))
    if name not in data:
        known = ", ".join(sorted(data))
        raise SystemExit(f"Unknown atlas '{name}'. Known atlases: {known}")
    return data[name]


def resolve_target(atlas: dict, target: str) -> tuple[int, int]:
    targets = atlas["targets"]
    if target not in targets:
        known = ", ".join(sorted(targets))
        raise SystemExit(f"Unknown target '{target}'. Known targets: {known}")
    spec = targets[target]
    return int(spec["width"]), int(spec["height"])


def parse_color(value: str) -> tuple[int, int, int]:
    value = value.strip()
    if value.startswith("#"):
        hex_value = value[1:]
        if len(hex_value) == 3:
            hex_value = "".join(ch * 2 for ch in hex_value)
        if len(hex_value) != 6 or re.search(r"[^0-9a-fA-F]", hex_value):
            raise SystemExit(f"Invalid hex color: {value}")
        return (
            int(hex_value[0:2], 16),
            int(hex_value[2:4], 16),
            int(hex_value[4:6], 16),
        )
    parts = [int(part.strip()) for part in value.split(",")]
    if len(parts) != 3:
        raise SystemExit(f"Invalid color '{value}'. Use #RRGGBB or R,G,B")
    return parts[0], parts[1], parts[2]


def standardize_sheet(image: Image.Image, width: int, height: int) -> Image.Image:
    if image.size == (width, height):
        return image.convert("RGBA") if image.mode != "RGBA" else image.copy()

    resized = image.resize((width, height), Image.Resampling.NEAREST)
    return resized.convert("RGBA")


def apply_chroma_key(
    image: Image.Image,
    key_rgb: tuple[int, int, int],
    tolerance: int,
) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    key_r, key_g, key_b = key_rgb

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            if (
                abs(r - key_r) <= tolerance
                and abs(g - key_g) <= tolerance
                and abs(b - key_b) <= tolerance
            ):
                pixels[x, y] = (r, g, b, 0)

    return rgba


def axis_bounds(total: int, parts: int, index: int) -> tuple[int, int]:
    """Distribute remainder pixels across cells (matches MonoGame atlas slicing)."""
    return total * index // parts, total * (index + 1) // parts


def slice_cells(
    sheet: Image.Image,
    columns: int,
    rows: int,
    cell_names: list[list[str | None]],
    output_dir: Path,
    *,
    skip_empty: bool,
    inset: int = 0,
) -> list[dict]:
    output_dir.mkdir(parents=True, exist_ok=True)

    exported: list[dict] = []
    for row in range(rows):
        for col in range(columns):
            name = cell_names[row][col]
            if name is None and skip_empty:
                continue

            x0, x1 = axis_bounds(sheet.width, columns, col)
            y0, y1 = axis_bounds(sheet.height, rows, row)
            if inset > 0:
                x0 = min(x0 + inset, x1 - 1)
                y0 = min(y0 + inset, y1 - 1)
                x1 = max(x1 - inset, x0 + 1)
                y1 = max(y1 - inset, y0 + 1)

            cell = sheet.crop((x0, y0, x1, y1))
            filename = f"{name}.png" if name else f"empty_r{row}_c{col}.png"
            out_path = output_dir / filename
            cell.save(out_path)
            exported.append(
                {
                    "name": name,
                    "row": row,
                    "col": col,
                    "file": rel_to_root(out_path),
                    "size": [cell.width, cell.height],
                }
            )
    return exported


def write_manifest(
    path: Path,
    *,
    atlas_name: str,
    source: Path,
    sheet_out: Path,
    target: str,
    sheet_size: tuple[int, int],
    cells: list[dict],
    chroma_key: str | None,
    tolerance: int | None,
) -> None:
    manifest = {
        "atlas": atlas_name,
        "source": str(source.resolve()),
        "standardized_sheet": str(sheet_out.resolve()),
        "target": target,
        "sheet_size": list(sheet_size),
        "chroma_key": chroma_key,
        "chroma_tolerance": tolerance,
        "cells": cells,
    }
    path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Standardize and slice a Deathborn icon sprite sheet.",
    )
    parser.add_argument("input", type=Path, help="Source sprite sheet PNG")
    parser.add_argument(
        "--atlas",
        required=True,
        choices=["ability", "cosmetic"],
        help="Atlas layout and cell names to use",
    )
    parser.add_argument(
        "--target",
        default="game",
        help="Output size preset from sprite_sheet_atlases.json (default: game)",
    )
    parser.add_argument(
        "--sheet-out",
        type=Path,
        help="Write processed full sheet here",
    )
    parser.add_argument(
        "--cells-out",
        type=Path,
        help="Directory for sliced cell PNGs",
    )
    parser.add_argument(
        "--install",
        action="store_true",
        help="Copy processed sheet to the atlas install_path in Content/Icons/",
    )
    parser.add_argument(
        "--skip-empty",
        action="store_true",
        help="Do not export unnamed/empty cells",
    )
    parser.add_argument(
        "--inset",
        type=int,
        default=0,
        help="Trim N pixels from each cell edge after slicing",
    )
    parser.add_argument(
        "--chroma-key",
        nargs="?",
        const="default",
        default=None,
        metavar="COLOR",
        help="Remove chroma-key background (#00FF00 by default). Use alone for green, "
        "or pass #FF00FF / R,G,B for a custom key color.",
    )
    parser.add_argument(
        "--tolerance",
        type=int,
        default=18,
        help="Per-channel tolerance for chroma-key removal (default: 18)",
    )
    parser.add_argument(
        "--manifest",
        type=Path,
        help="Write JSON manifest of exported cells",
    )
    return parser.parse_args()


def resolve_chroma_key(args: argparse.Namespace, atlas: dict) -> tuple[str, tuple[int, int, int]] | None:
    if args.chroma_key is None:
        return None

    if args.chroma_key == "default":
        raw = atlas.get("chroma_key", "#00FF00")
    else:
        raw = args.chroma_key

    rgb = parse_color(raw)
    return raw, rgb


def main() -> int:
    args = parse_args()
    if not args.input.is_file():
        raise SystemExit(f"Input file not found: {args.input}")

    atlas = load_atlas(args.atlas)
    width, height = resolve_target(atlas, args.target)
    chroma = resolve_chroma_key(args, atlas)

    stem = args.input.stem
    debug_base = DEBUG_ROOT / args.atlas / stem
    sheet_out = args.sheet_out or debug_base.with_name(f"{stem}_processed.png")
    cells_out = args.cells_out or debug_base.parent / f"{stem}_cells"
    manifest_out = args.manifest or cells_out / "manifest.json"

    with Image.open(args.input) as src:
        print(f"Input: {args.input} ({src.width}x{src.height}, {src.mode})")
        sheet = standardize_sheet(src, width, height)

    if chroma:
        key_label, key_rgb = chroma
        sheet = apply_chroma_key(sheet, key_rgb, max(0, args.tolerance))
        print(f"Chroma key removed: {key_label} (tolerance {args.tolerance})")

    sheet_out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(sheet_out)
    print(f"Processed sheet: {sheet_out} ({sheet.width}x{sheet.height}, RGBA)")

    cells = slice_cells(
        sheet,
        atlas["columns"],
        atlas["rows"],
        atlas["cells"],
        cells_out,
        skip_empty=args.skip_empty,
        inset=max(0, args.inset),
    )
    print(f"Sliced {len(cells)} cell(s) to {cells_out}")

    write_manifest(
        manifest_out,
        atlas_name=args.atlas,
        source=args.input,
        sheet_out=sheet_out,
        target=args.target,
        sheet_size=(sheet.width, sheet.height),
        cells=cells,
        chroma_key=chroma[0] if chroma else None,
        tolerance=args.tolerance if chroma else None,
    )
    print(f"Manifest: {manifest_out}")

    if args.install:
        install_path = ROOT / atlas["install_path"]
        install_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(sheet_out, install_path)
        print(f"Installed: {install_path}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
