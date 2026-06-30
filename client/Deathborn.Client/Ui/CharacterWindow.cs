using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Ui;

public sealed class CharacterWindow : UiWindow
{
    private const int Width = 240;
    private const int ContentPad = 10;
    private const int BarRowGap = 6;
    private const int BarHeight = 14;
    private const int SectionGap = 8;

    // Sized for ~22px line spacing with slack at the bottom.
    private const int Height = 284;

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

    private static Point DefaultPosition() => new(Config.MinimapMargin, Config.MinimapMargin);

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        var stats = _statsProvider?.Invoke();
        var name = _nameProvider?.Invoke() ?? "Player";
        if (stats == null) return;

        var inner = new Rectangle(
            area.X + ContentPad,
            area.Y + ContentPad,
            area.Width - ContentPad * 2,
            area.Height - ContentPad * 2);

        var y = inner.Y;
        sb.DrawString(font, name, new Vector2(inner.X, y), Color.White);
        y += font.LineSpacing + 4;

        var lvl = $"Lvl {stats.Level}";
        sb.DrawString(font, lvl, new Vector2(inner.X, y), PanelBorder);
        y += font.LineSpacing + SectionGap;

        var rowH = font.LineSpacing + BarHeight;
        DrawStatBar(sb, font, new Rectangle(inner.X, y, inner.Width, rowH), "HP", stats.Hp, stats.HpMax, HpFill, barHeight: BarHeight);
        y += rowH + BarRowGap;
        DrawStatBar(sb, font, new Rectangle(inner.X, y, inner.Width, rowH), "Stamina", stats.Stamina, stats.StaminaMax, StaminaFill, barHeight: BarHeight);
        y += rowH + BarRowGap;
        DrawStatBar(sb, font, new Rectangle(inner.X, y, inner.Width, rowH), "Mana", stats.Mana, stats.ManaMax, ManaFill, barHeight: BarHeight);
        y += rowH + BarRowGap;
        DrawStatBar(sb, font, new Rectangle(inner.X, y, inner.Width, rowH), "EXP",
            stats.ExpPercent, 100f, ExpFill, $"{stats.ExpPercent:0.00}%", barHeight: BarHeight);
    }
}
