using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Farm RPG Tileset House — floors + wall panels for homestead interiors.</summary>
public static class FarmRpgHouseInteriorTiles
{
    public const int Cell = 16;

    private static Texture2D? _sheet;

    // Top-row floors (16×16 cells).
    public static readonly Rectangle FloorHall = CellRect(20, 0);
    public static readonly Rectangle FloorHallAlt = CellRect(21, 0);
    public static readonly Rectangle FloorBedroom = CellRect(48, 2);
    public static readonly Rectangle FloorKitchen = CellRect(25, 0);

    // Wall panel columns (peach / wood / teal-ish) — mid solid fill + wood trim.
    // Sheet layout: after top misc, color columns; peach ≈ col 20, wood ≈ col 0 area of wall strip.
    public static readonly Rectangle WallPeach = CellRect(20, 6);
    public static readonly Rectangle WallPeachTrim = CellRect(20, 8);
    public static readonly Rectangle WallWood = CellRect(1, 6);
    public static readonly Rectangle WallWoodTrim = CellRect(1, 8);
    public static readonly Rectangle WallTeal = CellRect(34, 6);
    public static readonly Rectangle WallTealTrim = CellRect(34, 8);
    public static readonly Rectangle Beam = CellRect(0, 6);

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

    public static void DrawTile(SpriteBatch sb, Rectangle src, Vector2 screen, float scale, Color tint)
    {
        if (_sheet == null) return;
        sb.Draw(_sheet, screen, src, tint, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static Rectangle CellRect(int col, int row) =>
        new(col * Cell, row * Cell, Cell, Cell);
}
