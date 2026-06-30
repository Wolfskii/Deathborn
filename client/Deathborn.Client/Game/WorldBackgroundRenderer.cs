using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public sealed class WorldBackgroundRenderer
{
    private const float HalfExtent = 2400f;
    private const float Cell = 64f;

    private static readonly Color Ground = new(0.18f, 0.28f, 0.16f);
    private static readonly Color CellA = new(0.20f, 0.32f, 0.18f);
    private static readonly Color CellB = new(0.16f, 0.25f, 0.14f);
    private static readonly Color Grid = new(0.32f, 0.45f, 0.28f, 0.55f);
    private static readonly Color Origin = new(1f, 0.85f, 0.2f, 0.9f);

    public void Draw(SpriteBatch sb, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        Vector2 ToScreen(Vector2 w) => (w - camera) * zoom + screenCenter;
        var cellPx = Cell * zoom;

        DrawPrimitives.FillRect(sb,
            new Rectangle(0, 0, (int)(screenCenter.X * 2), (int)(screenCenter.Y * 2)), Ground);

        var cols = (int)(HalfExtent * 2 / Cell) + 1;
        var start = new Vector2(-HalfExtent, -HalfExtent);

        for (var row = 0; row < cols; row++)
        for (var col = 0; col < cols; col++)
        {
            var world = start + new Vector2(col * Cell, row * Cell);
            var screen = ToScreen(world);
            var rect = new Rectangle((int)screen.X, (int)screen.Y, (int)cellPx, (int)cellPx);
            DrawPrimitives.FillRect(sb, rect, (row + col) % 2 == 0 ? CellA : CellB);
        }

        for (var i = 0; i <= cols; i++)
        {
            var xWorld = start.X + i * Cell;
            var yWorld = start.Y + i * Cell;
            var xScreen = (int)ToScreen(new Vector2(xWorld, -HalfExtent)).X;
            var yScreen = (int)ToScreen(new Vector2(-HalfExtent, yWorld)).Y;
            var xEnd = (int)ToScreen(new Vector2(xWorld, HalfExtent)).Y;
            var yEnd = (int)ToScreen(new Vector2(HalfExtent, yWorld)).X;

            DrawPrimitives.FillRect(sb,
                new Rectangle(xScreen, (int)ToScreen(new Vector2(xWorld, -HalfExtent)).Y, 1, (int)(HalfExtent * 2 * zoom)), Grid);
            DrawPrimitives.FillRect(sb,
                new Rectangle((int)ToScreen(new Vector2(-HalfExtent, yWorld)).X, yScreen, (int)(HalfExtent * 2 * zoom), 1), Grid);
        }

        var origin = ToScreen(Vector2.Zero);
        DrawPrimitives.DrawLine(sb, origin + new Vector2(-24 * zoom, 0), origin + new Vector2(24 * zoom, 0), Origin, 2);
        DrawPrimitives.DrawLine(sb, origin + new Vector2(0, -24 * zoom), origin + new Vector2(0, 24 * zoom), Origin, 2);
        DrawPrimitives.DrawCircleOutline(sb, origin, 8 * zoom, Origin);
    }
}
