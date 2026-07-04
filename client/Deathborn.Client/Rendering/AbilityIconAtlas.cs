using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>5×4 ability/item icon sheet (1024×819) — see Content/Icons/ability_sheet.png.</summary>
public static class AbilityIconAtlas
{
    public const int Columns = 5;
    public const int Rows = 4;

    private static Texture2D? _sheet;
    private static int _cellWidth;
    private static int _cellHeight;

    private static readonly Dictionary<string, Point> Cells = new()
    {
        ["slash"] = new(0, 0),
        ["shield_bash"] = new(1, 0),
        ["whirlwind"] = new(2, 0),
        ["warrior_dash"] = new(3, 0),
        ["fireball"] = new(0, 1),
        ["ice_shard"] = new(1, 1),
        ["arc_bolt"] = new(2, 1),
        ["blood_bolt"] = new(3, 1),
        ["poison_cloud"] = new(4, 1),
        ["battle_shout"] = new(0, 2),
        ["iron_skin"] = new(1, 2),
        ["hunter_mark"] = new(2, 2),
        ["bandage"] = new(3, 2),
        ["second_wind"] = new(4, 2),
        ["health_potion"] = new(0, 3),
        ["mana_potion"] = new(1, 3),
        ["stamina_potion"] = new(2, 3),
        ["antidote"] = new(3, 3),
        ["house_key"] = new(4, 3),
    };

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content)
    {
        _sheet = content.Load<Texture2D>("Icons/ability_sheet");
        _cellWidth = _sheet.Width / Columns;
        _cellHeight = _sheet.Height / Rows;
    }

    public static bool HasIcon(string? id) =>
        !string.IsNullOrEmpty(id) && Cells.ContainsKey(id);

    public static bool TryDraw(SpriteBatch sb, string? id, Rectangle dest)
    {
        if (_sheet == null || string.IsNullOrEmpty(id) || !Cells.TryGetValue(id, out var cell))
            return false;

        var src = new Rectangle(cell.X * _cellWidth, cell.Y * _cellHeight, _cellWidth, _cellHeight);
        sb.Draw(_sheet, dest, src, Color.White);
        return true;
    }
}
