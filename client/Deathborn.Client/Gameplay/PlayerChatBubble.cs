using Deathborn.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Gameplay;

/// <summary>Speech bubble above a player with fade in/out.</summary>
public sealed class PlayerChatBubble
{
    private const float FadeIn = 0.25f;
    private const float FadeOut = 0.5f;

    private string _text = "";
    private float _timer;

    public bool IsVisible => !string.IsNullOrEmpty(_text) && Alpha > 0.001f;

    public float Alpha
    {
        get
        {
            if (string.IsNullOrEmpty(_text)) return 0f;
            if (_timer < FadeIn) return _timer / FadeIn;
            if (_timer < FadeIn + Config.ChatBubbleDuration) return 1f;
            if (_timer < FadeIn + Config.ChatBubbleDuration + FadeOut)
                return 1f - (_timer - FadeIn - Config.ChatBubbleDuration) / FadeOut;
            return 0f;
        }
    }

    public void Show(string text)
    {
        _text = text.Trim();
        _timer = 0;
    }

    public void Update(float dt)
    {
        if (string.IsNullOrEmpty(_text)) return;

        _timer += dt;
        if (_timer >= FadeIn + Config.ChatBubbleDuration + FadeOut)
            _text = "";
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 anchor, float zoom)
    {
        if (!IsVisible) return;

        var alpha = Alpha;
        ChatBubbleDraw.DrawSpeech(sb, font, anchor, _text, zoom, alpha);
    }
}
