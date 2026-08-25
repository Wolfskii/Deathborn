using Microsoft.Xna.Framework;

namespace Deathborn.Client.Gameplay;

/// <summary>Stardew-style crop and animal defs — keep in sync with server game/farm.go.</summary>
public sealed class CropInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SeedItem { get; init; }
    public required string ProduceItem { get; init; }
    public int Stages { get; init; }
    public bool Regrows { get; init; }
}

public sealed class FarmAnimalInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Item { get; init; }
    public string Product { get; init; } = "";
    public string ProductName { get; init; } = "";
}

public static class FarmCatalog
{
    public const float TileSize = 16f;
    public const float ActionRange = 40f;
    public const int MaxTiles = 24;
    public const int MaxAnimals = 4;

    public static readonly IReadOnlyDictionary<string, CropInfo> Crops = new Dictionary<string, CropInfo>
    {
        ["parsnip"] = C("parsnip", "Parsnip", 5),
        ["carrot"] = C("carrot", "Carrot", 6),
        ["potato"] = C("potato", "Potato", 6),
        ["strawberry"] = C("strawberry", "Strawberry", 6, regrows: true),
        ["tomato"] = C("tomato", "Tomato", 7, regrows: true),
        ["wheat"] = C("wheat", "Wheat", 6),
        ["melon"] = C("melon", "Melon", 6),
        ["pumpkin"] = C("pumpkin", "Pumpkin", 5),
        ["corn"] = C("corn", "Corn", 8, regrows: true),
        ["beetroot"] = C("beetroot", "Beetroot", 6),
    };

    public static readonly IReadOnlyDictionary<string, FarmAnimalInfo> Animals = new Dictionary<string, FarmAnimalInfo>
    {
        ["chicken"] = A("chicken", "Chicken", "chicken_egg", "Chicken Egg"),
        ["cow"] = A("cow", "Cow", "milk", "Milk"),
        ["sheep"] = A("sheep", "Sheep", "wool", "Wool"),
        ["pig"] = A("pig", "Pig"),
    };

    private static readonly Dictionary<string, string> SeedToCrop = Crops.Values
        .ToDictionary(c => c.SeedItem, c => c.Id, StringComparer.Ordinal);

    private static CropInfo C(string id, string name, int stages, bool regrows = false) => new()
    {
        Id = id, Name = name, SeedItem = id + "_seeds", ProduceItem = id, Stages = stages, Regrows = regrows,
    };

    private static FarmAnimalInfo A(string id, string name, string product = "", string productName = "") => new()
    {
        Id = id, Name = name, Item = id, Product = product, ProductName = productName,
    };

    public static CropInfo? Crop(string? id) =>
        id != null && Crops.TryGetValue(id, out var c) ? c : null;

    public static CropInfo? CropBySeed(string? seedId) =>
        seedId != null && SeedToCrop.TryGetValue(seedId, out var id) ? Crops[id] : null;

    public static FarmAnimalInfo? Animal(string? id) =>
        id != null && Animals.TryGetValue(id, out var a) ? a : null;

    public static bool IsTool(string? id) => id is "hoe" or "watering_can";

    public static bool IsSeed(string? id) => id != null && SeedToCrop.ContainsKey(id);

    public static bool IsAnimalItem(string? id) => id != null && Animals.ContainsKey(id);

    public static bool IsFeed(string? id) => id == "animal_feed";

    public static bool IsFarmHotbarItem(string? id) =>
        IsTool(id) || IsSeed(id) || IsAnimalItem(id) || IsFeed(id);

    public static int WorldTileX(float worldX) => (int)MathF.Floor(worldX / TileSize);

    public static int WorldTileY(float worldY) => (int)MathF.Floor(worldY / TileSize);

    public static Vector2 TileCenter(int tx, int ty) =>
        new((tx + 0.5f) * TileSize, (ty + 0.5f) * TileSize);

    public static Vector2 TileOrigin(int tx, int ty) =>
        new(tx * TileSize, ty * TileSize);

    public static string CropInteractId(long houseId, int tx, int ty) =>
        $"house_{houseId}_tile_{tx}_{ty}";

    public static string AnimalInteractId(long houseId, long animalId) =>
        $"house_{houseId}_animal_{animalId}";

    public static bool TryParseCropInteract(string id, out long houseId, out int tx, out int ty)
    {
        houseId = 0; tx = 0; ty = 0;
        // house_{id}_tile_{tx}_{ty}
        var parts = id.Split('_');
        if (parts.Length != 5 || parts[0] != "house" || parts[2] != "tile")
            return false;
        return long.TryParse(parts[1], out houseId)
            && int.TryParse(parts[3], out tx)
            && int.TryParse(parts[4], out ty);
    }

    public static bool TryParseAnimalInteract(string id, out long houseId, out long animalId)
    {
        houseId = 0; animalId = 0;
        var parts = id.Split('_');
        if (parts.Length != 4 || parts[0] != "house" || parts[2] != "animal")
            return false;
        return long.TryParse(parts[1], out houseId) && long.TryParse(parts[3], out animalId);
    }
}
