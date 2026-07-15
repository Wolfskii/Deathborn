#!/usr/bin/env python3
"""Copy Farm RPG inventory / hotbar UI sheets into MonoGame Content paths."""

from __future__ import annotations

import shutil
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
PACK = (
    REPO
    / "client/Deathborn.Client/Content/Characters/Farm RPG - Tiny Asset Pack - (All in One)"
)
OUT = REPO / "client/Deathborn.Client/Content/Ui/FarmRpg"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"

UI_FILES = {
    "Ui/FarmRpg/slots.png": "UI/Inventory/Slots.png",
    "Ui/FarmRpg/inventory.png": "UI/Inventory/inventory.png",
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


def main() -> None:
    if not PACK.is_dir():
        raise SystemExit(f"Farm RPG pack not found: {PACK}")

    OUT.mkdir(parents=True, exist_ok=True)
    installed: list[str] = []
    for content_rel, pack_rel in UI_FILES.items():
        src = PACK / pack_rel
        if not src.is_file():
            raise SystemExit(f"Missing UI asset: {src}")
        dest = REPO / "client/Deathborn.Client/Content" / content_rel.replace("/", "\\")
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dest)
        installed.append(content_rel)

    text = MGCB.read_text(encoding="utf-8")
    new_blocks: list[str] = []
    for rel in installed:
        marker = f"#begin {rel}"
        if marker in text:
            continue
        new_blocks.append(MGCB_BLOCK.format(mgcb_path=rel))
    if new_blocks:
        MGCB.write_text(text.rstrip() + "\n\n" + "\n".join(new_blocks) + "\n", encoding="utf-8")

    print(f"Installed {len(installed)} Farm RPG UI sheets")


if __name__ == "__main__":
    main()
