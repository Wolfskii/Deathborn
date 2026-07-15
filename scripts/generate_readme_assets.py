#!/usr/bin/env python3
"""Build README animated GIF from Farm RPG player walk sprites."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
WALK = ROOT / "client" / "Deathborn.Client" / "Content" / "Characters" / "FarmRpg" / "layers" / "skin-1" / "walk.png"
WALK_FALLBACK = (
    ROOT
    / "client"
    / "Deathborn.Client"
    / "Content"
    / "Characters"
    / "Farm RPG - Tiny Asset Pack - (All in One)"
    / "Character"
    / "Character"
    / "PNG"
    / "2. Walk"
    / "Skins"
    / "1.png"
)
OUT = ROOT / "docs" / "assets" / "readme-player-run.gif"

FRAME_WIDTH = 32
FRAME_HEIGHT = 32
FRAMES_PER_DIRECTION = 6
RIGHT_DIRECTION = 2  # Down, up, right, left
BG = (12, 10, 8)
DISPLAY = 128


def resolve_walk_sheet() -> Path:
    if WALK.is_file():
        return WALK
    if WALK_FALLBACK.is_file():
        return WALK_FALLBACK
    raise SystemExit(f"Missing Farm RPG walk sheet: {WALK} (or fallback {WALK_FALLBACK})")


def main() -> int:
    sheet = Image.open(resolve_walk_sheet()).convert("RGBA")
    frames: list[Image.Image] = []
    base_col = RIGHT_DIRECTION * FRAMES_PER_DIRECTION
    for i in range(FRAMES_PER_DIRECTION):
        x = (base_col + i) * FRAME_WIDTH
        crop = sheet.crop((x, 0, x + FRAME_WIDTH, FRAME_HEIGHT))
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
