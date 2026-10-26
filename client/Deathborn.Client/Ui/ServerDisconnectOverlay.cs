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
        const int pad = 28;
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;
        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 0.5f));

        var width = Math.Min(460, vw - 32);
        var textW = width - pad * 2;
        var message = UiText.Wrap(font, _message, textW);
        var hint = UiText.Wrap(font, "Your progress was saved. Log in again when the realm returns.", textW);
        var height = 56 + message.Count * font.LineSpacing + 10 + hint.Count * font.LineSpacing
            + FarmRpgUi.SectionGap + FarmRpgUi.ButtonHeight + 24;
        _panel = new Rectangle(vw / 2 - width / 2, vh / 2 - height / 2, width, height);

        FarmRpgUi.DrawWindowPanel(sb, _panel);
        FarmRpgUi.DrawTitlePlate(sb, font, "Disconnected", _panel.Center.X, _panel.Y + 12);

        var y = _panel.Y + 56;
        foreach (var line in message)
        {
            UiText.DrawCentered(sb, font, line, new Rectangle(_panel.X, y, _panel.Width, font.LineSpacing), FarmRpgUi.Ink);
            y += font.LineSpacing;
        }
        y += 10;
        foreach (var line in hint)
        {
            UiText.DrawCentered(sb, font, line, new Rectangle(_panel.X, y, _panel.Width, font.LineSpacing), FarmRpgUi.InkMuted);
            y += font.LineSpacing;
        }

        _button = new Rectangle(_panel.Center.X - 120, _panel.Bottom - 24 - FarmRpgUi.ButtonHeight, 240, FarmRpgUi.ButtonHeight);
        FarmRpgUi.DrawButton(sb, _button, pressed: _hover);
        var textArea = _hover ? new Rectangle(_button.X, _button.Y + 2, _button.Width, _button.Height - 2) : _button;
        UiText.DrawCentered(sb, font, "Return to login", textArea, FarmRpgUi.Ink);
    }
}
