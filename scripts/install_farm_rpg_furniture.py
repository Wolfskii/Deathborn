#!/usr/bin/env python3
"""Install homestead furniture and interior decor sprites from the Farm RPG pack.

Crops single sprites out of the pack's interior sheets into
client/Deathborn.Client/Content/Furniture/<name>.png and registers them in Content.mgcb.
Furniture ids match server game/housing.go furnitureTypes and the client decorate list.

Run: py -3 scripts/install_farm_rpg_furniture.py
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[1]
PACK = REPO / "client/Deathborn.Client/Models/Farm RPG - Tiny Asset Pack - (All in One)"
INTERIOR = PACK / "Objects/Interior"
OUT = REPO / "client/Deathborn.Client/Content/Furniture"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"

# name -> (sheet path, crop box left, top, right, bottom) in sheet pixels.
SPRITES: dict[str, tuple[Path, tuple[int, int, int, int]]] = {
    "bed": (INTERIOR / "Beds.png", (7, 317, 40, 351)),
    "table": (INTERIOR / "Tables and desks.png", (6, 230, 28, 250)),
    "chair": (INTERIOR / "Chairs.png", (146, 15, 157, 31)),
    "chest": (INTERIOR / "Dressers.png", (2, 98, 29, 110)),
    "fireplace": (INTERIOR / "Fireplace.png", (34, 6, 62, 48)),
    "rug": (PACK / "Tileset/carpet.png", (20, 68, 60, 108)),
    "sofa": (INTERIOR / "Sofa and armchair.png", (2, 11, 31, 31)),
    "armchair": (INTERIOR / "Sofa and armchair.png", (136, 107, 153, 127)),
    "bookshelf": (INTERIOR / "Others.png", (1, 46, 31, 80)),
    "dresser": (INTERIOR / "Dressers.png", (64, 40, 80, 58)),
    "plant": (INTERIOR / "Part 1 copiar.png", (1, 94, 16, 112)),
    "lamp": (INTERIOR / "Part 1 copiar.png", (81, 42, 93, 64)),
    "piano": (INTERIOR / "Others.png", (2, 93, 31, 111)),
    # Window curtains for the north walls (16 x 17, arched gap shows the pane behind).
    "curtain_bedroom": (INTERIOR / "Doors, windows and curtains.png", (192, 47, 208, 64)),
    "curtain_hall": (INTERIOR / "Doors, windows and curtains.png", (144, 15, 160, 32)),
    "curtain_kitchen": (INTERIOR / "Doors, windows and curtains.png", (144, 47, 160, 64)),
}

MGCB_BLOCK = """#begin {path}
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:ColorKeyColor=255,0,255,255
/processorParam:ColorKeyEnabled=False
/processorParam:GenerateMipmaps=False
/processorParam:PremultiplyAlpha=True
/processorParam:ResizeToPowerOfTwo=False
/processorParam:MakeSquare=False
/processorParam:TextureFormat=Color
/build:{path}

#end {path}
"""


def patch_mgcb(rel_paths: list[str]) -> None:
    text = MGCB.read_text(encoding="utf-8")
    nl = "\r\n" if "\r\n" in text else "\n"
    added = []
    for rel in rel_paths:
        if f"#begin {rel}" in text:
            continue
        added.append(MGCB_BLOCK.format(path=rel).replace("\n", nl))
    if added:
        if not text.endswith(nl):
            text += nl
        text += nl.join(added)
        MGCB.write_text(text, encoding="utf-8", newline="")


def main() -> None:
    if not INTERIOR.is_dir():
        raise SystemExit(f"Farm RPG pack not found: {PACK}")
    OUT.mkdir(parents=True, exist_ok=True)
    rel_paths = []
    for name, (sheet, box) in SPRITES.items():
        sprite = Image.open(sheet).convert("RGBA").crop(box)
        sprite.save(OUT / f"{name}.png")
        rel_paths.append(f"Furniture/{name}.png")
    patch_mgcb(rel_paths)
    print(f"Installed {len(rel_paths)} furniture sprites under {OUT.relative_to(REPO)}")


if __name__ == "__main__":
    main()
