#!/usr/bin/env python3
"""Build Inno Setup wizard bitmaps from Deathborn client art."""

from __future__ import annotations

import struct
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
CLIENT = ROOT / "client" / "Deathborn.Client"
OUT = Path(__file__).resolve().parent / "assets"

LOGO = CLIENT / "Content" / "Images" / "Logos" / "logo_no_text.png"
RUN = CLIENT / "Content" / "Characters" / "Swordsman" / "Run.png"
ICON = CLIENT / "Icon.ico"

# Matches SwordsmanSpriteSheet.cs (down-facing run row).
FRAME_START_X = 16
FRAME_STRIDE = 64
FRAME_COUNT = 8
RUN_ROW_Y = 16

BG = (12, 10, 8)
GOLD = (210, 170, 80)
TAGLINE = (200, 185, 140)


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


def load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        "C:/Windows/Fonts/georgia.ttf",
        "C:/Windows/Fonts/times.ttf",
        "C:/Windows/Fonts/arial.ttf",
    ]
    for candidate in candidates:
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            continue
    return ImageFont.load_default()


def fit_inside(img: Image.Image, max_w: int, max_h: int) -> Image.Image:
    scale = min(max_w / img.width, max_h / img.height, 1.0)
    if scale >= 0.999:
        return img
    size = (max(1, int(img.width * scale)), max(1, int(img.height * scale)))
    return img.resize(size, Image.Resampling.LANCZOS)


def make_wizard_large(logo: Image.Image) -> Image.Image:
    canvas = Image.new("RGB", (164, 314), BG)
    draw = ImageDraw.Draw(canvas)

    logo_fit = fit_inside(logo.convert("RGBA"), 148, 148)
    lx = (164 - logo_fit.width) // 2
    canvas.paste(logo_fit, (lx, 18), logo_fit)

    tag_font = load_font(11)
    tag = "You are born to die."
    tw = draw.textlength(tag, font=tag_font)
    draw.text(((164 - tw) / 2, 178), tag, fill=TAGLINE, font=tag_font)

    sub_font = load_font(9)
    sub = "Only skill decides when."
    sw = draw.textlength(sub, font=sub_font)
    draw.text(((164 - sw) / 2, 196), sub, fill=GOLD, font=sub_font)

    draw.line((18, 222, 146, 222), fill=(60, 48, 32), width=1)
    credit = "— Wolfskii"
    cw = draw.textlength(credit, font=sub_font)
    draw.text(((164 - cw) / 2, 236), credit, fill=(120, 100, 70), font=sub_font)

    return canvas


def make_wizard_small(logo: Image.Image) -> Image.Image:
    canvas = Image.new("RGB", (55, 58), BG)
    logo_fit = fit_inside(logo.convert("RGBA"), 48, 48)
    canvas.paste(
        logo_fit,
        ((55 - logo_fit.width) // 2, (58 - logo_fit.height) // 2),
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
    if not LOGO.is_file():
        print(f"Missing logo: {LOGO}", file=sys.stderr)
        return 1
    if not RUN.is_file():
        print(f"Missing run sheet: {RUN}", file=sys.stderr)
        return 1

    logo = Image.open(LOGO)
    run_sheet = Image.open(RUN)

    save_bmp24(make_wizard_large(logo), OUT / "wizard_large.bmp")
    save_bmp24(make_wizard_small(logo), OUT / "wizard_small.bmp")

    for i, frame in enumerate(extract_run_frames(run_sheet)):
        # Flatten onto dark panel for the installer animation.
        panel = Image.new("RGB", (160, 160), BG)
        panel.paste(frame, ((160 - frame.width) // 2, 24), frame)
        save_bmp24(panel, OUT / f"run_frame_{i:02d}.bmp")

    if ICON.is_file():
        icon = Image.open(ICON).convert("RGBA")
        icon_fit = fit_inside(icon, 48, 48)
        icon_canvas = Image.new("RGB", (55, 58), BG)
        icon_canvas.paste(
            icon_fit,
            ((55 - icon_fit.width) // 2, (58 - icon_fit.height) // 2),
            icon_fit,
        )
        save_bmp24(icon_canvas, OUT / "wizard_small.bmp")

    print(f"Installer assets written to {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
