using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>Farm RPG wooden fence sheet — homestead plot perimeter + south gate.</summary>
public static class FarmRpgFenceSprites
{
    public const int Cell = 16;

    private static Texture2D? _sheet;

    // Non-snow set (top half of Fence Wood.png).
    // Horizontal runs use the front-facing rail piece (BottomEdge) — the north autotile
    // (TopEdge) is a behind-the-fence post cap that looks broken when tiled along the top.
    private static readonly Rectangle CornerBL = CellRect(0, 2);
    private static readonly Rectangle HorizRail = CellRect(1, 2);
    private static readonly Rectangle CornerBR = CellRect(2, 2);
    private static readonly Rectangle LeftEdge = CellRect(0, 1);
    private static readonly Rectangle RightEdge = CellRect(2, 1);
    private static readonly Rectangle GateOpenL = CellRect(3, 2);
    private static readonly Rectangle GateOpenR = CellRect(5, 2);

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content)
    {
        _sheet = null;
        try
        {
            _sheet = content.Load<Texture2D>("Characters/FarmRpg/Buildings/fence_wood");
        }
        catch
        {
            // Content may not be built yet.
        }
    }

    /// <summary>Draw soft yard fill + wooden fence with a south entrance gate.</summary>
    public static void DrawPlot(
        SpriteBatch sb,
        Vector2 center,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom)
    {
        DrawLand(sb, center, camera, screenCenter, zoom);
        if (_sheet == null) return;

        HomesteadFence.GetLayout(center, out var layout);
        var scale = (layout.SpacingX / Cell) * zoom;
        var origin = new Vector2(Cell * 0.5f, Cell);

        Vector2 Pos(int i, int j) =>
            new(layout.Left + i * layout.SpacingX, layout.Top + j * layout.SpacingY);

        // North: same front-facing rail style as south (no gate).
        DrawCell(sb, CornerBL, Pos(0, 0), camera, screenCenter, zoom, scale, origin);
        for (var i = 1; i < layout.Nx; i++)
            DrawCell(sb, HorizRail, Pos(i, 0), camera, screenCenter, zoom, scale, origin);
        DrawCell(sb, CornerBR, Pos(layout.Nx, 0), camera, screenCenter, zoom, scale, origin);

        for (var j = 1; j < layout.Ny; j++)
        {
            DrawCell(sb, LeftEdge, Pos(0, j), camera, screenCenter, zoom, scale, origin);
            DrawCell(sb, RightEdge, Pos(layout.Nx, j), camera, screenCenter, zoom, scale, origin);
        }

        DrawCell(sb, CornerBL, Pos(0, layout.Ny), camera, screenCenter, zoom, scale, origin);
        for (var i = 1; i < layout.Nx; i++)
        {
            if (i == layout.GateL)
            {
                DrawCell(sb, GateOpenL, Pos(i, layout.Ny), camera, screenCenter, zoom, scale, origin);
                continue;
            }
            if (i == layout.GateL + 1)
                continue;
            if (i == layout.GateR)
            {
                DrawCell(sb, GateOpenR, Pos(i, layout.Ny), camera, screenCenter, zoom, scale, origin);
                continue;
            }
            DrawCell(sb, HorizRail, Pos(i, layout.Ny), camera, screenCenter, zoom, scale, origin);
        }
        DrawCell(sb, CornerBR, Pos(layout.Nx, layout.Ny), camera, screenCenter, zoom, scale, origin);
    }

    private static void DrawLand(
        SpriteBatch sb,
        Vector2 center,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom)
    {
        var tl = WorldToScreen(center + new Vector2(-HousingConstants.PlotHalfW, -HousingConstants.PlotHalfH), camera, screenCenter, zoom);
        var br = WorldToScreen(center + new Vector2(HousingConstants.PlotHalfW, HousingConstants.PlotHalfH), camera, screenCenter, zoom);
        var plot = new Rectangle(
            (int)tl.X,
            (int)tl.Y,
            Math.Max(1, (int)(br.X - tl.X)),
            Math.Max(1, (int)(br.Y - tl.Y)));
        DrawPrimitives.FillRect(sb, plot, new Color(0.24f, 0.42f, 0.22f, 0.38f));
    }

    private static void DrawCell(
        SpriteBatch sb,
        Rectangle src,
        Vector2 world,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        float scale,
        Vector2 origin)
    {
        if (_sheet == null) return;
        var screen = WorldToScreen(world, camera, screenCenter, zoom);
        sb.Draw(_sheet, screen, src, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static Vector2 WorldToScreen(Vector2 world, Vector2 camera, Vector2 screenCenter, float zoom) =>
        screenCenter + (world - camera) * zoom;

    private static Rectangle CellRect(int col, int row) =>
        new(col * Cell, row * Cell, Cell, Cell);
}
