using Microsoft.Xna.Framework;

namespace Deathborn.Client.Gameplay;

public static class BuffCatalog
{
    public static (string Name, string Description, Color Tint) Describe(string buffId) => buffId switch
    {
        "battle_shout" => ("Battle Shout", "+25% damage dealt", new Color(220, 140, 60)),
        "iron_skin" => ("Iron Skin", "30% less damage taken", new Color(150, 170, 200)),
        "hunter_mark" => ("Hunter's Mark", "Track marked foe", new Color(220, 80, 90)),
        _ => (buffId, "", Color.Gray),
    };
}
