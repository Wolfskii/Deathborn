using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>16x16 icons for gathered, smelted and looted materials (scripts/install_farm_rpg_materials.py).</summary>
public static class MaterialIconAtlas
{
    private static readonly string[] Ids =
    [
        "logs", "copper_ore", "copper_bar", "iron_ore", "iron_bar",
        "bone", "orc_tusk", "bat_wing", "slime_gel", "boss_trophy",
    ];

    private static readonly Dictionary<string, Texture2D> Icons = new(StringComparer.Ordinal);

    public static void Load(ContentManager content)
    {
        Icons.Clear();
        foreach (var id in Ids)
        {
            try
            {
                Icons[id] = content.Load<Texture2D>($"Icons/materials/{id}");
            }
            catch (ContentLoadException)
            {
            }
        }
    }

    public static bool TryDraw(SpriteBatch sb, string? id, Rectangle dest)
    {
        if (string.IsNullOrEmpty(id) || !Icons.TryGetValue(id, out var tex))
            return false;
        sb.Draw(tex, dest, Color.White);
        return true;
    }
}
