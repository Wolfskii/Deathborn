#!/usr/bin/env python3
"""Build Inno Setup wizard bitmaps from Deathborn client art."""

from __future__ import annotations

import struct
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
CLIENT = ROOT / "client" / "Deathborn.Client"
OUT = Path(__file__).resolve().parent / "assets"

LOGO = CLIENT / "Content" / "Images" / "Logos" / "logo_v2.png"
BANNER = CLIENT / "Content" / "Images" / "Logos" / "Banners" / "Banner V2.png"
RUN = CLIENT / "Content" / "Characters" / "Swordsman" / "Run.png"

# Matches SwordsmanSpriteSheet.cs (down-facing run row).
FRAME_START_X = 16
FRAME_STRIDE = 64
FRAME_COUNT = 8
RUN_ROW_Y = 16

BG = (12, 10, 8)

WIZARD_LARGE_SIZE = (164, 314)
WIZARD_SMALL_SIZE = (55, 58)


def save_bmp24(img: Image.Image, path: Path) -> None:
    """Write a 24-bit BMP (Inno Setup friendly)."""
    rgb = img.convert("RGB")
    w, h = rgb.size
    row_bytes = (w * 3 + 3) & ~3
    pad = row_bytes - w * 3
    pixel_data = bytearray()
    for y in range(h - 1, -1, -1):
        for x in range(w):
            r, g, b = rgb.getpixel((x, y))
            pixel_data.extend((b, g, r))
        pixel_data.extend(b"\x00" * pad)

    header = struct.pack(
        "<2sIHHI",
        b"BM",
        14 + 40 + len(pixel_data),
        0,
        0,
        14 + 40,
    )
    dib = struct.pack(
        "<IIIHHIIIIII",
        40,
        w,
        h,
        1,
        24,
        0,
        len(pixel_data),
        0,
        0,
        0,
        0,
    )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(header + dib + pixel_data)


def fit_inside(img: Image.Image, max_w: int, max_h: int) -> Image.Image:
    scale = min(max_w / img.width, max_h / img.height, 1.0)
    if scale >= 0.999:
        return img
    size = (max(1, int(img.width * scale)), max(1, int(img.height * scale)))
    return img.resize(size, Image.Resampling.LANCZOS)


def make_wizard_large(banner: Image.Image) -> Image.Image:
    """Side panel art — same banner as README / marketing."""
    canvas = Image.new("RGB", WIZARD_LARGE_SIZE, BG)
    banner_fit = fit_inside(banner.convert("RGBA"), *WIZARD_LARGE_SIZE)
    x = (WIZARD_LARGE_SIZE[0] - banner_fit.width) // 2
    y = (WIZARD_LARGE_SIZE[1] - banner_fit.height) // 2
    canvas.paste(banner_fit, (x, y), banner_fit)
    return canvas


def make_wizard_small(logo: Image.Image) -> Image.Image:
    canvas = Image.new("RGB", WIZARD_SMALL_SIZE, BG)
    logo_fit = fit_inside(logo.convert("RGBA"), 48, 48)
    canvas.paste(
        logo_fit,
        ((WIZARD_SMALL_SIZE[0] - logo_fit.width) // 2, (WIZARD_SMALL_SIZE[1] - logo_fit.height) // 2),
        logo_fit,
    )
    return canvas


def extract_run_frames(run_sheet: Image.Image) -> list[Image.Image]:
    frames: list[Image.Image] = []
    for i in range(FRAME_COUNT):
        x = FRAME_START_X + i * FRAME_STRIDE
        crop = run_sheet.crop((x, RUN_ROW_Y, x + FRAME_STRIDE, RUN_ROW_Y + FRAME_STRIDE))
        frames.append(crop.resize((128, 128), Image.Resampling.NEAREST))
    return frames


def main() -> int:
    missing = [path for path in (LOGO, BANNER, RUN) if not path.is_file()]
    if missing:
        for path in missing:
            print(f"Missing asset: {path}", file=sys.stderr)
        return 1

    logo = Image.open(LOGO)
    banner = Image.open(BANNER)
    run_sheet = Image.open(RUN)

    save_bmp24(make_wizard_large(banner), OUT / "wizard_large.bmp")
    save_bmp24(make_wizard_small(logo), OUT / "wizard_small.bmp")

    for i, frame in enumerate(extract_run_frames(run_sheet)):
        # Flatten onto dark panel for the installer animation.
        panel = Image.new("RGB", (160, 160), BG)
        panel.paste(frame, ((160 - frame.width) // 2, 24), frame)
        save_bmp24(panel, OUT / f"run_frame_{i:02d}.bmp")

    print(f"Installer assets written to {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
