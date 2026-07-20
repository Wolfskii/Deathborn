"""Shared load/save for Swarovia mainland REAL v1 collision and ELEV v2 elevation grids."""

from __future__ import annotations

import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COLLISION = ROOT / "shared" / "world" / "swarovia_mainland_collision.bin"
ELEVATION = ROOT / "shared" / "world" / "swarovia_mainland_elevation.bin"
SERVER_COLLISION = ROOT / "server" / "internal" / "worldmap" / "swarovia_mainland_collision.bin"
SERVER_ELEVATION = ROOT / "server" / "internal" / "worldmap" / "swarovia_mainland_elevation.bin"

TILE_SIZE = 32.0
RAMP_NONE = 0

# Elevation (-1..4) <-> Tiled tile index (0..5) / GID (index + 1)
ELEVATION_LEVELS: tuple[int, ...] = (-1, 0, 1, 2, 3, 4)
ELEVATION_NAMES: dict[int, str] = {
    -1: "water",
    0: "shore",
    1: "land_1",
    2: "land_2",
    3: "land_3",
    4: "land_4",
}
ELEVATION_COLORS: dict[int, tuple[int, int, int]] = {
    -1: (26, 80, 150),
    0: (196, 165, 116),
    1: (90, 158, 75),
    2: (74, 138, 61),
    3: (61, 114, 48),
    4: (45, 85, 36),
}

ELEV_TO_TILE_INDEX = {e: i for i, e in enumerate(ELEVATION_LEVELS)}
TILE_INDEX_TO_ELEV = {i: e for i, e in enumerate(ELEVATION_LEVELS)}


def load_collision(path: Path = COLLISION) -> tuple[int, int, float, list[list[bool]]]:
    data = path.read_bytes()
    if data[:4] != b"REAL" or data[4] != 1:
        raise ValueError(f"expected REAL v1 collision file: {path}")
    tw, th = struct.unpack_from("<HH", data, 5)
    tile_size = struct.unpack_from("<f", data, 9)[0]
    walkable: list[list[bool]] = []
    off = 13
    for _ in range(th):
        row = []
        for _ in range(tw):
            row.append(data[off] != 0)
            off += 1
        walkable.append(row)
    return tw, th, tile_size, walkable


def write_collision(path: Path, tw: int, th: int, walkable: list[list[bool]], tile_size: float = TILE_SIZE) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as f:
        f.write(b"REAL")
        f.write(struct.pack("<BHHf", 1, tw, th, tile_size))
        for row in walkable:
            for cell in row:
                f.write(struct.pack("B", 1 if cell else 0))


def load_elevation(path: Path = ELEVATION) -> tuple[int, int, list[list[int]], list[list[int]]]:
    data = path.read_bytes()
    if data[:4] != b"ELEV" or data[4] != 2:
        raise ValueError(f"expected ELEV v2 elevation file: {path}")
    tw, th = struct.unpack_from("<HH", data, 5)
    count = tw * th
    off = 7
    flat_elev = list(struct.unpack(f"<{count}b", data[off : off + count]))
    off += count
    flat_ramps = list(struct.unpack(f"<{count}B", data[off : off + count]))
    elev: list[list[int]] = []
    ramps: list[list[int]] = []
    for ty in range(th):
        elev.append([flat_elev[ty * tw + tx] for tx in range(tw)])
        ramps.append([flat_ramps[ty * tw + tx] for tx in range(tw)])
    return tw, th, elev, ramps


def write_elevation(path: Path, tw: int, th: int, elev: list[list[int]], ramps: list[list[int]]) -> None:
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


def sync_server_copies() -> None:
    SERVER_COLLISION.parent.mkdir(parents=True, exist_ok=True)
    SERVER_ELEVATION.parent.mkdir(parents=True, exist_ok=True)
    SERVER_COLLISION.write_bytes(COLLISION.read_bytes())
    SERVER_ELEVATION.write_bytes(ELEVATION.read_bytes())


def elevation_from_walkable_and_elev(walkable: list[list[bool]], elev: list[list[int]]) -> None:
    """Ensure water cells use elevation -1 and match walkability."""
    th = len(elev)
    tw = len(elev[0]) if th else 0
    for ty in range(th):
        for tx in range(tw):
            if not walkable[ty][tx]:
                elev[ty][tx] = -1


def walkable_from_elevation(elev: list[list[int]]) -> list[list[bool]]:
    return [[cell >= 0 for cell in row] for row in elev]
