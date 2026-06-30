using Deathborn.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>macOS Spotlight-style chat input centered on screen.</summary>
public sealed class ChatSpotlightOverlay
{
    private const float OpenAnimDuration = 0.18f;
    private const int FieldHeight = 44;
    private const int FieldWidth = 560;

    private readonly TextField _field = new() { Placeholder = "Say something…" };
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
        LayoutField();
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
        _field.Focused = false;
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

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (!IsOpen) return;

        var t = _openAnim / OpenAnimDuration;
        var ease = 1f - MathF.Pow(1f - t, 3f);
        var dimAlpha = 0.55f * ease;

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height),
            new Color(0, 0, 0, dimAlpha));

        var cx = GameViewport.Width / 2f;
        var cy = GameViewport.Height * 0.38f;
        var scale = 0.9f + 0.1f * ease;
        var w = (int)(FieldWidth * scale);
        var h = (int)(FieldHeight * scale);
        _field.Bounds = new Rectangle((int)(cx - w / 2f), (int)(cy - h / 2f), w, h);
        LayoutField();

        // Outer glow
        var glow = new Rectangle(_field.Bounds.X - 4, _field.Bounds.Y - 4, _field.Bounds.Width + 8, _field.Bounds.Height + 8);
        DrawPrimitives.FillRect(sb, glow, new Color(1f, 0.85f, 0.45f, 0.12f * ease));

        _field.Draw(sb, font);

        var hint = "Enter to send · Esc to cancel";
        var hintSize = font.MeasureString(hint);
        sb.DrawString(font, hint, new Vector2(cx - hintSize.X / 2f, _field.Bounds.Bottom + 10),
            new Color(200, 200, 210, (int)(180 * ease)));
    }

    private void LayoutField()
    {
        if (!IsOpen) return;
        var cx = GameViewport.Width / 2f;
        var cy = GameViewport.Height * 0.38f;
        var t = _openAnim / OpenAnimDuration;
        var ease = 1f - MathF.Pow(1f - t, 3f);
        var scale = 0.9f + 0.1f * ease;
        var w = (int)(FieldWidth * scale);
        var h = (int)(FieldHeight * scale);
        _field.Bounds = new Rectangle((int)(cx - w / 2f), (int)(cy - h / 2f), w, h);
    }
}
