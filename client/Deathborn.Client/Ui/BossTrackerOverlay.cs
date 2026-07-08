using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Edge-of-screen arrows pointing toward off-screen world bosses.</summary>
public sealed class BossTrackerOverlay
{
    private const float EdgePad = 42f;
    private const float ArrowSize = 18f;

    public void Draw(
        SpriteBatch sb, SpriteFont font,
        Vector2 cameraWorld, IEnumerable<WorldNpcEntity> bosses)
    {
        var center = GameViewport.Center;
        var halfW = GameViewport.Width / 2f - EdgePad;
        var halfH = GameViewport.Height / 2f - EdgePad;

        foreach (var boss in bosses)
        {
            var delta = boss.Position - cameraWorld;
            if (delta.LengthSquared() < 280f * 280f)
                continue;

            var dir = Vector2.Normalize(delta);
            var edge = ClampToScreenEdge(center, dir, halfW, halfH);
            DrawArrow(sb, edge, dir, boss.DefId);
            WorldNpcEntity.DrawBossIcon(sb, edge + dir * 14f, 0.75f);
        }
    }

    private static Vector2 ClampToScreenEdge(Vector2 center, Vector2 dir, float halfW, float halfH)
    {
        var tx = dir.X != 0 ? halfW / MathF.Abs(dir.X) : float.MaxValue;
        var ty = dir.Y != 0 ? halfH / MathF.Abs(dir.Y) : float.MaxValue;
        var t = MathF.Min(tx, ty);
        return center + dir * t;
    }

    private static void DrawArrow(SpriteBatch sb, Vector2 tip, Vector2 dir, string defId)
    {
        var color = defId switch
        {
            "iron_colossus" => new Color(200, 130, 70),
            "storm_wyrm" => new Color(100, 170, 240),
            "blight_herald" => new Color(110, 200, 80),
            _ => new Color(220, 90, 80),
        };
        var back = tip - dir * ArrowSize;
        var side = new Vector2(-dir.Y, dir.X) * (ArrowSize * 0.55f);
        DrawPrimitives.FillTriangle(sb, tip, back + side, back - side, color);
        DrawPrimitives.DrawCircleOutline(sb, tip - dir * 6f, 10f, color * 0.85f, 12, 2f);
    }
}
