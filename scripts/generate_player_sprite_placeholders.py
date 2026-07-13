"""Generate placeholder player sprite halves and merge into runtime sheets.

Reads prompts/sprites/Player/manifest.json.

Usage:
  python scripts/generate_player_sprite_placeholders.py          # all animations
  python scripts/generate_player_sprite_placeholders.py walk    # one animation
  python scripts/generate_player_sprite_placeholders.py --merge-only walk
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "prompts/sprites/Player/manifest.json"
CHROMA = (0, 255, 0, 255)
ROW_COLORS = [
    (220, 80, 80, 255),
    (230, 150, 60, 255),
    (80, 140, 220, 255),
    (150, 90, 200, 255),
]


def load_manifest() -> dict:
    return json.loads(MANIFEST.read_text(encoding="utf-8"))


def half_row_labels(manifest: dict, half: str) -> list[str]:
    if half == "cardinals":
        return manifest["cardinalsRowOrder"]
    if half == "diagonals":
        return manifest["diagonalsRowOrder"]
    raise ValueError(half)


def draw_half_sheet(
    *,
    animation: str,
    half: str,
    rows: int,
    cols: int,
    cell: int,
    row_labels: list[str],
) -> Image.Image:
    w, h = cols * cell, rows * cell
    im = Image.new("RGBA", (w, h), CHROMA)
    draw = ImageDraw.Draw(im)
    try:
        font = ImageFont.truetype("arial.ttf", max(10, cell // 6))
    except OSError:
        font = ImageFont.load_default()

    for row in range(rows):
        color = ROW_COLORS[row % len(ROW_COLORS)]
        label = row_labels[row]
        for col in range(cols):
            x0, y0 = col * cell, row * cell
            inset = 6
            shade = max(0, min(255, color[0] - col * 8))
            fill = (shade, color[1], color[2], 255)
            draw.rectangle(
                (x0 + inset, y0 + inset, x0 + cell - inset, y0 + cell - inset),
                fill=fill,
                outline=(40, 40, 40, 255),
                width=2,
            )
            text = f"{animation}\n{half}\n{label}\nF{col + 1}"
            draw.multiline_text(
                (x0 + cell // 2, y0 + cell // 2),
                text,
                fill=(255, 255, 255, 255),
                font=font,
                anchor="mm",
                align="center",
            )
    return im


def merge_halves(
    manifest: dict,
    cardinals: Image.Image,
    diagonals: Image.Image,
) -> Image.Image:
    cell = manifest["cellSizePx"]
    frames = cardinals.width // cell
    if cardinals.width != diagonals.width or cardinals.height != diagonals.height:
        raise ValueError("cardinals and diagonals must share cell size and column count")
    if cardinals.height != 4 * cell or diagonals.height != 4 * cell:
        raise ValueError("expected 4 rows per half sheet")

    merged = Image.new("RGBA", (frames * cell, 8 * cell), CHROMA)
    for merged_row, spec in enumerate(manifest["mergedRowOrder"]):
        half = spec["half"]
        src_row = spec["row"]
        src = cardinals if half == "cardinals" else diagonals
        y0 = src_row * cell
        y1 = y0 + cell
        strip = src.crop((0, y0, src.width, y1))
        merged.paste(strip, (0, merged_row * cell))
    return merged


def write_base_body(path: Path, cell: int) -> None:
    im = Image.new("RGBA", (cell * 2, cell * 2), CHROMA)
    draw = ImageDraw.Draw(im)
    draw.rectangle((cell // 2, cell // 2, cell * 3 // 2, cell * 3 // 2), fill=(190, 150, 110, 255), outline=(40, 40, 40, 255), width=2)
    try:
        font = ImageFont.truetype("arial.ttf", 12)
    except OSError:
        font = ImageFont.load_default()
    draw.text((cell, cell), "base-body", fill=(255, 255, 255, 255), font=font, anchor="mm")
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path)


def paths_for(manifest: dict, animation: str) -> tuple[Path, Path, Path, Path]:
    content = ROOT / manifest["contentDir"]
    source = ROOT / manifest["sourceDir"]
    anim = manifest["animations"][animation]
    cardinals = source / anim["source"]["cardinals"]
    diagonals = source / anim["source"]["diagonals"]
    merged = content / anim["merged"]
    return cardinals, diagonals, merged, source


def generate_animation(manifest: dict, animation: str, *, merge_only: bool) -> None:
    anim = manifest["animations"][animation]
    frames = anim["framesPerDirection"]
    cell = manifest["cellSizePx"]
    cardinals_path, diagonals_path, merged_path, source_root = paths_for(manifest, animation)

    if not merge_only:
        for half in manifest["halves"]:
            out = cardinals_path if half == "cardinals" else diagonals_path
            labels = half_row_labels(manifest, half)
            sheet = draw_half_sheet(
                animation=animation,
                half=half,
                rows=anim["halfRows"],
                cols=frames,
                cell=cell,
                row_labels=labels,
            )
            out.parent.mkdir(parents=True, exist_ok=True)
            sheet.save(out)
            print(f"wrote {out.relative_to(ROOT)}")

    cardinals = Image.open(cardinals_path).convert("RGBA")
    diagonals = Image.open(diagonals_path).convert("RGBA")
    merged = merge_halves(manifest, cardinals, diagonals)
    merged_path.parent.mkdir(parents=True, exist_ok=True)
    merged.save(merged_path)
    print(f"wrote {merged_path.relative_to(ROOT)}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("animations", nargs="*", help="animation ids from manifest (default: all)")
    parser.add_argument("--merge-only", action="store_true", help="merge existing _source halves only")
    args = parser.parse_args()

    manifest = load_manifest()
    names = args.animations or list(manifest["animations"].keys())

    for name in names:
        if name not in manifest["animations"]:
            raise SystemExit(f"unknown animation: {name}")
        generate_animation(manifest, name, merge_only=args.merge_only)

    if not args.merge_only and not args.animations:
        base_path = ROOT / manifest["sourceDir"] / manifest["baseBody"]["source"]
        write_base_body(base_path, manifest["cellSizePx"])
        print(f"wrote {base_path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
