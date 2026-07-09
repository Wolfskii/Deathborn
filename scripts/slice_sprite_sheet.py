"""Standardize and slice Deathborn icon sprite sheets (no AI).

Resizes incoming sheets to the game's atlas size with nearest-neighbor sampling,
optionally installs the standardized sheet into Content/Icons/, and exports
individual cell PNGs named from scripts/sprite_sheet_atlases.json.
"""

from __future__ import annotations

import argparse
import json
import shutil
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DEBUG_ROOT = ROOT / ".tile_debug" / "sheets"
ATLASES_PATH = Path(__file__).with_name("sprite_sheet_atlases.json")


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


def standardize_sheet(
    image: Image.Image,
    width: int,
    height: int,
) -> Image.Image:
    if image.size == (width, height):
        return image.convert("RGBA") if image.mode != "RGBA" else image.copy()

    resized = image.resize((width, height), Image.Resampling.NEAREST)
    if resized.mode != "RGBA":
        return resized.convert("RGBA")
    return resized


def slice_cells(
    sheet: Image.Image,
    columns: int,
    rows: int,
    cell_names: list[list[str | None]],
    output_dir: Path,
    *,
    skip_empty: bool,
) -> list[dict]:
    cell_w = sheet.width // columns
    cell_h = sheet.height // rows
    output_dir.mkdir(parents=True, exist_ok=True)

    exported: list[dict] = []
    for row in range(rows):
        for col in range(columns):
            name = cell_names[row][col]
            if name is None and skip_empty:
                continue

            box = (col * cell_w, row * cell_h, (col + 1) * cell_w, (row + 1) * cell_h)
            cell = sheet.crop(box)
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
) -> None:
    manifest = {
        "atlas": atlas_name,
        "source": str(source.resolve()),
        "standardized_sheet": str(sheet_out.resolve()),
        "target": target,
        "sheet_size": list(sheet_size),
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
        help="Write standardized full sheet here (default: <input_stem>_standardized.png)",
    )
    parser.add_argument(
        "--cells-out",
        type=Path,
        help="Directory for sliced cell PNGs (default: <input_stem>_cells/)",
    )
    parser.add_argument(
        "--install",
        action="store_true",
        help="Copy standardized sheet to the atlas install_path in Content/Icons/",
    )
    parser.add_argument(
        "--skip-empty",
        action="store_true",
        help="Do not export unnamed/empty cells",
    )
    parser.add_argument(
        "--manifest",
        type=Path,
        help="Write JSON manifest of exported cells (default: alongside cells dir)",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if not args.input.is_file():
        raise SystemExit(f"Input file not found: {args.input}")

    atlas = load_atlas(args.atlas)
    width, height = resolve_target(atlas, args.target)

    stem = args.input.stem
    debug_base = DEBUG_ROOT / args.atlas / stem
    sheet_out = args.sheet_out or debug_base.with_name(f"{stem}_standardized.png")
    cells_out = args.cells_out or debug_base.parent / f"{stem}_cells"
    manifest_out = args.manifest or cells_out / "manifest.json"

    with Image.open(args.input) as src:
        print(f"Input: {args.input} ({src.width}x{src.height}, {src.mode})")
        sheet = standardize_sheet(src, width, height)

    sheet_out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(sheet_out)
    print(f"Standardized sheet: {sheet_out} ({sheet.width}x{sheet.height})")

    cells = slice_cells(
        sheet,
        atlas["columns"],
        atlas["rows"],
        atlas["cells"],
        cells_out,
        skip_empty=args.skip_empty,
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
