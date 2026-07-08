"""Build walkability grid from shared/world/realik_reference.png."""

from __future__ import annotations

import struct
from collections import deque
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "shared" / "world" / "realik_reference.png"
OUT = ROOT / "shared" / "world" / "realik_collision.bin"
PREVIEW = ROOT / "shared" / "world" / "realik_preview.png"
SERVER_COPY = ROOT / "server" / "internal" / "worldmap" / "realik_collision.bin"

STEP = 2
TILE_SIZE = 32.0
SMALL_WATER_MAX = 9  # fill water pockets with <= this many tiles


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


def is_map_chrome(x: int, y: int, img_w: int, img_h: int) -> bool:
    """Parchment strip below the map frame only (not the mainland)."""
    return y > img_h * 0.945


def trim_title_letters(grid: list[list[bool]], tw: int, th: int) -> int:
    """Remove REALIK lettering east of the southeast peninsula tip."""
    removed = 0
    min_ty = int(th * 0.895)
    min_tx = int(tw * 0.66)
    for ty in range(min_ty, th):
        for tx in range(min_tx, tw):
            if grid[ty][tx]:
                grid[ty][tx] = False
                removed += 1

    # a few floating letter strokes just above the title block
    for ty in range(int(th * 0.892), min_ty):
        for tx in range(int(tw * 0.76), tw):
            if grid[ty][tx]:
                grid[ty][tx] = False
                removed += 1
    return removed


def flood_components(
    grid: list[list[bool]], tw: int, th: int, want_land: bool
) -> list[tuple[list[tuple[int, int]], int, int, int, int]]:
    seen = [[False] * tw for _ in range(th)]
    out: list[tuple[list[tuple[int, int]], int, int, int, int]] = []

    for sy in range(th):
        for sx in range(tw):
            if seen[sy][sx] or grid[sy][sx] != want_land:
                continue

            q: deque[tuple[int, int]] = deque([(sx, sy)])
            seen[sy][sx] = True
            cells: list[tuple[int, int]] = []
            min_tx = max_tx = sx
            min_ty = max_ty = sy

            while q:
                x, y = q.popleft()
                cells.append((x, y))
                min_tx = min(min_tx, x)
                max_tx = max(max_tx, x)
                min_ty = min(min_ty, y)
                max_ty = max(max_ty, y)
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if (
                        0 <= nx < tw
                        and 0 <= ny < th
                        and not seen[ny][nx]
                        and grid[ny][nx] == want_land
                    ):
                        seen[ny][nx] = True
                        q.append((nx, ny))

            out.append((cells, min_tx, min_ty, max_tx, max_ty))

    return out


def set_cells(grid: list[list[bool]], cells: list[tuple[int, int]], land: bool) -> None:
    for x, y in cells:
        grid[y][x] = land


def remove_label_islands(grid: list[list[bool]], tw: int, th: int) -> int:
    """Remove small elongated land blobs produced by location name plates on the art."""
    removed = 0
    for cells, min_tx, min_ty, max_tx, max_ty in flood_components(grid, tw, th, True):
        w = max_tx - min_tx + 1
        h = max_ty - min_ty + 1
        size = len(cells)
        aspect = max(w, h) / max(1, min(w, h))
        thin = min(w, h)

        is_label = (
            size < 400 and thin <= 8 and aspect >= 2.5
        ) or (
            size < 80 and aspect >= 2.0
        ) or (
            size < 160 and thin <= 6 and aspect >= 3.5
        )

        if is_label:
            set_cells(grid, cells, False)
            removed += size

    return removed


def fill_small_water_pockets(grid: list[list[bool]], tw: int, th: int) -> int:
    """Turn tiny inland lakes/ponds into walkable land."""
    filled = 0
    for cells, _, _, _, _ in flood_components(grid, tw, th, False):
        if len(cells) <= SMALL_WATER_MAX:
            set_cells(grid, cells, True)
            filled += len(cells)
    return filled


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
            chrome_votes = 0
            for sy in range(STEP):
                for sx in range(STEP):
                    x, y = tx * STEP + sx, ty * STEP + sy
                    if x >= w or y >= h:
                        continue
                    if is_map_chrome(x, y, w, h):
                        chrome_votes += 1
                        continue
                    r, g, b, a = px[x, y]
                    if is_water(r, g, b, a):
                        water_votes += 1
                    else:
                        land_votes += 1

            if chrome_votes > 0:
                row.append(False)
            else:
                row.append(land_votes >= water_votes)
        grid.append(row)

    labels_removed = remove_label_islands(grid, tw, th)
    title_removed = trim_title_letters(grid, tw, th)
    water_filled = fill_small_water_pockets(grid, tw, th)

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
    print(
        f"grid {tw}x{th} land={land} water={tw * th - land} "
        f"labels_removed={labels_removed} title_trim={title_removed} "
        f"water_filled={water_filled} -> {OUT}"
    )

    import subprocess
    import sys
    elev_script = ROOT / "scripts" / "generate_world_elevation.py"
    if elev_script.exists():
        subprocess.run([sys.executable, str(elev_script)], check=False)


if __name__ == "__main__":
    main()
