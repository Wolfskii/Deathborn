using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Alpha masks for rock and tree-stem foliage — circle vs opaque pixels only.</summary>
internal static class FoliagePixelCollider
{
    private const byte AlphaThreshold = 48;
    public const byte NoMaskId = 255;

    internal sealed class Mask
    {
        public required int Width { get; init; }
        public required int Height { get; init; }
        /// <summary>Unscaled px from sprite bottom-center to mask bottom-left.</summary>
        public required float OriginX { get; init; }
        public required byte[] Alpha { get; init; }
        /// <summary>Local px offsets (from mask bottom-left) for debug outline.</summary>
        public int[] EdgeX { get; init; } = [];
        public int[] EdgeY { get; init; } = [];

        public bool IsOpaque(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height && Alpha[y * Width + x] >= AlphaThreshold;
    }

    private static readonly Mask?[] Masks = new Mask[7];

    public static void Build(Texture2D? rock1, Texture2D? rock2, Texture2D? pine, Texture2D? maple)
    {
        for (var i = 0; i < Masks.Length; i++)
            Masks[i] = null;

        if (rock1 != null)
        {
            Masks[0] = ExtractRect(rock1, new Rectangle(0, 0, 16, 16));
            Masks[2] = ExtractRect(rock1, new Rectangle(32, 0, 16, 16));
        }
        if (rock2 != null)
        {
            Masks[1] = ExtractRect(rock2, new Rectangle(16, 0, 16, 16));
            Masks[3] = ExtractRect(rock2, new Rectangle(48, 0, 16, 16));
        }
        if (pine != null)
            Masks[4] = ExtractStem(pine, new Rectangle(64, 0, 32, 48), stemRows: 11);
        if (maple != null)
        {
            Masks[5] = ExtractStem(maple, new Rectangle(0, 48, 32, 48), stemRows: 11);
            Masks[6] = ExtractStem(maple, new Rectangle(64, 48, 32, 48), stemRows: 11);
        }
    }

    public static byte MaskIdFor(FoliageInstance f) => f.Kind switch
    {
        FoliageKind.Rock => (byte)(f.Variant % 4),
        FoliageKind.Tree when f.Variant == 0 => 4,
        FoliageKind.Tree => (byte)(5 + (f.Variant & 1)),
        _ => NoMaskId,
    };

    public static bool TryGetMask(byte id, out Mask? mask)
    {
        if (id >= Masks.Length)
        {
            mask = null;
            return false;
        }
        mask = Masks[id];
        return mask != null;
    }

    public static bool CircleOverlaps(FoliageInstance f, Mask mask, Vector2 circleCenter, float radius)
    {
        var scale = MathF.Max(0.01f, f.Scale);
        var localX = (circleCenter.X - f.Position.X) / scale + mask.OriginX;
        var localY = (f.Position.Y - circleCenter.Y) / scale;
        var r = radius / scale;

        var minX = (int)MathF.Floor(localX - r);
        var maxX = (int)MathF.Ceiling(localX + r);
        var minY = (int)MathF.Floor(localY - r);
        var maxY = (int)MathF.Ceiling(localY + r);
        var rSq = r * r;

        for (var py = minY; py <= maxY; py++)
        {
            for (var px = minX; px <= maxX; px++)
            {
                if (!mask.IsOpaque(px, py)) continue;
                var dx = px + 0.5f - localX;
                var dy = py + 0.5f - localY;
                if (dx * dx + dy * dy <= rSq)
                    return true;
            }
        }
        return false;
    }

    public static bool EllipseOverlaps(FoliageInstance f, Mask mask, Vector2 center, float rx, float ry)
    {
        var scale = MathF.Max(0.01f, f.Scale);
        var localX = (center.X - f.Position.X) / scale + mask.OriginX;
        var localY = (f.Position.Y - center.Y) / scale;
        var erx = rx / scale;
        var ery = ry / scale;

        var minX = (int)MathF.Floor(localX - erx);
        var maxX = (int)MathF.Ceiling(localX + erx);
        var minY = (int)MathF.Floor(localY - ery);
        var maxY = (int)MathF.Ceiling(localY + ery);

        for (var py = minY; py <= maxY; py++)
        {
            for (var px = minX; px <= maxX; px++)
            {
                if (!mask.IsOpaque(px, py)) continue;
                var dx = (px + 0.5f - localX) / erx;
                var dy = (py + 0.5f - localY) / ery;
                if (dx * dx + dy * dy <= 1f)
                    return true;
            }
        }
        return false;
    }

    public static Vector2 PushOut(FoliageInstance f, Mask mask, Vector2 center, float radius)
    {
        var scale = MathF.Max(0.01f, f.Scale);
        var localX = (center.X - f.Position.X) / scale + mask.OriginX;
        var localY = (f.Position.Y - center.Y) / scale;
        var r = radius / scale;
        var rSq = r * r;

        var minX = (int)MathF.Floor(localX - r - 1f);
        var maxX = (int)MathF.Ceiling(localX + r + 1f);
        var minY = (int)MathF.Floor(localY - r - 1f);
        var maxY = (int)MathF.Ceiling(localY + r + 1f);

        var bestDistSq = float.MaxValue;
        var bestDx = 0f;
        var bestDy = 0f;

        for (var py = minY; py <= maxY; py++)
        {
            for (var px = minX; px <= maxX; px++)
            {
                if (!mask.IsOpaque(px, py)) continue;
                var dx = px + 0.5f - localX;
                var dy = py + 0.5f - localY;
                var distSq = dx * dx + dy * dy;
                if (distSq >= bestDistSq) continue;
                bestDistSq = distSq;
                bestDx = dx;
                bestDy = dy;
            }
        }

        if (bestDistSq >= rSq || bestDistSq < 0.0001f)
            return center;

        var dist = MathF.Sqrt(bestDistSq) * scale;
        var push = (radius - dist + 0.35f) / MathF.Max(dist, 0.001f);
        return center + new Vector2(bestDx * push, -bestDy * push);
    }

    public static Vector2 PushOutEllipse(FoliageInstance f, Mask mask, Vector2 center, float rx, float ry)
    {
        var scale = MathF.Max(0.01f, f.Scale);
        var localX = (center.X - f.Position.X) / scale + mask.OriginX;
        var localY = (f.Position.Y - center.Y) / scale;
        var erx = rx / scale;
        var ery = ry / scale;

        var minX = (int)MathF.Floor(localX - erx - 1f);
        var maxX = (int)MathF.Ceiling(localX + erx + 1f);
        var minY = (int)MathF.Floor(localY - ery - 1f);
        var maxY = (int)MathF.Ceiling(localY + ery + 1f);

        var bestDistSq = float.MaxValue;
        var bestDx = 0f;
        var bestDy = 0f;

        for (var py = minY; py <= maxY; py++)
        {
            for (var px = minX; px <= maxX; px++)
            {
                if (!mask.IsOpaque(px, py)) continue;
                var dx = (px + 0.5f - localX) / erx;
                var dy = (py + 0.5f - localY) / ery;
                var distSq = dx * dx + dy * dy;
                if (distSq >= bestDistSq) continue;
                bestDistSq = distSq;
                bestDx = px + 0.5f - localX;
                bestDy = py + 0.5f - localY;
            }
        }

        if (bestDistSq >= 1f || bestDistSq < 0.0001f)
            return center;

        var nx = bestDx / erx;
        var ny = bestDy / ery;
        var norm = MathF.Sqrt(nx * nx + ny * ny);
        if (norm < 0.0001f)
            return center;
        nx /= norm;
        ny /= norm;
        var effR = 1f / MathF.Sqrt((nx / erx) * (nx / erx) + (ny / ery) * (ny / ery));
        var dist = MathF.Sqrt(bestDistSq) * MathF.Min(erx, ery);
        var push = (effR * scale - dist + 0.35f) / MathF.Max(dist, 0.001f);
        return center + new Vector2(nx * push, -ny * push);
    }

    /// <summary>Chroma-key green (#00FF00) collider outlines — toggle with F12 debug HUD.</summary>
    public static void DrawDebugMask(SpriteBatch sb, FoliageInstance f, Mask mask, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var scale = MathF.Max(0.01f, f.Scale);
        var edge = MathF.Max(1f, zoom);
        var color = Color.Lime;

        for (var i = 0; i < mask.EdgeX.Length; i++)
        {
            var worldX = f.Position.X + (mask.EdgeX[i] - mask.OriginX + 0.5f) * scale;
            var worldY = f.Position.Y - (mask.EdgeY[i] + 0.5f) * scale;
            var sx = (worldX - camera.X) * zoom + screenCenter.X;
            var sy = (worldY - camera.Y) * zoom + screenCenter.Y;
            DrawPrimitives.FillRect(sb, new Rectangle((int)sx, (int)sy, (int)MathF.Ceiling(edge), (int)MathF.Ceiling(edge)), color);
        }
    }

    public static void DrawDebugCircle(
        SpriteBatch sb, Vector2 worldCenter, float worldRadius, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var screen = new Vector2(
            (worldCenter.X - camera.X) * zoom + screenCenter.X,
            (worldCenter.Y - camera.Y) * zoom + screenCenter.Y);
        DrawPrimitives.DrawCircleOutline(sb, screen, worldRadius * zoom, Color.Lime, 32, MathF.Max(2f, 2f * zoom));
    }

    private static Mask ExtractRect(Texture2D tex, Rectangle src)
    {
        var pixels = new Color[src.Width * src.Height];
        tex.GetData(0, src, pixels, 0, pixels.Length);

        var alpha = new byte[src.Width * src.Height];
        for (var i = 0; i < pixels.Length; i++)
            alpha[i] = pixels[i].A;

        var (edgeX, edgeY) = BuildEdges(src.Width, src.Height, alpha);
        return new Mask
        {
            Width = src.Width,
            Height = src.Height,
            OriginX = src.Width * 0.5f,
            Alpha = alpha,
            EdgeX = edgeX,
            EdgeY = edgeY,
        };
    }

    private static (int[] X, int[] Y) BuildEdges(int width, int height, byte[] alpha)
    {
        var edgesX = new List<int>();
        var edgesY = new List<int>();
        bool Opaque(int x, int y) =>
            x >= 0 && y >= 0 && x < width && y < height && alpha[y * width + x] >= AlphaThreshold;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!Opaque(x, y)) continue;
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1
                    || !Opaque(x - 1, y) || !Opaque(x + 1, y) || !Opaque(x, y - 1) || !Opaque(x, y + 1))
                {
                    edgesX.Add(x);
                    edgesY.Add(y);
                }
            }
        }
        return (edgesX.ToArray(), edgesY.ToArray());
    }

    private static Mask ExtractStem(Texture2D tex, Rectangle spriteRect, int stemRows)
    {
        var stemH = Math.Min(stemRows, spriteRect.Height);
        var stemSrc = new Rectangle(spriteRect.X, spriteRect.Y + spriteRect.Height - stemH, spriteRect.Width, stemH);
        return ExtractRect(tex, stemSrc);
    }
}
