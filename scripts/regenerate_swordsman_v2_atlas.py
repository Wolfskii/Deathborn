"""Regenerate SwordsmanV2FrameAtlas.cs from measured sprite sheet layout."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SHEETS = ROOT / "client/Deathborn.Client/Content/Characters/Swordsman V2"
SOURCE = SHEETS / "_source"
MANIFEST = ROOT / "prompts/sprites/Player/manifest.json"
OUT = ROOT / "client/Deathborn.Client/Rendering/Characters/SwordsmanV2FrameAtlas.cs"
SPECS = ROOT / "client/Deathborn.Client/Rendering/Characters/SwordsmanV2AnimationSpecs.cs"

ATK_COLS = [0, 138, 330, 520, 708, 904, 1092, 1281, 1536]
ATK_ROWS = [0, 128, 256, 384, 512, 640, 768, 896, 1024]

def uniform_starts(length: int, count: int) -> list[int]:
    return [((i * length) // count) for i in range(count + 1)]


def detect_column_starts(im: Image.Image, y0: int, y1: int, frames: int) -> list[int]:
    """Find vertical gutters between frames (full-height empty columns)."""
    width = im.width
    full_green = [
        x
        for x in range(width)
        if all(not is_sprite_pixel(*im.getpixel((x, y))[:4]) for y in range(y0, y1))
    ]
    if not full_green:
        return uniform_starts(width, frames)

    runs: list[tuple[int, int]] = []
    run_start = full_green[0]
    prev = full_green[0]
    for x in full_green[1:]:
        if x != prev + 1:
            runs.append((run_start, prev))
            run_start = x
        prev = x
    runs.append((run_start, prev))
    gutters = [(a + b) // 2 for a, b in runs]

    starts = [0]
    min_cell = max(16, width // (frames * 2))
    for i in range(1, frames):
        expected = (i * width) // frames
        candidates = [g for g in gutters if g - starts[-1] >= min_cell and width - g >= (frames - i) * min_cell]
        starts.append(min(candidates, key=lambda g: abs(g - expected)) if candidates else expected)
    starts.append(width)
    return starts


def compute_merged_idle_row_starts() -> list[int]:
    """Row boundaries for interleaved cardinals/diagonals idle merge strips."""
    card_path = SOURCE / "idle" / "cardinals.png"
    if card_path.exists():
        card_h = Image.open(card_path).height
        half_rows = uniform_starts(card_h, 4)
    else:
        merged_h = Image.open(SHEETS / "idle.png").height
        half_rows = uniform_starts(merged_h // 2, 4)

    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    starts = [0]
    y = 0
    for spec in manifest["mergedRowOrder"]:
        row = spec["row"]
        h = half_rows[row + 1] - half_rows[row]
        y += h
        starts.append(y)
    return starts


def idle_per_row_column_starts(im: Image.Image, row_starts: list[int], rows: int, cols: int) -> list[list[int]]:
    return [
        detect_column_starts(im, row_starts[ri], row_starts[ri + 1], cols)
        for ri in range(rows)
    ]


def is_sprite_pixel(r: int, g: int, b: int, a: int) -> bool:
    if a <= 25:
        return False
    # Chroma-key green background left in AI sheets.
    if g > 200 and r < 100 and b < 100:
        return False
    return True


def largest_sprite_bbox(im: Image.Image, x0: int, y0: int, x1: int, y1: int) -> tuple[int, int, int, int]:
    """BBox of the largest connected sprite component — ignores stray shadow pixels."""
    width, height = x1 - x0, y1 - y0
    mask = [[False] * width for _ in range(height)]
    best: list[tuple[int, int]] = []

    for y in range(y0, y1):
        for x in range(x0, x1):
            r, g, b, a = im.getpixel((x, y))
            if not is_sprite_pixel(r, g, b, a):
                continue
            lx, ly = x - x0, y - y0
            if mask[ly][lx]:
                continue

            stack = [(lx, ly)]
            mask[ly][lx] = True
            pixels: list[tuple[int, int]] = []
            while stack:
                cx, cy = stack.pop()
                pixels.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height or mask[ny][nx]:
                        continue
                    pr, pg, pb, pa = im.getpixel((x0 + nx, y0 + ny))
                    if is_sprite_pixel(pr, pg, pb, pa):
                        mask[ny][nx] = True
                        stack.append((nx, ny))

            if len(pixels) > len(best):
                best = pixels

    if not best:
        return x0, y0, max(1, width), max(1, height)

    xs = [p[0] for p in best]
    ys = [p[1] for p in best]
    sx = min(xs) + x0
    sy = min(ys) + y0
    ex = max(xs) + x0
    ey = max(ys) + y0
    return sx, sy, ex - sx + 1, ey - sy + 1


def build_frames(
    im: Image.Image,
    row_starts: list[int],
    col_starts: list[int],
    rows: int,
    cols: int,
    *,
    use_cell_rect: bool,
) -> list[str]:
    lines: list[str] = []
    for ri in range(rows):
        ry0, ry1 = row_starts[ri], row_starts[ri + 1]
        for fi in range(cols):
            cx0, cx1 = col_starts[fi], col_starts[fi + 1]
            sx, sy, w, h = largest_sprite_bbox(im, cx0, ry0, cx1, ry1)
            feet_x = sx + w / 2.0
            feet_y = sy + h - 1

            if use_cell_rect:
                src_x, src_y = cx0, ry0
                src_w, src_h = cx1 - cx0, ry1 - ry0
                origin_x = feet_x - src_x
                origin_y = feet_y - src_y
            else:
                src_x, src_y, src_w, src_h = sx, sy, w, h
                origin_x = feet_x - sx
                origin_y = h - 1.0

            origin_x = max(0.0, min(origin_x, src_w - 1.0))
            origin_y = max(0.0, min(origin_y, src_h - 1.0))
            lines.append(
                f"        new({src_x}, {src_y}, {src_w}, {src_h}, {origin_x:.1f}f, {origin_y:.1f}f),"
            )
    return lines


def build_frames_idle_stable(
    im: Image.Image,
    row_starts: list[int],
    col_starts: list[int],
    rows: int,
    cols: int,
    *,
    per_row_col_starts: list[list[int]] | None = None,
) -> list[str]:
    """Tight idle frames with stable horizontal anchor to stop frame-to-frame jitter."""
    lines: list[str] = []
    for ri in range(rows):
        ry0, ry1 = row_starts[ri], row_starts[ri + 1]
        cols_for_row = per_row_col_starts[ri] if per_row_col_starts else col_starts
        for fi in range(cols):
            cx0, cx1 = cols_for_row[fi], cols_for_row[fi + 1]
            sx, sy, w, h = largest_sprite_bbox(im, cx0, ry0, cx1, ry1)
            origin_x = w / 2.0
            origin_y = h - 1.0
            lines.append(
                f"        new({sx}, {sy}, {w}, {h}, {origin_x:.1f}f, {origin_y:.1f}f),"
            )
    return lines


def robust_row_extent(values: list[int]) -> int:
    """Ignore one bloated frame when AI gutters mis-cut the last column."""
    if not values:
        return 1
    ordered = sorted(values)
    median = ordered[len(ordered) // 2]
    filtered = [v for v in values if v <= median * 1.35]
    return max(filtered) if filtered else median


def compute_row_body_heights(
    im: Image.Image,
    row_starts: list[int],
    col_starts: list[int],
    *,
    rows: int,
    cols: int,
    per_row_col_starts: list[list[int]] | None = None,
) -> list[int]:
    heights: list[int] = []
    for ri in range(rows):
        ry0, ry1 = row_starts[ri], row_starts[ri + 1]
        cols_for_row = per_row_col_starts[ri] if per_row_col_starts else col_starts
        frame_heights: list[int] = []
        for fi in range(cols):
            cx0, cx1 = cols_for_row[fi], cols_for_row[fi + 1]
            _, _, _, h = largest_sprite_bbox(im, cx0, ry0, cx1, ry1)
            frame_heights.append(h)
        heights.append(robust_row_extent(frame_heights))
    return heights


def compute_row_body_widths(
    im: Image.Image,
    row_starts: list[int],
    col_starts: list[int],
    *,
    rows: int,
    cols: int,
    per_row_col_starts: list[list[int]] | None = None,
) -> list[int]:
    widths: list[int] = []
    for ri in range(rows):
        ry0, ry1 = row_starts[ri], row_starts[ri + 1]
        cols_for_row = per_row_col_starts[ri] if per_row_col_starts else col_starts
        frame_widths: list[int] = []
        for fi in range(cols):
            cx0, cx1 = cols_for_row[fi], cols_for_row[fi + 1]
            _, _, w, _ = largest_sprite_bbox(im, cx0, ry0, cx1, ry1)
            frame_widths.append(w)
        widths.append(robust_row_extent(frame_widths))
    return widths


def compute_row_draw_scales(heights: list[int]) -> list[float]:
    ref = max(heights) if heights else 1
    return [ref / h if h > 0 else 1.0 for h in heights]


def compute_idle_row_draw_scales(
    idle_heights: list[int],
    idle_widths: list[int],
    walk_heights: list[int],
    walk_widths: list[int],
    walk_draw_scales: list[float],
) -> list[float]:
    """Match idle standing height to walk per row (uniform scale cannot fix narrow diagonal art)."""
    matched: list[float] = []
    for row, idle_h in enumerate(idle_heights):
        walk_h = walk_heights[row]
        walk_scale = walk_draw_scales[row]
        target_h = walk_h * walk_scale
        scale_h = target_h / idle_h if idle_h > 0 else 1.0
        matched.append(scale_h)
    return matched


def patch_float_array(specs_path: Path, name: str, scales: list[float]) -> None:
    import re

    lines = "\n".join(
        f"        {scale:.3f}f, // row {i}"
        for i, scale in enumerate(scales)
    )
    block = (
        f"    private static readonly float[] {name} =\n"
        "    [\n"
        f"{lines}\n"
        "    ];"
    )
    text = specs_path.read_text(encoding="utf-8")
    text, count = re.subn(
        rf"    private static readonly float\[\] {name} =\s*\[[\s\S]*?\];",
        block,
        text,
        count=1,
    )
    if count != 1:
        raise RuntimeError(f"Could not patch {name} in {specs_path}")
    specs_path.write_text(text, encoding="utf-8")


def patch_walk_row_draw_scales(scales: list[float]) -> None:
    patch_float_array(SPECS, "WalkRowDrawScale", scales)


def patch_idle_row_draw_scales(scales: list[float]) -> None:
    patch_float_array(SPECS, "IdleRowDrawScale", scales)


def patch_idle_row_map() -> None:
    import re

    block = """    private static readonly int[] IdleRowMap =
    [
        0, // Down — South
        7, // DownLeft — South-West
        6, // Left — West
        5, // UpLeft — North-West
        4, // Up — North
        3, // UpRight — North-East
        2, // Right — East
        1, // DownRight — South-East
    ];"""
    text = SPECS.read_text(encoding="utf-8")
    text, count = re.subn(
        r"    private static readonly int\[\] IdleRowMap =\s*\[[\s\S]*?\];",
        block,
        text,
        count=1,
    )
    if count != 1:
        raise RuntimeError("Could not patch IdleRowMap in SwordsmanV2AnimationSpecs.cs")
    SPECS.write_text(text, encoding="utf-8")


def main() -> None:
    idle_im = Image.open(SHEETS / "idle.png").convert("RGBA")
    idle_rows = compute_merged_idle_row_starts()
    idle_cols_per_row = idle_per_row_column_starts(idle_im, idle_rows, 8, 6)
    idle_cols = idle_cols_per_row[0]

    walk_im = Image.open(SHEETS / "walk.png").convert("RGBA")
    walk_rows = uniform_starts(walk_im.height, 8)
    walk_cols = uniform_starts(walk_im.width, 8)

    atk_im = Image.open(SHEETS / "one-handed-attack.png").convert("RGBA")
    atk_rows = uniform_starts(atk_im.height, 8)
    atk_cols = uniform_starts(atk_im.width, 8)

    cs = """using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

internal readonly struct TightFrame(int x, int y, int w, int h, float originX, float originY)
{
    public Rectangle Source => new(x, y, w, h);
    public Vector2 Origin => new(originX, originY);
}

internal static class SwordsmanV2FrameAtlas
{
    public static readonly TightFrame[] Idle =
    [
"""
    cs += "\n".join(
        build_frames_idle_stable(
            idle_im, idle_rows, idle_cols, 8, 6, per_row_col_starts=idle_cols_per_row
        )
    )
    cs += """
    ];

    public static readonly TightFrame[] Walk =
    [
"""
    cs += "\n".join(build_frames(walk_im, walk_rows, walk_cols, 8, 8, use_cell_rect=False))
    cs += """
    ];

    public static readonly TightFrame[] Attack =
    [
"""
    cs += "\n".join(build_frames(atk_im, atk_rows, atk_cols, 8, 8, use_cell_rect=True))
    cs += """
    ];

    public static TightFrame GetIdle(int row, int frame) => Idle[row * 6 + frame];
    public static TightFrame GetWalk(int row, int frame) => Walk[row * 8 + frame];
    public static TightFrame GetAttack(int row, int frame) => Attack[row * 8 + frame];
}
"""
    OUT.write_text(cs, encoding="utf-8")
    print(f"Wrote {OUT}")

    walk_heights = compute_row_body_heights(walk_im, walk_rows, walk_cols, rows=8, cols=8)
    walk_widths = compute_row_body_widths(walk_im, walk_rows, walk_cols, rows=8, cols=8)
    walk_scales = compute_row_draw_scales(walk_heights)
    patch_walk_row_draw_scales(walk_scales)
    print(f"Wrote walk row draw scales to {SPECS}")

    idle_heights = compute_row_body_heights(
        idle_im, idle_rows, idle_cols, rows=8, cols=6, per_row_col_starts=idle_cols_per_row
    )
    idle_widths = compute_row_body_widths(
        idle_im, idle_rows, idle_cols, rows=8, cols=6, per_row_col_starts=idle_cols_per_row
    )
    idle_scales = compute_idle_row_draw_scales(
        idle_heights, idle_widths, walk_heights, walk_widths, walk_scales
    )
    patch_idle_row_draw_scales(idle_scales)
    patch_idle_row_map()
    print(f"Wrote idle row draw scales to {SPECS}")


if __name__ == "__main__":
    main()
