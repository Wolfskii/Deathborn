#!/usr/bin/env python3
"""Generate platform app icons from logo_v2.png (Windows, macOS, Linux, mobile)."""

from __future__ import annotations

import struct
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
CLIENT = ROOT / "client" / "Deathborn.Client"
LOGO = CLIENT / "Content" / "Images" / "Logos" / "logo_v2.png"

ICO_SIZES = (16, 24, 32, 48, 64, 128, 256)
LINUX_ICON_SIZE = 256
# MonoGame DesktopGL loads Icon.bmp (embedded) for the SDL taskbar/window icon in dev builds.
WINDOW_ICON_BMP_SIZE = 256
ICON_PADDING_PX = 2
MAC_ICONSET = (
    ("icon_16x16.png", 16),
    ("icon_16x16@2x.png", 32),
    ("icon_32x32.png", 32),
    ("icon_32x32@2x.png", 64),
    ("icon_128x128.png", 128),
    ("icon_128x128@2x.png", 256),
    ("icon_256x256.png", 256),
    ("icon_256x256@2x.png", 512),
    ("icon_512x512.png", 512),
    ("icon_512x512@2x.png", 1024),
)
ANDROID_MIPMAPS = (
    ("mipmap-mdpi/ic_launcher.png", 48),
    ("mipmap-hdpi/ic_launcher.png", 72),
    ("mipmap-xhdpi/ic_launcher.png", 96),
    ("mipmap-xxhdpi/ic_launcher.png", 144),
    ("mipmap-xxxhdpi/ic_launcher.png", 192),
)
IOS_ICONS = (
    ("icon-20@2x.png", 40, "iphone", "20x20", "2x"),
    ("icon-20@3x.png", 60, "iphone", "20x20", "3x"),
    ("icon-29@2x.png", 58, "iphone", "29x29", "2x"),
    ("icon-29@3x.png", 87, "iphone", "29x29", "3x"),
    ("icon-40@2x.png", 80, "iphone", "40x40", "2x"),
    ("icon-40@3x.png", 120, "iphone", "40x40", "3x"),
    ("icon-60@2x.png", 120, "iphone", "60x60", "2x"),
    ("icon-60@3x.png", 180, "iphone", "60x60", "3x"),
    ("icon-1024.png", 1024, "ios-marketing", "1024x1024", "1x"),
)


def load_logo() -> Image.Image:
    if not LOGO.is_file():
        print(f"Missing logo: {LOGO}", file=sys.stderr)
        raise SystemExit(1)
    return Image.open(LOGO).convert("RGBA")


def trim_transparent(img: Image.Image) -> Image.Image:
    alpha = img.getchannel("A")
    bbox = alpha.getbbox()
    return img.crop(bbox) if bbox else img


def clean_alpha(img: Image.Image) -> Image.Image:
    """Drop RGB on fully transparent pixels so ICO/Windows don't show black halos."""
    cleaned = img.convert("RGBA")
    pixels = cleaned.load()
    width, height = cleaned.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                pixels[x, y] = (0, 0, 0, 0)
    return cleaned


def fit_icon(logo: Image.Image, size: int, padding: int = ICON_PADDING_PX) -> Image.Image:
    """Pixel-art friendly: trim, nearest-neighbor scale, transparent square canvas."""
    trimmed = trim_transparent(logo)
    inner = max(1, size - padding * 2)
    scale = min(inner / trimmed.width, inner / trimmed.height)
    new_w = max(1, int(round(trimmed.width * scale)))
    new_h = max(1, int(round(trimmed.height * scale)))
    resized = trimmed.resize((new_w, new_h), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(
        resized,
        ((size - new_w) // 2, (size - new_h) // 2),
        resized,
    )
    return clean_alpha(canvas)


def save_png(img: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, format="PNG", optimize=True)


def save_ico(logo: Image.Image, path: Path) -> None:
    frames = [fit_icon(logo, size) for size in ICO_SIZES]
    path.parent.mkdir(parents=True, exist_ok=True)
    # Pillow skips sizes larger than the base image — use 256px as the base frame.
    frames[-1].save(
        path,
        format="ICO",
        sizes=[(size, size) for size in ICO_SIZES],
        append_images=frames[:-1],
    )


def save_bmp32_rgba(logo: Image.Image, path: Path, size: int = WINDOW_ICON_BMP_SIZE) -> None:
    """32-bit BMP with alpha — MonoGame/SDL uses this for the taskbar icon in dev (dotnet exec)."""
    rgba = fit_icon(logo, size)
    w, h = rgba.size
    pixel_data = bytearray()
    for y in range(h - 1, -1, -1):
        for x in range(w):
            r, g, b, a = rgba.getpixel((x, y))
            pixel_data.extend((b, g, r, a))

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
        32,
        0,
        len(pixel_data),
        0,
        0,
        0,
        0,
    )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(header + dib + pixel_data)


def write_mac_iconset(logo: Image.Image, iconset_dir: Path) -> None:
    if iconset_dir.exists():
        for child in iconset_dir.iterdir():
            child.unlink()
    else:
        iconset_dir.mkdir(parents=True)
    for filename, size in MAC_ICONSET:
        save_png(fit_icon(logo, size), iconset_dir / filename)


def write_mobile_icons(logo: Image.Image, mobile_root: Path) -> None:
    android_root = mobile_root / "android"
    for rel_path, size in ANDROID_MIPMAPS:
        save_png(fit_icon(logo, size), android_root / rel_path)

    ios_root = mobile_root / "ios" / "AppIcon.appiconset"
    ios_root.mkdir(parents=True, exist_ok=True)
    images: list[str] = []
    for filename, size, idiom, size_key, scale in IOS_ICONS:
        save_png(fit_icon(logo, size), ios_root / filename)
        images.append(
            f'    {{"filename": "{filename}", "idiom": "{idiom}", '
            f'"scale": "{scale}", "size": "{size_key}"}}'
        )
    contents = (
        "{\n"
        '  "images": [\n'
        + ",\n".join(images)
        + '\n  ],\n  "info": {"author": "generate_app_icons.py", "version": 1}\n}\n'
    )
    (ios_root / "Contents.json").write_text(contents, encoding="utf-8")


def main() -> int:
    logo = load_logo()

    save_ico(logo, CLIENT / "Icon.ico")
    save_bmp32_rgba(logo, CLIENT / "Icon.bmp")
    save_png(fit_icon(logo, LINUX_ICON_SIZE), ROOT / "installer" / "linux" / "deathborn.png")
    write_mac_iconset(logo, ROOT / "installer" / "macos" / "AppIcon.iconset")
    write_mobile_icons(logo, ROOT / "installer" / "mobile")

    print(f"App icons generated from {LOGO.name}")
    print(f"  Windows: {CLIENT / 'Icon.ico'}, {CLIENT / 'Icon.bmp'}")
    print(f"  Linux:   {ROOT / 'installer' / 'linux' / 'deathborn.png'}")
    print(f"  macOS:   {ROOT / 'installer' / 'macos' / 'AppIcon.iconset'}")
    print(f"  Mobile:  {ROOT / 'installer' / 'mobile'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
