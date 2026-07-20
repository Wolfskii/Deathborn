namespace Deathborn.Client.Rendering;

internal enum OcclusionColliderShape
{
    PixelMask,
    Rect,
    Ellipse,
    Circle,
}

/// <summary>Picks a tight rect, ellipse, or circle for bush occlusion (build time only).</summary>
internal static class OcclusionSimplifier
{
    private const byte AlphaThreshold = 48;

    internal readonly struct Result
    {
        public OcclusionColliderShape Shape { get; init; }
        public float CenterLocalX { get; init; }
        public float CenterLocalY { get; init; }
        public float RadiusX { get; init; }
        public float RadiusY { get; init; }
        public int BackgroundPixels { get; init; }
        public float FillRatio { get; init; }
    }

    /// <summary>Round/puffy sprites (bushes, clouds) — centroid-fit ellipse.</summary>
    public static Result ChooseCentroidEllipse(int width, int height, byte[] alpha, int minX, int maxX, int minY, int maxY, int opaqueCount)
    {
        if (opaqueCount <= 0 || maxX < minX)
        {
            return new Result
            {
                Shape = OcclusionColliderShape.Ellipse,
                BackgroundPixels = 0,
                FillRatio = 1f,
            };
        }

        var (cx, cy) = OpaqueCentroid(width, alpha, minX, maxX, minY, maxY);
        var rx = 0.5f;
        var ry = 0.5f;
        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            if (!IsOpaque(alpha, width, x, y)) continue;
            rx = MathF.Max(rx, MathF.Abs(x + 0.5f - cx));
            ry = MathF.Max(ry, MathF.Abs(y + 0.5f - cy));
        }

        for (var i = 0; i < 48 && CountEllipse(width, height, alpha, cx, cy, rx, ry).MissedOpaque > 0; i++)
        {
            rx += 0.25f;
            ry += 0.25f;
        }

        var ellipse = CountEllipse(width, height, alpha, cx, cy, rx, ry);
        return new Result
        {
            Shape = OcclusionColliderShape.Ellipse,
            CenterLocalX = cx,
            CenterLocalY = cy,
            RadiusX = rx,
            RadiusY = ry,
            BackgroundPixels = ellipse.Background,
            FillRatio = ellipse.Opaque / (float)Math.Max(1, ellipse.Area),
        };
    }

    public static Result ChooseForBush(int width, int height, byte[] alpha, int minX, int maxX, int minY, int maxY, int opaqueCount) =>
        ChooseCentroidEllipse(width, height, alpha, minX, maxX, minY, maxY, opaqueCount);

    public static Result ChooseForCloud(int width, int height, byte[] alpha, int minX, int maxX, int minY, int maxY, int opaqueCount) =>
        ChooseCentroidEllipse(width, height, alpha, minX, maxX, minY, maxY, opaqueCount);

    /// <summary>General-purpose picker (trees etc.) — may choose rect when it wins on bg pixels.</summary>
    public static Result Choose(int width, int height, byte[] alpha, int minX, int maxX, int minY, int maxY, int opaqueCount)
    {
        if (opaqueCount <= 0 || maxX < minX)
        {
            return new Result
            {
                Shape = OcclusionColliderShape.Rect,
                BackgroundPixels = 0,
                FillRatio = 1f,
            };
        }

        var rectW = maxX - minX + 1;
        var rectH = maxY - minY + 1;
        var rectArea = rectW * rectH;
        var rectBg = rectArea - opaqueCount;
        var rectFill = opaqueCount / (float)rectArea;

        var cx = (minX + maxX + 1) * 0.5f;
        var cy = (minY + maxY + 1) * 0.5f;
        var rx = rectW * 0.5f;
        var ry = rectH * 0.5f;

        var best = ScoreRect(rectBg, rectFill, cx, cy, rx, ry);

        var ellipse = CountEllipse(width, height, alpha, cx, cy, rx, ry);
        if (ellipse.MissedOpaque == 0)
            TryCandidate(ref best, OcclusionColliderShape.Ellipse, cx, cy, rx, ry, ellipse.Background, ellipse.Opaque, ellipse.Area);

        var circleR = MathF.Max(rx, ry);
        var circle = CountCircle(width, height, alpha, cx, cy, circleR);
        if (circle.MissedOpaque == 0)
            TryCandidate(ref best, OcclusionColliderShape.Circle, cx, cy, circleR, circleR, circle.Background, circle.Opaque, circle.Area);

        var (centroidX, centroidY) = OpaqueCentroid(width, alpha, minX, maxX, minY, maxY);
        var minCircleR = MinRadiusContaining(width, alpha, minX, maxX, minY, maxY, centroidX, centroidY);
        if (minCircleR > 0f)
        {
            var tightCircle = CountCircle(width, height, alpha, centroidX, centroidY, minCircleR);
            if (tightCircle.MissedOpaque == 0)
            {
                TryCandidate(
                    ref best,
                    OcclusionColliderShape.Circle,
                    centroidX,
                    centroidY,
                    minCircleR,
                    minCircleR,
                    tightCircle.Background,
                    tightCircle.Opaque,
                    tightCircle.Area);
            }
        }

        return best;
    }

    private static Result ScoreRect(int bg, float fill, float cx, float cy, float rx, float ry) =>
        new()
        {
            Shape = OcclusionColliderShape.Rect,
            CenterLocalX = cx,
            CenterLocalY = cy,
            RadiusX = rx,
            RadiusY = ry,
            BackgroundPixels = bg,
            FillRatio = fill,
        };

    private static void TryCandidate(
        ref Result best,
        OcclusionColliderShape shape,
        float cx,
        float cy,
        float rx,
        float ry,
        int bg,
        int opaque,
        int area)
    {
        if (area <= 0) return;
        var fill = opaque / (float)area;
        if (bg > best.BackgroundPixels) return;
        if (bg == best.BackgroundPixels)
        {
            if (fill <= best.FillRatio) return;
            if (fill == best.FillRatio && ShapeRank(shape) >= ShapeRank(best.Shape)) return;
        }

        best = new Result
        {
            Shape = shape,
            CenterLocalX = cx,
            CenterLocalY = cy,
            RadiusX = rx,
            RadiusY = ry,
            BackgroundPixels = bg,
            FillRatio = fill,
        };
    }

    private static int ShapeRank(OcclusionColliderShape shape) => shape switch
    {
        OcclusionColliderShape.Ellipse => 0,
        OcclusionColliderShape.Circle => 1,
        OcclusionColliderShape.Rect => 2,
        _ => 3,
    };

    private readonly struct ShapeCount
    {
        public int Area { get; init; }
        public int Opaque { get; init; }
        public int Background { get; init; }
        public int MissedOpaque { get; init; }
    }

    private static ShapeCount CountEllipse(int width, int height, byte[] alpha, float cx, float cy, float rx, float ry)
    {
        var area = 0;
        var opaque = 0;
        var missed = 0;

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var isOpaque = IsOpaque(alpha, width, x, y);
            var inside = InsideEllipse(x + 0.5f, y + 0.5f, cx, cy, rx, ry);
            if (inside)
            {
                area++;
                if (isOpaque) opaque++;
            }
            else if (isOpaque)
            {
                missed++;
            }
        }

        return new ShapeCount
        {
            Area = area,
            Opaque = opaque,
            Background = area - opaque,
            MissedOpaque = missed,
        };
    }

    private static ShapeCount CountCircle(int width, int height, byte[] alpha, float cx, float cy, float radius) =>
        CountEllipse(width, height, alpha, cx, cy, radius, radius);

    private static (float X, float Y) OpaqueCentroid(int width, byte[] alpha, int minX, int maxX, int minY, int maxY)
    {
        double sumX = 0;
        double sumY = 0;
        var count = 0;
        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            if (!IsOpaque(alpha, width, x, y)) continue;
            sumX += x + 0.5;
            sumY += y + 0.5;
            count++;
        }

        if (count == 0) return (0f, 0f);
        return ((float)(sumX / count), (float)(sumY / count));
    }

    private static float MinRadiusContaining(
        int width, byte[] alpha, int minX, int maxX, int minY, int maxY, float cx, float cy)
    {
        var r = 0f;
        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            if (!IsOpaque(alpha, width, x, y)) continue;
            var dx = x + 0.5f - cx;
            var dy = y + 0.5f - cy;
            r = MathF.Max(r, MathF.Sqrt(dx * dx + dy * dy));
        }
        return r;
    }

    private static bool InsideEllipse(float px, float py, float cx, float cy, float rx, float ry)
    {
        if (rx <= 0f || ry <= 0f) return false;
        var dx = (px - cx) / rx;
        var dy = (py - cy) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    private static bool IsOpaque(byte[] alpha, int width, int x, int y) =>
        alpha[y * width + x] >= AlphaThreshold;
}
