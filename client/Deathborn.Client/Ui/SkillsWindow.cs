using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class SkillsWindow : UiWindow
{
    private const int Width = 300;
    private const int Height = 470;
    private const int RowHeight = 34;
    private const int ScrollStep = 28;

    private Func<PlayerSkills?>? _skills;
    private int _scrollY;
    private int _maxScroll;
    private Rectangle _scrollView;

    public SkillsWindow() : base("Skills", Width, Height, Keys.L, new Point(760, 80)) { }

    public void Bind(Func<PlayerSkills?> skills) => _skills = skills;

    protected override void UpdateContent(MouseState mouse, MouseState prevMouse)
    {
        if (!_scrollView.Contains(mouse.Position) || _maxScroll <= 0) return;
        var wheel = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (wheel != 0)
            _scrollY = Math.Clamp(_scrollY - wheel / 120 * ScrollStep, 0, _maxScroll);
    }

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        var skills = _skills?.Invoke();
        if (skills == null)
        {
            sb.DrawString(font, "No skill data.", new Vector2(area.X + 8, area.Y + 8), GoldDim);
            return;
        }

        var inner = FarmRpgUi.Inset(area, 8);
        sb.DrawString(font, $"Total level: {skills.TotalLevel}", new Vector2(inner.X, inner.Y), PanelBorder);
        sb.DrawString(font, $"Total XP: {skills.TotalXp:N0}",
            new Vector2(inner.X, inner.Y + font.LineSpacing), GoldDim);

        var listTop = inner.Y + font.LineSpacing * 2 + 8;
        _scrollView = new Rectangle(inner.X, listTop, inner.Width - 10, Math.Max(1, inner.Bottom - listTop));
        var y = listTop - _scrollY;
        DrawSection(sb, font, "Combat", SkillDefinitions.Combat, skills, ref y, _scrollView);
        DrawSection(sb, font, "Gathering", SkillDefinitions.Gathering, skills, ref y, _scrollView);
        DrawSection(sb, font, "Production", SkillDefinitions.Production, skills, ref y, _scrollView);

        var contentHeight = y + _scrollY - listTop;
        _maxScroll = Math.Max(0, contentHeight - _scrollView.Height);
        _scrollY = Math.Clamp(_scrollY, 0, _maxScroll);
        if (_maxScroll > 0)
        {
            var track = new Rectangle(inner.Right - 6, listTop, 6, _scrollView.Height);
            var thumbH = Math.Max(24, (int)(track.Height * (_scrollView.Height / (float)contentHeight)));
            var thumbY = track.Y + (int)((track.Height - thumbH) * (_scrollY / (float)_maxScroll));
            FarmRpgUi.DrawScrollbar(sb, track, new Rectangle(track.X - 2, thumbY, 10, thumbH));
        }
    }

    private static void DrawSection(
        SpriteBatch sb, SpriteFont font, string title, string[] ids, PlayerSkills skills,
        ref int y, Rectangle area)
    {
        if (y + font.LineSpacing >= area.Y && y < area.Bottom)
            sb.DrawString(font, title, new Vector2(area.X, y), FarmRpgUi.Ink);
        y += font.LineSpacing + 2;

        foreach (var id in ids)
        {
            if (y >= area.Y && y + RowHeight <= area.Bottom)
                DrawSkillRow(sb, font, id, skills, area, y);
            y += RowHeight;
        }
        y += 4;
    }

    private static void DrawSkillRow(
        SpriteBatch sb, SpriteFont font, string id, PlayerSkills skills, Rectangle area, int y)
    {
        var xp = skills.GetXp(id);
        var level = SkillDefinitions.LevelForXp(xp);
        var name = SkillDefinitions.DisplayName(id);

        var row = new Rectangle(area.X, y, area.Width, RowHeight - 2);
        FarmRpgUi.DrawInsetPanel(sb, row);
        sb.DrawString(font, name, new Vector2(row.X + 8, y + 2), FarmRpgUi.Ink);
        var lvlText = level >= SkillDefinitions.MaxLevel ? "99" : level.ToString();
        var lvlSize = font.MeasureString(lvlText);
        sb.DrawString(font, lvlText, new Vector2(row.Right - lvlSize.X - 8, y + 2), FarmRpgUi.Ink);

        var bar = new Rectangle(row.X + 8, y + 19, row.Width - 16, 11);
        DrawStatBar(sb, bar, SkillDefinitions.ProgressToNext(xp), SkillColor(id));
    }

    private static Color SkillColor(string id) => id switch
    {
        "attack" or "strength" => new Color(180, 60, 55),
        "defense" => new Color(90, 110, 170),
        "hitpoints" => new Color(180, 50, 50),
        "woodcutting" => new Color(70, 130, 55),
        "mining" => new Color(120, 100, 75),
        "fishing" => new Color(55, 110, 170),
        "farming" => new Color(100, 150, 60),
        "cooking" => new Color(190, 120, 50),
        _ => new Color(130, 130, 140),
    };

    private static void DrawStatBar(SpriteBatch sb, Rectangle bar, float pct, Color fill)
    {
        FarmRpgUi.DrawBar(sb, bar, pct, fill);
    }
}
