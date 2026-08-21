using Microsoft.Xna.Framework;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Player housing layout — mirrors server game/housing.go constants.</summary>
public static class HousingConstants
{
    private const float Hs = Config.WorldTileSize / Config.LegacyTileSize;

    public const float PlotHalfW = 100f * Hs;
    public const float PlotHalfH = 88f * Hs;
    public const float HouseHalfW = 52f * Hs;
    public const float HouseHalfH = 44f * Hs;

    /// <summary>Instanced interior — three connected rooms (bedroom | hall | kitchen).</summary>
    public const float InteriorHalfW = 210f * Hs;
    public const float InteriorHalfH = 125f * Hs;

    public static readonly Vector2[] GardenCropOffsets =
    [
        new(-48 * Hs, 52 * Hs), new(0, 58 * Hs), new(48 * Hs, 52 * Hs),
        new(-48 * Hs, 78 * Hs), new(0, 84 * Hs), new(48 * Hs, 78 * Hs),
    ];

    public static string CropTargetId(long houseId, int index) =>
        $"house_{houseId}_crop_{index}";

    public static bool InPlot(Vector2 world, Vector2 center) =>
        world.X >= center.X - PlotHalfW && world.X <= center.X + PlotHalfW
        && world.Y >= center.Y - PlotHalfH && world.Y <= center.Y + PlotHalfH;

    public static bool InHouseInterior(Vector2 world, Vector2 center) =>
        world.X >= center.X - InteriorHalfW && world.X <= center.X + InteriorHalfW
        && world.Y >= center.Y - InteriorHalfH && world.Y <= center.Y + InteriorHalfH - 8 * Hs;

    /// <summary>Exterior door on the homestead building (right-side door on Farm RPG cottage).</summary>
    public static Vector2 DoorWorldPosition(Vector2 center)
    {
        var door = FarmRpgHouseSprites.DoorOffsetFromFoot() * FarmRpgHouseSprites.DisplayScale;
        return center + door;
    }

    /// <summary>Exit door inside the instanced room (south wall).</summary>
    public static Vector2 InteriorDoorWorldPosition(Vector2 center) =>
        new(center.X, center.Y + InteriorHalfH - 20 * Hs);

    /// <summary>Feet spawn just inside the south door (matches server HouseInteriorSpawn).</summary>
    public static Vector2 InteriorSpawnPosition(Vector2 center)
    {
        var door = InteriorDoorWorldPosition(center);
        return new Vector2(door.X, door.Y - 18f);
    }

    public static Vector2 InteriorLocalMin => new(-InteriorHalfW, -InteriorHalfH);
    public static Vector2 InteriorLocalMax => new(InteriorHalfW, InteriorHalfH - 8 * Hs);

    /// <summary>Legacy radius — prefer <see cref="DoorHitBounds"/> for click / highlight.</summary>
    public const float DoorInteractRadius = 38f * Hs;

    /// <summary>Exterior door click/highlight box (world units) — tall door plate on the cottage.</summary>
    public const float DoorHitHalfW = 11f;
    public const float DoorHitHeight = 40f;
    public const float DoorHitBottomPad = 6f;

    public const float InteriorWallInset = 16f * Hs;

    /// <summary>World AABB of the exterior door plate (tall rectangle over the cottage door).</summary>
    public static void DoorHitBounds(Vector2 center, out float left, out float right, out float top, out float bottom)
    {
        var door = DoorWorldPosition(center);
        left = door.X - DoorHitHalfW;
        right = door.X + DoorHitHalfW;
        bottom = door.Y + DoorHitBottomPad;
        top = bottom - DoorHitHeight;
    }

    public static bool InDoorHit(Vector2 world, Vector2 center)
    {
        DoorHitBounds(center, out var left, out var right, out var top, out var bottom);
        return world.X >= left && world.X <= right && world.Y >= top && world.Y <= bottom;
    }

    public static Vector2 ClampToInterior(Vector2 world, Vector2 center)
    {
        var inset = InteriorWallInset;
        var minX = center.X - InteriorHalfW + inset;
        var maxX = center.X + InteriorHalfW - inset;
        var minY = center.Y - InteriorHalfH + inset;
        var maxY = center.Y + InteriorHalfH - 8 * Hs - inset;
        return new Vector2(
            Math.Clamp(world.X, minX, maxX),
            Math.Clamp(world.Y, minY, maxY));
    }

    public static Vector2 ResolveInteriorMove(Vector2 feet, Vector2 delta, Vector2 center) =>
        HousingCollision.ResolveInteriorMove(feet, delta, center);

    public static bool IsNearDoor(Vector2 world, Vector2 center) =>
        InDoorHit(world, center)
        || HousingCollision.InDoorApproach(world, center);

    public static bool IsNearInteriorExit(Vector2 world, Vector2 center)
    {
        var door = InteriorDoorWorldPosition(center);
        if (world.Y < door.Y - 14 * Hs) return false;
        return Vector2.Distance(world, door) <= 26f * Hs;
    }

    public static HousePlotZone? FindDoorAt(IEnumerable<HousePlotZone> houses, Vector2 world)
    {
        HousePlotZone? best = null;
        var bestDist = float.MaxValue;
        foreach (var house in houses)
        {
            var door = DoorWorldPosition(house.Center);
            var d = Vector2.DistanceSquared(world, door);
            var hit = InDoorHit(world, house.Center)
                || HousingCollision.InDoorApproach(world, house.Center);
            if (!hit) continue;
            if (d < bestDist)
            {
                bestDist = d;
                best = house;
            }
        }
        return best;
    }
}

public sealed class HousePlotZone
{
    public required long Id { get; init; }
    public required long OwnerId { get; init; }
    public required string OwnerName { get; init; }
    public required Vector2 Center { get; init; }
    public List<FurnitureItemState> Furniture { get; set; } = [];

    public bool Contains(Vector2 world) => HousingConstants.InPlot(world, Center);

    public string ZoneId => $"house:{Id}";

    public string DisplayName(long localCharacterId) =>
        OwnerId == localCharacterId ? "Your Homestead" : $"{OwnerName}'s Homestead";
}

public sealed class FurnitureItemState
{
    public string Type = "";
    public Vector2 Position;
}
