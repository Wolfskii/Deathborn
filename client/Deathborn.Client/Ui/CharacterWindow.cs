using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class CharacterWindow : UiWindow
{
    private const int Width = 240;
    private const int ContentPad = 10;
    private const int BarRowGap = 6;
    private const int BarHeight = 16;
    private const int SectionGap = 8;

    // Sized for ~22px line spacing with slack at the bottom.
    private const int Height = 320;

    private static readonly Color HpFill = new(0.78f, 0.22f, 0.2f);
    private static readonly Color StaminaFill = new(0.82f, 0.72f, 0.18f);
    private static readonly Color ManaFill = new(0.28f, 0.45f, 0.92f);
    private static readonly Color ExpFill = new(0.55f, 0.38f, 0.82f);

    private Func<CharacterStats?>? _statsProvider;
    private Func<string>? _nameProvider;
    private Func<PlayerSkills?>? _skillsProvider;

    public CharacterWindow()
        : base("Character", Width, Height, Keys.C, DefaultPosition())
    {
    }

    public void Bind(Func<CharacterStats?> stats, Func<string> name, Func<PlayerSkills?>? skills = null)
    {
        _statsProvider = stats;
        _nameProvider = name;
        _skillsProvider = skills;
    }

    private static Point DefaultPosition() => new(Config.MinimapMargin, 140);

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
        sb.DrawString(font, name, new Vector2(inner.X, y), FarmRpgUi.Ink);
        y += font.LineSpacing + 4;

        var lvl = _skillsProvider?.Invoke() is { } skills
            ? $"Total level {skills.TotalLevel}"
            : $"Lvl {stats.Level}";
        sb.DrawString(font, lvl, new Vector2(inner.X, y), FarmRpgUi.InkMuted);
        y += font.LineSpacing + 2;
        if (_skillsProvider?.Invoke() is { } sk)
            sb.DrawString(font, $"Total XP: {sk.TotalXp:N0}", new Vector2(inner.X, y), FarmRpgUi.InkMuted);
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
