using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

/// <summary>Public leaderboard with one tab per highscore category.</summary>
public sealed class LeaderboardScreen : RecordsScreen
{
    private const int RowHeight = 30;

    private static readonly (string Label, string Category)[] Tabs =
    {
        ("Survival", "survival"),
        ("XP", "xp"),
        ("PvP", "pvp"),
        ("Wealth", "wealth"),
        ("Skill", "level"),
    };

    private readonly Button[] _tabButtons = Tabs.Select(t => new Button { Label = t.Label }).ToArray();
    private int _tab;
    private volatile HighscoreResponse? _scores;
    private volatile LoadState _state = LoadState.Loading;

    public LeaderboardScreen(ScreenManager screens, Func<IScreen> back) : base(screens, back)
    {
    }

    protected override string Title => "Leaderboard";

    protected override int HeaderHeight => 92;

    private List<HighscoreEntry> Entries => _scores?.Entries ?? [];

    protected override int ContentHeight => Entries.Count * RowHeight;

    protected override void OnLayout(Rectangle panel)
    {
        const int gap = 4;
        var buttonWidth = (panel.Width - 32 - gap * (Tabs.Length - 1)) / Tabs.Length;
        for (var i = 0; i < _tabButtons.Length; i++)
        {
            _tabButtons[i].Bounds = new Rectangle(
                panel.X + 16 + i * (buttonWidth + gap), panel.Y + 46, buttonWidth, 30);
        }
    }

    protected override async Task LoadAsync()
    {
        var requested = _tab;
        _state = LoadState.Loading;
        ResetScroll();
        var result = await Screens.Net.GetHighscoresAsync(Tabs[requested].Category);
        if (requested != _tab)
            return; // the player moved on to another tab while this loaded
        _scores = result;
        _state = result is null ? LoadState.Failed : LoadState.Ready;
    }

    protected override bool HandleClick(Point p)
    {
        for (var i = 0; i < _tabButtons.Length; i++)
        {
            if (i == _tab || !_tabButtons[i].Contains(p))
                continue;
            _tab = i;
            _ = LoadAsync();
            return true;
        }
        return false;
    }

    protected override void DrawHeader(SpriteBatch sb, SpriteFont font)
    {
        for (var i = 0; i < _tabButtons.Length; i++)
            _tabButtons[i].Draw(sb, font, i == _tab || _tabButtons[i].Contains(MousePosition));
    }

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area, int scrollY)
    {
        if (_state == LoadState.Loading)
        {
            DrawCentered(sb, font, area, "Loading...");
            return;
        }
        if (_state == LoadState.Failed)
        {
            DrawCentered(sb, font, area, "Could not load the leaderboard.");
            return;
        }
        var entries = Entries;
        if (entries.Count == 0)
        {
            DrawCentered(sb, font, area, "No scores yet.");
            return;
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var y = area.Y - scrollY + i * RowHeight;
            if (!IsRowVisible(y, RowHeight, area))
                continue;
            var row = new Rectangle(area.X, y, area.Width, RowHeight - 4);
            DrawPrimitives.FillRect(sb, row, i % 2 == 0 ? new Color(18, 15, 12, 200) : new Color(28, 24, 18, 200));
            DrawEntry(sb, font, entries[i], row);
        }
    }

    private void DrawEntry(SpriteBatch sb, SpriteFont font, HighscoreEntry entry, Rectangle row)
    {
        var textY = row.Y + (row.Height - font.LineSpacing) / 2;
        var rank = $"#{entry.Rank}";
        SpriteFontSafe.DrawString(sb, font, rank, new Vector2(row.X + 8, textY), Dim);

        var valueText = FormatValue(entry.Value);
        var valueSize = SpriteFontSafe.MeasureString(font, valueText);
        SpriteFontSafe.DrawString(sb, font, valueText,
            new Vector2(row.Right - 10 - valueSize.X, textY), Gold);

        var nameX = row.X + 60;
        var nameWidth = row.Width - 60 - (int)valueSize.X - 24;
        SpriteFontSafe.DrawString(sb, font, Truncate(font, entry.Name, nameWidth),
            new Vector2(nameX, textY), Muted);
    }

    private string FormatValue(long value) => Tabs[_tab].Category switch
    {
        "survival" => FormatSurvival(value),
        "level" => $"Level {value}",
        _ => value.ToString(),
    };
}
