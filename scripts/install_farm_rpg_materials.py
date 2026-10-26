#!/usr/bin/env python3
"""Install Tiny Farm RPG icons for gathered and looted materials.

Vendor pack -> role-based Content paths (no pack name in destinations):
  Icons/materials/<item>.png

Covers woodcutting and mining resources, smelted bars, and mob loot.
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
EXTRAS = "Icons/RPG icons/Extras"

# item id -> (sheet, column, row) in 16px cells. Keep ids in sync with
# ItemCatalog.cs and the server's item ids.
ICONS: dict[str, tuple[str, int, int]] = {
    "logs": (f"{EXTRAS}/Wood.png", 0, 0),
    "copper_ore": (f"{EXTRAS}/Bars and ores.png", 1, 0),
    "copper_bar": (f"{EXTRAS}/Bars and ores.png", 0, 0),
    "iron_ore": (f"{EXTRAS}/Bars and ores.png", 3, 0),
    "iron_bar": (f"{EXTRAS}/Bars and ores.png", 2, 0),
    "bone": (f"{EXTRAS}/Bones.png", 0, 0),
    "orc_tusk": (f"{EXTRAS}/Bones.png", 1, 0),
    "bat_wing": (f"{EXTRAS}/Leather.png", 0, 0),
    "slime_gel": (f"{EXTRAS}/Gemstones.png", 5, 0),
    "boss_trophy": (f"{EXTRAS}/Gemstones.png", 1, 0),
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
        if f"#begin {rel}" in text:
            continue
        blocks.append(MGCB_BLOCK.format(mgcb_path=rel))
    if not blocks:
        print("Content.mgcb already up to date")
        return
    MGCB.write_text(text.rstrip() + "\n\n" + "\n".join(blocks) + "\n", encoding="utf-8")
    print(f"registered {len(blocks)} textures in Content.mgcb")


def main() -> None:
    if not PACK.is_dir():
        raise SystemExit(f"vendor pack missing: {PACK}")

    out_dir = CONTENT / "Icons" / "materials"
    out_dir.mkdir(parents=True, exist_ok=True)
    written: list[Path] = []
    for item_id, (rel, col, row) in ICONS.items():
        src = PACK / rel
        if not src.is_file():
            raise SystemExit(f"missing source icon: {src}")
        dest = out_dir / f"{item_id}.png"
        crop_cell(src, col, row).save(dest)
        written.append(dest)
        print(f"icon {item_id}")

    ensure_mgcb(written)


if __name__ == "__main__":
    main()
