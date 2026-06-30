using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Ui;

public sealed class CharacterWindow : UiWindow
{
    private const int Width = 228;
    private const int Height = 196;

    private static readonly Color HpFill = new(0.78f, 0.22f, 0.2f);
    private static readonly Color StaminaFill = new(0.82f, 0.72f, 0.18f);
    private static readonly Color ManaFill = new(0.28f, 0.45f, 0.92f);
    private static readonly Color ExpFill = new(0.55f, 0.38f, 0.82f);

    private Func<CharacterStats?>? _statsProvider;
    private Func<string>? _nameProvider;

    public CharacterWindow()
        : base("Character", Width, Height, Keys.C, DefaultPosition())
    {
    }

    public void Bind(Func<CharacterStats?> stats, Func<string> name)
    {
        _statsProvider = stats;
        _nameProvider = name;
    }

    private static Point DefaultPosition()
    {
        var minimapBottom = Config.MinimapMargin + (int)(Config.MinimapScreenRadius * 2) + 8;
        return new Point(GameViewport.Width - Width - Config.MinimapMargin, minimapBottom);
    }

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        var stats = _statsProvider?.Invoke();
        var name = _nameProvider?.Invoke() ?? "Player";
        if (stats == null) return;

        var y = area.Y + 4;
        sb.DrawString(font, name, new Vector2(area.X, y), Color.White);
        y += font.LineSpacing + 2;

        var lvl = $"Lvl {stats.Level}";
        sb.DrawString(font, lvl, new Vector2(area.X, y), PanelBorder);
        y += font.LineSpacing + 8;

        var rowH = font.LineSpacing + 18;
        DrawStatBar(sb, font, new Rectangle(area.X, y, area.Width, rowH), "HP", stats.Hp, stats.HpMax, HpFill);
        y += rowH + 4;
        DrawStatBar(sb, font, new Rectangle(area.X, y, area.Width, rowH), "Stamina", stats.Stamina, stats.StaminaMax, StaminaFill);
        y += rowH + 4;
        DrawStatBar(sb, font, new Rectangle(area.X, y, area.Width, rowH), "Mana", stats.Mana, stats.ManaMax, ManaFill);
        y += rowH + 4;
        DrawStatBar(sb, font, new Rectangle(area.X, y, area.Width, rowH), "EXP",
            stats.ExpPercent, 100f, ExpFill, $"{stats.ExpPercent:0.00}%");
    }
}
