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

    // Door plate in unscaled art px, relative to the foot anchor (measured from tiny_house_orange.png:
    // the plate spans x 39..54 and y 62..85 of a 72x86 sprite whose foot anchor is (36, 86)).
    public const float DoorPlateLeft = 3f;
    public const float DoorPlateRight = 19f;
    public const float DoorPlateTop = -24f;
    public const float DoorPlateBottom = 0f;

    /// <summary>Door threshold: bottom-centre of the door plate, relative to the foot anchor (art px).</summary>
    public static Vector2 DoorOffsetFromFoot() =>
        new((DoorPlateLeft + DoorPlateRight) * 0.5f, DoorPlateBottom);

    /// <summary>Door click/highlight box in art px relative to the foot anchor: the plate plus a 1px margin.</summary>
    public static void DoorHitArtBounds(out float left, out float top, out float right, out float bottom)
    {
        const float margin = 1f;
        left = DoorPlateLeft - margin;
        right = DoorPlateRight + margin;
        top = DoorPlateTop - margin;
        bottom = DoorPlateBottom + margin;
    }
}
