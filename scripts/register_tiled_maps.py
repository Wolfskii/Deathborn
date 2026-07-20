"""Register Tiled (.tmx) maps in Content/Maps/manifest.json.

Maps load at runtime from Content/Maps/ — no MGCB rebuild needed after editing in Tiled.

Usage:
  python scripts/register_tiled_maps.py
  python scripts/register_tiled_maps.py --map Maps/dungeons/my_room.tmx --id my_room --title "My Room"
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "client" / "Deathborn.Client" / "Content"
MANIFEST = CONTENT / "Maps" / "manifest.json"


def normalize_content_path(path: Path) -> str:
    rel = path.relative_to(CONTENT).as_posix()
    return rel


def load_manifest() -> dict:
    if MANIFEST.exists():
        return json.loads(MANIFEST.read_text(encoding="utf-8"))
    return {"maps": []}


def save_manifest(data: dict) -> None:
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def register_map(
    tmx_rel: str,
    map_id: str | None = None,
    title: str | None = None,
    tags: list[str] | None = None,
) -> None:
    tmx_path = CONTENT / tmx_rel.replace("/", "\\")
    if not tmx_path.exists():
        raise FileNotFoundError(tmx_path)

    content_path = normalize_content_path(tmx_path)
    stem = tmx_path.stem
    map_id = map_id or stem
    title = title or stem.replace("_", " ").title()
    tags = tags or ["dungeon"]

    manifest = load_manifest()
    maps: list[dict] = manifest.setdefault("maps", [])
    content_key = content_path.rsplit(".", 1)[0]
    entry = {
        "id": map_id,
        "contentPath": content_key,
        "title": title,
        "tags": tags,
    }
    maps[:] = [m for m in maps if m.get("id") != map_id]
    maps.append(entry)
    save_manifest(manifest)
    print(f"Registered {content_path} as '{map_id}'")


def register_all() -> None:
    for tmx in sorted((CONTENT / "Maps").rglob("*.tmx")):
        register_map(normalize_content_path(tmx))


def main() -> None:
    parser = argparse.ArgumentParser(description="Register Tiled maps in manifest.json")
    parser.add_argument("--map", help="Content-relative .tmx path, e.g. Maps/dungeons/room.tmx")
    parser.add_argument("--id", help="Map id for manifest.json")
    parser.add_argument("--title", help="Display title")
    parser.add_argument("--tag", action="append", dest="tags", help="Manifest tag (repeatable)")
    args = parser.parse_args()

    if args.map:
        register_map(args.map, map_id=args.id, title=args.title, tags=args.tags)
    else:
        register_all()


if __name__ == "__main__":
    main()
