namespace Deathborn.Client.Gameplay;

public sealed class CookRecipeInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Result { get; init; }
    public string Ingredient { get; init; } = "";
    public bool AnyFish { get; init; }
    public int Heal { get; init; }
    public int Stamina { get; init; }
}

public static class CookCatalog
{
    public const float ActionRange = 48f;

    public static readonly IReadOnlyList<CookRecipeInfo> Recipes =
    [
        R("fried_egg", "Fried Egg", "chicken_egg", heal: 22, stamina: 8),
        R("parsnip_soup", "Parsnip Soup", "parsnip", heal: 26, stamina: 6),
        R("baked_potato", "Baked Potato", "potato", heal: 24, stamina: 6),
        R("pumpkin_pie", "Pumpkin Pie", "pumpkin", heal: 42, stamina: 12),
        R("bread", "Bread", "wheat", heal: 18, stamina: 10),
        R("jam", "Jam", "strawberry", heal: 16, stamina: 18),
        new()
        {
            Id = "baked_fish", Name = "Baked Fish", Result = "baked_fish",
            AnyFish = true, Heal = 32, Stamina = 8,
        },
    ];

    public static bool IsCookingStation(string? type) =>
        type is "kitchen" or "fireplace";

    public static bool IsMeal(string? id) =>
        id != null && Recipes.Any(r => r.Result == id);

    public static CookRecipeInfo? RecipeFor(string? itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        foreach (var r in Recipes)
        {
            if (r.Ingredient == itemId) return r;
        }
        if (FishCatalog.IsFish(itemId))
            return Recipes.FirstOrDefault(r => r.AnyFish);
        return null;
    }

    public static bool IsIngredient(string? id) => RecipeFor(id) != null;

    public static string StationName(string type) => type == "fireplace" ? "Fireplace" : "Kitchen";

    private static CookRecipeInfo R(string id, string name, string ingredient, int heal, int stamina) => new()
    {
        Id = id, Name = name, Result = id, Ingredient = ingredient, Heal = heal, Stamina = stamina,
    };
}
