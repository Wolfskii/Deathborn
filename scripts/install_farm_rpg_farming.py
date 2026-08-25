#!/usr/bin/env python3
"""Install Tiny Farm RPG crops, tilled soil, animals, and item icons.

Vendor pack → role-based Content paths (no pack name in destinations):
  Crops/<id>.png
  Tiles/farm/tilled_*.png
  Characters/Animals/farm/<species>/{idle,walk}.png
  Icons/farm/<item>.png
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

# Crop strips: last opaque 16px cell is the harvested produce icon; earlier cells are growth.
CROPS: dict[str, str] = {
    "parsnip": "Crops/Spring/Parsnip.png",
    "carrot": "Crops/Spring/Carrot.png",
    "potato": "Crops/Spring/Potato.png",
    "strawberry": "Crops/Spring/Strawberry.png",
    "tomato": "Crops/Summer/Tomato.png",
    "wheat": "Crops/Summer/Wheat.png",
    "melon": "Crops/Summer/Melon.png",
    "pumpkin": "Crops/Fall/Pumpkin.png",
    "corn": "Crops/Fall/Corn.png",
    "beetroot": "Crops/Fall/Beetroot.png",
}

# Food icons are 32×16 (item | outlined). Tools match that layout.
FOOD_ICONS: dict[str, str] = {
    "parsnip": "Icons/Food Icons/Parsnip.png",
    "carrot": "Icons/Food Icons/Carrot.png",
    "potato": "Icons/Food Icons/Potato.png",
    "strawberry": "Icons/Food Icons/Strawberry.png",
    "tomato": "Icons/Food Icons/Tomato.png",
    "wheat": "Icons/Food Icons/Wheat.png",
    "melon": "Icons/Food Icons/Melon.png",
    "pumpkin": "Icons/Food Icons/Pumpkin.png",
    "corn": "Icons/Food Icons/Corn.png",
    "beetroot": "Icons/Food Icons/Beetroot.png",
    "chicken_egg": "Icons/Food Icons/Chicken Egg.png",
    "milk": "Icons/Food Icons/Small Cow Milk.png",
    "wool": "Icons/Food Icons/Wool.png",
    "animal_feed": "Icons/Food Icons/Animal Feed.png",
}

TOOL_ICONS: dict[str, str] = {
    "hoe": "Icons/RPG icons/Weapons and Armor/1. Wood/Hoe.png",
    "watering_can": "Icons/RPG icons/Weapons and Armor/1. Wood/Watering can.png",
}

ANIMALS: dict[str, tuple[str, int]] = {
    # vendor relative path, cell size
    "chicken": ("Animals/Farm/Chicken/Chicken White.png", 16),
    "cow": ("Animals/Farm/Cow/Common Cow/Female Cow Brown.png", 32),
    "sheep": ("Animals/Farm/Sheep/Sheep Female.png", 32),
    "pig": ("Animals/Farm/Pig/Pig Pink.png", 32),
}

# Isolated / fill cells on Tilled Soil and wet soil.png (16px).
# Row 0 = dry terracotta, row 4 = wet blue. Col 1 = fill, col 2 = hoed hole.
SOIL_CELLS = {
    "tilled_dry.png": (1, 0),
    "tilled_dry_hole.png": (2, 0),
    "tilled_wet.png": (1, 4),
    "tilled_wet_hole.png": (2, 4),
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


def compact_crop(src: Path) -> Image.Image:
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    cell = 16
    if w % cell != 0:
        raise ValueError(f"{src}: width {w} not divisible by {cell}")
    frames: list[Image.Image] = []
    for i in range(w // cell):
        if cell_has_pixels(im, i * cell, 0, cell, h):
            frames.append(im.crop((i * cell, 0, (i + 1) * cell, h)))
    if len(frames) < 2:
        raise ValueError(f"{src}: expected growth + produce frames, got {len(frames)}")
    # Drop harvested produce icon (last opaque cell).
    growth = frames[:-1]
    out = Image.new("RGBA", (cell * len(growth), h), (0, 0, 0, 0))
    for i, fr in enumerate(growth):
        out.paste(fr, (i * cell, 0))
    return out


def left_icon(src: Path) -> Image.Image:
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    if w >= 32 and h == 16:
        return im.crop((0, 0, 16, 16))
    if w == h:
        return im
    return im.crop((0, 0, min(w, h), min(w, h)))


def animal_clips(src: Path, cell: int) -> dict[str, Image.Image]:
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    if w % cell != 0 or h % cell != 0:
        raise ValueError(f"{src}: size {w}x{h} not multiples of {cell}")
    cols = w // cell
    # Row 0 = side walk (facing right). Idle = first walk frame.
    walk_frames = [im.crop((c * cell, 0, (c + 1) * cell, cell)) for c in range(cols)]
    idle = Image.new("RGBA", (cell, cell), (0, 0, 0, 0))
    idle.paste(walk_frames[0], (0, 0))
    walk = Image.new("RGBA", (cell * cols, cell), (0, 0, 0, 0))
    for i, fr in enumerate(walk_frames):
        walk.paste(fr, (i * cell, 0))
    return {"idle": idle, "walk": walk}


def animal_icon(idle: Image.Image) -> Image.Image:
    """Pad/crop idle frame into a 16×16 inventory icon."""
    im = idle.convert("RGBA")
    if im.size == (16, 16):
        return im
    # Fit 32×32 cow/sheep/pig into 16×16.
    return im.resize((16, 16), Image.NEAREST)


def crop_seed_icon(growth: Image.Image) -> Image.Image:
    cell = 16
    h = growth.height
    seed = growth.crop((0, max(0, h - cell), cell, h))
    if seed.size != (16, 16):
        canvas = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        canvas.paste(seed, (0, 16 - seed.height))
        return canvas
    return seed


def ensure_mgcb(paths: list[Path]) -> None:
    text = MGCB.read_text(encoding="utf-8") if MGCB.is_file() else ""
    blocks: list[str] = []
    for path in paths:
        rel = path.relative_to(CONTENT).as_posix()
        if f"#begin {rel}" in text:
            continue
        blocks.append(MGCB_BLOCK.format(mgcb_path=rel))
    if blocks:
        if text and not text.endswith("\n"):
            text += "\n"
        MGCB.write_text(text.rstrip() + "\n\n" + "\n".join(blocks) + "\n", encoding="utf-8")
        print(f"registered {len(blocks)} textures in Content.mgcb")
    else:
        print("Content.mgcb already up to date")


def main() -> None:
    if not PACK.is_dir():
        raise SystemExit(f"vendor pack missing: {PACK}")

    written: list[Path] = []

    crops_out = CONTENT / "Crops"
    crops_out.mkdir(parents=True, exist_ok=True)
    growth_by_id: dict[str, Image.Image] = {}
    for crop_id, rel in CROPS.items():
        src = PACK / rel
        if not src.is_file():
            raise SystemExit(f"missing crop: {src}")
        growth = compact_crop(src)
        dest = crops_out / f"{crop_id}.png"
        growth.save(dest)
        written.append(dest)
        growth_by_id[crop_id] = growth
        print(f"crop {crop_id}: {growth.size[0] // 16} stages, {growth.size[1]}px tall")

    soil_src = PACK / "Tileset/Tilled Soil and wet soil.png"
    if not soil_src.is_file():
        raise SystemExit(f"missing soil sheet: {soil_src}")
    soil = Image.open(soil_src).convert("RGBA")
    tiles_out = CONTENT / "Tiles" / "farm"
    tiles_out.mkdir(parents=True, exist_ok=True)
    for name, (tx, ty) in SOIL_CELLS.items():
        cell = soil.crop((tx * 16, ty * 16, tx * 16 + 16, ty * 16 + 16))
        dest = tiles_out / name
        cell.save(dest)
        written.append(dest)

    icons_out = CONTENT / "Icons" / "farm"
    icons_out.mkdir(parents=True, exist_ok=True)
    for item_id, rel in FOOD_ICONS.items():
        src = PACK / rel
        if not src.is_file():
            print(f"skip missing food icon {src}")
            continue
        dest = icons_out / f"{item_id}.png"
        left_icon(src).save(dest)
        written.append(dest)
    for item_id, rel in TOOL_ICONS.items():
        src = PACK / rel
        dest = icons_out / f"{item_id}.png"
        left_icon(src).save(dest)
        written.append(dest)
    for crop_id, growth in growth_by_id.items():
        dest = icons_out / f"{crop_id}_seeds.png"
        crop_seed_icon(growth).save(dest)
        written.append(dest)

    animals_root = CONTENT / "Characters" / "Animals" / "farm"
    for species, (rel, cell) in ANIMALS.items():
        src = PACK / rel
        if not src.is_file():
            raise SystemExit(f"missing animal: {src}")
        clips = animal_clips(src, cell)
        dest_dir = animals_root / species
        dest_dir.mkdir(parents=True, exist_ok=True)
        for clip, im in clips.items():
            dest = dest_dir / f"{clip}.png"
            im.save(dest)
            written.append(dest)
        icon = icons_out / f"{species}.png"
        animal_icon(clips["idle"]).save(icon)
        written.append(icon)
        print(f"animal {species}: cell {cell}px")

    ensure_mgcb(written)
    print(f"installed {len(written)} farm textures")


if __name__ == "__main__":
    main()
