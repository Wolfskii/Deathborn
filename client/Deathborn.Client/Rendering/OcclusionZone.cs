using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Optional unscaled ghost-zone rect from a sprite bottom-center anchor.
/// Left/top are negative, right/bottom are positive (bottom may extend into ground shadow).
/// </summary>
public readonly struct OcclusionColliderOverride
{
    public bool IsSet { get; init; }
    public float Left { get; init; }
    public float Right { get; init; }
    public float Top { get; init; }
    public float Bottom { get; init; }

    public static OcclusionColliderOverride FromLocalRect(float left, float right, float top, float bottom) =>
        new() { IsSet = true, Left = left, Right = right, Top = top, Bottom = bottom };
}

/// <summary>World object that can ghost/fade the player when walked under.</summary>
public interface IOcclusionHost
{
    Vector2 OcclusionAnchor { get; }
    float OcclusionScale { get; }
    byte OcclusionMaskId { get; }
    OcclusionColliderOverride OcclusionOverride { get; }

    /// <summary>
    /// When true, only collider overlap matters (aerial props like clouds).
    /// When false, the player must also be behind this object in Y-sort
    /// (player feet above <see cref="OcclusionDepthBottomY"/>).
    /// </summary>
    bool IsOverheadOccluder { get; }

    /// <summary>
    /// World Y of the object's feet / sort bottom. Used only when
    /// <see cref="IsOverheadOccluder"/> is false.
    /// </summary>
    float OcclusionDepthBottomY { get; }
}

public static class OcclusionZone
{
    public const byte NoMaskId = 255;

    public static bool TryGetWorldBounds(
        IOcclusionHost host,
        out float left,
        out float right,
        out float top,
        out float bottom)
    {
        left = right = top = bottom = 0f;
        var anchor = host.OcclusionAnchor;
        var scale = host.OcclusionScale;

        if (host.OcclusionOverride.IsSet)
        {
            var o = host.OcclusionOverride;
            left = anchor.X + o.Left * scale;
            right = anchor.X + o.Right * scale;
            top = anchor.Y + o.Top * scale;
            bottom = anchor.Y + o.Bottom * scale;
            return bottom > top;
        }

        if (!OcclusionMaskCache.TryGetMask(host.OcclusionMaskId, out var mask) || mask == null)
            return false;

        mask.GetWorldBounds(anchor, scale, out left, out right, out top, out bottom);
        return bottom > top;
    }

    /// <summary>
    /// True when the player collision ellipse overlaps the host ghost zone and
    /// (unless <see cref="IOcclusionHost.IsOverheadOccluder"/>) the player is behind it in Y-sort.
    /// </summary>
    public static bool EntityEllipseOverlaps(
        IOcclusionHost host,
        Vector2 feet,
        float rx,
        float ry)
    {
        // Ground props: if the player's feet are at/below the object's depth bottom,
        // the player is in front — do not occlude.
        if (!host.IsOverheadOccluder && feet.Y >= host.OcclusionDepthBottomY)
            return false;

        var center = PlayerEntity.CollisionCenter(feet);

        if (host.OcclusionOverride.IsSet)
        {
            if (!TryGetWorldBounds(host, out var left, out var right, out var top, out var bottom))
                return false;
            return PlayerEntity.EllipseOverlapsRect(center, rx, ry, left, right, top, bottom);
        }

        if (!OcclusionMaskCache.TryGetMask(host.OcclusionMaskId, out var mask) || mask == null)
            return false;

        return OcclusionMaskCache.ColliderOverlaps(host.OcclusionAnchor, host.OcclusionScale, mask, center, rx, ry);
    }

    public static void DrawDebug(
        SpriteBatch sb,
        IOcclusionHost host,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        Color color)
    {
        var thickness = MathF.Max(2f, 2f * zoom);
        if (host.OcclusionOverride.IsSet)
        {
            if (TryGetWorldBounds(host, out var left, out var right, out var top, out var bottom))
            {
                DrawPrimitives.DrawWorldRectOutline(
                    sb, left, top, right, bottom, camera, screenCenter, zoom, color, thickness);
            }
            return;
        }

        if (OcclusionMaskCache.TryGetMask(host.OcclusionMaskId, out var mask) && mask != null)
            OcclusionMaskCache.DrawDebugCollider(sb, host.OcclusionAnchor, host.OcclusionScale, mask, camera, screenCenter, zoom, color);
    }
}
