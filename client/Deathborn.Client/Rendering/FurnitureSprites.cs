using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Homestead furniture sprites (kitchen pot strip, 32px frames).</summary>
public static class FurnitureSprites
{
    private static Texture2D? _kitchen;
    private static int _kitchenFrames = 1;

    public static void Load(ContentManager content)
    {
        try
        {
            _kitchen = content.Load<Texture2D>("Furniture/kitchen");
            _kitchenFrames = Math.Max(1, _kitchen.Width / 32);
        }
        catch (ContentLoadException)
        {
            _kitchen = null;
            _kitchenFrames = 1;
        }
    }

    public static bool TryDrawKitchen(SpriteBatch sb, Vector2 screen, float zoom, float time)
    {
        if (_kitchen == null) return false;
        var frame = (int)(time * 6f) % _kitchenFrames;
        var src = new Rectangle(frame * 32, 0, 32, 32);
        var dest = new Rectangle(
            (int)MathF.Round(screen.X - 16 * zoom),
            (int)MathF.Round(screen.Y - 28 * zoom),
            (int)MathF.Round(32 * zoom),
            (int)MathF.Round(32 * zoom));
        sb.Draw(_kitchen, dest, src, Color.White);
        return true;
    }
}
