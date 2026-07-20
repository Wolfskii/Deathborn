using Microsoft.Xna.Framework;

namespace Deathborn.Client.Gameplay;

/// <summary>
/// Exterior cottage collision + interior wall segments.
/// Door approach (south of the right-side door) stays walkable and triggers enter.
/// </summary>
public static class HousingCollision
{
    /// <summary>Solid house footprint relative to plot center (foot anchor).</summary>
    public const float BodyHalfW = 50f;
    public const float BodyTop = -108f;
    public const float BodyBottom = -4f;
    public const float DoorGapHalfW = 20f;
    public const float DoorApproachSouth = 36f;

    public static void BodyBounds(Vector2 center, out float left, out float right, out float top, out float bottom)
    {
        left = center.X - BodyHalfW;
        right = center.X + BodyHalfW;
        top = center.Y + BodyTop;
        bottom = center.Y + BodyBottom;
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

        BodyBounds(center, out var left, out var right, out var top, out var bottom);
        var door = HousingConstants.DoorWorldPosition(center);
        var gapL = door.X - DoorGapHalfW;
        var gapR = door.X + DoorGapHalfW;

        // Main mass above the door sill.
        var sill = bottom - 10f;
        if (PlayerEntity.EllipseOverlapsRect(
                PlayerEntity.CollisionCenter(feet),
                PlayerEntity.CollisionRadiusX,
                PlayerEntity.CollisionRadiusY,
                left, right, top, sill))
            return true;

        // Bottom strip left of door.
        if (PlayerEntity.EllipseOverlapsRect(
                PlayerEntity.CollisionCenter(feet),
                PlayerEntity.CollisionRadiusX,
                PlayerEntity.CollisionRadiusY,
                left, gapL, sill, bottom))
            return true;

        // Bottom strip right of door.
        if (PlayerEntity.EllipseOverlapsRect(
                PlayerEntity.CollisionCenter(feet),
                PlayerEntity.CollisionRadiusX,
                PlayerEntity.CollisionRadiusY,
                gapR, right, sill, bottom))
            return true;

        return false;
    }

    public static bool BlocksAt(Vector2 feet, float radius, IEnumerable<HousePlotZone> houses)
    {
        foreach (var house in houses)
        {
            if (OverlapsHouseBody(feet, radius, house.Center))
                return true;
        }
        return false;
    }

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
