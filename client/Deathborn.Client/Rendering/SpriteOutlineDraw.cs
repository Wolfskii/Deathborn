using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Draws a solid-color silhouette border around a sprite by offsetting the same frame.</summary>
public static class SpriteOutlineDraw
{
    private static readonly Vector2[] Offsets =
    [
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
        new(-1, -1), new(1, -1), new(-1, 1), new(1, 1),
    ];

    private static readonly Vector2[] OuterRing =
    [
        new(-2, 0), new(2, 0), new(0, -2), new(0, 2),
    ];

    public static void Draw(
        SpriteBatch sb, Texture2D texture, Vector2 screenPos,
        Rectangle src, Vector2 origin, Color outlineColor, float scale, float thickness = 1f)
    {
        var thick = Math.Max(1f, thickness);
        foreach (var off in Offsets)
        {
            sb.Draw(
                texture, screenPos + off * thick, src, outlineColor,
                0f, origin, scale, SpriteEffects.None, 0f);
        }

        if (thick >= 2f)
        {
            foreach (var off in OuterRing)
            {
                sb.Draw(
                    texture, screenPos + off * (thick * 0.55f), src, outlineColor * 0.85f,
                    0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
