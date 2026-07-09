using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Blocking prompt when the world server disconnects.</summary>
public sealed class ServerDisconnectOverlay
{
    private Rectangle _panel;
    private Rectangle _button;
    private bool _hover;
    private string _message = "Connection to the server was lost.";

    public void SetMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            _message = message.Trim();
    }

    public event Action? ReturnToLoginRequested;

    public void Update(Point mouse, bool mouseClicked)
    {
        _hover = _button.Contains(mouse);
        if (mouseClicked && _hover)
            ReturnToLoginRequested?.Invoke();
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 0.55f));

        _panel = new Rectangle(vw / 2 - 220, vh / 2 - 110, 440, 200);
        DrawPrimitives.FillRect(sb, _panel, new Color(18, 16, 24, 235));
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Y, _panel.Width, 2), new Color(180, 90, 90));
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Bottom - 2, _panel.Width, 2), new Color(80, 45, 45));

        var title = "Server disconnected";
        var titleSize = font.MeasureString(title);
        sb.DrawString(font, title, new Vector2(_panel.Center.X - titleSize.X / 2f, _panel.Y + 20), new Color(235, 200, 200));

        DrawWrapped(sb, font, _message, new Rectangle(_panel.X + 24, _panel.Y + 52, _panel.Width - 48, 56),
            new Color(185, 175, 170));

        var hint = "Your progress was saved. Log in again when the realm returns.";
        var hintSize = font.MeasureString(hint);
        sb.DrawString(font, hint, new Vector2(_panel.Center.X - hintSize.X / 2f, _panel.Y + 112), new Color(140, 135, 145));

        _button = new Rectangle(_panel.Center.X - 120, _panel.Bottom - 48, 240, 36);
        var btnFill = _hover ? new Color(72, 42, 42) : new Color(48, 30, 30);
        DrawPrimitives.FillRect(sb, _button, btnFill);
        DrawPrimitives.FillRect(sb, new Rectangle(_button.X, _button.Y, _button.Width, 2), new Color(200, 130, 95));
        var label = "Return to login";
        var labelSize = font.MeasureString(label);
        sb.DrawString(font, label, new Vector2(_button.Center.X - labelSize.X / 2f, _button.Center.Y - labelSize.Y / 2f),
            new Color(245, 230, 210));
    }

    private static void DrawWrapped(SpriteBatch sb, SpriteFont font, string text, Rectangle bounds, Color color)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return;

        var line = words[0];
        var y = bounds.Y;
        for (var i = 1; i < words.Length; i++)
        {
            var next = line + " " + words[i];
            if (font.MeasureString(next).X > bounds.Width)
            {
                var size = font.MeasureString(line);
                sb.DrawString(font, line, new Vector2(bounds.X + (bounds.Width - size.X) / 2f, y), color);
                y += font.LineSpacing;
                line = words[i];
            }
            else
            {
                line = next;
            }
        }

        var lastSize = font.MeasureString(line);
        sb.DrawString(font, line, new Vector2(bounds.X + (bounds.Width - lastSize.X) / 2f, y), color);
    }
}
