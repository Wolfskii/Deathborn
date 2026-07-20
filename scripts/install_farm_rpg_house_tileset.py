#!/usr/bin/env python3
"""Install Farm RPG house interior tileset for homestead rooms."""
from __future__ import annotations

import shutil
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
VENDOR = (
    REPO
    / "client/Deathborn.Client/Content/Characters"
    / "Farm RPG - Tiny Asset Pack - (All in One)/Tileset/Tileset House.png"
)
OUT = REPO / "client/Deathborn.Client/Content/Characters/FarmRpg/Buildings/tileset_house.png"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"
MGCB_PATH = "Characters/FarmRpg/Buildings/tileset_house.png"

MGCB_BLOCK = f"""#begin {MGCB_PATH}
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:ColorKeyColor=255,0,255,255
/processorParam:ColorKeyEnabled=False
/processorParam:GenerateMipmaps=False
/processorParam:PremultiplyAlpha=True
/processorParam:ResizeToPowerOfTwo=False
/processorParam:MakeSquare=False
/processorParam:TextureFormat=Color
/build:{MGCB_PATH}

#end {MGCB_PATH}
"""


def main() -> None:
    if not VENDOR.is_file():
        raise SystemExit(f"missing vendor sheet: {VENDOR}")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(VENDOR, OUT)
    text = MGCB.read_text(encoding="utf-8")
    if MGCB_PATH not in text:
        MGCB.write_text(text.rstrip() + "\n\n" + MGCB_BLOCK, encoding="utf-8")
        print(f"registered {MGCB_PATH}")
    print(f"wrote {OUT.relative_to(REPO)}")


if __name__ == "__main__":
    main()
