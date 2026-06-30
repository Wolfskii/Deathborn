using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client;

public static class GameViewport
{
    public static int Width { get; private set; } = Config.DefaultWidth;
    public static int Height { get; private set; } = Config.DefaultHeight;

    public static Vector2 Center => new(Width / 2f, Height / 2f);

    /// <summary>Camera scale for the world screen; scales with window size for fair visible area.</summary>
    public static float WorldZoom => ComputeWorldZoom(Width, Height);

    private static float ComputeWorldZoom(float width, float height)
    {
        var refW = (float)Config.DefaultWidth;
        var refH = (float)Config.DefaultHeight;
        var scaleX = width / refW;
        var scaleY = height / refH;

        // Height drives baseline so 16:9 windows see similar vertical world at any size.
        var scale = scaleY;

        if (scaleX > scaleY)
        {
            // Ultrawide: a bit more horizontal view, but not the full extra width.
            var extra = scaleX / scaleY - 1f;
            var blend = MathF.Min(extra * 0.25f, 0.18f);
            scale = MathHelper.Lerp(scaleY, scaleX, blend);
        }
        else if (scaleX < scaleY)
        {
            // Tall/narrow: pull zoom out so the player does not dominate horizontally.
            var deficit = scaleY / scaleX - 1f;
            var blend = MathF.Min(deficit * 0.5f, 0.45f);
            scale = MathHelper.Lerp(scaleY, scaleX, blend);
        }

        return MathHelper.Clamp(Config.WorldZoomBase * scale, Config.WorldZoomMin, Config.WorldZoomMax);
    }

    public static void Set(int width, int height)
    {
        Width = Math.Max(320, width);
        Height = Math.Max(240, height);
    }

    public static void SyncFrom(GraphicsDevice device)
    {
        var pp = device.PresentationParameters;
        Set(pp.BackBufferWidth, pp.BackBufferHeight);
    }
}
