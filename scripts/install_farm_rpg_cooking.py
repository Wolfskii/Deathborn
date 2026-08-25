#!/usr/bin/env python3
"""Install Tiny Farm RPG kitchen furniture and cooked-meal icons.

Vendor pack → role-based Content paths (no pack name in destinations):
  Furniture/kitchen.png
  Icons/cook/<meal>.png
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

MEAL_ICONS: dict[str, str] = {
    "fried_egg": "Icons/Food Icons/Fried Egg.png",
    "baked_fish": "Icons/Food Icons/Baked Fish.png",
    "parsnip_soup": "Icons/Food Icons/Parsnip Soup.png",
    "baked_potato": "Icons/Food Icons/Baked Potato.png",
    "pumpkin_pie": "Icons/Food Icons/Pumpkin Pie.png",
    "bread": "Icons/Food Icons/Bread .png",
    "jam": "Icons/Food Icons/Jam.png",
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
    for y in range(y0, min(im.size[1], y0 + ch)):
        for x in range(x0, min(im.size[0], x0 + cw)):
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


def compact_strip(src: Path, cell: int = 32) -> Image.Image:
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
    furniture = CONTENT / "Furniture"
    furniture.mkdir(parents=True, exist_ok=True)
    pot_src = PACK / "Objects/Work Benches/Kitchen pot.png"
    if not pot_src.is_file():
        raise SystemExit(f"missing kitchen pot: {pot_src}")
    dest = furniture / "kitchen.png"
    compact_strip(pot_src, 32).save(dest)
    written.append(dest)
    print(f"kitchen {dest}")

    icons = CONTENT / "Icons" / "cook"
    icons.mkdir(parents=True, exist_ok=True)
    for item_id, rel in MEAL_ICONS.items():
        src = PACK / rel
        if not src.is_file():
            print(f"skip missing {src}")
            continue
        out = icons / f"{item_id}.png"
        first_icon(src).save(out)
        written.append(out)
        print(f"icon {item_id}")

    ensure_mgcb(written)
    print(f"installed {len(written)} cooking textures")


if __name__ == "__main__":
    main()
