namespace Deathborn.Client.Gameplay;

public enum NpcDisposition
{
    Hostile,
    Neutral,
    Friendly,
}

public enum NpcCategory
{
    Boss,
    Monster,
    WildAnimal,
    QuestNpc,
    Vendor,
    Guard,
}

public static class NpcCategoryRules
{
    public static bool BlocksTowns(NpcCategory category) =>
        category is NpcCategory.Monster or NpcCategory.WildAnimal or NpcCategory.Boss;

    public static bool IsAttackable(NpcDisposition disposition) =>
        disposition is NpcDisposition.Hostile or NpcDisposition.Neutral;
}
