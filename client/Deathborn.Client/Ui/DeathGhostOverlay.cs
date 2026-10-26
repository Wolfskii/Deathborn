using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Prompt after death: wander as a spirit or begin a new life.</summary>
public sealed class DeathGhostOverlay
{
    private const int PanelWidth = 480;
    private const int Pad = 28;
    private const int ButtonGap = 10;

    private Rectangle _panel;
    private Rectangle _spectateButton;
    private Rectangle _newLifeButton;
    private bool _spectateHover;
    private bool _newLifeHover;

    /// <summary>When true, the player is already drifting as a spirit (minimal bottom bar).</summary>
    public bool Spectating { get; set; }

    public event Action? SpectateRequested;
    public event Action? NewLifeRequested;

    private string? _reason;
    private string? _summary;

    /// <summary>Server-provided cause of death and life summary, shown on the death panel.</summary>
    public void SetDeathInfo(string? reason, string? summary)
    {
        _reason = reason;
        _summary = summary;
    }

    public void Update(Point mouse, bool mouseClicked)
    {
        _spectateHover = !Spectating && _spectateButton.Contains(mouse);
        _newLifeHover = _newLifeButton.Contains(mouse);
        if (!mouseClicked) return;

        if (_spectateHover)
            SpectateRequested?.Invoke();
        else if (_newLifeHover)
            NewLifeRequested?.Invoke();
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (Spectating)
            DrawSpectatingBar(sb, font);
        else
            DrawDeathPrompt(sb, font);
    }

    private void DrawSpectatingBar(SpriteBatch sb, SpriteFont font)
    {
        const int barH = 64;
        const int margin = 16;
        const int buttonW = 160;
        var width = Math.Min(560, GameViewport.Width - margin * 2);
        _panel = new Rectangle(GameViewport.Width / 2 - width / 2, GameViewport.Height - barH - margin, width, barH);
        FarmRpgUi.DrawCompactPanel(sb, _panel);

        var textX = _panel.X + 18;
        UiText.Draw(sb, font, "A shade upon the wind", new Vector2(textX, _panel.Y + 12), FarmRpgUi.Ink);
        UiText.Draw(sb, font, "WASD to drift", new Vector2(textX, _panel.Y + 12 + font.LineSpacing), FarmRpgUi.InkMuted);

        _newLifeButton = new Rectangle(_panel.Right - buttonW - 16, _panel.Center.Y - FarmRpgUi.ButtonHeight / 2,
            buttonW, FarmRpgUi.ButtonHeight);
        DrawButton(sb, font, _newLifeButton, _newLifeHover, "Begin anew");
        _spectateButton = Rectangle.Empty;
    }

    private void DrawDeathPrompt(SpriteBatch sb, SpriteFont font)
    {
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;
        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 0.4f));

        var width = Math.Min(PanelWidth, vw - 32);
        var textW = width - Pad * 2;
        var lines = new List<(string Text, Color Color)>();
        foreach (var line in UiText.Wrap(font, "Your spirit lingers above the mortal shell.", textW))
            lines.Add((line, FarmRpgUi.InkMuted));
        foreach (var line in UiText.Wrap(font, "Walk the veil to watch, or begin anew when ready.", textW))
            lines.Add((line, FarmRpgUi.InkMuted));

        var details = new List<(string Text, Color Color)>();
        if (!string.IsNullOrWhiteSpace(_reason))
            details.Add((UiText.Truncate(font, _reason, textW), FarmRpgUi.Danger));
        if (!string.IsNullOrWhiteSpace(_summary))
            details.Add((UiText.Truncate(font, _summary, textW), FarmRpgUi.Ink));

        var height = 56 + lines.Count * font.LineSpacing
            + (details.Count > 0 ? FarmRpgUi.SectionGap + details.Count * (font.LineSpacing + 2) : 0)
            + FarmRpgUi.SectionGap + FarmRpgUi.ButtonHeight * 2 + ButtonGap + 24;
        _panel = new Rectangle(vw / 2 - width / 2, vh / 2 - height / 2, width, height);

        FarmRpgUi.DrawWindowPanel(sb, _panel);
        FarmRpgUi.DrawTitlePlate(sb, font, "You have fallen", _panel.Center.X, _panel.Y + 12);

        var y = _panel.Y + 56;
        foreach (var (text, color) in lines)
        {
            UiText.DrawCentered(sb, font, text, new Rectangle(_panel.X, y, _panel.Width, font.LineSpacing), color);
            y += font.LineSpacing;
        }

        if (details.Count > 0)
        {
            y += FarmRpgUi.SectionGap / 2;
            var ruleW = Math.Min(180, textW);
            FarmRpgUi.DrawDivider(sb, _panel.Center.X - ruleW / 2, y, ruleW);
            y += FarmRpgUi.SectionGap / 2;
            foreach (var (text, color) in details)
            {
                UiText.DrawCentered(sb, font, text, new Rectangle(_panel.X, y, _panel.Width, font.LineSpacing), color);
                y += font.LineSpacing + 2;
            }
        }

        var buttonW = Math.Min(260, textW);
        var buttonX = _panel.Center.X - buttonW / 2;
        _newLifeButton = new Rectangle(buttonX, _panel.Bottom - 24 - FarmRpgUi.ButtonHeight, buttonW, FarmRpgUi.ButtonHeight);
        _spectateButton = new Rectangle(buttonX, _newLifeButton.Y - ButtonGap - FarmRpgUi.ButtonHeight, buttonW, FarmRpgUi.ButtonHeight);
        DrawButton(sb, font, _spectateButton, _spectateHover, "Walk the veil");
        DrawButton(sb, font, _newLifeButton, _newLifeHover, "Begin anew");
    }

    private static void DrawButton(SpriteBatch sb, SpriteFont font, Rectangle rect, bool hover, string label)
    {
        FarmRpgUi.DrawButton(sb, rect, pressed: hover);
        var textArea = hover ? new Rectangle(rect.X, rect.Y + 2, rect.Width, rect.Height - 2) : rect;
        UiText.DrawCentered(sb, font, label, textArea, FarmRpgUi.Ink);
    }
}
