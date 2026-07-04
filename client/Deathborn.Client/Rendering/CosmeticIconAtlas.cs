using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>5x4 cosmetic icon sheet (320x256 at 64px cells) — Content/Icons/cosmetic_sheet.png</summary>
public static class CosmeticIconAtlas
{
    public const int Columns = 5;
    public const int Rows = 4;
    public const int CellSize = 64;

    private static Texture2D? _sheet;

    private static readonly Dictionary<string, Point> Cells = new()
    {
        ["santa_hat"] = new(0, 0),
        ["party_hat"] = new(1, 0),
        ["jester_cap"] = new(2, 0),
        ["bucket_helmet"] = new(3, 0),
        ["pirate_hat"] = new(4, 0),
        ["propeller_hat"] = new(0, 1),
        ["bunny_ears"] = new(1, 1),
        ["top_hat"] = new(2, 1),
        ["traffic_cone"] = new(3, 1),
        ["beer_helm"] = new(4, 1),
        ["wizard_hat_torn"] = new(0, 2),
        ["crown_of_bones"] = new(1, 2),
        ["rubber_chicken_hat"] = new(2, 2),
        ["grim_hood"] = new(3, 2),
        ["gold_helm_rusty"] = new(4, 2),
        ["fedora_of_shame"] = new(0, 3),
        ["clown_nose_glasses"] = new(1, 3),
        ["severed_elf_hat"] = new(2, 3),
    };

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content)
    {
        try
        {
            _sheet = content.Load<Texture2D>("Icons/cosmetic_sheet");
        }
        catch
        {
            _sheet = null;
        }
    }

    public static bool HasIcon(string? id) =>
        !string.IsNullOrEmpty(id) && Cells.ContainsKey(id);

    public static bool TryDraw(SpriteBatch sb, string? id, Rectangle dest)
    {
        if (_sheet == null || string.IsNullOrEmpty(id) || !Cells.TryGetValue(id, out var cell))
            return false;

        var cellW = _sheet.Width / Columns;
        var cellH = _sheet.Height / Rows;
        var src = new Rectangle(cell.X * cellW, cell.Y * cellH, cellW, cellH);
        sb.Draw(_sheet, dest, src, Color.White);
        return true;
    }
}
