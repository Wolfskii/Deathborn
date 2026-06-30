using Microsoft.Xna.Framework;

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

/// <summary>Static zone layout for the Realik continent (tile anchors match server).</summary>
public static class WorldZones
{
    public const string WildernessId = "wilderness";

    private static IReadOnlyList<WorldZone> _towns = [];
    private static IReadOnlyList<(string A, string B)> _pathLinks = [];

    public static IReadOnlyList<WorldZone> Towns => _towns;
    public static IReadOnlyList<(string A, string B)> PathLinks => _pathLinks;

    public static void Initialize(WorldMap map)
    {
        var ts = map.TileSize;
        Vector2 Tile(int tx, int ty) => new((tx + 0.5f) * ts, (ty + 0.5f) * ts);

        _towns =
        [
            Zone("starter_town", "Starter Town", map.DefaultSpawn, 148, 128, TownStyle.Starter),
            Zone("northhaven", "Northhaven", Tile(234, 45), 136, 118, TownStyle.Castle),
            Zone("westmere", "Westmere", Tile(39, 189), 128, 112, TownStyle.Village),
            Zone("eastwatch", "Eastwatch", Tile(229, 128), 132, 116, TownStyle.Castle),
            Zone("southport", "Southport", Tile(112, 281), 140, 120, TownStyle.Port),
        ];

        _pathLinks =
        [
            ("starter_town", "northhaven"),
            ("starter_town", "eastwatch"),
            ("starter_town", "westmere"),
            ("starter_town", "southport"),
            ("northhaven", "eastwatch"),
            ("westmere", "southport"),
        ];
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

    public static bool IsSafe(Vector2 world) => ZoneAt(world)?.Safe == true;

    public static bool AllowPvP(Vector2 attacker, Vector2 target) =>
        !IsSafe(attacker) && !IsSafe(target);

    public static string DisplayName(Vector2 world) =>
        ZoneAt(world)?.Name ?? "The Wilderness";

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
