#!/usr/bin/env python3
"""Install Tiny Farm RPG fishing icons, bait, rod, and water splash.

Vendor pack → role-based Content paths (no pack name in destinations):
  Icons/fish/<item>.png
  Props/water/splash.png
"""
from __future__ import annotations

from pathlib import Path

try:
    from PIL import Image
except ImportError as e:
    raise SystemExit("Pillow required: pip install Pillow") from e

REPO = Path(__file__).resolve().parents[1]
PACK = (
    REPO
    / "client/Deathborn.Client/Content/Characters"
    / "Farm RPG - Tiny Asset Pack - (All in One)"
)
CONTENT = REPO / "client/Deathborn.Client/Content"
MGCB = CONTENT / "Content.mgcb"

FISH_ICONS: dict[str, str] = {
    "sunfish": "Icons/Fish/River/Sunfish.png",
    "chub": "Icons/Fish/River/Chub.png",
    "perch": "Icons/Fish/River/Perch.png",
    "carp": "Icons/Fish/River/Carp.png",
    "largemouth_bass": "Icons/Fish/River/Large Mouth Bass.png",
    "pike": "Icons/Fish/River/Pike Fish.png",
    "tiger_trout": "Icons/Fish/River/Tiger Trout.png",
    "walleye": "Icons/Fish/River/Walleye.png",
    "sturgeon": "Icons/Fish/River/Sturgeon.png",
    "golden_fish": "Icons/Fish/River/Golden Fish.png",
    "anchovy": "Icons/Fish/Sea/Anchovy.png",
    "sardine": "Icons/Fish/Sea/Sardine.png",
    "herring": "Icons/Fish/Sea/Herring.png",
    "salmon": "Icons/Fish/Sea/Salmon.png",
    "red_snapper": "Icons/Fish/Sea/Red Snapper.png",
    "tuna": "Icons/Fish/Sea/Tuna.png",
    "flounder": "Icons/Fish/Sea/Flounder.png",
    "pufferfish": "Icons/Fish/Sea/pufferfish.png",
    "albacore": "Icons/Fish/Sea/Albacore.png",
    "anglerfish": "Icons/Fish/Sea/Anglerfish.png",
}

TOOL_ICONS: dict[str, str] = {
    "fishing_rod": "Icons/RPG icons/Weapons and Armor/1. Wood/Fishing Rod.png",
    "worm_bait": "Icons/Fish/Worm bait.png",
}

MGCB_BLOCK = """#begin {mgcb_path}
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:ColorKeyColor=255,0,255,255
/processorParam:ColorKeyEnabled=False
/processorParam:GenerateMipmaps=False
/processorParam:PremultiplyAlpha=True
/processorParam:ResizeToPowerOfTwo=False
/processorParam:MakeSquare=False
/processorParam:TextureFormat=Color
/build:{mgcb_path}

#end {mgcb_path}
"""


def cell_has_pixels(im: Image.Image, x0: int, y0: int, cw: int, ch: int) -> bool:
    px = im.load()
    for y in range(y0, y0 + ch):
        for x in range(x0, x0 + cw):
            if px[x, y][3] > 8:
                return True
    return False


def first_icon(src: Path, cell: int = 16) -> Image.Image:
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    if w == cell and h == cell:
        return im
    if h == cell and w >= cell:
        return im.crop((0, 0, cell, cell))
    cols = max(1, w // cell)
    rows = max(1, h // cell)
    for row in range(rows):
        for col in range(cols):
            x0, y0 = col * cell, row * cell
            if cell_has_pixels(im, x0, y0, cell, cell):
                return im.crop((x0, y0, x0 + cell, y0 + cell))
    canvas = Image.new("RGBA", (cell, cell), (0, 0, 0, 0))
    canvas.paste(im.crop((0, 0, min(w, cell), min(h, cell))), (0, 0))
    return canvas


def compact_strip(src: Path, cell: int = 16) -> Image.Image:
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    if w % cell != 0:
        raise ValueError(f"{src}: width {w} not divisible by {cell}")
    frames = []
    for i in range(w // cell):
        if cell_has_pixels(im, i * cell, 0, cell, min(h, cell)):
            frames.append(im.crop((i * cell, 0, (i + 1) * cell, min(h, cell))))
    if not frames:
        raise ValueError(f"{src}: no opaque frames")
    out = Image.new("RGBA", (cell * len(frames), cell), (0, 0, 0, 0))
    for i, fr in enumerate(frames):
        out.paste(fr, (i * cell, 0))
    return out


def ensure_mgcb(paths: list[Path]) -> None:
    text = MGCB.read_text(encoding="utf-8") if MGCB.is_file() else ""
    blocks: list[str] = []
    for path in paths:
        rel = path.relative_to(CONTENT).as_posix()
        marker = f"#begin {rel}"
        if marker in text:
            continue
        blocks.append(MGCB_BLOCK.format(mgcb_path=rel))
    if not blocks:
        print("Content.mgcb already up to date")
        return
    if text and not text.endswith("\n"):
        text += "\n"
    MGCB.write_text(text.rstrip() + "\n\n" + "\n".join(blocks) + "\n", encoding="utf-8")
    print(f"registered {len(blocks)} textures in Content.mgcb")


def main() -> None:
    if not PACK.is_dir():
        raise SystemExit(f"vendor pack missing: {PACK}")

    written: list[Path] = []
    icons_out = CONTENT / "Icons" / "fish"
    icons_out.mkdir(parents=True, exist_ok=True)
    for item_id, rel in {**FISH_ICONS, **TOOL_ICONS}.items():
        src = PACK / rel
        if not src.is_file():
            print(f"skip missing {src}")
            continue
        dest = icons_out / f"{item_id}.png"
        first_icon(src).save(dest)
        written.append(dest)
        print(f"icon {item_id}")

    splash_src = PACK / "Objects/Props/Sprash.png"
    if splash_src.is_file():
        props = CONTENT / "Props" / "water"
        props.mkdir(parents=True, exist_ok=True)
        dest = props / "splash.png"
        compact_strip(splash_src).save(dest)
        written.append(dest)
        print(f"splash {dest.size if hasattr(dest, 'size') else dest}")

    ensure_mgcb(written)
    print(f"installed {len(written)} fishing textures")


if __name__ == "__main__":
    main()
