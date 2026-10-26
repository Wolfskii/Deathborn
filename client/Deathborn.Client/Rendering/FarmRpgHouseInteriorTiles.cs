using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Farm RPG "Tileset House" sheet (16 px cells): floors in the top-right block, and wallpaper
/// groups four columns wide below it. In each group the first column is a narrow piece and the
/// next three are a left end, a repeatable middle and a right end. Wall rows come in pairs, an
/// upper face over a lower skirting row (e.g. rows 10/11, 16/17, 22/23).
/// </summary>
public static class FarmRpgHouseInteriorTiles
{
    public const int Cell = 16;

    private static Texture2D? _sheet;

    public static bool IsLoaded => _sheet != null;
    public static Texture2D? Sheet => _sheet;

    public static void Load(ContentManager content)
    {
        _sheet = null;
        try
        {
            _sheet = content.Load<Texture2D>("Characters/FarmRpg/Buildings/tileset_house");
        }
        catch
        {
            // Content may not be built yet.
        }
    }

    /// <summary>Draws one cell stretched to <paramref name="dest"/> (whole screen pixels, so tiles meet without seams).</summary>
    public static void DrawCell(SpriteBatch sb, Rectangle src, Rectangle dest, Color tint)
    {
        if (_sheet == null) return;
        sb.Draw(_sheet, dest, src, tint);
    }

    public static Rectangle CellRect(int col, int row) =>
        new(col * Cell, row * Cell, Cell, Cell);
}
