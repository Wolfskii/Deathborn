using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>
/// Skills in two columns, grouped by section, so every skill fits without
/// scrolling. Hovering a skill shows its exact XP and what is left to the next level.
/// </summary>
public sealed class SkillsWindow : UiWindow
{
    private const int Width = 460;
    private const int ColumnGap = 24;
    private const int CellGapY = 14;
    private const int BarHeight = 8;

    private Func<PlayerSkills?>? _skills;
    private Point _mouse;
    private string? _hoverSkill;
    private Rectangle _hoverRect;
    private readonly List<(string Id, Rectangle Rect)> _cells = [];

    public SkillsWindow() : base("Skills", Width, 480, Keys.L, new Point(760, 80)) { }

    public void Bind(Func<PlayerSkills?> skills) => _skills = skills;

    protected override void UpdateContent(MouseState mouse, MouseState prevMouse) => _mouse = mouse.Position;

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        _cells.Clear();
        var skills = _skills?.Invoke();
        if (skills == null)
        {
            UiText.Draw(sb, font, "No skill data yet.", new Vector2(area.X, area.Y), FarmRpgUi.InkMuted);
            return;
        }

        var y = area.Y;
        UiText.Draw(sb, font, $"Total level {skills.TotalLevel}", new Vector2(area.X, y), FarmRpgUi.Ink);
        UiText.DrawRight(sb, font, $"{UiText.Number(skills.TotalXp)} XP", area.Right, y, FarmRpgUi.InkMuted);
        y += font.LineSpacing + FarmRpgUi.SectionGap;

        y = DrawSection(sb, font, "Combat", SkillDefinitions.Combat, skills, area, y);
        y = DrawSection(sb, font, "Gathering", SkillDefinitions.Gathering, skills, area, y);
        DrawSection(sb, font, "Production", SkillDefinitions.Production, skills, area, y);

        _hoverSkill = null;
        foreach (var (id, rect) in _cells)
        {
            if (!rect.Contains(_mouse)) continue;
            _hoverSkill = id;
            _hoverRect = rect;
            break;
        }
    }

    /// <summary>Fits the window to its content so there is never a scrollbar.</summary>
    protected override int? MeasureContentHeight(SpriteFont font)
    {
        var cellH = font.LineSpacing + 4 + BarHeight;
        var height = font.LineSpacing + FarmRpgUi.SectionGap;
        foreach (var ids in new[] { SkillDefinitions.Combat, SkillDefinitions.Gathering, SkillDefinitions.Production })
        {
            var rows = (ids.Length + 1) / 2;
            height += font.LineSpacing + 10 + rows * cellH + (rows - 1) * CellGapY + FarmRpgUi.SectionGap;
        }
        return height - FarmRpgUi.SectionGap;
    }

    protected override void DrawOverlay(SpriteBatch sb, SpriteFont font)
    {
        if (_hoverSkill == null || _skills?.Invoke() is not { } skills) return;
        var xp = skills.GetXp(_hoverSkill);
        var level = SkillDefinitions.LevelForXp(xp);
        var lines = new List<string> { $"{UiText.Number(xp)} XP" };
        if (level < SkillDefinitions.MaxLevel)
            lines.Add($"{UiText.Number(SkillDefinitions.XpToNextLevel(xp))} XP to level {level + 1}");
        else
            lines.Add("Maximum level");
        UiTooltip.Draw(sb, font, $"{SkillDefinitions.DisplayName(_hoverSkill)} - level {level}", lines,
            _hoverRect, new Point(GameViewport.Width, GameViewport.Height));
    }

    private int DrawSection(
        SpriteBatch sb, SpriteFont font, string title, string[] ids, PlayerSkills skills, Rectangle area, int y)
    {
        y += FarmRpgUi.DrawSectionHeader(sb, font, title, area.X, y, area.Width) + 10;

        var cellW = (area.Width - ColumnGap) / 2;
        var cellH = font.LineSpacing + 4 + BarHeight;
        for (var i = 0; i < ids.Length; i++)
        {
            var col = i % 2;
            var cell = new Rectangle(area.X + col * (cellW + ColumnGap), y, cellW, cellH);
            DrawSkillCell(sb, font, ids[i], skills, cell);
            _cells.Add((ids[i], cell));
            if (col == 1 || i == ids.Length - 1)
                y += cellH + CellGapY;
        }
        return y - CellGapY + FarmRpgUi.SectionGap;
    }

    private static void DrawSkillCell(SpriteBatch sb, SpriteFont font, string id, PlayerSkills skills, Rectangle cell)
    {
        var xp = skills.GetXp(id);
        var level = SkillDefinitions.LevelForXp(xp);
        UiText.Draw(sb, font, SkillDefinitions.DisplayName(id), new Vector2(cell.X, cell.Y), FarmRpgUi.Ink);
        UiText.DrawRight(sb, font, level.ToString(), cell.Right, cell.Y, FarmRpgUi.Ink);

        var bar = new Rectangle(cell.X, cell.Y + font.LineSpacing + 4, cell.Width, BarHeight);
        FarmRpgUi.DrawBar(sb, bar, SkillDefinitions.ProgressToNext(xp), SkillColor(id));
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
        "smithing" => new Color(150, 150, 160),
        _ => new Color(130, 130, 140),
    };
}
