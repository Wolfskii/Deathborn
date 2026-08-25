#!/usr/bin/env python3
"""Install Tiny Farm RPG woodcutting/mining tool and resource icons.

Vendor pack → role-based Content paths (no pack name in destinations):
  Icons/gather/<item>.png
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

# (rel path, col, row) in 16px cells.
ICONS: dict[str, tuple[str, int, int]] = {
    "axe": ("Icons/RPG icons/Weapons and Armor/1. Wood/Axe.png", 1, 0),
    "pickaxe": ("Icons/RPG icons/Weapons and Armor/1. Wood/Pickaxe.png", 1, 0),
    "wood": ("Icons/RPG icons/Extras/Wood.png", 0, 0),
    "hardwood": ("Icons/RPG icons/Extras/Wood.png", 2, 0),
    "stone": ("Icons/RPG icons/Extras/Stones.png", 0, 0),
    "copper_ore": ("Icons/RPG icons/Extras/Bars and ores.png", 0, 0),
    "iron_ore": ("Icons/RPG icons/Extras/Bars and ores.png", 4, 0),
    "coal": ("Icons/RPG icons/Extras/Coal.png", 0, 0),
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


def crop_cell(src: Path, col: int, row: int, cell: int = 16) -> Image.Image:
    im = Image.open(src).convert("RGBA")
    x0, y0 = col * cell, row * cell
    return im.crop((x0, y0, x0 + cell, y0 + cell))


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
    icons = CONTENT / "Icons" / "gather"
    icons.mkdir(parents=True, exist_ok=True)
    for item_id, (rel, col, row) in ICONS.items():
        src = PACK / rel
        if not src.is_file():
            print(f"skip missing {src}")
            continue
        dest = icons / f"{item_id}.png"
        crop_cell(src, col, row).save(dest)
        written.append(dest)
        print(f"icon {item_id}")

    ensure_mgcb(written)
    print(f"installed {len(written)} gather textures")


if __name__ == "__main__":
    main()
