using Microsoft.Xna.Framework;

namespace Deathborn.Client.Gameplay;

/// <summary>Client-side RTS house placement validation (mirrors server CanBuildAt + range).</summary>
public static class HousePlacement
{
    /// <summary>Max distance from player feet to proposed plot center.</summary>
    public const float MaxPlaceDistance = 320f;

    /// <summary>Matches server HouseMinSeparation.</summary>
    public const float MinSeparation = 440f;

    public static bool IsValid(
        Vector2 center,
        Vector2 playerFeet,
        out string reason)
    {
        reason = "";
        if (Vector2.DistanceSquared(center, playerFeet) > MaxPlaceDistance * MaxPlaceDistance)
        {
            reason = "Move closer to place the house.";
            return false;
        }

        if (NearTownExclusion(center))
        {
            reason = "Too close to a town — build farther out.";
            return false;
        }

        foreach (var house in WorldZones.Houses)
        {
            if (Vector2.DistanceSquared(center, house.Center) < MinSeparation * MinSeparation)
            {
                reason = "Too close to another house.";
                return false;
            }
        }

        if (!PlotClear(center))
        {
            reason = "Blocked by terrain or objects.";
            return false;
        }

        return true;
    }

    private static bool NearTownExclusion(Vector2 world)
    {
        foreach (var zone in WorldZones.Towns)
        {
            if (!zone.Safe) continue;
            var pad = WorldZones.MonsterExclusionPad;
            if (world.X >= zone.Center.X - zone.HalfWidth - pad
                && world.X <= zone.Center.X + zone.HalfWidth + pad
                && world.Y >= zone.Center.Y - zone.HalfHeight - pad
                && world.Y <= zone.Center.Y + zone.HalfHeight + pad)
                return true;
        }
        return false;
    }

    private static bool PlotClear(Vector2 center)
    {
        var map = WorldMap.SwaroviaMainland;
        if (map == null) return false;

        var hw = HousingConstants.PlotHalfW * 0.85f;
        var hh = HousingConstants.PlotHalfH * 0.85f;
        Span<Vector2> samples =
        [
            center,
            center + new Vector2(-hw, -hh),
            center + new Vector2(hw, -hh),
            center + new Vector2(-hw, hh),
            center + new Vector2(hw, hh),
            center + new Vector2(0, -hh),
            center + new Vector2(0, hh),
            center + new Vector2(-hw, 0),
            center + new Vector2(hw, 0),
        ];

        foreach (var s in samples)
        {
            if (!map.IsWalkable(s.X, s.Y, PlayerEntity.Radius))
                return false;
        }

        HousingCollision.BodyBounds(center, out var left, out var right, out var top, out var bottom);
        Span<Vector2> body =
        [
            new((left + right) * 0.5f, (top + bottom) * 0.5f),
            new(left + 8f, top + 8f),
            new(right - 8f, top + 8f),
            new(left + 8f, bottom - 8f),
            new(right - 8f, bottom - 8f),
        ];
        foreach (var s in body)
        {
            if (!map.IsWalkable(s.X, s.Y, 4f))
                return false;
        }

        return true;
    }
}
