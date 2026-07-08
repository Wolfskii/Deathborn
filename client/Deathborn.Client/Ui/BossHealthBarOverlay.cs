using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Large boss health bar when the local player is near a world boss.</summary>
public sealed class BossHealthBarOverlay
{
    private const float ShowRange = 420f;

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 cameraWorld, IEnumerable<WorldNpcEntity> bosses)
    {
        WorldNpcEntity? closest = null;
        var bestDist = ShowRange * ShowRange;
        foreach (var b in bosses)
        {
            var d = Vector2.DistanceSquared(b.Position, cameraWorld);
            if (d < bestDist)
            {
                bestDist = d;
                closest = b;
            }
        }

        if (closest == null || closest.HpMax <= 0) return;

        var panelW = Math.Min(520, GameViewport.Width - 80);
        var panelH = 46;
        var x = (GameViewport.Width - panelW) / 2;
        var y = 52;
        var panel = new Rectangle(x, y, panelW, panelH);

        DrawPrimitives.FillRect(sb, panel, new Color(16, 12, 10, 210));
        DrawBorder(sb, panel, new Color(190, 120, 50));

        var name = SpriteFontSafe.Filter(closest.Name);
        SpriteFontSafe.DrawString(sb, font, name, new Vector2(panel.X + 12, panel.Y + 6), new Color(255, 220, 150));
        WorldNpcEntity.DrawBossIcon(sb, new Vector2(panel.Right - 22, panel.Y + 18), 0.85f);

        var bar = new Rectangle(panel.X + 12, panel.Y + 26, panel.Width - 24, 12);
        DrawPrimitives.FillRect(sb, bar, new Color(30, 22, 18));
        var pct = Math.Clamp(closest.Hp / closest.HpMax, 0f, 1f);
        var fillW = (int)((bar.Width - 2) * pct);
        if (fillW > 0)
            DrawPrimitives.FillRect(sb, new Rectangle(bar.X + 1, bar.Y + 1, fillW, bar.Height - 2), BossBarColor(closest.DefId));

        var hpText = $"{(int)closest.Hp} / {(int)closest.HpMax}";
        var hpSize = SpriteFontSafe.MeasureString(font, hpText) * 0.7f;
        SpriteFontSafe.DrawString(sb, font, hpText,
            new Vector2(panel.Right - hpSize.X - 10, panel.Y + 8),
            new Color(220, 210, 190), 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    private static Color BossBarColor(string defId) => defId switch
    {
        "iron_colossus" => new Color(180, 90, 45),
        "storm_wyrm" => new Color(60, 130, 220),
        "blight_herald" => new Color(80, 170, 55),
        _ => new Color(180, 50, 45),
    };

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
    }
}
