using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class CharacterWindow : UiWindow
{
    private const int Width = 320;
    private const int BarRowGap = 14;

    private static readonly Color HpFill = new(0.78f, 0.22f, 0.2f);
    private static readonly Color StaminaFill = new(0.82f, 0.72f, 0.18f);
    private static readonly Color ManaFill = new(0.28f, 0.45f, 0.92f);
    private static readonly Color ExpFill = new(0.55f, 0.38f, 0.82f);

    private Func<CharacterStats?>? _statsProvider;
    private Func<string>? _nameProvider;
    private Func<PlayerSkills?>? _skillsProvider;

    public CharacterWindow()
        : base("Character", Width, 360, Keys.C, DefaultPosition())
    {
    }

    public void Bind(Func<CharacterStats?> stats, Func<string> name, Func<PlayerSkills?>? skills = null)
    {
        _statsProvider = stats;
        _nameProvider = name;
        _skillsProvider = skills;
    }

    private static Point DefaultPosition() => new(Config.MinimapMargin, 140);

    protected override int? MeasureContentHeight(SpriteFont font) =>
        font.LineSpacing * 2 + 6 + FarmRpgUi.SectionGap
        + StatBarHeight(font) * 4 + BarRowGap * 3;

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        var stats = _statsProvider?.Invoke();
        var name = _nameProvider?.Invoke() ?? "Player";
        if (stats == null) return;

        var y = area.Y;
        UiText.Draw(sb, font, UiText.Truncate(font, name, area.Width), new Vector2(area.X, y), FarmRpgUi.Ink);
        y += font.LineSpacing + 6;

        var summary = _skillsProvider?.Invoke() is { } skills
            ? $"Total level {skills.TotalLevel}  -  {UiText.Number(skills.TotalXp)} XP"
            : $"Level {stats.Level}";
        UiText.Draw(sb, font, UiText.Truncate(font, summary, area.Width), new Vector2(area.X, y), FarmRpgUi.InkMuted);
        y += font.LineSpacing + FarmRpgUi.SectionGap;

        var rowH = StatBarHeight(font);
        DrawStatBar(sb, font, new Rectangle(area.X, y, area.Width, rowH), "Health", stats.Hp, stats.HpMax, HpFill);
        y += rowH + BarRowGap;
        DrawStatBar(sb, font, new Rectangle(area.X, y, area.Width, rowH), "Stamina", stats.Stamina, stats.StaminaMax, StaminaFill);
        y += rowH + BarRowGap;
        DrawStatBar(sb, font, new Rectangle(area.X, y, area.Width, rowH), "Mana", stats.Mana, stats.ManaMax, ManaFill);
        y += rowH + BarRowGap;
        DrawStatBar(sb, font, new Rectangle(area.X, y, area.Width, rowH), "Experience",
            stats.ExpPercent, 100f, ExpFill,
            stats.ExpPercent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%");
    }
}
