using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Screens;

/// <summary>"Past Lives": the signed-in account's dead characters, newest first.</summary>
public sealed class LegacyScreen : RecordsScreen
{
    private const int RowHeight = 88;

    private volatile HistoryResponse? _history;
    private volatile LoadState _state = LoadState.Loading;

    public LegacyScreen(ScreenManager screens, Func<IScreen> back) : base(screens, back)
    {
    }

    protected override string Title => "Past Lives";

    private List<HistoryLife> Lives => _history?.Lives ?? [];

    protected override int ContentHeight => Lives.Count * RowHeight;

    protected override async Task LoadAsync()
    {
        _state = LoadState.Loading;
        ResetScroll();
        var result = await Screens.Net.GetHistoryAsync();
        _history = result;
        _state = result is null ? LoadState.Failed : LoadState.Ready;
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
            DrawCentered(sb, font, area, "Could not load history.");
            return;
        }
        var lives = Lives;
        if (lives.Count == 0)
        {
            DrawCentered(sb, font, area, "No past lives yet.");
            return;
        }

        for (var i = 0; i < lives.Count; i++)
        {
            var y = area.Y - scrollY + i * RowHeight;
            if (IsRowVisible(y, RowHeight, area))
                DrawLife(sb, font, lives[i], new Rectangle(area.X, y, area.Width, RowHeight - 6));
        }
    }

    private static void DrawLife(SpriteBatch sb, SpriteFont font, HistoryLife life, Rectangle row)
    {
        DrawPrimitives.FillRect(sb, row, new Color(18, 15, 12, 200));
        var textX = row.X + 10;
        var textWidth = row.Width - 20;

        SpriteFontSafe.DrawString(sb, font, Truncate(font, life.Name, textWidth * 0.6f),
            new Vector2(textX, row.Y + 6), Gold);

        var died = life.DiedAt.ToLocalTime().ToString("yyyy-MM-dd");
        var diedSize = SpriteFontSafe.MeasureString(font, died);
        SpriteFontSafe.DrawString(sb, font, died,
            new Vector2(row.Right - 10 - diedSize.X, row.Y + 6), Dim);

        SpriteFontSafe.DrawString(sb, font, Truncate(font, life.Reason, textWidth),
            new Vector2(textX, row.Y + 24), Muted);

        var stats = $"XP {life.TotalXp}   Kills {life.PvpKills}   Survived {FormatSurvival(life.SurvivalSeconds)}";
        SpriteFontSafe.DrawString(sb, font, Truncate(font, stats, textWidth),
            new Vector2(textX, row.Y + 42), Muted);

        if (!string.IsNullOrWhiteSpace(life.Epitaph))
        {
            SpriteFontSafe.DrawString(sb, font, Truncate(font, "\"" + life.Epitaph + "\"", textWidth),
                new Vector2(textX, row.Y + 60), Dim);
        }
    }
}
