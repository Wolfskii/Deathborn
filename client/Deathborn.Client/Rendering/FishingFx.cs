using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>Bobber splash on the water tile while the local player is fishing.</summary>
public static class FishingFx
{
    private static Texture2D? _splash;
    private static int _frames;

    public static void Load(ContentManager content)
    {
        try
        {
            _splash = content.Load<Texture2D>("Props/water/splash");
            _frames = Math.Max(1, _splash.Width / 16);
        }
        catch (ContentLoadException)
        {
            _splash = null;
            _frames = 0;
        }
    }

    public static void DrawBobber(
        SpriteBatch sb, int tx, int ty, Vector2 camera, Vector2 screenCenter, float zoom, float time)
    {
        var origin = new Vector2(tx * FarmCatalog.TileSize, ty * FarmCatalog.TileSize);
        var screen = origin - camera + screenCenter;
        var dest = new Rectangle(
            (int)MathF.Round(screen.X),
            (int)MathF.Round(screen.Y),
            (int)MathF.Round(FarmCatalog.TileSize * zoom),
            (int)MathF.Round(FarmCatalog.TileSize * zoom));

        if (_splash != null && _frames > 0)
        {
            var frame = (int)(time * 8f) % _frames;
            var src = new Rectangle(frame * 16, 0, 16, 16);
            sb.Draw(_splash, dest, src, Color.White);
            return;
        }

        DrawPrimitives.FillCircle(sb,
            new Vector2(dest.X + dest.Width * 0.5f, dest.Y + dest.Height * 0.5f),
            dest.Width * 0.28f,
            new Color(0.85f, 0.92f, 1f, 0.7f));
    }

    public static void DrawTileHighlight(
        SpriteBatch sb, int tx, int ty, Vector2 camera, Vector2 screenCenter, float zoom,
        Color fill, Color outline)
    {
        FarmRenderer.DrawTileHighlight(sb, tx, ty, camera, screenCenter, zoom, fill, outline);
    }
}
