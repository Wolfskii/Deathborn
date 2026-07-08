namespace Deathborn.Client.Gameplay;

public sealed class NpcCatalogEntry
{
    public required string DefId { get; init; }
    public required string Name { get; init; }
    public required NpcCategory Category { get; init; }
    public required NpcDisposition Disposition { get; init; }
    public required string SpriteId { get; init; }
    public float Radius { get; init; } = 14f;
    public float DisplayScale { get; init; } = 0.36f;
}

public static class NpcCatalog
{
    private static readonly Dictionary<string, NpcCatalogEntry> ById = new(StringComparer.Ordinal)
    {
        ["iron_colossus"] = new() { DefId = "iron_colossus", Name = "Iron Colossus", Category = NpcCategory.Boss, Disposition = NpcDisposition.Hostile, SpriteId = "", Radius = 24f },
        ["storm_wyrm"] = new() { DefId = "storm_wyrm", Name = "Storm Wyrm", Category = NpcCategory.Boss, Disposition = NpcDisposition.Hostile, SpriteId = "", Radius = 20f },
        ["blight_herald"] = new() { DefId = "blight_herald", Name = "Blight Herald", Category = NpcCategory.Boss, Disposition = NpcDisposition.Hostile, SpriteId = "", Radius = 22f },
        ["forest_skeleton"] = new() { DefId = "forest_skeleton", Name = "Skeleton", Category = NpcCategory.Monster, Disposition = NpcDisposition.Hostile, SpriteId = "skeleton", Radius = 14f },
        ["forest_slime"] = new() { DefId = "forest_slime", Name = "Slime", Category = NpcCategory.Monster, Disposition = NpcDisposition.Hostile, SpriteId = "slime", Radius = 12f, DisplayScale = 0.34f },
        ["forest_orc"] = new() { DefId = "forest_orc", Name = "Orc", Category = NpcCategory.Monster, Disposition = NpcDisposition.Hostile, SpriteId = "orc", Radius = 16f },
        ["wild_bat"] = new() { DefId = "wild_bat", Name = "Bat", Category = NpcCategory.WildAnimal, Disposition = NpcDisposition.Hostile, SpriteId = "bat", Radius = 10f, DisplayScale = 0.32f },
        ["town_guard"] = new() { DefId = "town_guard", Name = "Town Guard", Category = NpcCategory.Guard, Disposition = NpcDisposition.Friendly, SpriteId = "soldier", Radius = 14f },
        ["town_priest"] = new() { DefId = "town_priest", Name = "Priest", Category = NpcCategory.QuestNpc, Disposition = NpcDisposition.Friendly, SpriteId = "priest", Radius = 14f },
        ["town_wizard"] = new() { DefId = "town_wizard", Name = "Wizard", Category = NpcCategory.QuestNpc, Disposition = NpcDisposition.Friendly, SpriteId = "wizard", Radius = 14f },
    };

    public static NpcCatalogEntry Get(string defId)
    {
        if (ById.TryGetValue(defId, out var entry))
            return entry;
        return new NpcCatalogEntry
        {
            DefId = defId,
            Name = defId,
            Category = NpcCategory.Monster,
            Disposition = NpcDisposition.Hostile,
            SpriteId = "",
            Radius = WorldNpcEntity.DefaultRadius,
        };
    }

    public static NpcDisposition ParseDisposition(string? value) =>
        value switch
        {
            "friendly" => NpcDisposition.Friendly,
            "neutral" => NpcDisposition.Neutral,
            _ => NpcDisposition.Hostile,
        };

    public static NpcCategory ParseCategory(string? value, bool isBoss) =>
        value switch
        {
            "monster" => NpcCategory.Monster,
            "wild_animal" => NpcCategory.WildAnimal,
            "quest_npc" => NpcCategory.QuestNpc,
            "vendor" => NpcCategory.Vendor,
            "guard" => NpcCategory.Guard,
            "boss" => NpcCategory.Boss,
            _ => isBoss ? NpcCategory.Boss : NpcCategory.Monster,
        };
}
