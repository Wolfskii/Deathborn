using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Screens;

/// <summary>"Hall of the Fallen": the most recent deaths on the server, with last words.</summary>
public sealed class FallenScreen : RecordsScreen
{
    private const int RowHeight = 88;

    private volatile FallenResponse? _response;
    private volatile LoadState _state = LoadState.Loading;

    public FallenScreen(ScreenManager screens, Func<IScreen> back) : base(screens, back)
    {
    }

    protected override string Title => "Hall of the Fallen";

    private List<FallenDeath> Deaths => _response?.Deaths ?? [];

    protected override int ContentHeight => Deaths.Count * RowHeight;

    protected override async Task LoadAsync()
    {
        _state = LoadState.Loading;
        ResetScroll();
        var result = await Screens.Net.GetFallenAsync();
        _response = result;
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
            DrawCentered(sb, font, area, "Could not load the hall.");
            return;
        }
        var deaths = Deaths;
        if (deaths.Count == 0)
        {
            DrawCentered(sb, font, area, "No one has fallen yet.");
            return;
        }

        for (var i = 0; i < deaths.Count; i++)
        {
            var y = area.Y - scrollY + i * RowHeight;
            if (IsRowVisible(y, RowHeight, area))
                DrawDeath(sb, font, deaths[i], new Rectangle(area.X, y, area.Width, RowHeight - 6));
        }
    }

    private static void DrawDeath(SpriteBatch sb, SpriteFont font, FallenDeath death, Rectangle row)
    {
        DrawPrimitives.FillRect(sb, row, new Color(18, 15, 12, 200));
        var textX = row.X + 10;
        var textWidth = row.Width - 20;

        SpriteFontSafe.DrawString(sb, font, Truncate(font, death.Name, textWidth * 0.6f),
            new Vector2(textX, row.Y + 6), Gold);

        var died = death.DiedAt.ToLocalTime().ToString("yyyy-MM-dd");
        var diedSize = SpriteFontSafe.MeasureString(font, died);
        SpriteFontSafe.DrawString(sb, font, died, new Vector2(row.Right - 10 - diedSize.X, row.Y + 6), Dim);

        SpriteFontSafe.DrawString(sb, font, Truncate(font, death.Reason, textWidth),
            new Vector2(textX, row.Y + 24), Muted);

        var lived = $"Survived {FormatSurvival(death.SurvivalSeconds)}";
        SpriteFontSafe.DrawString(sb, font, lived, new Vector2(textX, row.Y + 42), Muted);

        if (!string.IsNullOrWhiteSpace(death.Epitaph))
        {
            SpriteFontSafe.DrawString(sb, font, Truncate(font, "\"" + death.Epitaph + "\"", textWidth),
                new Vector2(textX, row.Y + 60), Dim);
        }
    }
}
