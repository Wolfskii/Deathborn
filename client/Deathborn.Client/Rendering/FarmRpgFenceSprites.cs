using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>Farm RPG wooden fence sheet — homestead plot perimeter + gate.</summary>
public static class FarmRpgFenceSprites
{
    public const int Cell = 16;

    private static Texture2D? _sheet;

    // Non-snow set (top half of Fence Wood.png).
    private static readonly Rectangle CornerTL = CellRect(0, 0);
    private static readonly Rectangle TopEdge = CellRect(1, 0);
    private static readonly Rectangle CornerTR = CellRect(2, 0);
    private static readonly Rectangle LeftEdge = CellRect(0, 1);
    private static readonly Rectangle RightEdge = CellRect(2, 1);
    private static readonly Rectangle CornerBL = CellRect(0, 2);
    private static readonly Rectangle BottomEdge = CellRect(1, 2);
    private static readonly Rectangle CornerBR = CellRect(2, 2);
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

        var left = center.X - HousingConstants.PlotHalfW;
        var top = center.Y - HousingConstants.PlotHalfH;
        var spanX = HousingConstants.PlotHalfW * 2f;
        var spanY = HousingConstants.PlotHalfH * 2f;

        var stepHint = Config.WorldTileSize;
        var nx = Math.Max(4, (int)MathF.Round(spanX / stepHint));
        var ny = Math.Max(4, (int)MathF.Round(spanY / stepHint));
        var spacingX = spanX / nx;
        var spacingY = spanY / ny;
        var scale = (spacingX / Cell) * zoom;
        var origin = new Vector2(Cell * 0.5f, Cell);

        // South gate: open posts with one empty cell between.
        var gateL = Math.Clamp(nx / 2 - 1, 1, nx - 3);
        var gateR = gateL + 2;

        Vector2 Pos(int i, int j) => new(left + i * spacingX, top + j * spacingY);

        DrawCell(sb, CornerTL, Pos(0, 0), camera, screenCenter, zoom, scale, origin);
        for (var i = 1; i < nx; i++)
            DrawCell(sb, TopEdge, Pos(i, 0), camera, screenCenter, zoom, scale, origin);
        DrawCell(sb, CornerTR, Pos(nx, 0), camera, screenCenter, zoom, scale, origin);

        for (var j = 1; j < ny; j++)
        {
            DrawCell(sb, LeftEdge, Pos(0, j), camera, screenCenter, zoom, scale, origin);
            DrawCell(sb, RightEdge, Pos(nx, j), camera, screenCenter, zoom, scale, origin);
        }

        DrawCell(sb, CornerBL, Pos(0, ny), camera, screenCenter, zoom, scale, origin);
        for (var i = 1; i < nx; i++)
        {
            if (i == gateL)
            {
                DrawCell(sb, GateOpenL, Pos(i, ny), camera, screenCenter, zoom, scale, origin);
                continue;
            }
            if (i == gateL + 1)
                continue;
            if (i == gateR)
            {
                DrawCell(sb, GateOpenR, Pos(i, ny), camera, screenCenter, zoom, scale, origin);
                continue;
            }
            DrawCell(sb, BottomEdge, Pos(i, ny), camera, screenCenter, zoom, scale, origin);
        }
        DrawCell(sb, CornerBR, Pos(nx, ny), camera, screenCenter, zoom, scale, origin);
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
