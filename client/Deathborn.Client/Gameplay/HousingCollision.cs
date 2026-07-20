using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>
/// Exterior cottage collision + interior wall segments.
/// Cottage solid = bottom wall AABB + roof triangle (cheap). Door approach stays walkable.
/// </summary>
public static class HousingCollision
{
    /// <summary>Solid house footprint relative to plot center (foot anchor).</summary>
    public const float BodyHalfW = 50f;
    public const float BodyBottom = -4f;
    /// <summary>Top of the wall rectangle / base of the roof triangle.</summary>
    public const float RoofEave = -64f;
    /// <summary>Roof apex (peak).</summary>
    public const float RoofApex = -108f;
    /// <summary>Overall top of the collider AABB (roof peak).</summary>
    public const float BodyTop = RoofApex;
    public const float DoorGapHalfW = 20f;
    public const float DoorApproachSouth = 36f;

    public static void BodyBounds(Vector2 center, out float left, out float right, out float top, out float bottom)
    {
        left = center.X - BodyHalfW;
        right = center.X + BodyHalfW;
        top = center.Y + BodyTop;
        bottom = center.Y + BodyBottom;
    }

    public static void WallBounds(Vector2 center, out float left, out float right, out float top, out float bottom)
    {
        left = center.X - BodyHalfW;
        right = center.X + BodyHalfW;
        top = center.Y + RoofEave;
        bottom = center.Y + BodyBottom;
    }

    public static void RoofTriangle(
        Vector2 center,
        out Vector2 apex,
        out Vector2 baseLeft,
        out Vector2 baseRight)
    {
        apex = new Vector2(center.X, center.Y + RoofApex);
        baseLeft = new Vector2(center.X - BodyHalfW, center.Y + RoofEave);
        baseRight = new Vector2(center.X + BodyHalfW, center.Y + RoofEave);
    }

    public static bool InDoorApproach(Vector2 world, Vector2 center)
    {
        var door = HousingConstants.DoorWorldPosition(center);
        BodyBounds(center, out _, out _, out _, out var bodyBottom);
        return world.X >= door.X - DoorGapHalfW
            && world.X <= door.X + DoorGapHalfW
            && world.Y >= bodyBottom - 6f
            && world.Y <= bodyBottom + DoorApproachSouth;
    }

    public static bool OverlapsHouseBody(Vector2 feet, float radius, Vector2 center)
    {
        if (InDoorApproach(feet, center))
            return false;

        var c = PlayerEntity.CollisionCenter(feet);
        var rx = PlayerEntity.CollisionRadiusX;
        var ry = PlayerEntity.CollisionRadiusY;

        WallBounds(center, out var left, out var right, out var wallTop, out var bottom);
        if (PlayerEntity.EllipseOverlapsRect(c, rx, ry, left, right, wallTop, bottom))
            return true;

        RoofTriangle(center, out var apex, out var bl, out var br);
        return EllipseOverlapsTriangle(c, rx, ry, apex, bl, br);
    }

    /// <summary>
    /// Ellipse vs triangle via unit-circle space (same idea as <see cref="PlayerEntity.EllipseOverlapsRect"/>).
    /// </summary>
    public static bool EllipseOverlapsTriangle(
        Vector2 center, float rx, float ry, Vector2 a, Vector2 b, Vector2 c)
    {
        if (rx < 0.0001f || ry < 0.0001f)
            return false;

        static Vector2 ToUnit(Vector2 p, Vector2 o, float trx, float try_) =>
            new((p.X - o.X) / trx, (p.Y - o.Y) / try_);

        var ta = ToUnit(a, center, rx, ry);
        var tb = ToUnit(b, center, rx, ry);
        var tc = ToUnit(c, center, rx, ry);
        if (PointInTriangle(Vector2.Zero, ta, tb, tc))
            return true;
        var closest = ClosestPointOnTriangle(Vector2.Zero, ta, tb, tc);
        return closest.LengthSquared() <= 1f;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // Barycentric — same winding either way.
        var v0 = c - a;
        var v1 = b - a;
        var v2 = p - a;
        var dot00 = Vector2.Dot(v0, v0);
        var dot01 = Vector2.Dot(v0, v1);
        var dot02 = Vector2.Dot(v0, v2);
        var dot11 = Vector2.Dot(v1, v1);
        var dot12 = Vector2.Dot(v1, v2);
        var denom = dot00 * dot11 - dot01 * dot01;
        if (MathF.Abs(denom) < 0.0000001f) return false;
        var u = (dot11 * dot02 - dot01 * dot12) / denom;
        var v = (dot00 * dot12 - dot01 * dot02) / denom;
        return u >= 0f && v >= 0f && u + v <= 1f;
    }

    private static Vector2 ClosestPointOnTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var ab = ClosestPointOnSegment(p, a, b);
        var bc = ClosestPointOnSegment(p, b, c);
        var ca = ClosestPointOnSegment(p, c, a);
        var dab = Vector2.DistanceSquared(p, ab);
        var dbc = Vector2.DistanceSquared(p, bc);
        var dca = Vector2.DistanceSquared(p, ca);
        if (dab <= dbc && dab <= dca) return ab;
        if (dbc <= dca) return bc;
        return ca;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lenSq = ab.LengthSquared();
        if (lenSq < 0.0001f) return a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / lenSq, 0f, 1f);
        return a + ab * t;
    }

    public static bool BlocksAt(Vector2 feet, float radius, IEnumerable<HousePlotZone> houses)
    {
        foreach (var house in houses)
        {
            if (OverlapsHouseBody(feet, radius, house.Center))
                return true;
            if (HomesteadFence.Overlaps(feet, house.Center))
                return true;
        }
        return false;
    }

    /// <summary>F12: wall AABB + roof triangle (+ fence strips).</summary>
    public static void DrawDebugColliders(
        SpriteBatch sb,
        IEnumerable<HousePlotZone> houses,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom)
    {
        var wallColor = new Color(0.35f, 1f, 0.45f) * 0.9f;
        var roofColor = new Color(1f, 0.85f, 0.25f) * 0.9f;
        var fenceColor = new Color(0.4f, 0.75f, 1f) * 0.85f;
        var thick = Math.Max(1.5f, 2f * zoom);

        foreach (var house in houses)
        {
            WallBounds(house.Center, out var l, out var r, out var t, out var b);
            DrawPrimitives.DrawWorldRectOutline(sb, l, t, r, b, camera, screenCenter, zoom, wallColor, thick);

            RoofTriangle(house.Center, out var apex, out var bl, out var br);
            var sa = ToScreen(apex, camera, screenCenter, zoom);
            var sbL = ToScreen(bl, camera, screenCenter, zoom);
            var sbR = ToScreen(br, camera, screenCenter, zoom);
            DrawPrimitives.DrawLine(sb, sa, sbL, roofColor, thick);
            DrawPrimitives.DrawLine(sb, sbL, sbR, roofColor, thick);
            DrawPrimitives.DrawLine(sb, sbR, sa, roofColor, thick);

            var fences = new List<(float L, float R, float T, float B)>(5);
            HomesteadFence.AppendAabbs(house.Center, fences);
            foreach (var f in fences)
                DrawPrimitives.DrawWorldRectOutline(sb, f.L, f.T, f.R, f.B, camera, screenCenter, zoom, fenceColor, thick);
        }
    }

    private static Vector2 ToScreen(Vector2 world, Vector2 camera, Vector2 screenCenter, float zoom) =>
        screenCenter + (world - camera) * zoom;

    public static Vector2 ResolveMove(Vector2 fromFeet, Vector2 toFeet, float radius, IEnumerable<HousePlotZone> houses)
    {
        var list = houses as IList<HousePlotZone> ?? houses.ToList();
        if (list.Count == 0) return toFeet;

        if (!BlocksAt(toFeet, radius, list))
            return toFeet;

        // Axis slides.
        var ax = new Vector2(toFeet.X, fromFeet.Y);
        var ay = new Vector2(fromFeet.X, toFeet.Y);
        var aOk = !BlocksAt(ax, radius, list);
        var bOk = !BlocksAt(ay, radius, list);
        if (aOk && bOk)
        {
            var da = (ax - fromFeet).LengthSquared();
            var db = (ay - fromFeet).LengthSquared();
            return da >= db ? ax : ay;
        }
        if (aOk) return ax;
        if (bOk) return ay;

        // Binary clamp along path.
        var lo = 0f;
        var hi = 1f;
        var best = fromFeet;
        for (var i = 0; i < 8; i++)
        {
            var mid = (lo + hi) * 0.5f;
            var p = Vector2.Lerp(fromFeet, toFeet, mid);
            if (!BlocksAt(p, radius, list))
            {
                best = p;
                lo = mid;
            }
            else hi = mid;
        }
        return best;
    }

    /// <summary>Interior wall AABBs in world space (openings already cut).</summary>
    public static void GetInteriorWalls(Vector2 center, List<(float L, float R, float T, float B)> walls)
    {
        walls.Clear();
        var hw = HousingConstants.InteriorHalfW;
        var hh = HousingConstants.InteriorHalfH;
        const float t = 18f;
        var door = HousingConstants.InteriorDoorWorldPosition(center);
        var exitHalf = 28f;
        var midL = center.X - hw / 3f;
        var midR = center.X + hw / 3f;
        var openHalf = 26f;
        var openY = center.Y;

        // Outer walls
        walls.Add((center.X - hw, center.X + hw, center.Y - hh, center.Y - hh + t)); // north
        walls.Add((center.X - hw, center.X - hw + t, center.Y - hh, center.Y + hh)); // west
        walls.Add((center.X + hw - t, center.X + hw, center.Y - hh, center.Y + hh)); // east

        // South with exit gap
        walls.Add((center.X - hw, door.X - exitHalf, center.Y + hh - t - 8f, center.Y + hh - 8f));
        walls.Add((door.X + exitHalf, center.X + hw, center.Y + hh - t - 8f, center.Y + hh - 8f));

        // Divider L|C with opening
        walls.Add((midL - t * 0.5f, midL + t * 0.5f, center.Y - hh, openY - openHalf));
        walls.Add((midL - t * 0.5f, midL + t * 0.5f, openY + openHalf, center.Y + hh - 8f));

        // Divider C|R with opening
        walls.Add((midR - t * 0.5f, midR + t * 0.5f, center.Y - hh, openY - openHalf));
        walls.Add((midR - t * 0.5f, midR + t * 0.5f, openY + openHalf, center.Y + hh - 8f));
    }

    public static bool OverlapsInteriorWalls(Vector2 feet, Vector2 center)
    {
        var walls = new List<(float L, float R, float T, float B)>(12);
        GetInteriorWalls(center, walls);
        var c = PlayerEntity.CollisionCenter(feet);
        var rx = PlayerEntity.CollisionRadiusX;
        var ry = PlayerEntity.CollisionRadiusY;
        foreach (var w in walls)
        {
            if (PlayerEntity.EllipseOverlapsRect(c, rx, ry, w.L, w.R, w.T, w.B))
                return true;
        }
        return false;
    }

    public static Vector2 ResolveInteriorMove(Vector2 fromFeet, Vector2 delta, Vector2 center)
    {
        var clamped = HousingConstants.ClampToInterior(fromFeet + delta, center);
        if (!OverlapsInteriorWalls(clamped, center))
            return clamped;

        var ax = HousingConstants.ClampToInterior(new Vector2(fromFeet.X + delta.X, fromFeet.Y), center);
        var ay = HousingConstants.ClampToInterior(new Vector2(fromFeet.X, fromFeet.Y + delta.Y), center);
        var aOk = !OverlapsInteriorWalls(ax, center);
        var bOk = !OverlapsInteriorWalls(ay, center);
        if (aOk && bOk)
            return (ax - fromFeet).LengthSquared() >= (ay - fromFeet).LengthSquared() ? ax : ay;
        if (aOk) return ax;
        if (bOk) return ay;

        var lo = 0f;
        var hi = 1f;
        var best = fromFeet;
        for (var i = 0; i < 8; i++)
        {
            var mid = (lo + hi) * 0.5f;
            var p = HousingConstants.ClampToInterior(Vector2.Lerp(fromFeet, fromFeet + delta, mid), center);
            if (!OverlapsInteriorWalls(p, center))
            {
                best = p;
                lo = mid;
            }
            else hi = mid;
        }
        return best;
    }
}
