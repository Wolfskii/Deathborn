"""Build walkability grid from shared/world/realik_reference.png."""

from __future__ import annotations

import struct
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "shared" / "world" / "realik_reference.png"
OUT = ROOT / "shared" / "world" / "realik_collision.bin"
PREVIEW = ROOT / "shared" / "world" / "realik_preview.png"
SERVER_COPY = ROOT / "server" / "internal" / "worldmap" / "realik_collision.bin"

STEP = 2
TILE_SIZE = 16.0


def is_water(r: int, g: int, b: int, a: int) -> bool:
    if a < 40:
        return True
    if r > 215 and g > 205 and b > 175 and abs(int(r) - int(g)) < 35:
        return True
    if b >= 70 and b >= r + 12 and b >= g - 8:
        return True
    if b >= 95 and g >= 70 and b > r and (b + g) > (r + 60):
        return True
    if b > 55 and r < 70 and g < 95 and b > r + 10:
        return True
    return False


def main() -> None:
    im = Image.open(SRC).convert("RGBA")
    w, h = im.size
    px = im.load()

    tw, th = (w + STEP - 1) // STEP, (h + STEP - 1) // STEP
    grid: list[list[bool]] = []

    for ty in range(th):
        row: list[bool] = []
        for tx in range(tw):
            land_votes = 0
            water_votes = 0
            for sy in range(STEP):
                for sx in range(STEP):
                    x, y = tx * STEP + sx, ty * STEP + sy
                    if x >= w or y >= h:
                        continue
                    r, g, b, a = px[x, y]
                    if is_water(r, g, b, a):
                        water_votes += 1
                    else:
                        land_votes += 1
            row.append(land_votes >= water_votes)
        grid.append(row)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    with OUT.open("wb") as f:
        f.write(b"REAL")
        f.write(struct.pack("<BHHf", 1, tw, th, TILE_SIZE))
        for row in grid:
            for walk in row:
                f.write(struct.pack("B", 1 if walk else 0))

    prev = Image.new("RGB", (tw, th))
    pp = prev.load()
    for ty in range(th):
        for tx in range(tw):
            if grid[ty][tx]:
                v = 110 + ((tx * 7 + ty * 13) % 5) * 8
                pp[tx, ty] = (v, v, v)
            else:
                v = 40 + ((tx * 3 + ty * 5) % 4) * 12
                pp[tx, ty] = (20, 50 + v, 140 + v // 2)
    prev.resize((tw * 4, th * 4), Image.NEAREST).save(PREVIEW)

    SERVER_COPY.parent.mkdir(parents=True, exist_ok=True)
    SERVER_COPY.write_bytes(OUT.read_bytes())
    land = sum(sum(row) for row in grid)
    print(f"grid {tw}x{th} land={land} water={tw * th - land} -> {OUT}")


if __name__ == "__main__":
    main()
