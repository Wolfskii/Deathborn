"""Shared Farm RPG tileset helpers for Swarovia mainland Tiled export/import."""

from __future__ import annotations

import base64
import gzip
import re
import struct
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

from PIL import Image

# Layer class convention: ground_1 / water_1 = base height; _2 is one tier higher, etc.
LAYER_CLASS_RE = re.compile(r"^(ground|water)_(\d+)$", re.IGNORECASE)

TILE_ART_PX = 16
MAP_TILE_PX = 32
PLAIN_GRASS = (9, 2)
SHORE_CELL = (4, 8)

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "client" / "Deathborn.Client" / "Content"
FARMRPG = CONTENT / "Tiles" / "FarmRpg"
OVERWORLD = CONTENT / "Maps" / "overworld"
FARMRPG_REL = "../../Tiles/FarmRpg"
OVERWORLD_MAP_ID = "swarovia_mainland"
OVERWORLD_TMX_NAME = "swarovia_mainland.tmx"
REFERENCE_SRC = ROOT / "shared" / "world" / "realik_reference.png"
REFERENCE_NAME = "realik_reference.png"


@dataclass(frozen=True)
class TilesetSpec:
    tsx_name: str
    png_name: str
    tile_props: dict[int, dict[str, str]]

    @property
    def rel_image(self) -> str:
        return f"{FARMRPG_REL}/{self.png_name}"


@dataclass
class TilesetLayout:
    spec: TilesetSpec
    cols: int
    rows: int
    firstgid: int

    @property
    def tilecount(self) -> int:
        return self.cols * self.rows

    def gid(self, col: int, row: int) -> int:
        return self.firstgid + row * self.cols + col


@dataclass(frozen=True)
class ClassifiedLayer:
    """Tile layer tagged with ground_N / water_N for gameplay height."""

    name: str
    class_name: str
    kind: str  # "ground" | "water"
    level: int
    gids: list[list[int]]
    order: int  # document order (0 = bottom)


OVERWORLD_TILESETS: tuple[TilesetSpec, ...] = (
    TilesetSpec(
        "farmrpg_water.tsx",
        "water_fill.png",
        {0: {"walkable": "false", "elevation": "-1", "label": "water"}},
    ),
    TilesetSpec("farmrpg_grass_water.tsx", "grass_water.png", {}),
    TilesetSpec("farmrpg_grass_elev0.tsx", "grass_elev0.png", {}),
    TilesetSpec("farmrpg_grass_elev1.tsx", "grass_elev1.png", {}),
    TilesetSpec("farmrpg_grass_elev2.tsx", "grass_elev2.png", {}),
    TilesetSpec("farmrpg_grass_elev3.tsx", "grass_elev3.png", {}),
    TilesetSpec("farmrpg_grass_elev4.tsx", "grass_elev4.png", {}),
    TilesetSpec("farmrpg_cliff_elev0.tsx", "cliff_elev0.png", {}),
    TilesetSpec("farmrpg_cliff_elev1.tsx", "cliff_elev1.png", {}),
    TilesetSpec("farmrpg_cliff_elev2.tsx", "cliff_elev2.png", {}),
    TilesetSpec("farmrpg_cliff_elev3.tsx", "cliff_elev3.png", {}),
    TilesetSpec("farmrpg_cliff_elev4.tsx", "cliff_elev4.png", {}),
)


def _seed_props_for_grass_sheet(cols: int, elevation: int) -> dict[int, dict[str, str]]:
    tile_id = PLAIN_GRASS[1] * cols + PLAIN_GRASS[0]
    return {
        tile_id: {
            "walkable": "true",
            "elevation": str(elevation),
            "label": f"plain_grass_elev{elevation}",
        }
    }


def image_size(png_name: str) -> tuple[int, int]:
    path = FARMRPG / png_name
    if not path.exists():
        raise FileNotFoundError(path)
    with Image.open(path) as img:
        return img.size


def layout_tilesets(specs: tuple[TilesetSpec, ...]) -> list[TilesetLayout]:
    layouts: list[TilesetLayout] = []
    firstgid = 1
    for spec in specs:
        w, h = image_size(spec.png_name)
        cols = w // TILE_ART_PX
        rows = h // TILE_ART_PX
        layouts.append(TilesetLayout(spec, cols, rows, firstgid))
        firstgid += cols * rows
    return layouts


def seed_gid_for_elevation(layouts: list[TilesetLayout], elev: int) -> int:
    if elev == -1:
        return layouts[0].gid(0, 0)
    if elev == 0:
        return layouts[1].gid(*SHORE_CELL)
    if 1 <= elev <= 4:
        return layouts[1 + elev].gid(*PLAIN_GRASS)
    raise ValueError(f"unsupported elevation {elev}")


def merged_tile_props(layout: TilesetLayout) -> dict[int, dict[str, str]]:
    props = dict(layout.spec.tile_props)
    name = layout.spec.png_name
    if name == "grass_water.png":
        tile_id = SHORE_CELL[1] * layout.cols + SHORE_CELL[0]
        props[tile_id] = {"walkable": "true", "elevation": "0", "label": "shore"}
    elif name.startswith("grass_elev") and name.endswith(".png"):
        suffix = name.removeprefix("grass_elev").removesuffix(".png")
        if suffix.isdigit():
            props.update(_seed_props_for_grass_sheet(layout.cols, int(suffix)))
    return props


def write_tsx(out_dir: Path, layout: TilesetLayout) -> None:
    props_by_id = merged_tile_props(layout)
    w = layout.cols * TILE_ART_PX
    h = layout.rows * TILE_ART_PX
    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        f'<tileset version="1.10" tiledversion="1.11.2" name="{layout.spec.tsx_name.removesuffix(".tsx")}" '
        f'tilewidth="{TILE_ART_PX}" tileheight="{TILE_ART_PX}" '
        f'tilecount="{layout.tilecount}" columns="{layout.cols}">',
        f' <image source="{layout.spec.rel_image}" width="{w}" height="{h}"/>',
    ]
    for tile_id in sorted(props_by_id):
        lines.append(f' <tile id="{tile_id}">')
        lines.append("  <properties>")
        for key, value in props_by_id[tile_id].items():
            if key == "walkable":
                lines.append(f'   <property name="{key}" type="bool" value="{value}"/>')
            elif key == "elevation":
                lines.append(f'   <property name="{key}" type="int" value="{value}"/>')
            else:
                lines.append(f'   <property name="{key}" value="{value}"/>')
        lines.append("  </properties>")
        lines.append(" </tile>")
    lines.append("</tileset>")
    lines.append("")
    (out_dir / layout.spec.tsx_name).write_text("\n".join(lines), encoding="utf-8")


def ensure_farmrpg_tiles() -> None:
    missing = [spec.png_name for spec in OVERWORLD_TILESETS if not (FARMRPG / spec.png_name).exists()]
    if missing:
        raise SystemExit(
            "Missing Farm RPG tile PNGs:\n  "
            + "\n  ".join(missing)
            + "\nRun: python scripts/install_farm_rpg_terrain.py"
        )


def _parse_tileset_file(tsx_path: Path) -> tuple[int, dict[int, dict[str, str]]]:
    root = ET.parse(tsx_path).getroot()
    tilecount = int(root.get("tilecount", "0"))
    props: dict[int, dict[str, str]] = {}
    for tile_el in root.findall("tile"):
        tile_id = int(tile_el.get("id", "0"))
        tile_props: dict[str, str] = {}
        for prop in tile_el.findall("properties/property"):
            tile_props[prop.get("name", "")] = prop.get("value", "")
        props[tile_id] = tile_props
    return tilecount, props


def _default_props_for_tsx(path: Path) -> dict[str, str]:
    name = path.name.lower()
    if "water" in name and "grass" not in name:
        return {"walkable": "false", "elevation": "-1"}
    if "cliff" in name:
        return {"walkable": "false", "elevation": "1"}
    return {"walkable": "true", "elevation": "1"}


def load_gid_properties(tmx_path: Path) -> dict[int, dict[str, str]]:
    root = ET.parse(tmx_path).getroot()
    out: dict[int, dict[str, str]] = {}
    for ts_el in root.findall("tileset"):
        tsx_path = tmx_path
        if ts_el.get("source"):
            tsx_path = (tmx_path.parent / ts_el.get("source", "")).resolve()

        if ts_el.get("source"):
            tilecount, tile_props = _parse_tileset_file(tsx_path)
        else:
            tilecount = int(ts_el.get("tilecount", "0"))
            tile_props = {}
            for tile_el in ts_el.findall("tile"):
                tile_id = int(tile_el.get("id", "0"))
                props: dict[str, str] = {}
                for prop in tile_el.findall("properties/property"):
                    props[prop.get("name", "")] = prop.get("value", "")
                tile_props[tile_id] = props

        firstgid = int(ts_el.get("firstgid", "1"))
        defaults = _default_props_for_tsx(tsx_path)
        for tile_id, props in tile_props.items():
            out[firstgid + tile_id] = props
        for tile_id in range(tilecount):
            gid = firstgid + tile_id
            if gid not in out:
                out[gid] = dict(defaults)
    return out


def encode_layer_gzip(gids: list[list[int]]) -> str:
    """Tiled base64+gzip tile layer (uint32 LE gids, row-major)."""
    flat = [gid for row in gids for gid in row]
    raw = struct.pack(f"<{len(flat)}I", *flat)
    return base64.b64encode(gzip.compress(raw, compresslevel=9)).decode("ascii")


def decode_layer_data(data_el: ET.Element, tw: int, th: int) -> list[list[int]]:
    encoding = data_el.get("encoding")
    compression = data_el.get("compression")
    text = (data_el.text or "").strip()
    if not text:
        raise ValueError("layer data element is empty")

    if encoding == "base64":
        raw = base64.b64decode(text)
        if compression == "gzip":
            raw = gzip.decompress(raw)
        elif compression == "zlib":
            import zlib

            raw = zlib.decompress(raw)
        elif compression:
            raise ValueError(f"unsupported layer compression: {compression}")
        count = tw * th
        flat = struct.unpack(f"<{count}I", raw)
        return [list(flat[y * tw : (y + 1) * tw]) for y in range(th)]

    # CSV (legacy / hand-edited)
    rows = [line.strip() for line in text.splitlines() if line.strip()]
    if len(rows) != th:
        raise ValueError(f"expected {th} CSV rows, got {len(rows)}")
    gids: list[list[int]] = []
    for row in rows:
        cells = [c.strip() for c in row.split(",") if c.strip() != ""]
        if len(cells) != tw:
            raise ValueError(f"expected {tw} columns, got {len(cells)}")
        gids.append([int(c) for c in cells])
    return gids


def parse_layer_class(class_attr: str | None) -> tuple[str, int] | None:
    """Parse ground_N / water_N. Returns (kind, level) or None."""
    if not class_attr:
        return None
    match = LAYER_CLASS_RE.match(class_attr.strip())
    if not match:
        return None
    level = int(match.group(2))
    if level < 1:
        return None
    return match.group(1).lower(), level


def layer_class_name(kind: str, level: int) -> str:
    return f"{kind}_{level}"


def write_overworld_tmx(
    path: Path,
    tw: int,
    th: int,
    gids: list[list[int]],
    layouts: list[TilesetLayout],
    *,
    include_reference: bool = True,
) -> None:
    """Split a flat grass/water gid grid into Sea (water_1) + Land (ground_1)."""
    water_gid = seed_gid_for_elevation(layouts, -1)
    water = blank_layer(tw, th, water_gid)
    ground = [
        [0 if gid in (0, water_gid) else gid for gid in row]
        for row in gids
    ]
    write_overworld_tmx_layers(
        path,
        tw,
        th,
        {"Sea": water, "Land": ground},
        layouts,
        layer_order=("Sea", "Land"),
        layer_classes={"Sea": "water_1", "Land": "ground_1"},
        layer_locked={"Sea": True},
        include_reference=include_reference,
    )


def parse_tile_layers(tmx_path: Path) -> tuple[int, int, dict[str, list[list[int]]]]:
    root = ET.parse(tmx_path).getroot()
    tw = int(root.get("width", "0"))
    th = int(root.get("height", "0"))
    if tw <= 0 or th <= 0:
        raise ValueError(f"invalid map size in {tmx_path}")

    layers: dict[str, list[list[int]]] = {}
    for el in root.findall("layer"):
        name = el.get("name") or f"layer_{el.get('id', '0')}"
        data_el = el.find("data")
        if data_el is None:
            continue
        layers[name] = decode_layer_data(data_el, tw, th)
    if not layers:
        raise ValueError(f"no tile layers in {tmx_path}")
    return tw, th, layers


def parse_classified_tile_layers(tmx_path: Path) -> tuple[int, int, list[ClassifiedLayer]]:
    """Return tile layers that have a ground_N / water_N class, in document order."""
    root = ET.parse(tmx_path).getroot()
    tw = int(root.get("width", "0"))
    th = int(root.get("height", "0"))
    if tw <= 0 or th <= 0:
        raise ValueError(f"invalid map size in {tmx_path}")

    classified: list[ClassifiedLayer] = []
    for order, el in enumerate(root.findall("layer")):
        parsed = parse_layer_class(el.get("class"))
        if parsed is None:
            continue
        kind, level = parsed
        data_el = el.find("data")
        if data_el is None:
            continue
        name = el.get("name") or f"layer_{el.get('id', '0')}"
        class_name = layer_class_name(kind, level)
        classified.append(
            ClassifiedLayer(
                name=name,
                class_name=class_name,
                kind=kind,
                level=level,
                gids=decode_layer_data(data_el, tw, th),
                order=order,
            )
        )
    return tw, th, classified


def merge_layer_gids(
    layers: dict[str, list[list[int]]],
    *,
    ground: str = "Ground",
    water: str = "Water",
) -> list[list[int]]:
    """Legacy composite: ground tiles win; empty cells use water (by layer name)."""
    if ground not in layers:
        # Common rename: Land / Sea
        if "Land" in layers:
            ground = "Land"
        else:
            raise ValueError(f"layer '{ground}' not found")
    if water not in layers and "Sea" in layers:
        water = "Sea"
    ground_gids = layers[ground]
    th = len(ground_gids)
    tw = len(ground_gids[0])
    water_gids = layers.get(water)
    merged: list[list[int]] = []
    for ty in range(th):
        row: list[int] = []
        for tx in range(tw):
            gid = ground_gids[ty][tx]
            if gid == 0 and water_gids is not None:
                gid = water_gids[ty][tx]
            row.append(gid)
        merged.append(row)
    return merged


def _classified_cell_wins(challenger: ClassifiedLayer, incumbent: ClassifiedLayer | None) -> bool:
    if incumbent is None:
        return True
    if challenger.level != incumbent.level:
        return challenger.level > incumbent.level
    if challenger.kind != incumbent.kind:
        return challenger.kind == "ground"
    return challenger.order > incumbent.order


def gameplay_from_classified_layers(
    layers: list[ClassifiedLayer],
    gid_props: dict[int, dict[str, str]],
) -> tuple[list[list[bool]], list[list[int]]]:
    """Derive walkability + elevation from ground_N / water_N layer classes.

    Per cell, the winning painted tile is the highest level; at the same level
    ground beats water; later document order breaks remaining ties.
    water_N keeps elevation N (non-walkable) for elevated lakes / future swim.
    Empty cells are ocean: non-walkable, elevation -1.
    """
    if not layers:
        raise ValueError("no classified ground_N / water_N layers")

    th = len(layers[0].gids)
    tw = len(layers[0].gids[0])
    walkable: list[list[bool]] = []
    elev: list[list[int]] = []

    for ty in range(th):
        w_row: list[bool] = []
        e_row: list[int] = []
        for tx in range(tw):
            winner: ClassifiedLayer | None = None
            win_gid = 0
            for layer in layers:
                gid = layer.gids[ty][tx]
                if gid == 0:
                    continue
                if _classified_cell_wins(layer, winner):
                    winner = layer
                    win_gid = gid
            if winner is None:
                w_row.append(False)
                e_row.append(-1)
                continue

            if winner.kind == "water":
                w_row.append(False)
                e_row.append(winner.level)
                continue

            props = gid_props.get(win_gid, {})
            if "walkable" in props:
                walk = props["walkable"].lower() in {"true", "1", "yes"}
            else:
                walk = True
            w_row.append(walk)
            e_row.append(winner.level)
        walkable.append(w_row)
        elev.append(e_row)
    return walkable, elev


def content_bbox(gids: list[list[int]]) -> tuple[int, int, int, int] | None:
    min_x = min_y = 10**9
    max_x = max_y = -1
    for ty, row in enumerate(gids):
        for tx, gid in enumerate(row):
            if gid == 0:
                continue
            min_x = min(min_x, tx)
            min_y = min(min_y, ty)
            max_x = max(max_x, tx)
            max_y = max(max_y, ty)
    if max_x < 0:
        return None
    return min_x, min_y, max_x, max_y


def crop_gids(gids: list[list[int]], min_x: int, min_y: int, max_x: int, max_y: int) -> list[list[int]]:
    return [row[min_x : max_x + 1] for row in gids[min_y : max_y + 1]]


def scale_gids_nearest(gids: list[list[int]], factor: int) -> list[list[int]]:
    if factor <= 1:
        return [row[:] for row in gids]
    out_h = len(gids) * factor
    out_w = len(gids[0]) * factor
    out = [[0] * out_w for _ in range(out_h)]
    for sy, row in enumerate(gids):
        for sx, gid in enumerate(row):
            if gid == 0:
                continue
            for dy in range(factor):
                for dx in range(factor):
                    out[sy * factor + dy][sx * factor + dx] = gid
    return out


def blank_layer(tw: int, th: int, fill: int = 0) -> list[list[int]]:
    return [[fill] * tw for _ in range(th)]


def blit_layer(
    dest: list[list[int]],
    src: list[list[int]],
    offset_x: int,
    offset_y: int,
    *,
    overwrite_empty_only: bool = False,
) -> None:
    for sy, row in enumerate(src):
        dy = offset_y + sy
        if dy < 0 or dy >= len(dest):
            continue
        for sx, gid in enumerate(row):
            if gid == 0:
                continue
            dx = offset_x + sx
            if dx < 0 or dx >= len(dest[0]):
                continue
            if overwrite_empty_only and dest[dy][dx] != 0:
                continue
            dest[dy][dx] = gid


def write_overworld_tmx_layers(
    path: Path,
    tw: int,
    th: int,
    layers: dict[str, list[list[int]]],
    layouts: list[TilesetLayout],
    *,
    layer_order: tuple[str, ...] = ("Sea", "Land"),
    layer_classes: dict[str, str] | None = None,
    layer_locked: dict[str, bool] | None = None,
    include_reference: bool = False,
) -> None:
    """Write a multi-layer overworld TMX (gzip tile data per layer).

    Default classes: Sea=water_1, Land=ground_1. Pass layer_classes to override.
    """
    if layer_classes is None:
        layer_classes = {
            "Sea": "water_1",
            "Land": "ground_1",
            "Water": "water_1",
            "Ground": "ground_1",
        }
    if layer_locked is None:
        layer_locked = {}

    ref_w = ref_h = 0
    if include_reference and REFERENCE_SRC.exists():
        dest = path.parent / REFERENCE_NAME
        dest.write_bytes(REFERENCE_SRC.read_bytes())
        with Image.open(REFERENCE_SRC) as img:
            ref_w, ref_h = img.size

    next_layer_id = 1
    next_ids = len(layer_order) + (1 if ref_w else 0) + 1
    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        f'<map version="1.10" tiledversion="1.11.2" orientation="orthogonal" '
        f'renderorder="right-down" width="{tw}" height="{th}" '
        f'tilewidth="{MAP_TILE_PX}" tileheight="{MAP_TILE_PX}" infinite="0" '
        f'nextlayerid="{next_ids}" nextobjectid="1">',
        " <properties>",
        f'  <property name="map_id" value="{OVERWORLD_MAP_ID}"/>',
        '  <property name="title" value="Swarovia Mainland"/>',
        f'  <property name="tile_size" value="{MAP_TILE_PX}"/>',
        '  <property name="render_mode" value="painted"/>',
        f'  <property name="source" value="{OVERWORLD_TMX_NAME}"/>',
        " </properties>",
    ]
    for layout in layouts:
        lines.append(
            f' <tileset firstgid="{layout.firstgid}" source="{layout.spec.tsx_name}"/>'
        )

    for layer_name in layer_order:
        if layer_name not in layers:
            raise ValueError(f"missing layer '{layer_name}'")
        payload = encode_layer_gzip(layers[layer_name])
        attrs = [f'id="{next_layer_id}"', f'name="{layer_name}"']
        class_name = layer_classes.get(layer_name)
        if class_name:
            attrs.append(f'class="{class_name}"')
        attrs.append(f'width="{tw}"')
        attrs.append(f'height="{th}"')
        if layer_locked.get(layer_name):
            attrs.append('locked="1"')
        lines.extend(
            [
                f' <layer {" ".join(attrs)}>',
                f'  <data encoding="base64" compression="gzip">{payload}</data>',
                " </layer>",
            ]
        )
        next_layer_id += 1

    if ref_w:
        lines.extend(
            [
                f' <imagelayer id="{next_layer_id}" name="Reference" locked="1">',
                f'  <image source="{REFERENCE_NAME}" width="{ref_w}" height="{ref_h}"/>',
                " </imagelayer>",
            ]
        )

    lines.extend(["</map>", ""])
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines), encoding="utf-8")


def parse_tile_layer(
    tmx_path: Path,
    primary: str = "Land",
    fallback: str = "Ground",
) -> tuple[int, int, list[list[int]]]:
    root = ET.parse(tmx_path).getroot()
    tw = int(root.get("width", "0"))
    th = int(root.get("height", "0"))
    if tw <= 0 or th <= 0:
        raise ValueError(f"invalid map size in {tmx_path}")

    layer = None
    for name in (primary, fallback, "Terrain"):
        for el in root.findall("layer"):
            if el.get("name") == name:
                layer = el
                break
        if layer is not None:
            break
    if layer is None:
        raise ValueError(f"layer '{primary}' (or fallbacks) not found in {tmx_path}")

    data_el = layer.find("data")
    if data_el is None:
        raise ValueError(f"{layer.get('name')} layer has no <data>")

    gids = decode_layer_data(data_el, tw, th)
    return tw, th, gids


def gameplay_from_gids(gids: list[list[int]], gid_props: dict[int, dict[str, str]]) -> tuple[list[list[bool]], list[list[int]]]:
    walkable: list[list[bool]] = []
    elev: list[list[int]] = []
    for row in gids:
        w_row: list[bool] = []
        e_row: list[int] = []
        for gid in row:
            if gid == 0:
                w_row.append(False)
                e_row.append(-1)
                continue
            props = gid_props.get(gid, {})
            if "walkable" in props:
                walk = props["walkable"].lower() in {"true", "1", "yes"}
            else:
                walk = int(props.get("elevation", "1")) >= 0
            elevation = int(props.get("elevation", "1" if walk else "-1"))
            if not walk:
                elevation = -1
            w_row.append(walk)
            e_row.append(elevation)
        walkable.append(w_row)
        elev.append(e_row)
    return walkable, elev
