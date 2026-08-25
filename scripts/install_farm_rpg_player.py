#!/usr/bin/env python3
"""Copy Farm RPG modular character layers into MonoGame-friendly Content paths."""

from __future__ import annotations

import json
import shutil
from pathlib import Path

try:
    from PIL import Image, ImageDraw
except ImportError:
    Image = None  # type: ignore[assignment,misc]
    ImageDraw = None  # type: ignore[assignment,misc]

REPO = Path(__file__).resolve().parents[1]
MODEL_PACK = (
    REPO
    / "client/Deathborn.Client/Models/Farm RPG - Tiny Asset Pack - (All in One)"
    / "Character/Character/PNG"
)
LEGACY_PACK = (
    REPO
    / "client/Deathborn.Client/Content/Characters/Farm RPG - Tiny Asset Pack - (All in One)"
    / "Character/Character/PNG"
)
PACK = MODEL_PACK if MODEL_PACK.is_dir() else LEGACY_PACK
OUT = REPO / "client/Deathborn.Client/Content/Characters/FarmRpg"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"

CLIP_FOLDERS: dict[str, str] = {
    "idle": "1. Idle",
    "walk": "2. Walk",
    "run": "3. Run",
    "attack": "8. SwordAttack",
    "cast": "23. Mage",
    "shield_bash": "13.4 Carrying - Throwing items",
    "hurt": "10. Damage",
    "death": "11. Death",
    "fish_wait": "12.1. Fishing - Wait",
    "fish_reel": "12.3. Fishing - Reel",
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
    "eyes-female-brown": {"tpl": "Eyes/Female/Brown.png"},
    "eyes-female-blue": {"tpl": "Eyes/Female/Blue.png"},
    "eyes-female-green": {"tpl": "Eyes/Female/Green.png"},
    "eyes-female-black": {"tpl": "Eyes/Female/Black.png"},
    "hair-josh-brown": {"tpl": "Hair's/Josh/Brown.png"},
    "hair-josh-black": {"tpl": "Hair's/Josh/Black.png"},
    "hair-josh-blonde": {"tpl": "Hair's/Josh/Blonde.png"},
    "hair-josh-ginger": {"tpl": "Hair's/Josh/Ginger.png"},
    "hair-lyria-brown": {"tpl": "Hair's/Lyria/Brown.png"},
    "hair-lyria-black": {"tpl": "Hair's/Lyria/Black.png"},
    "hair-lyria-blonde": {"tpl": "Hair's/Lyria/Blonde.png"},
    "hair-lyria-ginger": {"tpl": "Hair's/Lyria/Ginger.png"},
    "outfit-farm-blue": {"tpl": "Clothers/Farm/Blue.png"},
    "outfit-farm-green": {"tpl": "Clothers/Farm/Green.png"},
    "outfit-farm-red": {"tpl": "Clothers/Farm/Red.png"},
    "weapon-sword": {"tpl": "Weapons/Sword/1.png", "clips": ["attack"]},
    "weapon-staff": {"tpl": "Healer Staff.png", "clips": ["cast"]},
    "fx-cast": {"tpl": "Fx.png", "clips": ["cast"]},
    "weapon-shield": {"generated": True, "clips": ["shield_bash"]},
    "weapon-fishing-rod": {"tpl": "Weapons/1.png", "clips": ["fish_wait", "fish_reel"]},
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
    # Fishing folders are complete for starter layers; fall back to idle if a variant is missing.
    if clip in ("fish_wait", "fish_reel"):
        fallback = PACK / CLIP_FOLDERS["idle"] / tpl
        if fallback.exists():
            return fallback
    raise FileNotFoundError(src)


def copy_layer(layer_id: str, spec: dict[str, str]) -> list[str]:
    if spec.get("generated"):
        return generate_layer(layer_id, spec)

    clips = spec.get("clips", list(CLIP_FOLDERS))
    rel_paths: list[str] = []
    for clip in clips:
        src = resolve_src(clip, spec["tpl"])
        dest = OUT / "layers" / layer_id / f"{clip}.png"
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dest)
        rel_paths.append(f"Characters/FarmRpg/layers/{layer_id}/{clip}.png")
    return rel_paths


def generate_layer(layer_id: str, spec: dict[str, str]) -> list[str]:
    clips = spec.get("clips", list(CLIP_FOLDERS))
    rel_paths: list[str] = []
    for clip in clips:
        if clip == "shield_bash" and layer_id == "weapon-shield":
            dest = OUT / "layers" / layer_id / f"{clip}.png"
            dest.parent.mkdir(parents=True, exist_ok=True)
            write_shield_bash_weapon(dest)
            rel_paths.append(f"Characters/FarmRpg/layers/{layer_id}/{clip}.png")
        else:
            raise ValueError(f"unsupported generated layer {layer_id}/{clip}")
    return rel_paths


def write_shield_bash_weapon(dest: Path) -> None:
    """Simple wood-and-iron shield strip aligned to the throwing-items bash pose."""
    if Image is None or ImageDraw is None:
        raise SystemExit("Pillow is required to generate weapon-shield layers (pip install pillow)")

    frames_per_dir = 5
    cell = 32
    sheet = Image.new("RGBA", (cell * frames_per_dir * 4, cell), (0, 0, 0, 0))
    draw = ImageDraw.Draw(sheet)

    # (cx, cy) per frame — down, up, right, left (Farm RPG direction order).
    layouts: dict[int, list[tuple[int, int]]] = {
        0: [(16, 15), (16, 14), (16, 20), (16, 21), (16, 16)],  # down
        1: [(16, 11), (16, 10), (16, 8), (16, 7), (16, 12)],    # up
        2: [(14, 17), (16, 17), (24, 17), (26, 17), (18, 17)],  # right
        3: [(18, 17), (16, 17), (8, 17), (6, 17), (14, 17)],    # left
    }

    wood = (139, 90, 43, 255)
    rim = (192, 192, 192, 255)
    boss = (96, 96, 112, 255)

    for direction, points in layouts.items():
        for frame, (cx, cy) in enumerate(points):
            col = direction * frames_per_dir + frame
            ox = col * cell
            w, h = (10, 12) if direction in (0, 1) else (12, 10)
            left = ox + cx - w // 2
            top = cy - h // 2
            draw.rectangle((left, top, left + w - 1, top + h - 1), fill=wood, outline=rim)
            draw.rectangle((left + 2, top + 2, left + w - 3, top + h - 3), outline=boss)
            draw.ellipse((cx + ox - 2, cy - 2, cx + ox + 1, cy + 1), fill=boss)

    sheet.save(dest)


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
            "shield_bash": {"frames": 5, "folder": "13.4 Carrying - Throwing items"},
            "hurt": {"frames": 4, "folder": "10. Damage"},
            "death": {"frames": 4, "folder": "11. Death"},
            "fish_wait": {"frames": 4, "folder": "12.1. Fishing - Wait"},
            "fish_reel": {"frames": 4, "folder": "12.3. Fishing - Reel"},
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
