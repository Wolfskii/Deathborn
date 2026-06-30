using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class SkillsWindow : UiWindow
{
    private const int Width = 300;
    private const int Height = 420;
    private const int RowHeight = 36;

    private Func<PlayerSkills?>? _skills;

    public SkillsWindow() : base("Skills", Width, Height, Keys.L, new Point(760, 80)) { }

    public void Bind(Func<PlayerSkills?> skills) => _skills = skills;

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        var skills = _skills?.Invoke();
        if (skills == null)
        {
            sb.DrawString(font, "No skill data.", new Vector2(area.X + 8, area.Y + 8), GoldDim);
            return;
        }

        sb.DrawString(font, $"Total level: {skills.TotalLevel}", new Vector2(area.X + 8, area.Y + 2), PanelBorder);
        sb.DrawString(font, $"Total XP: {skills.TotalXp:N0}", new Vector2(area.X + 8, area.Y + 16), GoldDim);

        var y = area.Y + 36;
        DrawSection(sb, font, "Combat", SkillDefinitions.Combat, skills, ref y, area);
        DrawSection(sb, font, "Gathering", SkillDefinitions.Gathering, skills, ref y, area);
        DrawSection(sb, font, "Production", SkillDefinitions.Production, skills, ref y, area);
    }

    private static void DrawSection(
        SpriteBatch sb, SpriteFont font, string title, string[] ids, PlayerSkills skills,
        ref int y, Rectangle area)
    {
        if (y > area.Bottom - RowHeight) return;
        sb.DrawString(font, title, new Vector2(area.X + 8, y), new Color(200, 175, 110));
        y += font.LineSpacing + 2;

        foreach (var id in ids)
        {
            if (y > area.Bottom - RowHeight) break;
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

        sb.DrawString(font, name, new Vector2(area.X + 10, y + 2), Color.White);
        var lvlText = level >= SkillDefinitions.MaxLevel ? "99" : level.ToString();
        var lvlSize = font.MeasureString(lvlText);
        sb.DrawString(font, lvlText, new Vector2(area.Right - lvlSize.X - 10, y + 2), new Color(235, 220, 160));

        var bar = new Rectangle(area.X + 10, y + 20, area.Width - 20, 8);
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
        DrawPrimitives.FillRect(sb, bar, new Color(18, 16, 14));
        DrawPrimitives.FillRect(sb, new Rectangle(bar.X, bar.Y, bar.Width, 1), GoldDim);
        var fillW = (int)((bar.Width - 2) * Math.Clamp(pct, 0f, 1f));
        if (fillW > 0)
            DrawPrimitives.FillRect(sb, new Rectangle(bar.X + 1, bar.Y + 1, fillW, bar.Height - 2), fill);
    }
}
