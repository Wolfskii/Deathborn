using Microsoft.Xna.Framework;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Client-side RTS house placement validation (mirrors server CanBuildAt + range).</summary>
public static class HousePlacement
{
    /// <summary>Max distance from player feet to proposed plot center.</summary>
    public const float MaxPlaceDistance = 160f / Config.ExteriorTerrainFocusScale;

    /// <summary>Matches server HouseMinSeparation.</summary>
    public const float MinSeparation = 220f / Config.ExteriorTerrainFocusScale;

    public static bool IsValid(
        Vector2 center,
        Vector2 playerFeet,
        IReadOnlyDictionary<long, PlayerEntity>? players,
        IReadOnlyDictionary<long, WorldNpcEntity>? npcs,
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

        if (!PlotClear(center, out reason))
            return false;

        if (PlotOverlapsOccupants(center, players, npcs, out reason))
            return false;

        return true;
    }

    private static bool PlotOverlapsOccupants(
        Vector2 center,
        IReadOnlyDictionary<long, PlayerEntity>? players,
        IReadOnlyDictionary<long, WorldNpcEntity>? npcs,
        out string reason)
    {
        reason = "";
        if (players != null)
        {
            foreach (var p in players.Values)
            {
                if (p.IsDead) continue;
                if (EntityOverlapsPlot(center, p.Position, PlayerEntity.CollisionRadiusX, PlayerEntity.CollisionRadiusY))
                {
                    reason = p.IsLocal
                        ? "Move away - the house would cover you."
                        : "Another player is in the plot.";
                    return true;
                }
            }
        }

        if (npcs != null)
        {
            foreach (var npc in npcs.Values)
            {
                if (npc.Hp <= 0) continue;
                var feet = NpcHitboxes.HitTestAnchor(npc);
                var rx = npc.Radius;
                var ry = rx * (PlayerEntity.CollisionRadiusY / PlayerEntity.CollisionRadiusX);
                if (EntityOverlapsPlot(center, feet, rx, ry))
                {
                    reason = "Clear creatures from the plot first.";
                    return true;
                }
            }
        }

        return false;
    }

    private static bool EntityOverlapsPlot(Vector2 center, Vector2 feet, float rx, float ry)
    {
        if (HousingCollision.OverlapsHouseBody(feet, center, rx, ry, allowDoorApproach: false))
            return true;
        return HomesteadFence.Overlaps(feet, center, rx, ry);
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

    private static bool PlotClear(Vector2 center, out string reason)
    {
        reason = "";
        var map = WorldMap.SwaroviaMainland;
        if (map == null)
        {
            reason = "Map not loaded.";
            return false;
        }

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
            {
                reason = DescribeBlocked(map, s);
                return false;
            }
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
            {
                reason = DescribeBlocked(map, s);
                return false;
            }
        }

        if (WorldFoliage.PlotOverlapsFoliage(center, HousingConstants.PlotHalfW, HousingConstants.PlotHalfH))
        {
            reason = "Clear tree trunks from the plot first.";
            return false;
        }

        return true;
    }

    private static string DescribeBlocked(WorldMap map, Vector2 world)
    {
        if (world.X < map.TileSize || world.Y < map.TileSize
            || world.X > map.WorldWidth - map.TileSize
            || world.Y > map.WorldHeight - map.TileSize)
            return "Too close to the map edge.";

        var tx = (int)(world.X / map.TileSize);
        var ty = (int)(world.Y / map.TileSize);
        if (map.HasElevation && map.GetElevation(tx, ty) < 0)
            return "Cannot build on water.";

        if (WorldFoliage.BlocksFeet(world, PlayerEntity.Radius))
            return "Blocked by trees or rocks.";

        return "Need clear flat land (not water or cliffs).";
    }
}
