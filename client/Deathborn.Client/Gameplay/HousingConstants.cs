using Microsoft.Xna.Framework;

namespace Deathborn.Client.Gameplay;

/// <summary>Player housing layout — mirrors server game/housing.go constants.</summary>
public static class HousingConstants
{
    private const float Hs = Config.WorldTileSize / Config.LegacyTileSize;

    public const float PlotHalfW = 100f * Hs;
    public const float PlotHalfH = 88f * Hs;
    public const float HouseHalfW = 52f * Hs;
    public const float HouseHalfH = 44f * Hs;

    /// <summary>Instanced interior room (larger than exterior shell).</summary>
    public const float InteriorHalfW = 140f * Hs;
    public const float InteriorHalfH = 105f * Hs;

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

    /// <summary>Exterior door on the homestead building.</summary>
    public static Vector2 DoorWorldPosition(Vector2 center) =>
        new(center.X, center.Y + HouseHalfH - 34 * Hs);

    /// <summary>Exit door inside the instanced room.</summary>
    public static Vector2 InteriorDoorWorldPosition(Vector2 center) =>
        new(center.X, center.Y + InteriorHalfH - 20 * Hs);

    public static Vector2 InteriorLocalMin => new(-InteriorHalfW, -InteriorHalfH);
    public static Vector2 InteriorLocalMax => new(InteriorHalfW, InteriorHalfH - 8 * Hs);

    public const float DoorInteractRadius = 38f * Hs;

    public static bool IsNearDoor(Vector2 world, Vector2 center) =>
        Vector2.Distance(world, DoorWorldPosition(center)) <= DoorInteractRadius;

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
            if (d > DoorInteractRadius * DoorInteractRadius) continue;
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
