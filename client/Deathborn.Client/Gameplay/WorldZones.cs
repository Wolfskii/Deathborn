using Microsoft.Xna.Framework;
using Deathborn.Client.Net;

namespace Deathborn.Client.Gameplay;

public enum TownStyle { Starter, Village, Castle, Port}

/// <summary>Named world region — safe towns block PvP inside their bounds.</summary>
public sealed class WorldZone
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool Safe { get; init; }
    public required Vector2 Center { get; init; }
    public required float HalfWidth { get; init; }
    public required float HalfHeight { get; init; }
    public TownStyle Style { get; init; }

    public Rectangle Bounds => new(
        (int)(Center.X - HalfWidth),
        (int)(Center.Y - HalfHeight),
        (int)(HalfWidth * 2),
        (int)(HalfHeight * 2));

    public bool Contains(Vector2 world) =>
        world.X >= Center.X - HalfWidth && world.X <= Center.X + HalfWidth
        && world.Y >= Center.Y - HalfHeight && world.Y <= Center.Y + HalfHeight;
}

/// <summary>Static zone layout for the Swarovia mainland (tile anchors match server).</summary>
public static class WorldZones
{
    public const string WildernessId = "wilderness";

    private static IReadOnlyList<WorldZone> _towns = [];
    private static IReadOnlyList<(string A, string B)> _pathLinks = [];
    private static List<HousePlotZone> _houses = [];

    public static IReadOnlyList<WorldZone> Towns => _towns;
    public static IReadOnlyList<(string A, string B)> PathLinks => _pathLinks;
    public static IReadOnlyList<HousePlotZone> Houses => _houses;

    public static void Initialize(WorldMap map)
    {
        _ = map;
        _towns = [];
        _pathLinks = [];
    }

    public static WorldZone? Get(string id) =>
        _towns.FirstOrDefault(z => z.Id == id);

    /// <summary>Safe town containing the point, if any.</summary>
    public static WorldZone? ZoneAt(Vector2 world)
    {
        foreach (var zone in _towns)
        {
            if (zone.Contains(world))
                return zone;
        }
        return null;
    }

    public static HousePlotZone? HouseAt(Vector2 world)
    {
        foreach (var house in _houses)
        {
            if (house.Contains(world))
                return house;
        }
        return null;
    }

    public static HousePlotZone? HouseByOwner(long characterId) =>
        _houses.FirstOrDefault(h => h.OwnerId == characterId);

    public static HousePlotZone? HouseById(long houseId) =>
        houseId <= 0 ? null : _houses.FirstOrDefault(h => h.Id == houseId);

    public static bool HasHouse(long characterId) => HouseByOwner(characterId) != null;

    /// <summary>Plot owned by the character or linked to their homestead key.</summary>
    public static HousePlotZone? HomesteadFor(long characterId, long houseKeyHouseId = 0)
    {
        var owned = HouseByOwner(characterId);
        if (owned != null) return owned;
        if (houseKeyHouseId <= 0) return null;
        return _houses.FirstOrDefault(h => h.Id == houseKeyHouseId);
    }

    public static void SyncHouses(IEnumerable<HouseState>? houses)
    {
        _houses = [];
        if (houses == null) return;
        foreach (var h in houses)
        {
            _houses.Add(new HousePlotZone
            {
                Id = h.Id,
                OwnerId = h.OwnerId,
                OwnerName = h.OwnerName,
                Center = new Vector2((float)h.X, (float)h.Y),
                Furniture = (h.Furniture ?? []).Select(f => new FurnitureItemState
                {
                    Type = f.Type,
                    Position = new Vector2((float)f.X, (float)f.Y),
                }).ToList(),
            });
        }
    }

    public static bool IsSafe(Vector2 world) =>
        ZoneAt(world)?.Safe == true || HouseAt(world) != null;

    public static bool AllowPvP(Vector2 attacker, Vector2 target) =>
        !WorldBossEventActive && !IsSafe(attacker) && !IsSafe(target);

    public static string DisplayName(Vector2 world, long localCharacterId = -1)
    {
        var house = HouseAt(world);
        if (house != null)
            return house.DisplayName(localCharacterId);
        return ZoneAt(world)?.Name ?? "The Wilderness";
    }

    public const float MonsterExclusionPad = 96f * (Config.WorldTileSize / Config.LegacyTileSize);

    public static bool WorldBossEventActive { get; set; }

    /// <summary>Monsters and bosses may not enter this padded area around safe towns and house plots.</summary>
    public static bool InMonsterExclusion(Vector2 world)
    {
        foreach (var zone in _towns)
        {
            if (!zone.Safe) continue;
            if (world.X >= zone.Center.X - zone.HalfWidth - MonsterExclusionPad
                && world.X <= zone.Center.X + zone.HalfWidth + MonsterExclusionPad
                && world.Y >= zone.Center.Y - zone.HalfHeight - MonsterExclusionPad
                && world.Y <= zone.Center.Y + zone.HalfHeight + MonsterExclusionPad)
                return true;
        }

        foreach (var house in _houses)
        {
            var c = house.Center;
            if (HousingConstants.InPlot(world, c))
                return true;
        }

        return false;
    }

    private static WorldZone Zone(string id, string name, Vector2 center, float halfW, float halfH, TownStyle style) =>
        new()
        {
            Id = id,
            Name = name,
            Safe = true,
            Center = center,
            HalfWidth = halfW,
            HalfHeight = halfH,
            Style = style,
        };
}
