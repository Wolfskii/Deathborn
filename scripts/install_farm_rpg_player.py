#!/usr/bin/env python3
"""Copy Farm RPG modular character layers into MonoGame-friendly Content paths."""

from __future__ import annotations

import json
import shutil
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
PACK = (
    REPO
    / "client/Deathborn.Client/Content/Characters/Farm RPG - Tiny Asset Pack - (All in One)"
    / "Character/Character/PNG"
)
OUT = REPO / "client/Deathborn.Client/Content/Characters/FarmRpg"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"

CLIP_FOLDERS: dict[str, str] = {
    "idle": "1. Idle",
    "walk": "2. Walk",
    "run": "3. Run",
    "attack": "8. SwordAttack",
    "cast": "23. Mage",
    "hurt": "10. Damage",
    "death": "11. Death",
}

# Starter modular character — Josh + farm overalls + sword (character-creator-ready ids).
LAYERS: dict[str, dict[str, str]] = {
    "skin-1": {"tpl": "Skins/1.png"},
    "skin-2": {"tpl": "Skins/2.png"},
    "skin-3": {"tpl": "Skins/3.png"},
    "skin-4": {"tpl": "Skins/4.png"},
    "eyes-male-brown": {"tpl": "Eyes/Male/Brown.png"},
    "eyes-male-blue": {"tpl": "Eyes/Male/Blue.png"},
    "eyes-male-green": {"tpl": "Eyes/Male/Green.png"},
    "eyes-male-black": {"tpl": "Eyes/Male/Black.png"},
    "hair-josh-brown": {"tpl": "Hair's/Josh/Brown.png"},
    "hair-josh-black": {"tpl": "Hair's/Josh/Black.png"},
    "hair-josh-blonde": {"tpl": "Hair's/Josh/Blonde.png"},
    "hair-josh-ginger": {"tpl": "Hair's/Josh/Ginger.png"},
    "outfit-farm-blue": {"tpl": "Clothers/Farm/Blue.png"},
    "outfit-farm-green": {"tpl": "Clothers/Farm/Green.png"},
    "outfit-farm-red": {"tpl": "Clothers/Farm/Red.png"},
    "weapon-sword": {"tpl": "Weapons/Sword/1.png", "clips": ["attack"]},
    "weapon-staff": {"tpl": "Healer Staff.png", "clips": ["cast"]},
    "fx-cast": {"tpl": "Fx.png", "clips": ["cast"]},
}

MAGIC_FX_SRC = PACK.parent / "Others/Arrow/Magic.png"
MAGIC_FX_DEST = OUT / "Effects/magic.png"

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


def resolve_src(clip: str, tpl: str) -> Path:
    src = PACK / CLIP_FOLDERS[clip] / tpl
    if src.exists():
        return src
    # Some clips omit variant eyes — reuse idle art.
    if tpl.startswith("Eyes/"):
        fallback = PACK / CLIP_FOLDERS["idle"] / tpl
        if fallback.exists():
            return fallback
    raise FileNotFoundError(src)


def copy_layer(layer_id: str, spec: dict[str, str]) -> list[str]:
    clips = spec.get("clips", list(CLIP_FOLDERS))
    rel_paths: list[str] = []
    for clip in clips:
        src = resolve_src(clip, spec["tpl"])
        dest = OUT / "layers" / layer_id / f"{clip}.png"
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dest)
        rel_paths.append(f"Characters/FarmRpg/layers/{layer_id}/{clip}.png")
    return rel_paths


def write_manifest(installed: dict[str, list[str]]) -> None:
    manifest = {
        "body_type_id": "farm_rpg",
        "frame_size": 32,
        "directions": 4,
        "direction_order": ["down", "up", "right", "left"],
        "clips": {
            "idle": {"frames": 4, "folder": "1. Idle"},
            "walk": {"frames": 6, "folder": "2. Walk"},
            "run": {"frames": 8, "folder": "3. Run"},
            "attack": {"frames": 10, "folder": "8. SwordAttack"},
            "cast": {"frames": 6, "folder": "23. Mage"},
            "hurt": {"frames": 4, "folder": "10. Damage"},
            "death": {"frames": 4, "folder": "11. Death"},
        },
        "starter_appearance": {
            "skin_layer": "skin-1",
            "eyes_layer": "eyes-male-brown",
            "hair_layer": "hair-josh-brown",
            "outfit_layer": "outfit-farm-blue",
            "weapon_layer": "weapon-sword",
        },
        "installed_layers": installed,
    }
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def patch_mgcb(rel_paths: list[str]) -> None:
    text = MGCB.read_text(encoding="utf-8")
    new_blocks: list[str] = []
    for rel in sorted(set(rel_paths)):
        marker = f"#begin {rel}"
        if marker in text:
            continue
        new_blocks.append(MGCB_BLOCK.format(mgcb_path=rel))
    if not new_blocks:
        return
    MGCB.write_text(text.rstrip() + "\n\n" + "\n".join(new_blocks) + "\n", encoding="utf-8")


def copy_magic_fx() -> str | None:
    if not MAGIC_FX_SRC.is_file():
        return None
    MAGIC_FX_DEST.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(MAGIC_FX_SRC, MAGIC_FX_DEST)
    return "Characters/FarmRpg/Effects/magic.png"


def main() -> None:
    if not PACK.is_dir():
        raise SystemExit(f"Farm RPG pack not found: {PACK}")

    installed: dict[str, list[str]] = {}
    all_paths: list[str] = []
    for layer_id, spec in LAYERS.items():
        paths = copy_layer(layer_id, spec)
        installed[layer_id] = paths
        all_paths.extend(paths)

    magic_path = copy_magic_fx()
    if magic_path:
        all_paths.append(magic_path)

    write_manifest(installed)
    patch_mgcb(all_paths)
    print(f"Installed {len(all_paths)} Farm RPG layer sheets under {OUT.relative_to(REPO)}")


if __name__ == "__main__":
    main()
