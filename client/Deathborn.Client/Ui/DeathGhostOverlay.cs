using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Prompt shown while the player is a ghost after death.</summary>
public sealed class DeathGhostOverlay
{
    private Rectangle _panel;
    private Rectangle _button;
    private bool _hover;

    public void Update(Point mouse, bool mouseClicked)
    {
        _hover = _button.Contains(mouse);
        if (mouseClicked && _hover)
            NewLifeRequested?.Invoke();
    }

    public event Action? NewLifeRequested;

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 0.35f));

        _panel = new Rectangle(vw / 2 - 200, vh / 2 - 90, 400, 180);
        DrawPrimitives.FillRect(sb, _panel, new Color(18, 16, 24, 230));
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Y, _panel.Width, 2), new Color(180, 140, 90));
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Bottom - 2, _panel.Width, 2), new Color(80, 65, 45));

        var title = "You have fallen";
        var titleSize = font.MeasureString(title);
        sb.DrawString(font, title, new Vector2(_panel.Center.X - titleSize.X / 2f, _panel.Y + 22), new Color(235, 220, 200));

        var hint = "Your spirit wanders above the mortal shell.";
        var hintSize = font.MeasureString(hint);
        sb.DrawString(font, hint, new Vector2(_panel.Center.X - hintSize.X / 2f, _panel.Y + 52), new Color(170, 165, 175));

        var hint2 = "Begin a new life when you are ready.";
        var hint2Size = font.MeasureString(hint2);
        sb.DrawString(font, hint2, new Vector2(_panel.Center.X - hint2Size.X / 2f, _panel.Y + 72), new Color(140, 138, 150));

        _button = new Rectangle(_panel.Center.X - 120, _panel.Bottom - 52, 240, 36);
        var btnFill = _hover ? new Color(72, 58, 42) : new Color(48, 40, 30);
        DrawPrimitives.FillRect(sb, _button, btnFill);
        DrawPrimitives.FillRect(sb, new Rectangle(_button.X, _button.Y, _button.Width, 2), new Color(200, 165, 95));
        var label = "Create new character";
        var labelSize = font.MeasureString(label);
        sb.DrawString(font, label, new Vector2(_button.Center.X - labelSize.X / 2f, _button.Center.Y - labelSize.Y / 2f),
            new Color(245, 235, 210));
    }
}
