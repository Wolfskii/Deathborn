using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>RS-style stone bowl with chunky animated pixel flames.</summary>
public sealed class PixelBrazier
{
    private const int Pixel = 4;

    private static readonly Color Stone = new(58, 52, 44);
    private static readonly Color StoneDark = new(38, 34, 28);
    private static readonly Color StoneLight = new(78, 70, 58);
    private static readonly Color Rim = new(175, 138, 58);
    private static readonly Color RimGlow = new(210, 170, 80);
    private static readonly Color FlameYellow = new(255, 238, 90);
    private static readonly Color FlameOrange = new(255, 155, 45);
    private static readonly Color FlameRed = new(215, 58, 22);
    private static readonly Color FlameCore = new(255, 255, 180);

    private static readonly Color?[][][] FlameFrames =
    [
        ParseFlame(
            "..Y...",
            ".YOY..",
            "YOROY.",
            "YORRRY",
            ".YOOY.",
            "..OY..",
            "...Y.."),
        ParseFlame(
            "...Y..",
            "..YOY.",
            ".YOROY",
            "YORRRY",
            ".YOOY.",
            "..OY..",
            "...Y.."),
        ParseFlame(
            "..Y...",
            ".YOYY.",
            "YOROY.",
            "YORRRY",
            ".YOOY.",
            "..OY..",
            "....Y."),
        ParseFlame(
            "...Y..",
            "..YOY.",
            ".YOROY",
            ".ORRRY",
            "YOOOY.",
            "..OY..",
            "...Y.."),
    ];

    private double _time;

    public void Update(GameTime gameTime) => _time += gameTime.ElapsedGameTime.TotalSeconds;

    public void Draw(SpriteBatch sb, int centerX, int bottomY)
    {
        var frame = ((int)(_time / 0.11) % FlameFrames.Length + FlameFrames.Length) % FlameFrames.Length;
        var flame = FlameFrames[frame];
        var flameW = flame[0].Length;
        var flameH = flame.Length;
        var bowlW = 13;
        var bowlH = 5;
        var totalH = (flameH + bowlH + 1) * Pixel;
        var left = centerX - Math.Max(flameW, bowlW) * Pixel / 2;
        var top = bottomY - totalH;

        DrawGlow(sb, centerX, top + flameH * Pixel / 2, flameH * Pixel);

        var flameLeft = centerX - flameW * Pixel / 2;
        DrawFlame(sb, flameLeft, top, flame);

        var bowlLeft = centerX - bowlW * Pixel / 2;
        var bowlTop = top + flameH * Pixel;
        DrawBowl(sb, bowlLeft, bowlTop, bowlW, bowlH);

        DrawBowlRim(sb, bowlLeft, bowlTop, bowlW);
    }

    private static void DrawGlow(SpriteBatch sb, int centerX, int centerY, int radius)
    {
        var glow = new Color(255, 120, 30, 28);
        DrawPrimitives.FillCircle(sb, new Vector2(centerX, centerY), radius * 0.55f, glow);
        DrawPrimitives.FillCircle(sb, new Vector2(centerX, centerY), radius * 0.35f, new Color(255, 180, 60, 18));
    }

    private static void DrawFlame(SpriteBatch sb, int left, int top, Color?[][] grid)
    {
        for (var y = 0; y < grid.Length; y++)
        for (var x = 0; x < grid[y].Length; x++)
        {
            var color = grid[y][x];
            if (color is null) continue;
            DrawPixel(sb, left + x * Pixel, top + y * Pixel, color.Value);
        }
    }

    private static void DrawBowl(SpriteBatch sb, int left, int top, int w, int h)
    {
        for (var row = 0; row < h; row++)
        {
            var inset = row;
            var width = w - inset * 2;
            if (width <= 0) continue;
            var x = left + inset * Pixel;
            var y = top + row * Pixel;
            var shade = row == 0 ? StoneLight : row == h - 1 ? StoneDark : Stone;
            DrawPixelRect(sb, x, y, width * Pixel, Pixel, shade);
        }

        DrawPixelRect(sb, left + Pixel * 2, top + h * Pixel, (w - 4) * Pixel, Pixel, StoneDark);
    }

    private static void DrawBowlRim(SpriteBatch sb, int left, int top, int w)
    {
        var rimLeft = left + Pixel * 2;
        var rimW = (w - 4) * Pixel;
        DrawPixelRect(sb, rimLeft, top - Pixel, rimW, Pixel, Rim);
        DrawPixelRect(sb, rimLeft + Pixel, top - Pixel, rimW - Pixel * 2, Pixel, RimGlow);
    }

    private static void DrawPixel(SpriteBatch sb, int x, int y, Color color) =>
        DrawPrimitives.FillRect(sb, new Rectangle(x, y, Pixel, Pixel), color);

    private static void DrawPixelRect(SpriteBatch sb, int x, int y, int w, int h, Color color) =>
        DrawPrimitives.FillRect(sb, new Rectangle(x, y, w, h), color);

    private static Color?[][] ParseFlame(params string[] rows)
    {
        return rows.Select(row => row.Select(ParseFlameCell).ToArray()).ToArray();
    }

    private static Color? ParseFlameCell(char c) => c switch
    {
        'Y' => FlameYellow,
        'O' => FlameOrange,
        'R' => FlameRed,
        'C' => FlameCore,
        _ => null,
    };
}
