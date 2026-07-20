"""Generate per-tile elevation grid for Realik (Tiny Swords terrain system).

Reads shared/world/swarovia_mainland_collision.bin and writes swarovia_mainland_elevation.bin (ELEV v2).
See .tile_debug/tinyswords_guide.json for placement rules.
"""

from __future__ import annotations

import argparse
import random
import struct
from collections import deque
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COLLISION = ROOT / "shared" / "world" / "swarovia_mainland_collision.bin"
OUT = ROOT / "shared" / "world" / "swarovia_mainland_elevation.bin"
SERVER_COPY = ROOT / "server" / "internal" / "worldmap" / "swarovia_mainland_elevation.bin"

SEED = 0x0EA11C
MIN_PLATEAU_CELLS = 40
PLATEAU_COUNT = 5
PLATEAU_RADIUS = (22, 38)
MIN_RAMP_SEPARATION = 8
MIN_CLIFF_GAP_ROWS = 4

RAMP_NONE = 0
RAMP_LEFT = 1
RAMP_RIGHT = 2


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


def autopad(elev: list[list[int]], ramps: list[list[int]], tw: int, th: int) -> None:
    """Ensure land-to-land south drops are at most one elevation step (south demotion only)."""
    changed = True
    while changed:
        changed = False
        for ty in range(th):
            for tx in range(tw):
                if ramps[ty][tx] != RAMP_NONE:
                    continue
                e = elev[ty][tx]
                if e <= 0:
                    continue
                if ty + 1 < th:
                    s = elev[ty + 1][tx]
                    if 0 <= s < e - 1:
                        elev[ty + 1][tx] = e - 1
                        changed = True


def has_south_drop(elev: list[list[int]], walkable: list[list[bool]], tx: int, ty: int, tw: int, th: int) -> bool:
    if not (0 <= tx < tw and 0 <= ty < th):
        return False
    if not walkable[ty][tx]:
        return False
    e = elev[ty][tx]
    if e <= 0:
        return False
    if ty + 1 >= th:
        return False
    s = elev[ty + 1][tx]
    return s >= 0 and e > s


def is_south_lip(elev: list[list[int]], tx: int, ty: int, th: int) -> bool:
    if elev[ty][tx] <= 0:
        return False
    if ty + 1 >= th:
        return True
    return elev[ty][tx] > elev[ty + 1][tx]


def collapse_cliff_base_ledges(
    elev: list[list[int]], walkable: list[list[bool]], tw: int, th: int
) -> None:
    """Remove 1-cell ledges that are both a cliff base (north higher) and a lip (south lower)."""
    changed = True
    while changed:
        changed = False
        for ty in range(1, th):
            for tx in range(tw):
                if not walkable[ty][tx]:
                    continue
                e = elev[ty][tx]
                if e <= 0:
                    continue
                north = elev[ty - 1][tx]
                south = elev[ty + 1][tx] if ty + 1 < th else -1
                if north <= e:
                    continue
                if south < 0 or south < e:
                    target = 0 if south < 0 else south
                    if e != target:
                        elev[ty][tx] = target
                        changed = True


def enforce_cliff_column_spacing(elev: list[list[int]], tw: int, th: int, min_gap: int) -> None:
    """Keep south-facing cliff lips in the same column at least min_gap rows apart."""
    changed = True
    while changed:
        changed = False
        for tx in range(tw):
            lips: list[int] = []
            for ty in range(th):
                if is_south_lip(elev, tx, ty, th):
                    lips.append(ty)
            for i in range(len(lips) - 1):
                if lips[i + 1] - lips[i] < min_gap:
                    south_ty = lips[i + 1] + 1
                    target = elev[south_ty][tx] if south_ty < th else -1
                    if target < 0:
                        target = 0
                    for ty in range(lips[i] + 1, lips[i + 1] + 1):
                        if elev[ty][tx] > target:
                            elev[ty][tx] = target
                            changed = True
                    break


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

        placed += 1
    return placed


def region_size(
    elev: list[list[int]], walkable: list[list[bool]], start_tx: int, start_ty: int, tier: int, tw: int, th: int
) -> int:
    if not (0 <= start_tx < tw and 0 <= start_ty < th):
        return 0
    if not walkable[start_ty][start_tx] or elev[start_ty][start_tx] != tier:
        return 0
    seen = {(start_tx, start_ty)}
    q: deque[tuple[int, int]] = deque([(start_tx, start_ty)])
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if (nx, ny) in seen:
                continue
            if not (0 <= nx < tw and 0 <= ny < th):
                continue
            if not walkable[ny][nx] or elev[ny][nx] != tier:
                continue
            seen.add((nx, ny))
            q.append((nx, ny))
    return len(seen)


def chebyshev(ax: int, ay: int, bx: int, by: int) -> int:
    return max(abs(ax - bx), abs(ay - by))


def apron_ok_left(elev: list[list[int]], walkable: list[list[bool]], tx: int, ty: int, landing: int, tw: int, th: int) -> bool:
    for dx, dy in ((-1, 0), (-2, 0), (-1, 1), (-2, 1), (0, 1)):
        nx, ny = tx + dx, ty + dy
        if not (0 <= nx < tw and 0 <= ny < th):
            return False
        if not walkable[ny][nx] or elev[ny][nx] != landing:
            return False
    return True


def apron_ok_right(elev: list[list[int]], walkable: list[list[bool]], tx: int, ty: int, landing: int, tw: int, th: int) -> bool:
    for dx, dy in ((1, 0), (2, 0), (1, 1), (2, 1), (0, 1)):
        nx, ny = tx + dx, ty + dy
        if not (0 <= nx < tw and 0 <= ny < th):
            return False
        if not walkable[ny][nx] or elev[ny][nx] != landing:
            return False
    return True


FOLIAGE_SEED = 0xB00B5
FOLIAGE_LAND_STRIDE = 2


def foliage_hash(tx: int, ty: int, salt: int) -> int:
    h = (FOLIAGE_SEED ^ (tx * 73856093) ^ (ty * 19349663) ^ (salt * 83492791)) & 0xFFFFFFFF
    h ^= (h >> 16) & 0xFFFF
    h = (h * 0x85EBCA6B) & 0xFFFFFFFF
    h ^= (h >> 13) & 0x1FFF
    h = (h * 0xC2B2AE35) & 0xFFFFFFFF
    h ^= (h >> 16) & 0xFFFF
    return h


def _is_inland(walkable: list[list[bool]], tx: int, ty: int, tw: int, th: int) -> bool:
    for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
        nx, ny = tx + dx, ty + dy
        if not (0 <= nx < tw and 0 <= ny < th) or not walkable[ny][nx]:
            return False
    return True


def would_spawn_blocking_foliage(
    tx: int, ty: int, elev: list[list[int]], walkable: list[list[bool]], tw: int, th: int
) -> bool:
    """Matches server/client foliage placement for trees and rocks (movement blockers)."""
    if tx % FOLIAGE_LAND_STRIDE != 0 or ty % FOLIAGE_LAND_STRIDE != 0:
        return False
    if not (0 <= tx < tw and 0 <= ty < th):
        return False
    if not walkable[ty][tx] or elev[ty][tx] < 1:
        return False
    if foliage_hash(tx, ty, 1) % 1000 < 16 and _is_inland(walkable, tx, ty, tw, th):
        return True
    return foliage_hash(tx, ty, 2) % 1000 < 12


def _ramp_foliage_clear(
    tx: int, ty: int, elev: list[list[int]], walkable: list[list[bool]], tw: int, th: int
) -> bool:
    for nx in range(tx - 2, tx + 5):
        for ny in range(ty - 3, ty + 2):
            if would_spawn_blocking_foliage(nx, ny, elev, walkable, tw, th):
                return False
    return True


def ramp_clear_left(
    elev: list[list[int]],
    walkable: list[list[bool]],
    sea: list[list[bool]],
    tx: int,
    ty: int,
    tw: int,
    th: int,
) -> bool:
    landing = elev[ty][tx]
    platform = landing + 1
    footprint = ((tx, ty), (tx, ty - 1), (tx + 1, ty - 1))
    for nx, ny in footprint:
        if not (0 <= nx < tw and 0 <= ny < th):
            return False
        if not walkable[ny][nx] or elev[ny][nx] < 0 or sea[ny][nx]:
            return False
    for dx in (1, 2, 3):
        nx, ny = tx + dx, ty - 1
        if not walkable[ny][nx] or elev[ny][nx] != platform or sea[ny][nx]:
            return False
    if not walkable[ty + 1][tx] or elev[ty + 1][tx] != landing:
        return False
    if 0 <= tx - 1 < tw and walkable[ty - 1][tx - 1] and elev[ty - 1][tx - 1] > platform:
        return False
    if ty >= 2 and walkable[ty - 2][tx] and elev[ty - 2][tx] > platform:
        return False
    return _ramp_foliage_clear(tx, ty, elev, walkable, tw, th)


def ramp_clear_right(
    elev: list[list[int]],
    walkable: list[list[bool]],
    sea: list[list[bool]],
    tx: int,
    ty: int,
    tw: int,
    th: int,
) -> bool:
    landing = elev[ty][tx]
    platform = landing + 1
    footprint = ((tx, ty), (tx, ty - 1), (tx - 1, ty - 1))
    for nx, ny in footprint:
        if not (0 <= nx < tw and 0 <= ny < th):
            return False
        if not walkable[ny][nx] or elev[ny][nx] < 0 or sea[ny][nx]:
            return False
    for dx in (1, 2, 3):
        nx, ny = tx - dx, ty - 1
        if not walkable[ny][nx] or elev[ny][nx] != platform or sea[ny][nx]:
            return False
    if not walkable[ty + 1][tx] or elev[ty + 1][tx] != landing:
        return False
    if 0 <= tx + 1 < tw and walkable[ty - 1][tx + 1] and elev[ty - 1][tx + 1] > platform:
        return False
    if ty >= 2 and walkable[ty - 2][tx] and elev[ty - 2][tx] > platform:
        return False
    return _ramp_foliage_clear(tx, ty, elev, walkable, tw, th)


def try_left_ramp(
    elev: list[list[int]], walkable: list[list[bool]], sea: list[list[bool]], tx: int, ty: int, tw: int, th: int
) -> bool:
    if ty < 1:
        return False
    if not walkable[ty][tx]:
        return False
    landing = elev[ty][tx]
    if landing < 0:
        return False
    px, py = tx + 1, ty - 1
    if not (0 <= px < tw and 0 <= py < th):
        return False
    if not walkable[py][px] or elev[py][px] != landing + 1:
        return False
    if not has_south_drop(elev, walkable, px, py, tw, th):
        return False
    if not apron_ok_left(elev, walkable, tx, ty, landing, tw, th):
        return False
    return ramp_clear_left(elev, walkable, sea, tx, ty, tw, th)


def try_right_ramp(
    elev: list[list[int]], walkable: list[list[bool]], sea: list[list[bool]], tx: int, ty: int, tw: int, th: int
) -> bool:
    if ty < 1:
        return False
    if not walkable[ty][tx]:
        return False
    landing = elev[ty][tx]
    if landing < 0:
        return False
    px, py = tx - 1, ty - 1
    if not (0 <= px < tw and 0 <= py < th):
        return False
    if not walkable[py][px] or elev[py][px] != landing + 1:
        return False
    if not has_south_drop(elev, walkable, px, py, tw, th):
        return False
    if not apron_ok_right(elev, walkable, tx, ty, landing, tw, th):
        return False
    return ramp_clear_right(elev, walkable, sea, tx, ty, tw, th)


def place_ramps(
    elev: list[list[int]], walkable: list[list[bool]], sea: list[list[bool]], tw: int, th: int
) -> list[list[int]]:
    ramps = [[RAMP_NONE] * tw for _ in range(th)]
    candidates: list[tuple[int, int, int, int]] = []

    for ty in range(1, th):
        for tx in range(tw):
            if try_left_ramp(elev, walkable, sea, tx, ty, tw, th):
                landing = elev[ty][tx]
                px, py = tx + 1, ty - 1
                if region_size(elev, walkable, px, py, landing + 1, tw, th) >= MIN_PLATEAU_CELLS:
                    candidates.append((RAMP_LEFT, tx, ty, landing + 1))
            if try_right_ramp(elev, walkable, sea, tx, ty, tw, th):
                landing = elev[ty][tx]
                px, py = tx - 1, ty - 1
                if region_size(elev, walkable, px, py, landing + 1, tw, th) >= MIN_PLATEAU_CELLS:
                    candidates.append((RAMP_RIGHT, tx, ty, landing + 1))

    # Prefer lower tiers first so base ground gets ramps before higher plateaus.
    candidates.sort(key=lambda c: (c[3], c[2], c[1]))

    placed: list[tuple[int, int]] = []
    covered_regions: set[tuple[int, int, int]] = set()

    for kind, tx, ty, platform_tier in candidates:
        if any(chebyshev(tx, ty, px, py) < MIN_RAMP_SEPARATION for px, py in placed):
            continue
        px, py = (tx + 1, ty - 1) if kind == RAMP_LEFT else (tx - 1, ty - 1)
        region_key = (px, py, platform_tier)
        if region_key in covered_regions:
            continue
        ramps[ty][tx] = kind
        placed.append((tx, ty))
        covered_regions.add(region_key)

    return ramps


def write_elevation(
    path: Path, tw: int, th: int, elev: list[list[int]], ramps: list[list[int]]
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as f:
        f.write(b"ELEV")
        f.write(struct.pack("<BHH", 2, tw, th))
        for ty in range(th):
            for tx in range(tw):
                f.write(struct.pack("<b", elev[ty][tx]))
        for ty in range(th):
            for tx in range(tw):
                f.write(struct.pack("<B", ramps[ty][tx]))


def refine_painted_elevation(
    elev: list[list[int]], walkable: list[list[bool]], tw: int, th: int
) -> tuple[list[list[int]], list[list[int]]]:
    """After Tiled import: fix cliff steps and place ramps without rerolling plateaus."""
    ramps = [[RAMP_NONE] * tw for _ in range(th)]
    sea = flood_sea(walkable, tw, th)
    autopad(elev, ramps, tw, th)
    collapse_cliff_base_ledges(elev, walkable, tw, th)
    enforce_cliff_column_spacing(elev, tw, th, MIN_CLIFF_GAP_ROWS)
    ramps = place_ramps(elev, walkable, sea, tw, th)
    return elev, ramps


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate or refine Realik elevation grid")
    parser.add_argument(
        "--preserve-elev",
        action="store_true",
        help="Keep elevation from swarovia_mainland_elevation.bin (after Tiled import); only refine cliffs/ramps",
    )
    args = parser.parse_args()

    tw, th, tile_size, walkable = load_collision(COLLISION)

    if args.preserve_elev:
        from swarovia_mainland_world_io import load_elevation as load_elev_bins

        tw, th, elev, _ = load_elev_bins(OUT)
        if tw != len(walkable[0]) or th != len(walkable):
            raise SystemExit("collision and elevation grid size mismatch")
        elev, ramps = refine_painted_elevation(elev, walkable, tw, th)
        write_elevation(OUT, tw, th, elev, ramps)
        SERVER_COPY.parent.mkdir(parents=True, exist_ok=True)
        SERVER_COPY.write_bytes(OUT.read_bytes())
        ramp_count = sum(1 for row in ramps for v in row if v != RAMP_NONE)
        hist: dict[int, int] = {}
        for row in elev:
            for v in row:
                hist[v] = hist.get(v, 0) + 1
        print(
            f"refined painted elevation {tw}x{th} tile={tile_size} "
            f"ramps={ramp_count} hist={dict(sorted(hist.items()))}"
        )
        print(f"-> {OUT}")
        return

    elev = [[-1 if not walkable[ty][tx] else 1 for tx in range(tw)] for ty in range(th)]
    ramps = [[RAMP_NONE] * tw for _ in range(th)]

    sea = flood_sea(walkable, tw, th)
    mark_sea_shoreline(elev, walkable, sea, tw, th)

    rng = random.Random(SEED)
    plateaus = place_plateaus(elev, walkable, sea, tw, th, rng)
    autopad(elev, ramps, tw, th)
    collapse_cliff_base_ledges(elev, walkable, tw, th)
    enforce_cliff_column_spacing(elev, tw, th, MIN_CLIFF_GAP_ROWS)
    ramps = place_ramps(elev, walkable, sea, tw, th)

    write_elevation(OUT, tw, th, elev, ramps)
    SERVER_COPY.parent.mkdir(parents=True, exist_ok=True)
    SERVER_COPY.write_bytes(OUT.read_bytes())

    hist: dict[int, int] = {}
    for row in elev:
        for v in row:
            hist[v] = hist.get(v, 0) + 1
    ramp_count = sum(1 for row in ramps for v in row if v != RAMP_NONE)
    print(
        f"elevation grid {tw}x{th} tile={tile_size} plateaus={plateaus} "
        f"ramps={ramp_count} hist={dict(sorted(hist.items()))}"
    )
    print(f"-> {OUT}")


if __name__ == "__main__":
    main()
