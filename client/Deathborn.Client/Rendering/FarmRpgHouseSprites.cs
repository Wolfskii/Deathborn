using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Farm RPG Tiny House exterior used for player homesteads.</summary>
public static class FarmRpgHouseSprites
{
    /// <summary>World draw scale so ~72×86 art matches the homestead footprint.</summary>
    public const float DisplayScale = 1.55f;

    private static Texture2D? _orangeCottage;

    public static bool IsLoaded => _orangeCottage != null;
    public static Texture2D? OrangeCottage => _orangeCottage;

    public static void Load(ContentManager content)
    {
        _orangeCottage = null;
        try
        {
            _orangeCottage = content.Load<Texture2D>("Characters/FarmRpg/Buildings/tiny_house_orange");
        }
        catch
        {
            // Content may not be built yet.
        }
    }

    /// <summary>Bottom-center of the cottage sprite (draw origin).</summary>
    public static Vector2 FootAnchor()
    {
        if (_orangeCottage == null)
            return new Vector2(36f, 86f);
        return new Vector2(_orangeCottage.Width * 0.5f, _orangeCottage.Height);
    }

    /// <summary>Door interact offset from foot anchor in unscaled art px (right-side door).</summary>
    public static Vector2 DoorOffsetFromFoot()
    {
        var h = _orangeCottage?.Height ?? 86;
        var w = _orangeCottage?.Width ?? 72;
        return new Vector2(w * 0.22f, -h * 0.12f);
    }
}
