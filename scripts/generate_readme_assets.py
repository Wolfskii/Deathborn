#!/usr/bin/env python3
"""Build README animated GIFs from in-game sprite sheets."""

from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "docs" / "assets"
RUN = ROOT / "client" / "Deathborn.Client" / "Content" / "Characters" / "Swordsman" / "Run.png"
BANNER = (
    ROOT
    / "client"
    / "Deathborn.Client"
    / "Content"
    / "Images"
    / "Logos"
    / "Banners"
    / "Banner V2 - animated.png"
)
OUT_SWORDSMAN = ASSETS / "readme-swordsman-run.gif"
OUT_BANNER = ASSETS / "readme-banner.gif"

FRAME_START_X = 16
FRAME_STRIDE = 64
FRAME_COUNT = 8
RUN_ROW_Y = 144  # Right-facing row (matches SwordsmanSpriteSheet DirectionRowTops).
BG = (12, 10, 8)
BANNER_BG = (0, 0, 0)
DISPLAY = 128
BANNER_DISPLAY_W = 480
BANNER_DISPLAY_H = 320
BANNER_COLUMNS = 4
BANNER_ROWS = 4
BANNER_FRAME_COUNT = BANNER_COLUMNS * BANNER_ROWS


def _save_gif(frames: list[Image.Image], out: Path, duration_ms: int) -> None:
    out.parent.mkdir(parents=True, exist_ok=True)
    frames[0].save(
        out,
        save_all=True,
        append_images=frames[1:],
        duration=duration_ms,
        loop=0,
        optimize=True,
    )
    print(f"Wrote {out}")


def build_swordsman_gif() -> None:
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

    _save_gif(frames, OUT_SWORDSMAN, duration_ms=120)


def build_banner_gif() -> None:
    if not BANNER.is_file():
        raise SystemExit(f"Missing banner sheet: {BANNER}")

    sheet = Image.open(BANNER).convert("RGBA")
    frame_w = sheet.width // BANNER_COLUMNS
    frame_h = sheet.height // BANNER_ROWS
    frames: list[Image.Image] = []
    for i in range(BANNER_FRAME_COUNT):
        col = i % BANNER_COLUMNS
        row = i // BANNER_COLUMNS
        x = col * frame_w
        y = row * frame_h
        crop = sheet.crop((x, y, x + frame_w, y + frame_h))
        crop = crop.resize((BANNER_DISPLAY_W, BANNER_DISPLAY_H), Image.Resampling.LANCZOS)
        flat = Image.new("RGB", crop.size, BANNER_BG)
        flat.paste(crop, mask=crop.split()[3])
        frames.append(flat)

    _save_gif(frames, OUT_BANNER, duration_ms=150)


def main() -> int:
    build_swordsman_gif()
    build_banner_gif()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
