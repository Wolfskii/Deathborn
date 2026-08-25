namespace Deathborn.Client.Gameplay;

public sealed class FishInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool Sea { get; init; }
    public int Level { get; init; }
}

public static class FishCatalog
{
    public const float TileSize = 16f;
    public const float ActionRange = 48f;
    public const int MaxChebyshev = 3;

    public static readonly IReadOnlyList<FishInfo> All =
    [
        F("sunfish", "Sunfish", 1),
        F("chub", "Chub", 1),
        F("perch", "Perch", 3),
        F("carp", "Carp", 4),
        F("largemouth_bass", "Largemouth Bass", 6),
        F("pike", "Pike", 8),
        F("tiger_trout", "Tiger Trout", 9),
        F("walleye", "Walleye", 10),
        F("sturgeon", "Sturgeon", 14),
        F("golden_fish", "Golden Fish", 18),
        F("anchovy", "Anchovy", 1, sea: true),
        F("sardine", "Sardine", 1, sea: true),
        F("herring", "Herring", 3, sea: true),
        F("salmon", "Salmon", 5, sea: true),
        F("red_snapper", "Red Snapper", 6, sea: true),
        F("tuna", "Tuna", 8, sea: true),
        F("flounder", "Flounder", 7, sea: true),
        F("pufferfish", "Pufferfish", 11, sea: true),
        F("albacore", "Albacore", 12, sea: true),
        F("anglerfish", "Anglerfish", 20, sea: true),
    ];

    public static bool IsRod(string? id) => id == "fishing_rod";
    public static bool IsBait(string? id) => id == "worm_bait";
    public static bool IsFish(string? id) => id != null && All.Any(f => f.Id == id);

    public static FishInfo? PreviewFish(bool sea, int level)
    {
        FishInfo? best = null;
        foreach (var f in All)
        {
            if (f.Sea != sea || f.Level > Math.Max(1, level) + 2) continue;
            if (best == null || f.Level > best.Level)
                best = f;
        }
        return best ?? All.FirstOrDefault(f => f.Sea == sea);
    }

    private static FishInfo F(string id, string name, int level, bool sea = false) => new()
    {
        Id = id, Name = name, Level = level, Sea = sea,
    };
}
