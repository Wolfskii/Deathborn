namespace Deathborn.Client.Gameplay;

/// <summary>RuneScape-style skill progression (levels 1-99).</summary>
public static class SkillDefinitions
{
    public const int MaxLevel = 99;

    public static readonly string[] All =
    [
        "attack", "strength", "defense", "hitpoints",
        "woodcutting", "mining", "fishing", "farming", "cooking",
    ];

    public static readonly string[] Combat = ["attack", "strength", "defense", "hitpoints"];
    public static readonly string[] Gathering = ["woodcutting", "mining", "fishing", "farming"];
    public static readonly string[] Production = ["cooking"];

    public static string DisplayName(string id) => id switch
    {
        "attack" => "Attack",
        "strength" => "Strength",
        "defense" => "Defense",
        "hitpoints" => "Hitpoints",
        "woodcutting" => "Woodcutting",
        "mining" => "Mining",
        "fishing" => "Fishing",
        "farming" => "Farming",
        "cooking" => "Cooking",
        _ => id,
    };

    public static int LevelForXp(long xp)
    {
        if (xp <= 0) return 1;
        for (var level = MaxLevel; level >= 1; level--)
        {
            if (xp >= XpForLevel(level))
                return level;
        }
        return 1;
    }

    public static long XpForLevel(int level)
    {
        if (level <= 1) return 0;
        if (level > MaxLevel) level = MaxLevel;
        double points = 0;
        for (var i = 1; i < level; i++)
            points += Math.Floor(i + 300 * Math.Pow(2, i / 7.0));
        return (long)Math.Floor(points / 4);
    }

    public static long XpToNextLevel(long xp)
    {
        var level = LevelForXp(xp);
        if (level >= MaxLevel) return 0;
        return XpForLevel(level + 1) - xp;
    }

    public static float ProgressToNext(long xp)
    {
        var level = LevelForXp(xp);
        if (level >= MaxLevel) return 1f;
        var cur = XpForLevel(level);
        var next = XpForLevel(level + 1);
        if (next <= cur) return 1f;
        return (xp - cur) / (float)(next - cur);
    }

    public static float HitpointsMax(int hpLevel) => Math.Max(100, 90 + hpLevel * 10);
}
