"""Build walkability grid from shared/world/realik_reference.png."""

from __future__ import annotations

import struct
from collections import deque
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "shared" / "world" / "realik_reference.png"
OUT = ROOT / "shared" / "world" / "swarovia_mainland_collision.bin"
PREVIEW = ROOT / "shared" / "world" / "swarovia_mainland_preview.png"
SERVER_COPY = ROOT / "server" / "internal" / "worldmap" / "swarovia_mainland_collision.bin"

STEP = 1
TILE_SIZE = 16.0
SMALL_WATER_MAX = 36  # fill water pockets with <= this many 16px tiles
# Town icon art leaves compact land specks and enclosed water holes on the reference map.
TOWN_MARKER_LAND_MAX = 480
TOWN_MARKER_DIMENSION_MAX = 28
ENCLOSED_WATER_MAX = 480


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


def remove_town_marker_islands(grid: list[list[bool]], tw: int, th: int) -> int:
    """Remove compact land blobs from town icon circles on the reference art."""
    removed = 0
    for cells, min_tx, min_ty, max_tx, max_ty in flood_components(grid, tw, th, True):
        w = max_tx - min_tx + 1
        h = max_ty - min_ty + 1
        size = len(cells)
        aspect = max(w, h) / max(1, min(w, h))
        if (
            size <= TOWN_MARKER_LAND_MAX
            and max(w, h) <= TOWN_MARKER_DIMENSION_MAX
            and aspect <= 1.5
        ):
            set_cells(grid, cells, False)
            removed += size
    return removed


def mark_open_water(grid: list[list[bool]], tw: int, th: int) -> list[list[bool]]:
    """Water connected to the map border (ocean), not enclosed lakes."""
    open_water = [[False] * tw for _ in range(th)]
    q: deque[tuple[int, int]] = deque()
    for ty in range(th):
        for tx in range(tw):
            if grid[ty][tx]:
                continue
            on_border = tx == 0 or ty == 0 or tx == tw - 1 or ty == th - 1
            if on_border and not open_water[ty][tx]:
                open_water[ty][tx] = True
                q.append((tx, ty))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if (
                0 <= nx < tw
                and 0 <= ny < th
                and not grid[ny][nx]
                and not open_water[ny][nx]
            ):
                open_water[ny][nx] = True
                q.append((nx, ny))
    return open_water


def fill_enclosed_water_pockets(grid: list[list[bool]], tw: int, th: int) -> int:
    """Fill water fully surrounded by land (town icon interiors on the art)."""
    open_water = mark_open_water(grid, tw, th)
    filled = 0
    seen = [[False] * tw for _ in range(th)]
    for ty in range(th):
        for tx in range(tw):
            if seen[ty][tx] or grid[ty][tx] or open_water[ty][tx]:
                continue
            q: deque[tuple[int, int]] = deque([(tx, ty)])
            seen[ty][tx] = True
            cells: list[tuple[int, int]] = []
            while q:
                x, y = q.popleft()
                cells.append((x, y))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if not (0 <= nx < tw and 0 <= ny < th):
                        continue
                    if seen[ny][nx] or grid[ny][nx] or open_water[ny][nx]:
                        continue
                    seen[ny][nx] = True
                    q.append((nx, ny))
            if len(cells) <= ENCLOSED_WATER_MAX:
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
    town_removed = remove_town_marker_islands(grid, tw, th)
    water_filled = fill_small_water_pockets(grid, tw, th)
    enclosed_filled = fill_enclosed_water_pockets(grid, tw, th)

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
        f"town_markers_removed={town_removed} water_filled={water_filled} "
        f"enclosed_water_filled={enclosed_filled} -> {OUT}"
    )

    import subprocess
    import sys
    elev_script = ROOT / "scripts" / "generate_world_elevation.py"
    if elev_script.exists():
        subprocess.run([sys.executable, str(elev_script)], check=False)


if __name__ == "__main__":
    main()
