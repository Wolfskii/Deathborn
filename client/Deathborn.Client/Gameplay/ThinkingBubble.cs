using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Rounded thought cloud shown while a player is composing chat.</summary>
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

        var cloud = new Color(0.94f, 0.92f, 0.88f, alpha);
        var border = new Color(0.22f, 0.2f, 0.18f, alpha);
        var dotColor = new Color(0.38f, 0.35f, 0.32f, alpha);

        // Trail of small circles leading down toward the player (diagonal)
        DrawCloudCircle(sb, anchor + new Vector2(-2f * zoom, 26f * zoom), 2.8f * zoom, cloud, border);
        DrawCloudCircle(sb, anchor + new Vector2(-5f * zoom, 16f * zoom), 3.6f * zoom, cloud, border);

        // Main thought cloud — overlapping circles for a soft round shape
        DrawCloudCircle(sb, anchor + new Vector2(-11f * zoom, 1f * zoom), 8.5f * zoom, cloud, border);
        DrawCloudCircle(sb, anchor + new Vector2(10f * zoom, -1f * zoom), 9f * zoom, cloud, border);
        DrawCloudCircle(sb, anchor + new Vector2(0f, 3f * zoom), 10f * zoom, cloud, border);

        // Bouncing ellipsis inside the cloud
        for (var i = 0; i < 3; i++)
        {
            var bounce = MathF.Sin(_anim * 5f + i * 1.2f) * 1.8f * zoom;
            var x = anchor.X - 9f * zoom + i * 9f * zoom;
            var y = anchor.Y + 2f * zoom + bounce;
            DrawCloudCircle(sb, new Vector2(x, y), 2.2f * zoom, dotColor, border);
        }
    }

    private static void DrawCloudCircle(SpriteBatch sb, Vector2 center, float radius, Color fill, Color border)
    {
        DrawPrimitives.FillCircle(sb, center, radius + 1.2f, border, 20);
        DrawPrimitives.FillCircle(sb, center, radius, fill, 20);
    }
}
