#!/usr/bin/env python3
"""Copy Farm RPG terrain + foliage sheets into MonoGame Content paths."""

from __future__ import annotations

import json
import shutil
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
PACK = (
    REPO
    / "client/Deathborn.Client/Content/Characters/Farm RPG - Tiny Asset Pack - (All in One)"
)
TILES_OUT = REPO / "client/Deathborn.Client/Content/Tiles/FarmRpg"
DECOR_OUT = REPO / "client/Deathborn.Client/Content/Decorations/FarmRpg"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"

# Elevation palette: spring coast/base, then summer / fall / deep forest plateaus.
GRASS_BY_ELEV = {
    "grass_elev0.png": "Tileset/Tileset Grass Spring.png",
    "grass_elev1.png": "Tileset/Tileset Grass Spring.png",
    "grass_elev2.png": "Tileset/Tileset Grass Summer.png",
    "grass_elev3.png": "Tileset/Tileset Grass Fall.png",
    "grass_elev4.png": "Tileset/Tileset Grass Deep Forest.png",
}

CLIFF_BY_ELEV = {
    "cliff_elev0.png": "Tileset/Tileset Grass Cliff Tileset Spring.png",
    "cliff_elev1.png": "Tileset/Tileset Grass Cliff Tileset Spring.png",
    "cliff_elev2.png": "Tileset/Tileset Grass Cliff Tileset Summer.png",
    "cliff_elev3.png": "Tileset/Tileset Grass Cliff Tileset Fall.png",
    "cliff_elev4.png": "Tileset/Tileset Grass Cliff Tileset Deep Forest.png",
}

TERRAIN_FILES = {
    **{f"Tiles/FarmRpg/{name}": rel for name, rel in GRASS_BY_ELEV.items()},
    **{f"Tiles/FarmRpg/{name}": rel for name, rel in CLIFF_BY_ELEV.items()},
    "Tiles/FarmRpg/grass_water.png": "Tileset/Tileset Grass Water Spring.png",
    "Tiles/FarmRpg/water_fill.png": "Tileset/Water tile.png",
    "Tiles/FarmRpg/water_anim.png": "Tileset/Water Ground animations tiles.png",
    "Tiles/FarmRpg/shadow.png": "Tileset/Shadow.png",
    "Tiles/FarmRpg/props_seasons.png": "Tileset/ALL props seasons.png",
}

DECORATION_FILES = {
    "Decorations/FarmRpg/tree_pine.png": "Objects/Tree/Common/Shadow/Pine Tree.png",
    "Decorations/FarmRpg/tree_maple.png": "Objects/Tree/Common/Shadow/Maple Tree.png",
    "Decorations/FarmRpg/tree_pine_anim.png": "Objects/Tree/Common/Shadow/Pine Tree Animation.png",
    "Decorations/FarmRpg/tree_maple_anim.png": "Objects/Tree/Common/Shadow/Maple Tree Animation.png",
    "Decorations/FarmRpg/bush_1.png": "Objects/Tree/Deep Forest/bushes.png",
    "Decorations/FarmRpg/bush_2.png": "Objects/Tree/Deep Forest/bushes.png",
    "Decorations/FarmRpg/bush_3.png": "Objects/Tree/Deep Forest/bushes.png",
    "Decorations/FarmRpg/rock_1.png": "Objects/Props/Spring/Stones.png",
    "Decorations/FarmRpg/rock_2.png": "Objects/Props/Summer/Stones Summer.png",
    "Decorations/FarmRpg/water_rock_1.png": "Objects/Props/Spring/props water.png",
    "Decorations/FarmRpg/water_rock_2.png": "Objects/Props/Summer/PropsWater Summer.png",
    "Decorations/FarmRpg/clouds.png": "Objects/Props/clouds.png",
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


def copy_assets() -> list[str]:
    installed: list[str] = []
    seen_src: dict[Path, Path] = {}

    def stage(content_rel: str, pack_rel: str) -> None:
        src = PACK / pack_rel
        if not src.is_file():
            raise FileNotFoundError(src)
        dest = REPO / "client/Deathborn.Client/Content" / content_rel.replace("/", "\\")
        if content_rel.startswith("Tiles/"):
            dest = TILES_OUT / Path(content_rel).name
        elif content_rel.startswith("Decorations/"):
            dest = DECOR_OUT / Path(content_rel).name
        else:
            dest.parent.mkdir(parents=True, exist_ok=True)
        dest.parent.mkdir(parents=True, exist_ok=True)
        if src not in seen_src:
            shutil.copy2(src, dest)
            seen_src[src] = dest
        elif seen_src[src] != dest:
            shutil.copy2(src, dest)
        installed.append(content_rel.replace("\\", "/"))

    for content_rel, pack_rel in TERRAIN_FILES.items():
        stage(content_rel, pack_rel)
    for content_rel, pack_rel in DECORATION_FILES.items():
        stage(content_rel, pack_rel)

    return sorted(set(installed))


def write_manifest(installed: list[str]) -> None:
    manifest = {
        "tile_size_px": 16,
        "world_tile_size": 16,
        "plain_grass_cell": [9, 2],
        "grass_autotile_origin": [5, 1],
        "grass_water_land_origin": [4, 8],
        "cliff_lip_row": 17,
        "cliff_base_row": 18,
        "cliff_lip_cols": [16, 17, 18, 19],
        "elevation_grass_sheets": [f"Tiles/FarmRpg/grass_elev{i}.png" for i in range(5)],
        "installed": installed,
    }
    TILES_OUT.mkdir(parents=True, exist_ok=True)
    (TILES_OUT / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def patch_mgcb(rel_paths: list[str]) -> None:
    text = MGCB.read_text(encoding="utf-8")
    new_blocks: list[str] = []
    for rel in rel_paths:
        marker = f"#begin {rel}"
        if marker in text:
            continue
        new_blocks.append(MGCB_BLOCK.format(mgcb_path=rel))
    if not new_blocks:
        return
    MGCB.write_text(text.rstrip() + "\n\n" + "\n".join(new_blocks) + "\n", encoding="utf-8")


def main() -> None:
    if not PACK.is_dir():
        raise SystemExit(f"Farm RPG pack not found: {PACK}")

    installed = copy_assets()
    write_manifest(installed)
    patch_mgcb(installed)
    print(f"Installed {len(installed)} Farm RPG terrain/foliage sheets")


if __name__ == "__main__":
    main()
