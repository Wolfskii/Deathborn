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

    /// <summary>One fence sprite cell, anchored bottom-centre at <see cref="World"/> (its depth-sort Y).</summary>
    public readonly record struct FenceCell(Rectangle Source, Vector2 World, float ArtScale);

    /// <summary>
    /// Fence cells for a plot (south entrance gate left open). Cells are drawn one by one in the
    /// exterior Y-sort, so a player north of a rail is hidden behind it and one south of it is not.
    /// </summary>
    public static void AppendCells(Vector2 center, List<FenceCell> cells)
    {
        HomesteadFence.GetLayout(center, out var layout);
        var artScale = layout.SpacingX / Cell;

        void Add(Rectangle src, int i, int j) =>
            cells.Add(new FenceCell(src, new Vector2(layout.Left + i * layout.SpacingX, layout.Top + j * layout.SpacingY), artScale));

        // North: same front-facing rail style as south (no gate).
        Add(CornerBL, 0, 0);
        for (var i = 1; i < layout.Nx; i++)
            Add(HorizRail, i, 0);
        Add(CornerBR, layout.Nx, 0);

        for (var j = 1; j < layout.Ny; j++)
        {
            Add(LeftEdge, 0, j);
            Add(RightEdge, layout.Nx, j);
        }

        Add(CornerBL, 0, layout.Ny);
        for (var i = 1; i < layout.Nx; i++)
        {
            if (i == layout.GateL)
            {
                Add(GateOpenL, i, layout.Ny);
                continue;
            }
            if (i == layout.GateL + 1)
                continue;
            if (i == layout.GateR)
            {
                Add(GateOpenR, i, layout.Ny);
                continue;
            }
            Add(HorizRail, i, layout.Ny);
        }
        Add(CornerBR, layout.Nx, layout.Ny);
    }

    /// <summary>Draw the whole fence at once (placement previews and other flat passes).</summary>
    public static void DrawPlot(
        SpriteBatch sb,
        Vector2 center,
        Vector2 camera,
        Vector2 screenCenter,
        float positionZoom,
        float visualZoom)
    {
        if (_sheet == null) return;
        var cells = new List<FenceCell>(40);
        AppendCells(center, cells);
        foreach (var cell in cells)
            DrawCell(sb, cell, camera, screenCenter, positionZoom);
    }

    public static void DrawCell(SpriteBatch sb, FenceCell cell, Vector2 camera, Vector2 screenCenter, float positionZoom)
    {
        if (_sheet == null) return;
        var screen = WorldToScreen(cell.World, camera, screenCenter, positionZoom);
        var origin = new Vector2(Cell * 0.5f, Cell);
        sb.Draw(_sheet, screen, cell.Source, Color.White, 0f, origin, cell.ArtScale * positionZoom, SpriteEffects.None, 0f);
    }

    private static Vector2 WorldToScreen(Vector2 world, Vector2 camera, Vector2 screenCenter, float zoom) =>
        screenCenter + (world - camera) * zoom;

    private static Rectangle CellRect(int col, int row) =>
        new(col * Cell, row * Cell, Cell, Cell);
}
