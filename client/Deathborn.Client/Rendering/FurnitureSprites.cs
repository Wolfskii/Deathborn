using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Homestead furniture and interior decor sprites (Content/Furniture, installed by
/// scripts/install_farm_rpg_furniture.py). One art pixel is one world unit inside the house.
/// The kitchen is an animated strip of 32 px frames; everything else is a single image.
/// </summary>
public static class FurnitureSprites
{
    /// <summary>Placeable furniture, in decorate-menu order (server game/housing.go furnitureTypes).</summary>
    public static readonly string[] PlaceableTypes =
    [
        "bed", "dresser", "chest", "table", "chair", "sofa", "armchair",
        "bookshelf", "piano", "fireplace", "kitchen", "lamp", "plant", "rug",
    ];

    private static readonly string[] DecorSprites = ["curtain_bedroom", "curtain_hall", "curtain_kitchen"];

    private static readonly Dictionary<string, Texture2D> Sprites = new(StringComparer.Ordinal);
    private static Texture2D? _kitchen;
    private static int _kitchenFrames = 1;

    public static void Load(ContentManager content)
    {
        Sprites.Clear();
        foreach (var name in PlaceableTypes.Concat(DecorSprites))
        {
            try
            {
                Sprites[name] = content.Load<Texture2D>($"Furniture/{name}");
            }
            catch (ContentLoadException)
            {
                // Missing art falls back to a placeholder block.
            }
        }

        _kitchen = Sprites.GetValueOrDefault("kitchen");
        _kitchenFrames = _kitchen != null ? Math.Max(1, _kitchen.Width / 32) : 1;
    }

    /// <summary>Flat pieces lie on the floor and are drawn under players instead of depth-sorted.</summary>
    public static bool IsFloorPiece(string type) => type == "rug";

    /// <summary>Display name for the decorate menu.</summary>
    public static string DisplayName(string type) => type switch
    {
        "kitchen" => "Kitchen stove",
        "bookshelf" => "Bookshelf",
        "armchair" => "Armchair",
        _ => char.ToUpperInvariant(type[0]) + type[1..],
    };

    /// <summary>Draws a furniture piece with its bottom-centre at <paramref name="screen"/>.</summary>
    public static bool TryDraw(SpriteBatch sb, string type, Vector2 screen, float zoom, float time, Color tint)
    {
        if (type == "kitchen")
            return TryDrawKitchen(sb, screen, zoom, time, tint);
        return TryDrawSprite(sb, type, screen, zoom, tint);
    }

    /// <summary>Single-image sprite, bottom-centre anchored. Rugs are centred instead.</summary>
    public static bool TryDrawSprite(SpriteBatch sb, string name, Vector2 screen, float zoom, Color tint)
    {
        if (!Sprites.TryGetValue(name, out var tex)) return false;
        var w = tex.Width * zoom;
        var h = tex.Height * zoom;
        var top = IsFloorPiece(name) ? screen.Y - h * 0.5f : screen.Y - h;
        var dest = new Rectangle(
            (int)MathF.Round(screen.X - w * 0.5f),
            (int)MathF.Round(top),
            (int)MathF.Round(w),
            (int)MathF.Round(h));
        sb.Draw(tex, dest, tint);
        return true;
    }

    /// <summary>Unscaled size of a piece in art pixels (for hit tests and previews).</summary>
    public static Point Size(string type)
    {
        if (type == "kitchen") return new Point(32, 32);
        return Sprites.TryGetValue(type, out var tex) ? new Point(tex.Width, tex.Height) : new Point(16, 16);
    }

    public static bool TryDrawKitchen(SpriteBatch sb, Vector2 screen, float zoom, float time) =>
        TryDrawKitchen(sb, screen, zoom, time, Color.White);

    private static bool TryDrawKitchen(SpriteBatch sb, Vector2 screen, float zoom, float time, Color tint)
    {
        if (_kitchen == null) return false;
        // Frame 0 is the unlit pot. Cycle only the lit frames so the stove never looks off mid-cook.
        var frame = _kitchenFrames > 1
            ? 1 + (int)(time * 6f) % (_kitchenFrames - 1)
            : 0;
        var src = new Rectangle(frame * 32, 0, 32, 32);
        var dest = new Rectangle(
            (int)MathF.Round(screen.X - 16 * zoom),
            (int)MathF.Round(screen.Y - 28 * zoom),
            (int)MathF.Round(32 * zoom),
            (int)MathF.Round(32 * zoom));
        sb.Draw(_kitchen, dest, src, tint);
        return true;
    }
}
