using Deathborn.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Chat input anchored above the local player.</summary>
public sealed class ChatSpotlightOverlay
{
    private const float OpenAnimDuration = 0.18f;
    private const int FieldHeight = 36;
    private const int FieldWidth = 340;

    private readonly TextField _field = new()
    {
        Placeholder = "Say something...",
        PlaceholderColor = Color.White,
    };
    private float _openAnim;
    private bool _awaitEnterRelease;

    public bool IsOpen { get; private set; }

    public event Action<string>? Submitted;
    public event Action<bool>? TypingChanged;

    public void Toggle()
    {
        if (IsOpen) Close(submit: false);
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        _openAnim = 0;
        _field.Text = "";
        _awaitEnterRelease = true;
        _field.Focused = true;
        TypingChanged?.Invoke(true);
    }

    public void Close(bool submit)
    {
        if (!IsOpen) return;

        if (submit)
        {
            var text = _field.Text.Trim();
            if (text.Length > 0)
                Submitted?.Invoke(text.Length > Config.ChatMaxLength ? text[..Config.ChatMaxLength] : text);
        }

        IsOpen = false;
        _awaitEnterRelease = false;
        TextField.ReleaseFocus();
        TypingChanged?.Invoke(false);
    }

    public void Update(GameTime gameTime, KeyboardState kb, KeyboardState prevKb)
    {
        if (!IsOpen)
            return;

        _openAnim = MathF.Min(OpenAnimDuration, _openAnim + (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (_awaitEnterRelease)
        {
            if (!InputKeys.IsEnterDown(kb))
                _awaitEnterRelease = false;
        }
        else if (InputKeys.EnterPressed(kb, prevKb))
        {
            if (_field.Text.Trim().Length > 0)
                Close(submit: true);
            return;
        }

        if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
        {
            Close(submit: false);
            return;
        }

        if (_field.Text.Length > Config.ChatMaxLength)
            _field.Text = _field.Text[..Config.ChatMaxLength];

        _field.Update(gameTime, kb, prevKb);
    }

    public void LayoutField(Vector2 playerScreenPos, float zoom)
    {
        if (!IsOpen) return;

        var t = _openAnim / OpenAnimDuration;
        var ease = 1f - MathF.Pow(1f - t, 3f);
        var uiScale = MathF.Min(zoom, 1.35f);
        var w = (int)(FieldWidth * uiScale);
        var h = (int)(FieldHeight * uiScale);
        var cx = playerScreenPos.X;
        var top = playerScreenPos.Y - (52f + h) * zoom;
        _field.Bounds = new Rectangle((int)(cx - w / 2f), (int)top, w, h);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 playerScreenPos, float zoom)
    {
        if (!IsOpen) return;

        LayoutField(playerScreenPos, zoom);
        _field.Draw(sb, font);

        var t = _openAnim / OpenAnimDuration;
        var ease = 1f - MathF.Pow(1f - t, 3f);
        var hint = "Enter to send - Esc to cancel";
        var hintSize = font.MeasureString(hint);
        var hintX = _field.Bounds.X + _field.Bounds.Width / 2f - hintSize.X / 2f;
        sb.DrawString(font, hint, new Vector2(hintX, _field.Bounds.Bottom + 4),
            new Color(200, 200, 210, (int)(160 * ease)));
    }
}
