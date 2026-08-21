"""Regenerate overworld binaries when authored Tiled files changed."""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OVERWORLD = ROOT / "client" / "Deathborn.Client" / "Content" / "Maps" / "overworld"
WORLD_OUTPUTS = (
    ROOT / "shared" / "world" / "swarovia_mainland_collision.bin",
    ROOT / "shared" / "world" / "swarovia_mainland_elevation.bin",
)
WORLD_SOURCES = tuple(
    path
    for suffix in ("*.tmx", "*.tsx")
    for path in OVERWORLD.glob(suffix)
)


def needs_sync() -> bool:
    if not WORLD_SOURCES:
        return False
    if any(not path.exists() for path in WORLD_OUTPUTS):
        return True
    newest_source = max(path.stat().st_mtime_ns for path in WORLD_SOURCES)
    oldest_output = min(path.stat().st_mtime_ns for path in WORLD_OUTPUTS)
    return newest_source > oldest_output


def main() -> None:
    if not needs_sync():
        if os.environ.get("WORLD_SYNC_QUIET") != "1":
            print("World data is up to date.")
        return

    env = os.environ.copy()
    env["PYTHONDONTWRITEBYTECODE"] = "1"
    importer = ROOT / "scripts" / "import_tiled_overworld.py"
    subprocess.run([sys.executable, str(importer)], check=True, cwd=ROOT, env=env)


if __name__ == "__main__":
    main()
