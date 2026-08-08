#!/usr/bin/env python3
"""Install Farm RPG slime enemies into MonoGame Content paths.

Vendor pack (combined sheets + Blue clip folders) →
  Content/Characters/FarmRpg/Enemies/slimes/<color>_<size>/{idle,walk,damage,dead}.png

All output clips are horizontal strips of 32×32 frames (MGCB TextureProcessor).
"""
from __future__ import annotations

import shutil
from pathlib import Path

try:
    from PIL import Image
except ImportError as e:
    raise SystemExit("Pillow required: pip install Pillow") from e

REPO = Path(__file__).resolve().parents[1]
VENDOR = (
    REPO
    / "client/Deathborn.Client/Content/Characters"
    / "Farm RPG - Tiny Asset Pack - (All in One)/Enemy/Slimes"
)
OUT = REPO / "client/Deathborn.Client/Content/Characters/FarmRpg/Enemies/slimes"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"

COLORS = {
    "blue": "Blue",
    "black": "Black",
    "golden": "Golden",
    "green": "Green",
    "pink": "Pink",
    "purple": "Pupple",  # vendor folder spelling
}
SIZES = {
    "small": "Small Slime.png",
    "normal": "Slime.png",
    "big": "Big Slime.png",
}
CLIPS = ("idle", "walk", "damage", "dead")
FRAME = 32

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


def clear_orphan_ground_specks(im: Image.Image, frame: int = FRAME) -> Image.Image:
    """Remove stray 1-row outline crumbs left under airborne bounce frames.

    Some Big Slime walk/idle cells keep a short dark line on the bottom row while
    the body has bounced up — it reads as a floating bar under the sprite in-game.
    """
    im = im.convert("RGBA")
    w, h = im.size
    if w % frame != 0 or h != frame:
        return im

    px = im.load()
    cols = w // frame
    for col in range(cols):
        x0 = col * frame
        row_has = [
            any(px[x0 + x, y][3] > 0 for x in range(frame))
            for y in range(frame)
        ]
        y = frame - 1
        while y >= 0 and not row_has[y]:
            y -= 1
        if y < 0:
            continue
        bottom_block: list[int] = []
        while y >= 0 and row_has[y]:
            bottom_block.append(y)
            y -= 1
        gap = 0
        while y >= 0 and not row_has[y]:
            gap += 1
            y -= 1
        # Isolated 1–2 px ground speck separated from the body by empty rows.
        if gap >= 2 and bottom_block and (max(bottom_block) - min(bottom_block)) <= 1:
            for by in bottom_block:
                for x in range(frame):
                    px[x0 + x, by] = (0, 0, 0, 0)
    return im


def to_horizontal_strip(im: Image.Image) -> Image.Image:
    """Normalize any clip sheet to a single-row strip of 32×32 frames."""
    im = im.convert("RGBA")
    w, h = im.size
    if w % FRAME != 0 or h % FRAME != 0:
        raise ValueError(f"unexpected size {w}x{h}, need multiples of {FRAME}")

    cols = w // FRAME
    rows = h // FRAME
    frames: list[Image.Image] = []
    for row in range(rows):
        for col in range(cols):
            box = (col * FRAME, row * FRAME, (col + 1) * FRAME, (row + 1) * FRAME)
            frames.append(im.crop(box))

    out = Image.new("RGBA", (FRAME * len(frames), FRAME), (0, 0, 0, 0))
    for i, fr in enumerate(frames):
        out.paste(fr, (i * FRAME, 0))
    return clear_orphan_ground_specks(out)


def slice_combined(sheet: Path) -> dict[str, Image.Image]:
    """Combined sheets stack Idle/Walk/Damage/Dead as equal-height bands."""
    im = Image.open(sheet).convert("RGBA")
    w, h = im.size
    if h % 4 != 0:
        raise ValueError(f"{sheet}: height {h} not divisible by 4 clips")
    band = h // 4
    return {
        clip: to_horizontal_strip(im.crop((0, i * band, w, (i + 1) * band)))
        for i, clip in enumerate(CLIPS)
    }


def load_blue_folder(size_folder: str) -> dict[str, Image.Image] | None:
    folder = VENDOR / "Blue" / size_folder
    if not folder.is_dir():
        return None
    names = {"idle": "Idle.png", "walk": "Walk.png", "damage": "Damage.png", "dead": "Dead.png"}
    out: dict[str, Image.Image] = {}
    for clip, name in names.items():
        path = folder / name
        if not path.is_file():
            return None
        out[clip] = to_horizontal_strip(Image.open(path))
    return out


def ensure_mgcb(rel_posix: str, text: str) -> str:
    marker = f"#begin {rel_posix}"
    if marker in text:
        return text
    block = MGCB_BLOCK.format(mgcb_path=rel_posix)
    if not text.endswith("\n"):
        text += "\n"
    return text + "\n" + block


def install_variant(color_id: str, vendor_color: str, size_id: str, sheet_name: str) -> list[Path]:
    dest_dir = OUT / f"{color_id}_{size_id}"
    dest_dir.mkdir(parents=True, exist_ok=True)

    clips: dict[str, Image.Image] | None = None
    if color_id == "blue":
        folder_name = "Small Slime" if size_id == "small" else ("Slime" if size_id == "normal" else None)
        if folder_name:
            clips = load_blue_folder(folder_name)

    if clips is None:
        sheet = VENDOR / vendor_color / sheet_name
        if not sheet.is_file():
            raise FileNotFoundError(sheet)
        clips = slice_combined(sheet)

    written: list[Path] = []
    for clip, im in clips.items():
        path = dest_dir / f"{clip}.png"
        im.save(path)
        written.append(path)
    return written


def main() -> None:
    if not VENDOR.is_dir():
        raise SystemExit(f"vendor pack missing: {VENDOR}")

    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir(parents=True)

    all_files: list[Path] = []
    for color_id, vendor_color in COLORS.items():
        for size_id, sheet_name in SIZES.items():
            paths = install_variant(color_id, vendor_color, size_id, sheet_name)
            all_files.extend(paths)
            print(f"installed {color_id}_{size_id} ({len(paths)} clips)")

    mgcb_text = MGCB.read_text(encoding="utf-8") if MGCB.is_file() else ""
    content_root = REPO / "client/Deathborn.Client/Content"
    for path in all_files:
        rel = path.relative_to(content_root).as_posix()
        mgcb_text = ensure_mgcb(rel, mgcb_text)
    MGCB.write_text(mgcb_text, encoding="utf-8")
    print(f"registered {len(all_files)} textures in Content.mgcb")


if __name__ == "__main__":
    main()
