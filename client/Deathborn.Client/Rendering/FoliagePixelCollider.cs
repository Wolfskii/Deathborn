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

    private static Mask ExtractRect(Texture2D tex, Rectangle src)
    {
        var pixels = new Color[src.Width * src.Height];
        tex.GetData(0, src, pixels, 0, pixels.Length);

        var alpha = new byte[src.Width * src.Height];
        for (var i = 0; i < pixels.Length; i++)
            alpha[i] = pixels[i].A;

        return new Mask
        {
            Width = src.Width,
            Height = src.Height,
            OriginX = src.Width * 0.5f,
            Alpha = alpha,
        };
    }

    private static Mask ExtractStem(Texture2D tex, Rectangle spriteRect, int stemRows)
    {
        var stemH = Math.Min(stemRows, spriteRect.Height);
        var stemSrc = new Rectangle(spriteRect.X, spriteRect.Y + spriteRect.Height - stemH, spriteRect.Width, stemH);
        return ExtractRect(tex, stemSrc);
    }
}
