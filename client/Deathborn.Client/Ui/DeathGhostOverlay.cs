using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Prompt after death — wander as a spirit or begin a new life.</summary>
public sealed class DeathGhostOverlay
{
    private Rectangle _panel;
    private Rectangle _spectateButton;
    private Rectangle _newLifeButton;
    private bool _spectateHover;
    private bool _newLifeHover;

    /// <summary>When true, the player is already drifting as a spirit (minimal bottom bar).</summary>
    public bool Spectating { get; set; }

    public event Action? SpectateRequested;
    public event Action? NewLifeRequested;

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
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;
        const int barH = 52;
        const int margin = 14;
        const int btnW = 148;
        const int btnH = 32;

        _panel = new Rectangle(margin, vh - barH - margin, vw - margin * 2, barH);
        DrawPrimitives.FillRect(sb, _panel, new Color(12, 10, 18, 210));
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Y, _panel.Width, 2), new Color(140, 115, 80));

        var title = "A shade upon the wind";
        var hint = "WASD to drift";
        sb.DrawString(font, title, new Vector2(_panel.X + 16, _panel.Y + 10), new Color(220, 210, 195));
        sb.DrawString(font, hint, new Vector2(_panel.X + 16, _panel.Y + 28), new Color(150, 148, 160));

        _newLifeButton = new Rectangle(_panel.Right - btnW - 12, _panel.Y + (barH - btnH) / 2, btnW, btnH);
        DrawButton(sb, font, _newLifeButton, _newLifeHover, "Begin anew");
        _spectateButton = Rectangle.Empty;
    }

    private void DrawDeathPrompt(SpriteBatch sb, SpriteFont font)
    {
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 0.35f));

        const int panelH = 200;
        _panel = new Rectangle(vw / 2 - 200, vh / 2 - panelH / 2, 400, panelH);
        DrawPrimitives.FillRect(sb, _panel, new Color(18, 16, 24, 230));
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Y, _panel.Width, 2), new Color(180, 140, 90));
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Bottom - 2, _panel.Width, 2), new Color(80, 65, 45));

        var title = "You have fallen";
        var titleSize = font.MeasureString(title);
        sb.DrawString(font, title, new Vector2(_panel.Center.X - titleSize.X / 2f, _panel.Y + 22), new Color(235, 220, 200));

        var hint = "Your spirit lingers above the mortal shell.";
        var hintSize = font.MeasureString(hint);
        sb.DrawString(font, hint, new Vector2(_panel.Center.X - hintSize.X / 2f, _panel.Y + 52),
            new Color(170, 165, 175));

        var hint2 = "Walk the veil to watch, or begin anew when ready.";
        var hint2Size = font.MeasureString(hint2);
        sb.DrawString(font, hint2, new Vector2(_panel.Center.X - hint2Size.X / 2f, _panel.Y + 72),
            new Color(140, 138, 150));

        _spectateButton = new Rectangle(_panel.Center.X - 120, _panel.Bottom - 92, 240, 36);
        DrawButton(sb, font, _spectateButton, _spectateHover, "Walk the veil");

        _newLifeButton = new Rectangle(_panel.Center.X - 120, _panel.Bottom - 48, 240, 36);
        DrawButton(sb, font, _newLifeButton, _newLifeHover, "Begin anew");
    }

    private static void DrawButton(SpriteBatch sb, SpriteFont font, Rectangle rect, bool hover, string label)
    {
        var btnFill = hover ? new Color(72, 58, 42) : new Color(48, 40, 30);
        DrawPrimitives.FillRect(sb, rect, btnFill);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Color(200, 165, 95));
        var labelSize = font.MeasureString(label);
        sb.DrawString(font, label, new Vector2(rect.Center.X - labelSize.X / 2f, rect.Center.Y - labelSize.Y / 2f),
            new Color(245, 235, 210));
    }
}
