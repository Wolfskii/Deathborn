using Microsoft.Xna.Framework;

namespace Deathborn.Client.Gameplay;

/// <summary>Player housing layout — mirrors server game/housing.go constants.</summary>
public static class HousingConstants
{
    public const float PlotHalfW = 100f;
    public const float PlotHalfH = 88f;
    public const float HouseHalfW = 52f;
    public const float HouseHalfH = 44f;

    public static readonly Vector2[] GardenCropOffsets =
    [
        new(-48, 52), new(0, 58), new(48, 52),
        new(-48, 78), new(0, 84), new(48, 78),
    ];

    public static string CropTargetId(long houseId, int index) =>
        $"house_{houseId}_crop_{index}";

    public static bool InPlot(Vector2 world, Vector2 center) =>
        world.X >= center.X - PlotHalfW && world.X <= center.X + PlotHalfW
        && world.Y >= center.Y - PlotHalfH && world.Y <= center.Y + PlotHalfH;

    public static bool InHouseInterior(Vector2 world, Vector2 center) =>
        world.X >= center.X - HouseHalfW && world.X <= center.X + HouseHalfW
        && world.Y >= center.Y - HouseHalfH - 12 && world.Y <= center.Y + HouseHalfH - 28;

    public static Vector2 DoorWorldPosition(Vector2 center) =>
        new(center.X, center.Y + HouseHalfH - 34);

    public const float DoorInteractRadius = 38f;

    public static bool IsNearDoor(Vector2 world, Vector2 center) =>
        Vector2.Distance(world, DoorWorldPosition(center)) <= DoorInteractRadius;

    public static bool IsNearInteriorExit(Vector2 world, Vector2 center) =>
        Vector2.Distance(world, DoorWorldPosition(center)) <= 32f;

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
