#!/usr/bin/env python3
"""Build README animated GIF from in-game swordsman run sprites."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
RUN = ROOT / "client" / "Deathborn.Client" / "Content" / "Characters" / "Swordsman" / "Run.png"
OUT = ROOT / "docs" / "assets" / "readme-swordsman-run.gif"

FRAME_START_X = 16
FRAME_STRIDE = 64
FRAME_COUNT = 8
RUN_ROW_Y = 144  # Right-facing row (matches SwordsmanSpriteSheet DirectionRowTops).
BG = (12, 10, 8)
DISPLAY = 128


def main() -> int:
    if not RUN.is_file():
        raise SystemExit(f"Missing run sheet: {RUN}")

    sheet = Image.open(RUN).convert("RGBA")
    frames: list[Image.Image] = []
    for i in range(FRAME_COUNT):
        x = FRAME_START_X + i * FRAME_STRIDE
        crop = sheet.crop((x, RUN_ROW_Y, x + FRAME_STRIDE, RUN_ROW_Y + FRAME_STRIDE))
        crop = crop.resize((DISPLAY, DISPLAY), Image.Resampling.NEAREST)
        flat = Image.new("RGB", crop.size, BG)
        flat.paste(crop, mask=crop.split()[3])
        frames.append(flat)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    frames[0].save(
        OUT,
        save_all=True,
        append_images=frames[1:],
        duration=120,
        loop=0,
        optimize=True,
    )
    print(f"Wrote {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
