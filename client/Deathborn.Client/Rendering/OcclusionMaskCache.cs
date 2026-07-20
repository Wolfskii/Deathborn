using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Sprite-derived alpha masks for default occlusion zones (bushes, tree canopies).</summary>
internal static class OcclusionMaskCache
{
    private const byte AlphaThreshold = 48;

    internal sealed class Mask
    {
        public required int Width { get; init; }
        public required int Height { get; init; }
        /// <summary>Unscaled px from sprite bottom-center to mask bottom-left.</summary>
        public required float OriginX { get; init; }
        public required byte[] Alpha { get; init; }
        public int MinLocalX { get; init; }
        public int MaxLocalX { get; init; }
        public int MinLocalY { get; init; }
        public int MaxLocalY { get; init; }
        public OcclusionColliderShape ColliderShape { get; init; }
        public float EllipseCenterLocalX { get; init; }
        public float EllipseCenterLocalY { get; init; }
        public float EllipseRadiusX { get; init; }
        public float EllipseRadiusY { get; init; }
        public int[] EdgeX { get; init; } = [];
        public int[] EdgeY { get; init; } = [];

        public bool UsesSimplifiedCollider => ColliderShape is not OcclusionColliderShape.PixelMask;

        public bool IsOpaque(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height && Alpha[y * Width + x] >= AlphaThreshold;

        public void GetWorldBounds(Vector2 anchor, float scale, out float left, out float right, out float top, out float bottom)
        {
            left = anchor.X + (MinLocalX - OriginX) * scale;
            right = anchor.X + (MaxLocalX + 1 - OriginX) * scale;
            bottom = anchor.Y - MinLocalY * scale;
            top = anchor.Y - (MaxLocalY + 1) * scale;
        }

        public void GetWorldEllipse(Vector2 anchor, float scale, out Vector2 center, out float radiusX, out float radiusY)
        {
            center = new Vector2(
                anchor.X + (EllipseCenterLocalX - OriginX) * scale,
                anchor.Y - EllipseCenterLocalY * scale);
            radiusX = EllipseRadiusX * scale;
            radiusY = EllipseRadiusY * scale;
        }
    }

    // 0–2 bush, 3–5 tree canopy, 6–9 cloud body variants
    private static readonly Mask?[] Masks = new Mask[10];
    public const byte CloudMaskBaseId = 6;

    public static void Build(
        Texture2D? bush1,
        Texture2D? bush2,
        Texture2D? bush3,
        Texture2D? pine,
        Texture2D? maple)
    {
        for (var i = 0; i < Masks.Length; i++)
            Masks[i] = null;

        if (bush1 != null)
            Masks[0] = ExtractRect(bush1, new Rectangle(0, 0, 48, 32), OcclusionSimplifyMode.BushEllipse);
        if (bush2 != null)
            Masks[1] = ExtractRect(bush2, new Rectangle(48, 0, 48, 32), OcclusionSimplifyMode.BushEllipse);
        if (bush3 != null)
            Masks[2] = ExtractRect(bush3, new Rectangle(0, 0, 48, 32), OcclusionSimplifyMode.BushEllipse);
        if (pine != null)
            Masks[3] = ExtractCanopy(pine, new Rectangle(64, 0, 32, 48), stemRows: 11);
        if (maple != null)
        {
            Masks[4] = ExtractCanopy(maple, new Rectangle(0, 48, 32, 48), stemRows: 11);
            Masks[5] = ExtractCanopy(maple, new Rectangle(64, 48, 32, 48), stemRows: 11);
        }
    }

    public static void BuildClouds(Texture2D? clouds, ReadOnlySpan<Rectangle> bodyRects)
    {
        if (clouds == null) return;

        var sheet = new Color[clouds.Width * clouds.Height];
        clouds.GetData(sheet);

        for (var i = 0; i < bodyRects.Length && CloudMaskBaseId + i < Masks.Length; i++)
            Masks[CloudMaskBaseId + i] = ExtractCloudBody(sheet, clouds.Width, bodyRects[i]);
    }

    public static byte CloudMaskIdFor(int variant) => (byte)(CloudMaskBaseId + variant);

    public static byte MaskIdFor(FoliageInstance f) => f.Kind switch
    {
        FoliageKind.Bush => (byte)(f.Variant % 3),
        FoliageKind.Tree when f.Variant == 0 => 3,
        FoliageKind.Tree => (byte)(4 + (f.AnimPhase & 1)),
        _ => OcclusionZone.NoMaskId,
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

    public static bool ColliderOverlaps(Vector2 anchor, float scale, Mask mask, Vector2 center, float rx, float ry)
    {
        if (mask.UsesSimplifiedCollider)
            return SimplifiedOverlaps(anchor, scale, mask, center, rx, ry);
        return PixelMaskOverlaps(anchor, scale, mask, center, rx, ry);
    }

    public static bool SimplifiedOverlaps(Vector2 anchor, float scale, Mask mask, Vector2 center, float rx, float ry)
    {
        switch (mask.ColliderShape)
        {
            case OcclusionColliderShape.Rect:
                mask.GetWorldBounds(anchor, scale, out var left, out var right, out var top, out var bottom);
                return PlayerEntity.EllipseOverlapsRect(center, rx, ry, left, right, top, bottom);
            case OcclusionColliderShape.Ellipse:
            case OcclusionColliderShape.Circle:
                mask.GetWorldEllipse(anchor, scale, out var ocCenter, out var orx, out var ory);
                return PlayerEntity.EllipseOverlapsEllipse(center, rx, ry, ocCenter, orx, ory);
            default:
                return PixelMaskOverlaps(anchor, scale, mask, center, rx, ry);
        }
    }

    public static bool PixelMaskOverlaps(Vector2 anchor, float scale, Mask mask, Vector2 center, float rx, float ry)
    {
        scale = MathF.Max(0.01f, scale);
        var localX = (center.X - anchor.X) / scale + mask.OriginX;
        var localY = (anchor.Y - center.Y) / scale;
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

    public static void DrawDebugCollider(
        SpriteBatch sb,
        Vector2 anchor,
        float scale,
        Mask mask,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        Color color)
    {
        var thickness = MathF.Max(2f, 2f * zoom);
        if (mask.UsesSimplifiedCollider)
        {
            switch (mask.ColliderShape)
            {
                case OcclusionColliderShape.Rect:
                    mask.GetWorldBounds(anchor, scale, out var left, out var right, out var top, out var bottom);
                    DrawPrimitives.DrawWorldRectOutline(
                        sb, left, top, right, bottom, camera, screenCenter, zoom, color, thickness);
                    return;
                case OcclusionColliderShape.Ellipse:
                case OcclusionColliderShape.Circle:
                    mask.GetWorldEllipse(anchor, scale, out var center, out var rx, out var ry);
                    DrawPrimitives.DrawWorldEllipseOutline(
                        sb, center, rx, ry, camera, screenCenter, zoom, color, thickness);
                    return;
            }
        }

        DrawDebugEdges(sb, anchor, scale, mask, camera, screenCenter, zoom, color);
    }

    public static void DrawDebugEdges(
        SpriteBatch sb,
        Vector2 anchor,
        float scale,
        Mask mask,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        Color color)
    {
        scale = MathF.Max(0.01f, scale);
        var edgePx = MathF.Max(1f, zoom);
        for (var i = 0; i < mask.EdgeX.Length; i++)
        {
            var worldX = anchor.X + (mask.EdgeX[i] - mask.OriginX + 0.5f) * scale;
            var worldY = anchor.Y - (mask.EdgeY[i] + 0.5f) * scale;
            var sx = (worldX - camera.X) * zoom + screenCenter.X;
            var sy = (worldY - camera.Y) * zoom + screenCenter.Y;
            DrawPrimitives.FillRect(
                sb,
                new Rectangle((int)sx, (int)sy, (int)MathF.Ceiling(edgePx), (int)MathF.Ceiling(edgePx)),
                color);
        }
    }

    private static Mask ExtractRect(Texture2D tex, Rectangle src, OcclusionSimplifyMode simplify = OcclusionSimplifyMode.None)
    {
        var pixels = new Color[src.Width * src.Height];
        tex.GetData(0, src, pixels, 0, pixels.Length);

        var alpha = new byte[src.Width * src.Height];
        for (var i = 0; i < pixels.Length; i++)
            alpha[i] = pixels[i].A;

        return BuildMask(src.Width, src.Height, src.Width * 0.5f, alpha, simplify);
    }

    private static Mask ExtractCanopy(Texture2D tex, Rectangle spriteRect, int stemRows)
    {
        var canopyH = Math.Max(1, spriteRect.Height - stemRows);
        var src = new Rectangle(spriteRect.X, spriteRect.Y, spriteRect.Width, canopyH);
        return ExtractRect(tex, src);
    }

    /// <summary>Cloud body pixels with local Y=0 at the body bottom (matches draw anchor).</summary>
    private static Mask ExtractCloudBody(Color[] sheet, int sheetWidth, Rectangle body)
    {
        var w = body.Width;
        var h = body.Height;
        var alpha = new byte[w * h];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var px = sheet[(body.Y + y) * sheetWidth + body.X + x];
                alpha[(h - 1 - y) * w + x] = px.A;
            }
        }
        return BuildMask(w, h, w * 0.5f, alpha, OcclusionSimplifyMode.CloudEllipse);
    }

    private enum OcclusionSimplifyMode
    {
        None,
        BushEllipse,
        CloudEllipse,
    }

    private static Mask BuildMask(int width, int height, float originX, byte[] alpha, OcclusionSimplifyMode simplify = OcclusionSimplifyMode.None)
    {
        var minX = width;
        var maxX = -1;
        var minY = height;
        var maxY = -1;
        var opaqueCount = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (alpha[y * width + x] < AlphaThreshold) continue;
                opaqueCount++;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < 0)
        {
            minX = maxX = minY = maxY = 0;
        }

        OcclusionColliderShape shape = OcclusionColliderShape.PixelMask;
        float ellipseCx = 0f;
        float ellipseCy = 0f;
        float ellipseRx = 0f;
        float ellipseRy = 0f;
        int[] edgeX;
        int[] edgeY;

        if (simplify != OcclusionSimplifyMode.None && opaqueCount > 0)
        {
            var simplified = simplify switch
            {
                OcclusionSimplifyMode.CloudEllipse => OcclusionSimplifier.ChooseForCloud(
                    width, height, alpha, minX, maxX, minY, maxY, opaqueCount),
                _ => OcclusionSimplifier.ChooseForBush(
                    width, height, alpha, minX, maxX, minY, maxY, opaqueCount),
            };
            shape = simplified.Shape;
            ellipseCx = simplified.CenterLocalX;
            ellipseCy = simplified.CenterLocalY;
            ellipseRx = simplified.RadiusX;
            ellipseRy = simplified.RadiusY;
            edgeX = [];
            edgeY = [];
        }
        else
        {
            (edgeX, edgeY) = BuildEdges(width, height, alpha);
        }

        return new Mask
        {
            Width = width,
            Height = height,
            OriginX = originX,
            Alpha = alpha,
            MinLocalX = minX,
            MaxLocalX = maxX,
            MinLocalY = minY,
            MaxLocalY = maxY,
            ColliderShape = shape,
            EllipseCenterLocalX = ellipseCx,
            EllipseCenterLocalY = ellipseCy,
            EllipseRadiusX = ellipseRx,
            EllipseRadiusY = ellipseRy,
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
}
