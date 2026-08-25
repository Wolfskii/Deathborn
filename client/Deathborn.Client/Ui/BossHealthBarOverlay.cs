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
        var panelH = 54;
        var x = (GameViewport.Width - panelW) / 2;
        var y = 52;
        var panel = new Rectangle(x, y, panelW, panelH);

        FarmRpgUi.DrawWindowPanel(sb, panel, 0.96f);
        var titlePanel = new Rectangle(panel.X + 7, panel.Y + 5, panel.Width - 14, 22);
        FarmRpgUi.DrawTitle(sb, titlePanel, 0.94f);

        var name = SpriteFontSafe.Filter(closest.Name);
        SpriteFontSafe.DrawString(sb, font, name, new Vector2(titlePanel.X + 7, titlePanel.Y + 3), FarmRpgUi.Ink);
        WorldNpcEntity.DrawBossIcon(sb, new Vector2(panel.Right - 22, panel.Y + 17), 0.85f);

        var bar = new Rectangle(panel.X + 11, panel.Y + 29, panel.Width - 22, 17);
        var pct = Math.Clamp(closest.Hp / closest.HpMax, 0f, 1f);
        FarmRpgUi.DrawBar(sb, bar, pct, BossBarColor(closest.DefId));

        var hpText = $"{(int)closest.Hp} / {(int)closest.HpMax}";
        var hpSize = SpriteFontSafe.MeasureString(font, hpText) * 0.7f;
        SpriteFontSafe.DrawString(sb, font, hpText,
            new Vector2(panel.Right - hpSize.X - 32, titlePanel.Y + 5),
            FarmRpgUi.InkMuted, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    private static Color BossBarColor(string defId) => defId switch
    {
        "iron_colossus" => new Color(180, 90, 45),
        "storm_wyrm" => new Color(60, 130, 220),
        "blight_herald" => new Color(80, 170, 55),
        _ => new Color(180, 50, 45),
    };
}
