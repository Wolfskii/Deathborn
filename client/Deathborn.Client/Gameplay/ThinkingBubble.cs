using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Pixel "thinking" cloud shown while a player is composing chat.</summary>
public sealed class ThinkingBubble
{
    private float _anim;

    public bool Active { get; set; }

    public void Update(float dt)
    {
        if (Active) _anim += dt;
    }

    public void Draw(SpriteBatch sb, Vector2 anchor, float zoom, float alpha = 1f)
    {
        if (!Active || alpha < 0.01f) return;

        var px = MathF.Max(1f, zoom);
        var baseY = anchor.Y - 6f * zoom;
        var cloudColor = new Color(0.92f, 0.9f, 0.86f, alpha);
        var border = new Color(0.18f, 0.16f, 0.14f, alpha);
        var dotColor = new Color(0.35f, 0.32f, 0.3f, alpha);

        // Small trailing puff
        DrawPixelRect(sb, anchor.X - 10f * zoom, baseY + 4f * zoom, 5f * px, 4f * px, cloudColor, border);
        DrawPixelRect(sb, anchor.X - 4f * zoom, baseY, 18f * px, 10f * px, cloudColor, border);

        // Bouncing thought dots
        for (var i = 0; i < 3; i++)
        {
            var bounce = MathF.Sin(_anim * 5f + i * 1.2f) * 2f * zoom;
            var x = anchor.X - 8f * zoom + i * 8f * zoom;
            var y = baseY + 3f * zoom + bounce;
            DrawPixelRect(sb, x, y, 3f * px, 3f * px, dotColor, border);
        }
    }

    private static void DrawPixelRect(SpriteBatch sb, float x, float y, float w, float h, Color fill, Color border)
    {
        var outer = new Rectangle((int)MathF.Floor(x - 1), (int)MathF.Floor(y - 1), (int)MathF.Ceiling(w + 2), (int)MathF.Ceiling(h + 2));
        DrawPrimitives.FillRect(sb, outer, border);
        DrawPrimitives.FillRect(sb, new Rectangle((int)MathF.Floor(x), (int)MathF.Floor(y), (int)MathF.Ceiling(w), (int)MathF.Ceiling(h)), fill);
    }
}
