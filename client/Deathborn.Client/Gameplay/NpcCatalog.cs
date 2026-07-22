namespace Deathborn.Client.Gameplay;

public sealed class NpcCatalogEntry
{
    public required string DefId { get; init; }
    public required string Name { get; init; }
    public required NpcCategory Category { get; init; }
    public required NpcDisposition Disposition { get; init; }
    public required string SpriteId { get; init; }
    public float Radius { get; init; } = 14f;
    // Tiny RPG strips are ~15px art centered in 100px cells; scale compensates vs tight player atlas.
    // Humanoid mobs target ~92% of FarmRpg player draw scale (1.5×1.35 ≈ 2.025).
    public float DisplayScale { get; init; } = 2.1f;
    public const float HumanoidMobDisplayScale = 1.86f;
    /// <summary>When true, server chases and melee-attacks players within AggroRange.</summary>
    public bool Aggro { get; init; }
    /// <summary>Detection radius in world px (unscaled catalog value; server scales by tile size).</summary>
    public float AggroRange { get; init; }
    /// <summary>Unscaled art px — half-width of body hit box (before DisplayScale).</summary>
    public float HitHalfWidth { get; init; } = 22f;
    /// <summary>Unscaled art px — half-height from hit center to top/bottom edge.</summary>
    public float HitHalfHeight { get; init; } = 28f;
    /// <summary>Unscaled art px — hit center Y offset from foot anchor (negative = up).</summary>
    public float HitCenterYOffset { get; init; } = -26f;
    /// <summary>Extra multiplier on hit half extents so shots graze the sprite outline.</summary>
    public float HitPadding { get; init; } = 1.12f;
}

public static class NpcCatalog
{
    private static readonly Dictionary<string, NpcCatalogEntry> ById = new(StringComparer.Ordinal)
    {
        ["iron_colossus"] = new() { DefId = "iron_colossus", Name = "Iron Colossus", Category = NpcCategory.Boss, Disposition = NpcDisposition.Hostile, SpriteId = "", Radius = 24f },
        ["storm_wyrm"] = new() { DefId = "storm_wyrm", Name = "Storm Wyrm", Category = NpcCategory.Boss, Disposition = NpcDisposition.Hostile, SpriteId = "", Radius = 20f },
        ["blight_herald"] = new() { DefId = "blight_herald", Name = "Blight Herald", Category = NpcCategory.Boss, Disposition = NpcDisposition.Hostile, SpriteId = "", Radius = 22f },
        ["forest_skeleton"] = new() { DefId = "forest_skeleton", Name = "Skeleton", Category = NpcCategory.Monster, Disposition = NpcDisposition.Hostile, SpriteId = "skeleton", Radius = 14f, DisplayScale = NpcCatalogEntry.HumanoidMobDisplayScale, Aggro = true, AggroRange = 40f, HitHalfWidth = 20f, HitHalfHeight = 28f, HitCenterYOffset = -26f },
        // SpriteId / scale / hit box come from Farm RPG slime variant (slime_{color}_{size}) at spawn.
        ["forest_slime"] = new() { DefId = "forest_slime", Name = "Slime", Category = NpcCategory.Monster, Disposition = NpcDisposition.Hostile, SpriteId = "slime_green_normal", Radius = 12f, DisplayScale = 2.05f, Aggro = true, AggroRange = 48f, HitHalfWidth = 8f, HitHalfHeight = 6f, HitCenterYOffset = -12f },
        ["forest_orc"] = new() { DefId = "forest_orc", Name = "Orc", Category = NpcCategory.Monster, Disposition = NpcDisposition.Hostile, SpriteId = "orc", Radius = 16f, DisplayScale = NpcCatalogEntry.HumanoidMobDisplayScale, Aggro = true, AggroRange = 32f, HitHalfWidth = 24f, HitHalfHeight = 30f, HitCenterYOffset = -28f },
        ["wild_bat"] = new() { DefId = "wild_bat", Name = "Bat", Category = NpcCategory.WildAnimal, Disposition = NpcDisposition.Hostile, SpriteId = "bat", Radius = 10f, DisplayScale = 1.45f, Aggro = true, AggroRange = 72f, HitHalfWidth = 14f, HitHalfHeight = 28f, HitCenterYOffset = -30f },
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
