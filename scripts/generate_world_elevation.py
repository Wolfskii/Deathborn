"""Generate per-tile elevation grid for Realik (Tiny Swords terrain system).

Reads shared/world/realik_collision.bin and writes realik_elevation.bin.
See .tile_debug/tinyswords_guide.json for placement rules.
"""

from __future__ import annotations

import random
import struct
from collections import deque
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COLLISION = ROOT / "shared" / "world" / "realik_collision.bin"
OUT = ROOT / "shared" / "world" / "realik_elevation.bin"
SERVER_COPY = ROOT / "server" / "internal" / "worldmap" / "realik_elevation.bin"

SEED = 0xREAL1C
MIN_PLATEAU_CELLS = 40
PLATEAU_COUNT = 5
PLATEAU_RADIUS = (22, 38)  # ~6-10 tiles at 64px; map cells are 16px


def load_collision(path: Path) -> tuple[int, int, float, list[list[bool]]]:
    data = path.read_bytes()
    if data[:4] != b"REAL" or data[4] != 1:
        raise ValueError("expected REAL v1 collision file")
    tw, th = struct.unpack_from("<HH", data, 5)
    tile_size = struct.unpack_from("<f", data, 9)[0]
    walkable = []
    off = 13
    for ty in range(th):
        row = []
        for tx in range(tw):
            row.append(data[off] != 0)
            off += 1
        walkable.append(row)
    return tw, th, tile_size, walkable


def flood_sea(walkable: list[list[bool]], tw: int, th: int) -> list[list[bool]]:
    """Water connected to the west map edge (the left sea)."""
    sea = [[False] * tw for _ in range(th)]
    q: deque[tuple[int, int]] = deque()
    for ty in range(th):
        for tx in range(tw):
            if tx == 0 and not walkable[ty][tx]:
                sea[ty][tx] = True
                q.append((tx, ty))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < tw and 0 <= ny < th and not walkable[ny][nx] and not sea[ny][nx]:
                sea[ny][nx] = True
                q.append((nx, ny))
    return sea


def neighbors8(tx: int, ty: int) -> list[tuple[int, int]]:
    out = []
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue
            out.append((tx + dx, ty + dy))
    return out


def mark_sea_shoreline(
    elev: list[list[int]], walkable: list[list[bool]], sea: list[list[bool]], tw: int, th: int
) -> None:
    for ty in range(th):
        for tx in range(tw):
            if not walkable[ty][tx]:
                continue
            for nx, ny in neighbors8(tx, ty):
                if 0 <= nx < tw and 0 <= ny < th and sea[ny][nx]:
                    elev[ty][tx] = 0
                    break


def autopad(elev: list[list[int]], tw: int, th: int) -> None:
    changed = True
    while changed:
        changed = False
        for ty in range(th):
            for tx in range(tw):
                e = elev[ty][tx]
                if e <= 0:
                    continue
                for nx, ny in ((tx, ty - 1), (tx - 1, ty), (tx + 1, ty)):
                    if 0 <= nx < tw and 0 <= ny < th:
                        k = elev[ny][nx]
                        if 0 <= k < e:
                            elev[ny][nx] = e
                            changed = True
                if ty + 1 < th:
                    s = elev[ty + 1][tx]
                    if 0 <= s < e - 1:
                        elev[ty + 1][tx] = e - 1
                        changed = True


def place_plateaus(
    elev: list[list[int]],
    walkable: list[list[bool]],
    sea: list[list[bool]],
    tw: int,
    th: int,
    rng: random.Random,
) -> int:
    placed = 0
    margin = 24
    tries = 0
    while placed < PLATEAU_COUNT and tries < 400:
        tries += 1
        cx = rng.randint(margin, tw - margin - 1)
        cy = rng.randint(margin, th - margin - 1)
        if not walkable[cy][cx] or elev[cy][cx] != 1:
            continue
        if any(
            sea[ny][nx]
            for nx, ny in neighbors8(cx, cy)
            if 0 <= nx < tw and 0 <= ny < th
        ):
            continue

        r = rng.randint(*PLATEAU_RADIUS)
        blob: list[tuple[int, int]] = []
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if dx * dx + dy * dy > r * r:
                    continue
                tx, ty = cx + dx, cy + dy
                if 0 <= tx < tw and 0 <= ty < th and walkable[ty][tx] and elev[ty][tx] == 1:
                    blob.append((tx, ty))
        if len(blob) < MIN_PLATEAU_CELLS:
            continue

        for tx, ty in blob:
            elev[ty][tx] = 2

        # Nested higher tiers, shifted slightly north.
        inner_r = max(8, r // 2)
        for tx, ty in blob:
            if (tx - cx) ** 2 + (ty - (cy - 6)) ** 2 <= inner_r * inner_r:
                elev[ty][tx] = 3
        core_r = max(5, inner_r // 2)
        for tx, ty in blob:
            if (tx - cx) ** 2 + (ty - (cy - 10)) ** 2 <= core_r * core_r:
                elev[ty][tx] = 4

        placed += 1
    return placed


def write_elevation(path: Path, tw: int, th: int, elev: list[list[int]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as f:
        f.write(b"ELEV")
        f.write(struct.pack("<BHH", 1, tw, th))
        for ty in range(th):
            for tx in range(tw):
                f.write(struct.pack("<b", elev[ty][tx]))


def main() -> None:
    tw, th, tile_size, walkable = load_collision(COLLISION)
    elev = [[-1 if not walkable[ty][tx] else 1 for tx in range(tw)] for ty in range(th)]

    sea = flood_sea(walkable, tw, th)
    mark_sea_shoreline(elev, walkable, sea, tw, th)

    rng = random.Random(SEED)
    plateaus = place_plateaus(elev, walkable, sea, tw, th, rng)
    autopad(elev, tw, th)

    write_elevation(OUT, tw, th, elev)
    SERVER_COPY.parent.mkdir(parents=True, exist_ok=True)
    SERVER_COPY.write_bytes(OUT.read_bytes())

    hist: dict[int, int] = {}
    for row in elev:
        for v in row:
            hist[v] = hist.get(v, 0) + 1
    print(f"elevation grid {tw}x{th} tile={tile_size} plateaus={plateaus} hist={dict(sorted(hist.items()))}")
    print(f"-> {OUT}")


if __name__ == "__main__":
    main()
