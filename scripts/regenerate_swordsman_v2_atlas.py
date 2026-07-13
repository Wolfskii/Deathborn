"""Regenerate SwordsmanV2FrameAtlas.cs from measured sprite sheet layout."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SHEETS = ROOT / "client/Deathborn.Client/Content/Characters/Swordsman V2"
OUT = ROOT / "client/Deathborn.Client/Rendering/Characters/SwordsmanV2FrameAtlas.cs"
SPECS = ROOT / "client/Deathborn.Client/Rendering/Characters/SwordsmanV2AnimationSpecs.cs"

IDLE_COLS = [0, 230, 397, 565, 733, 903, 1254]
IDLE_ROWS = [0, 156, 313, 470, 627, 783, 940, 1097, 1254]
ATK_COLS = [0, 138, 330, 520, 708, 904, 1092, 1281, 1536]
ATK_ROWS = [0, 128, 256, 384, 512, 640, 768, 896, 1024]

def uniform_starts(length: int, count: int) -> list[int]:
    return [((i * length) // count) for i in range(count + 1)]


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


def compute_walk_row_draw_scales(
    im: Image.Image,
    row_starts: list[int],
    col_starts: list[int],
    *,
    rows: int = 8,
    cols: int = 8,
) -> list[float]:
    cardinal_rows = {0, 2, 4, 6}
    row_heights: list[int] = []
    for ri in range(rows):
        ry0, ry1 = row_starts[ri], row_starts[ri + 1]
        max_h = 0
        for fi in range(cols):
            cx0, cx1 = col_starts[fi], col_starts[fi + 1]
            _, _, _, h = largest_sprite_bbox(im, cx0, ry0, cx1, ry1)
            max_h = max(max_h, h)
        row_heights.append(max_h)

    ref = max(row_heights[r] for r in cardinal_rows)
    return [1.0 if ri in cardinal_rows else ref / row_heights[ri] for ri in range(rows)]


def patch_walk_row_draw_scales(scales: list[float]) -> None:
    import re

    lines = "\n".join(
        f"        {scale:.3f}f, // row {i}"
        for i, scale in enumerate(scales)
    )
    block = (
        "    private static readonly float[] WalkRowDrawScale =\n"
        "    [\n"
        f"{lines}\n"
        "    ];"
    )
    text = SPECS.read_text(encoding="utf-8")
    text, count = re.subn(
        r"    private static readonly float\[\] WalkRowDrawScale =\s*\[[\s\S]*?\];",
        block,
        text,
        count=1,
    )
    if count != 1:
        raise RuntimeError("Could not patch WalkRowDrawScale in SwordsmanV2AnimationSpecs.cs")
    SPECS.write_text(text, encoding="utf-8")


def main() -> None:
    idle_im = Image.open(SHEETS / "idle.png").convert("RGBA")
    idle_rows = uniform_starts(idle_im.height, 8)
    idle_cols = uniform_starts(idle_im.width, 6)

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
    cs += "\n".join(build_frames(idle_im, idle_rows, idle_cols, 8, 6, use_cell_rect=True))
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

    walk_scales = compute_walk_row_draw_scales(walk_im, walk_rows, walk_cols)
    patch_walk_row_draw_scales(walk_scales)
    print(f"Wrote walk row draw scales to {SPECS}")


if __name__ == "__main__":
    main()
