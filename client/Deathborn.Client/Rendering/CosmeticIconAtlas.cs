using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>5x4 cosmetic icon sheet (1024x819) — Content/Icons/cosmetic_sheet.png</summary>
public static class CosmeticIconAtlas
{
    public const int Columns = 5;
    public const int Rows = 4;

    private static Texture2D? _sheet;
    private static int _cellWidth;
    private static int _cellHeight;

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
            _cellWidth = _sheet.Width / Columns;
            _cellHeight = _sheet.Height / Rows;
        }
        catch
        {
            _sheet = null;
            _cellWidth = 0;
            _cellHeight = 0;
        }
    }

    public static bool HasIcon(string? id) =>
        !string.IsNullOrEmpty(id) && Cells.ContainsKey(id);

    public static bool TryDraw(SpriteBatch sb, string? id, Rectangle dest, bool trimFrame = true)
    {
        if (_sheet == null || string.IsNullOrEmpty(id) || !Cells.TryGetValue(id, out var cell))
            return false;

        var src = new Rectangle(cell.X * _cellWidth, cell.Y * _cellHeight, _cellWidth, _cellHeight);
        if (trimFrame)
        {
            var insetX = Math.Max(2, _cellWidth / 10);
            var insetY = Math.Max(2, _cellHeight / 10);
            src = new Rectangle(
                src.X + insetX, src.Y + insetY,
                src.Width - insetX * 2, src.Height - insetY * 2);
        }

        sb.Draw(_sheet, dest, src, Color.White);
        return true;
    }
}
