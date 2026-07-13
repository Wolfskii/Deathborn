"""Regenerate SwordsmanV2FrameAtlas.cs from measured sprite sheet layout."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SHEETS = ROOT / "client/Deathborn.Client/Content/Characters/Swordsman V2"
OUT = ROOT / "client/Deathborn.Client/Rendering/Characters/SwordsmanV2FrameAtlas.cs"

IDLE_COLS = [0, 230, 397, 565, 733, 903, 1254]
IDLE_ROWS = [0, 156, 313, 470, 627, 783, 940, 1097, 1254]
WALK_COLS = [0, 134, 280, 426, 573, 721, 869, 1015, 1254]
WALK_ROWS = [0, 221, 418, 615, 810, 1004, 1254]
ATK_COLS = [0, 138, 330, 520, 708, 904, 1092, 1281, 1536]
ATK_ROWS = [0, 128, 256, 384, 512, 640, 768, 896, 1024]


def tight_bbox(im: Image.Image, x0: int, y0: int, x1: int, y1: int) -> tuple[int, int, int, int]:
    bbox = None
    for y in range(y0, y1):
        for x in range(x0, x1):
            if im.getpixel((x, y))[3] > 25:
                if bbox is None:
                    bbox = [x, y, x, y]
                else:
                    bbox[0] = min(bbox[0], x)
                    bbox[1] = min(bbox[1], y)
                    bbox[2] = max(bbox[2], x)
                    bbox[3] = max(bbox[3], y)
    if bbox is None:
        return x0, y0, max(1, x1 - x0), max(1, y1 - y0)
    sx, sy, bx, by = bbox
    return sx, sy, bx - sx + 1, by - sy + 1


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
            sx, sy, w, h = tight_bbox(im, cx0, ry0, cx1, ry1)
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


def main() -> None:
    idle_im = Image.open(SHEETS / "idle.png").convert("RGBA")
    walk_im = Image.open(SHEETS / "walk.png").convert("RGBA")
    atk_im = Image.open(SHEETS / "one-handed-attack.png").convert("RGBA")

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
    cs += "\n".join(build_frames(idle_im, IDLE_ROWS, IDLE_COLS, 8, 6, use_cell_rect=True))
    cs += """
    ];

    public static readonly TightFrame[] Walk =
    [
"""
    cs += "\n".join(build_frames(walk_im, WALK_ROWS, WALK_COLS, 6, 8, use_cell_rect=True))
    cs += """
    ];

    public static readonly TightFrame[] Attack =
    [
"""
    cs += "\n".join(build_frames(atk_im, ATK_ROWS, ATK_COLS, 8, 8, use_cell_rect=False))
    cs += """
    ];

    public static TightFrame GetIdle(int row, int frame) => Idle[row * 6 + frame];
    public static TightFrame GetWalk(int row, int frame) => Walk[row * 8 + frame];
    public static TightFrame GetAttack(int row, int frame) => Attack[row * 8 + frame];
}
"""
    OUT.write_text(cs, encoding="utf-8")
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
