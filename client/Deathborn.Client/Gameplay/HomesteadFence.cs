using Microsoft.Xna.Framework;

namespace Deathborn.Client.Gameplay;

/// <summary>
/// Shared homestead fence layout (draw + collision). Collision uses a few long thin AABBs,
/// not per-post shapes.
/// </summary>
public static class HomesteadFence
{
    /// <summary>Thickness of each fence AABB (world units).</summary>
    public const float Thickness = 14f;

    public readonly struct Layout
    {
        public float Left { get; init; }
        public float Top { get; init; }
        public float Right { get; init; }
        public float Bottom { get; init; }
        public int Nx { get; init; }
        public int Ny { get; init; }
        public float SpacingX { get; init; }
        public float SpacingY { get; init; }
        public int GateL { get; init; }
        public int GateR { get; init; }
        public float GateGapLeft { get; init; }
        public float GateGapRight { get; init; }
    }

    public static void GetLayout(Vector2 center, out Layout layout)
    {
        var left = center.X - HousingConstants.PlotHalfW;
        var top = center.Y - HousingConstants.PlotHalfH;
        var spanX = HousingConstants.PlotHalfW * 2f;
        var spanY = HousingConstants.PlotHalfH * 2f;
        var nx = Math.Max(4, (int)MathF.Round(spanX / Config.WorldTileSize));
        var ny = Math.Max(4, (int)MathF.Round(spanY / Config.WorldTileSize));
        var spacingX = spanX / nx;
        var spacingY = spanY / ny;
        var gateL = Math.Clamp(nx / 2 - 1, 1, nx - 3);
        var gateR = gateL + 2;

        layout = new Layout
        {
            Left = left,
            Top = top,
            Right = left + spanX,
            Bottom = top + spanY,
            Nx = nx,
            Ny = ny,
            SpacingX = spacingX,
            SpacingY = spacingY,
            GateL = gateL,
            GateR = gateR,
            GateGapLeft = left + gateL * spacingX,
            GateGapRight = left + gateR * spacingX,
        };
    }

    /// <summary>
    /// Up to 5 thin AABBs: north, west, east, south-left, south-right (gate gap open).
    /// </summary>
    public static void AppendAabbs(Vector2 center, List<(float L, float R, float T, float B)> walls)
    {
        GetLayout(center, out var layout);
        var t = Thickness * 0.5f;
        var left = layout.Left;
        var right = layout.Right;
        var top = layout.Top;
        var bottom = layout.Bottom;

        walls.Add((left - t, right + t, top - t, top + t)); // north
        walls.Add((left - t, left + t, top - t, bottom + t)); // west
        walls.Add((right - t, right + t, top - t, bottom + t)); // east
        walls.Add((left - t, layout.GateGapLeft, bottom - t, bottom + t)); // south L
        walls.Add((layout.GateGapRight, right + t, bottom - t, bottom + t)); // south R
    }

    public static bool Overlaps(Vector2 feet, Vector2 center)
    {
        // Reuse a tiny scratch via stackalloc-style locals — 5 rects, no heap.
        GetLayout(center, out var layout);
        var t = Thickness * 0.5f;
        var c = PlayerEntity.CollisionCenter(feet);
        var rx = PlayerEntity.CollisionRadiusX;
        var ry = PlayerEntity.CollisionRadiusY;

        if (Hit(c, rx, ry, layout.Left - t, layout.Right + t, layout.Top - t, layout.Top + t))
            return true;
        if (Hit(c, rx, ry, layout.Left - t, layout.Left + t, layout.Top - t, layout.Bottom + t))
            return true;
        if (Hit(c, rx, ry, layout.Right - t, layout.Right + t, layout.Top - t, layout.Bottom + t))
            return true;
        if (Hit(c, rx, ry, layout.Left - t, layout.GateGapLeft, layout.Bottom - t, layout.Bottom + t))
            return true;
        if (Hit(c, rx, ry, layout.GateGapRight, layout.Right + t, layout.Bottom - t, layout.Bottom + t))
            return true;
        return false;
    }

    private static bool Hit(Vector2 c, float rx, float ry, float l, float r, float top, float bottom) =>
        PlayerEntity.EllipseOverlapsRect(c, rx, ry, l, r, top, bottom);
}
