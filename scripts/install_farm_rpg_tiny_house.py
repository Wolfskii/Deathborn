#!/usr/bin/env python3
"""Extract the orange Tiny House (2nd bottom example) for player homesteads.

Vendor: Objects/Exterior/Houses/Tiny House.png →
  Content/Characters/FarmRpg/Buildings/tiny_house_orange.png

Crops land / neighbouring house bleed; keeps roof + walls + awning only.
"""
from __future__ import annotations

from collections import deque
from pathlib import Path

try:
    from PIL import Image
except ImportError as e:
    raise SystemExit("Pillow required: pip install Pillow") from e

REPO = Path(__file__).resolve().parents[1]
VENDOR = (
    REPO
    / "client/Deathborn.Client/Content/Characters"
    / "Farm RPG - Tiny Asset Pack - (All in One)/Objects/Exterior/Houses/Tiny House.png"
)
OUT = REPO / "client/Deathborn.Client/Content/Characters/FarmRpg/Buildings/tiny_house_orange.png"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"

# Second bottom-row cottage (0-based index 1): structure x-run ~84–155.
CROP = (84, 377, 156, 463)

MGCB_PATH = "Characters/FarmRpg/Buildings/tiny_house_orange.png"
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


def is_bg(c: tuple[int, int, int, int]) -> bool:
    return c[3] < 10 or (c[0] < 8 and c[1] < 8 and c[2] < 8)


def is_dirt_or_deep_grass(c: tuple[int, int, int, int]) -> bool:
    r, g, b, a = c
    if a < 10:
        return False
    if 35 < r < 95 and 25 < g < 75 and b < 45 and abs(r - g) < 30:
        return True
    bright = r + g + b
    if g > r + 25 and g > b + 20 and g > 100 and bright < 280 and r < 90:
        return True
    return False


def extract(sheet: Image.Image) -> Image.Image:
    crop = sheet.crop(CROP).convert("RGBA")
    pix = crop.load()
    w, h = crop.size

    seeds: list[tuple[int, int]] = []
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix[x, y]
            if a < 200:
                continue
            if r > 200 and 90 < g < 140 and b < 50 and y > 55:
                seeds.append((x, y))
                break
        if seeds:
            break
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix[x, y]
            if a < 200:
                continue
            if r > 220 and 150 < g < 200 and 90 < b < 150 and 30 < y < 55:
                seeds.append((x, y))
                break
        if len(seeds) >= 2:
            break
    if not seeds:
        raise SystemExit("could not find house seed pixels")

    visited: set[tuple[int, int]] = set(seeds)
    q: deque[tuple[int, int]] = deque(seeds)

    def try_add(nx: int, ny: int) -> None:
        if not (0 <= nx < w and 0 <= ny < h) or (nx, ny) in visited:
            return
        c = pix[nx, ny]
        if is_bg(c) or is_dirt_or_deep_grass(c):
            return
        visited.add((nx, ny))
        q.append((nx, ny))

    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            try_add(x + dx, y + dy)

    # Bridge the intentional transparent gap between roof overhang and walls.
    for x, y in list(visited):
        for dist in range(1, 4):
            for ny in (y + dist, y - dist):
                if not (0 <= ny < h) or (x, ny) in visited:
                    continue
                c = pix[x, ny]
                if is_bg(c) or is_dirt_or_deep_grass(c):
                    continue
                step = 1 if ny > y else -1
                if any(not is_bg(pix[x, ty]) for ty in range(y + step, ny, step)):
                    continue
                try_add(x, ny)
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            try_add(x + dx, y + dy)

    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    op = out.load()
    for x, y in visited:
        op[x, y] = pix[x, y]

    xs = [x for x, _ in visited]
    ys = [y for _, y in visited]
    return out.crop((min(xs), min(ys), max(xs) + 1, max(ys) + 1))


def register_mgcb() -> None:
    text = MGCB.read_text(encoding="utf-8")
    if MGCB_PATH in text:
        return
    MGCB.write_text(text.rstrip() + "\n\n" + MGCB_BLOCK, encoding="utf-8")
    print(f"registered {MGCB_PATH} in Content.mgcb")


def main() -> None:
    if not VENDOR.is_file():
        raise SystemExit(f"missing vendor sheet: {VENDOR}")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    result = extract(Image.open(VENDOR))
    result.save(OUT)
    register_mgcb()
    print(f"wrote {OUT.relative_to(REPO)} ({result.size[0]}x{result.size[1]})")


if __name__ == "__main__":
    main()
