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

/// <summary>
/// World object that can ghost/fade the player when walked under its occlusion collider.
///
/// Depth rules (Y increases downward on screen):
/// <list type="bullet">
/// <item>
/// <see cref="IsOverheadOccluder"/> = false (trees, bushes, ground props):
/// ghost when the prop would paint over the player in exterior Y-sort
/// (player <see cref="Gameplay.PlayerEntity.SortY"/> vs <see cref="OcclusionDepthBottomY"/>)
/// and the player overlaps the occlusion collider (ellipse, or its AABB near tips).
/// </item>
/// <item>
/// <see cref="IsOverheadOccluder"/> = true (clouds / aerial props):
/// perspective is "above the camera," so front/behind Y-sort does not apply.
/// Ghost whenever the player collider overlaps the occlusion collider.
/// </item>
/// </list>
/// </summary>
public interface IOcclusionHost
{
    Vector2 OcclusionAnchor { get; }
    float OcclusionScale { get; }
    byte OcclusionMaskId { get; }
    OcclusionColliderOverride OcclusionOverride { get; }

    /// <summary>
    /// Aerial / sky-style occluder. When true, skip Y-sort front/behind and ghost on overlap only.
    /// Prefer this over ground foliage for anything that should always sit "above" the player in POV
    /// (clouds, flying props). Default for ground foliage is false.
    /// </summary>
    bool IsOverheadOccluder { get; }

    /// <summary>
    /// World Y of the object's visual feet / sort bottom (not the southern tip of the
    /// yellow occlusion ellipse). Compared to the player's collision bottom when
    /// <see cref="IsOverheadOccluder"/> is false.
    /// Unused for overhead occluders.
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
    /// Effective Y-sort depth for ghosting — matches exterior draw order.
    /// Uses <see cref="IOcclusionHost.OcclusionDepthBottomY"/> (bush yellow tip / tree feet).
    /// </summary>
    public static float EffectiveDepthBottomY(IOcclusionHost host) => host.OcclusionDepthBottomY;

    /// <summary>
    /// True when the player should be ghosted by this host.
    /// Requires collider overlap, and for ground props also Y-sort depth
    /// (see <see cref="IOcclusionHost"/>).
    /// </summary>
    public static bool EntityEllipseOverlaps(
        IOcclusionHost host,
        Vector2 feet,
        float rx,
        float ry)
    {
        // Same Y as exterior draw sorting (player.SortY vs OcclusionDepthBottomY).
        // Bushes use the yellow collider southern tip so ghosting is not delayed vs FootInset.
        if (!host.IsOverheadOccluder
            && PlayerEntity.SortYFromFeet(feet) >= EffectiveDepthBottomY(host))
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
