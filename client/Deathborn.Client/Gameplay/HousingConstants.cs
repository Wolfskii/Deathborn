using Microsoft.Xna.Framework;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Player housing layout — mirrors server game/housing.go constants.</summary>
public static class HousingConstants
{
    private const float Hs = Config.WorldTileSize / Config.LegacyTileSize;
    private const float Es = Hs / Config.ExteriorTerrainFocusScale;

    // Homestead yard: two tiles wider and taller than the original 100x88 half-sizes (server housing.go).
    public const float PlotHalfW = 132f * Es;
    public const float PlotHalfH = 120f * Es;
    public const float HouseHalfW = 52f * Es;
    public const float HouseHalfH = 44f * Es;

    /// <summary>Instanced interior: 26 x 15 tiles, three rooms (see <see cref="HouseInteriorLayout"/>).</summary>
    public const float InteriorHalfW = 208f * Hs;
    public const float InteriorHalfH = 120f * Hs;

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

    /// <summary>Keeps tilled soil this far inside the fence line (server fenceSoilInset).</summary>
    private const float FenceSoilInset = 2f;

    /// <summary>True when the whole farm tile lies inside the fence (server tileInsideFence).</summary>
    public static bool TileInsideFence(int tx, int ty, Vector2 center)
    {
        var origin = FarmCatalog.TileOrigin(tx, ty);
        var size = FarmCatalog.TileSize;
        return origin.X >= center.X - PlotHalfW + FenceSoilInset
            && origin.X + size <= center.X + PlotHalfW - FenceSoilInset
            && origin.Y >= center.Y - PlotHalfH + FenceSoilInset
            && origin.Y + size <= center.Y + PlotHalfH - FenceSoilInset;
    }

    /// <summary>On the interior floor, inside the outer walls (server InHouseInterior).</summary>
    public static bool InHouseInterior(Vector2 world, Vector2 center)
    {
        var min = HouseInteriorLayout.TileWorld(center, 1, HouseInteriorLayout.NorthSolidRows + 1);
        var max = HouseInteriorLayout.TileWorld(center, HouseInteriorLayout.Cols - 1, HouseInteriorLayout.Rows - 1);
        return world.X >= min.X && world.X <= max.X && world.Y >= min.Y && world.Y <= max.Y;
    }

    /// <summary>World units per cottage art pixel (the sprite is drawn at DisplayScale × WorldZoom, world at 2 × WorldZoom).</summary>
    private const float ArtToWorld = FarmRpgHouseSprites.DisplayScale * Es;

    /// <summary>World AABB of the drawn cottage sprite (foot anchor at the plot centre).</summary>
    public static void CottageWorldBounds(Vector2 center, out float left, out float right, out float top, out float bottom)
    {
        var tex = FarmRpgHouseSprites.OrangeCottage;
        var artW = tex?.Width ?? 72f;
        var artH = tex?.Height ?? 86f;
        left = center.X - artW * 0.5f * ArtToWorld;
        right = center.X + artW * 0.5f * ArtToWorld;
        top = center.Y - artH * ArtToWorld;
        bottom = center.Y;
    }

    /// <summary>Exterior door threshold on the homestead cottage (bottom-centre of the door plate).</summary>
    public static Vector2 DoorWorldPosition(Vector2 center) =>
        center + FarmRpgHouseSprites.DoorOffsetFromFoot() * ArtToWorld;

    /// <summary>Exit door inside the instanced room: middle of the south wall gap (server HouseInteriorDoorPosition).</summary>
    public static Vector2 InteriorDoorWorldPosition(Vector2 center)
    {
        var left = HouseInteriorLayout.TileWorld(center, HouseInteriorLayout.ExitLeft, HouseInteriorLayout.Rows - 1);
        var right = HouseInteriorLayout.TileWorld(center, HouseInteriorLayout.ExitRight + 1, HouseInteriorLayout.Rows - 1);
        return new Vector2((left.X + right.X) * 0.5f, left.Y + HouseInteriorLayout.Tile * 0.5f);
    }

    /// <summary>Feet spawn just inside the south door (matches server HouseInteriorSpawn).</summary>
    public static Vector2 InteriorSpawnPosition(Vector2 center)
    {
        var door = InteriorDoorWorldPosition(center);
        return new Vector2(door.X, door.Y - 18f);
    }

    public static Vector2 InteriorLocalMin => new(-InteriorHalfW, -InteriorHalfH);
    public static Vector2 InteriorLocalMax => new(InteriorHalfW, InteriorHalfH);

    /// <summary>Legacy radius — prefer <see cref="DoorHitBounds"/> for click / highlight.</summary>
    public const float DoorInteractRadius = 38f * Es;

    public const float InteriorWallInset = 16f * Hs;

    /// <summary>World AABB of the exterior door plate, so the highlight sits exactly on the sprite's door.</summary>
    public static void DoorHitBounds(Vector2 center, out float left, out float right, out float top, out float bottom)
    {
        FarmRpgHouseSprites.DoorHitArtBounds(out var l, out var t, out var r, out var b);
        left = center.X + l * ArtToWorld;
        right = center.X + r * ArtToWorld;
        top = center.Y + t * ArtToWorld;
        bottom = center.Y + b * ArtToWorld;
    }

    public static bool InDoorHit(Vector2 world, Vector2 center)
    {
        DoorHitBounds(center, out var left, out var right, out var top, out var bottom);
        return world.X >= left && world.X <= right && world.Y >= top && world.Y <= bottom;
    }

    /// <summary>Safety box only (server clampToInterior); the interior walls do the real blocking.</summary>
    public static Vector2 ClampToInterior(Vector2 world, Vector2 center)
    {
        var min = HouseInteriorLayout.TileWorld(center, 1, HouseInteriorLayout.NorthSolidRows);
        var max = HouseInteriorLayout.TileWorld(center, HouseInteriorLayout.Cols - 1, HouseInteriorLayout.Rows);
        var minX = min.X;
        var maxX = max.X;
        var minY = min.Y;
        var maxY = max.Y - 2f;
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
    public List<FarmCropState> Crops { get; set; } = [];
    public List<FarmAnimalState> Animals { get; set; } = [];

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
