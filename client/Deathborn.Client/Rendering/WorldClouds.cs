using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public sealed class CloudInstance
{
    public int Variant;
    public Vector2 Position;
    public float Scale;
    public float DriftSpeed;
    public Rectangle BodyRect;
    public Rectangle ShadowRect;
    public List<CloudSatellite> Satellites = [];
    public OcclusionColliderOverride OcclusionOverride;

    public void SetOcclusionOverride(float left, float right, float top, float bottom) =>
        OcclusionOverride = OcclusionColliderOverride.FromLocalRect(left, right, top, bottom);

    public CloudOcclusionPart ToOcclusionPart() =>
        new(Position, Variant, Scale, ShadowRect, OcclusionOverride);
}

public sealed class CloudSatellite
{
    public int Variant;
    public Vector2 Offset;
    public float Scale;
    public Rectangle BodyRect;
    public Rectangle ShadowRect;
    public OcclusionColliderOverride OcclusionOverride;

    public void SetOcclusionOverride(float left, float right, float top, float bottom) =>
        OcclusionOverride = OcclusionColliderOverride.FromLocalRect(left, right, top, bottom);

    public CloudOcclusionPart ToOcclusionPart(Vector2 shadowAnchor) =>
        new(shadowAnchor, Variant, Scale, ShadowRect, OcclusionOverride);
}

/// <summary>
/// One cloud body (primary or satellite) as an <see cref="IOcclusionHost"/>.
/// Always <see cref="IOcclusionHost.IsOverheadOccluder"/> — clouds sit above the scene in POV,
/// so Y-sort front/behind is ignored and only collider overlap ghosts the player.
/// </summary>
public readonly struct CloudOcclusionPart : IOcclusionHost
{
    public CloudOcclusionPart(
        Vector2 shadowAnchor,
        int variant,
        float scale,
        Rectangle shadowRect,
        OcclusionColliderOverride occlusionOverride = default)
    {
        ShadowAnchor = shadowAnchor;
        Variant = variant;
        Scale = scale;
        ShadowRect = shadowRect;
        OcclusionOverride = occlusionOverride;
    }

    public Vector2 ShadowAnchor { get; }
    public int Variant { get; }
    public float Scale { get; }
    public Rectangle ShadowRect { get; }
    public OcclusionColliderOverride OcclusionOverride { get; }

    public Vector2 OcclusionAnchor => new(ShadowAnchor.X, WorldClouds.GetBodyBottomWorldY(ShadowAnchor, ShadowRect, Scale));
    float IOcclusionHost.OcclusionScale => Scale;
    byte IOcclusionHost.OcclusionMaskId => OcclusionMaskCache.CloudMaskIdFor(Variant);
    OcclusionColliderOverride IOcclusionHost.OcclusionOverride => OcclusionOverride;
    // Aerial: overlap-only ghosting (see IOcclusionHost depth rules).
    bool IOcclusionHost.IsOverheadOccluder => true;
    // Unused while IsOverheadOccluder is true; kept for interface completeness.
    float IOcclusionHost.OcclusionDepthBottomY => OcclusionAnchor.Y;
}

/// <summary>
/// Overhead Farm RPG clouds — body and ground shadow drawn separately; ghost tint only when the local player is under the body.
/// </summary>
public static class WorldClouds
{
    private const uint Seed = 0xC10D05;
    private const int CloudStride = 10;
    private const float UnderCloudAlpha = 0.42f;
    private const float BodyShadowGap = 3f;
    private const int VariantCount = 4;
    private const int TinyVariant = 2;
    private static readonly int[] PrimaryVariants = [0, 1, 3];
    // Keep a consistent world-px : source-px ratio so tiny cloud art is not upscaled with chunky borders.
    private const float BaseSourceScale = 5.4f;
    private const float MaxSourceScale = 6.75f;
    private const float ScaleJitterRange = 1.35f;

    private static readonly CloudInstance[] VariantTemplate =
    [
        new() { BodyRect = new(5, 6, 39, 22), ShadowRect = new(5, 70, 39, 22) },
        new() { BodyRect = new(54, 13, 35, 15), ShadowRect = new(54, 77, 35, 15) },
        new() { BodyRect = new(97, 15, 8, 6), ShadowRect = new(97, 79, 8, 6) },
        new() { BodyRect = new(106, 7, 37, 20), ShadowRect = new(106, 71, 37, 20) },
    ];

    private const float MinDriftSpeed = 0.4f;

    private static readonly float[] VariantDrift = [-0.9f, -2.8f, 1.6f, 0.75f];
    private static readonly float[] VariantScaleMul = [1.08f, 1f, 0.78f, 1.04f];

    private static readonly List<CloudInstance> Instances = [];
    private static Texture2D? _texture;
    private static bool _initialized;

    public static bool IsLoaded => _texture != null;

    public static float GetBodyBottomWorldY(Vector2 shadowAnchor, Rectangle shadowRect, float scale) =>
        shadowAnchor.Y - shadowRect.Height * scale - BodyShadowGap;

    public static void Load(ContentManager content)
    {
        _texture = content.Load<Texture2D>("Decorations/FarmRpg/clouds");
        Span<Rectangle> bodies = stackalloc Rectangle[VariantCount];
        for (var i = 0; i < VariantCount; i++)
            bodies[i] = VariantTemplate[i].BodyRect;
        OcclusionMaskCache.BuildClouds(_texture, bodies);
    }

    public static void Initialize(WorldMap map)
    {
        if (_initialized) return;
        _initialized = true;
        Instances.Clear();
        Generate(map);
    }

    public static void Update(float dt, WorldMap map)
    {
        if (Instances.Count == 0) return;

        var wrapMargin = map.TileSize * 12f;
        var minX = -wrapMargin;
        var maxX = map.WorldWidth + wrapMargin;

        foreach (var c in Instances)
        {
            c.Position.X += c.DriftSpeed * dt;
            if (c.Position.X < minX)
                c.Position.X = maxX - (minX - c.Position.X);
            else if (c.Position.X > maxX)
                c.Position.X = minX + (c.Position.X - maxX);
        }
    }

    public static bool EntityUnderCloud(CloudInstance c, Vector2 pos, float entityRadius)
    {
        _ = entityRadius;
        if (EntityUnderOcclusionPart(c.ToOcclusionPart(), pos))
            return true;

        foreach (var sat in c.Satellites)
        {
            if (EntityUnderOcclusionPart(sat.ToOcclusionPart(c.Position + sat.Offset), pos))
                return true;
        }

        return false;
    }

    public static void GetVisible(
        WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom, List<CloudInstance> visible)
    {
        visible.Clear();
        if (!IsLoaded || Instances.Count == 0) return;

        var margin = map.TileSize * 14f;
        var halfViewW = screenCenter.X / zoom + margin;
        var halfViewH = screenCenter.Y / zoom + margin;
        var minX = camera.X - halfViewW;
        var maxX = camera.X + halfViewW;
        var minY = camera.Y - halfViewH;
        var maxY = camera.Y + halfViewH;

        foreach (var c in Instances)
        {
            GetBodyWorldBounds(c.Variant, c.Position, c.Scale, c.BodyRect, c.ShadowRect,
                out _, out var bodyTop, out var halfW, out _);
            halfW = MathF.Max(halfW, c.ShadowRect.Width * c.Scale * 0.5f);
            var cloudTop = bodyTop;
            var cloudBottom = c.Position.Y;

            foreach (var sat in c.Satellites)
            {
                var satPos = c.Position + sat.Offset;
                GetBodyWorldBounds(sat.Variant, satPos, sat.Scale, sat.BodyRect, sat.ShadowRect,
                    out _, out var satTop, out var satHalfW, out _);
                halfW = MathF.Max(halfW, satHalfW);
                halfW = MathF.Max(halfW, sat.ShadowRect.Width * sat.Scale * 0.5f);
                cloudTop = MathF.Min(cloudTop, satTop);
                cloudBottom = MathF.Max(cloudBottom, satPos.Y);
            }

            if (c.Position.X + halfW < minX || c.Position.X - halfW > maxX)
                continue;
            if (cloudBottom < minY || cloudTop > maxY)
                continue;
            visible.Add(c);
        }
    }

    public static void DrawInstance(
        SpriteBatch sb,
        CloudInstance c,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        ReadOnlySpan<Vector2> localPlayerPositions,
        float entityRadius = PlayerEntity.Radius)
    {
        if (_texture == null) return;

        DrawCloudPart(sb, c.Position, c.Variant, c.Scale, c.BodyRect, c.ShadowRect,
            BodyAlphaForPart(c.ToOcclusionPart(), localPlayerPositions, entityRadius),
            camera, screenCenter, zoom);
        foreach (var sat in c.Satellites)
        {
            var satPos = c.Position + sat.Offset;
            DrawCloudPart(sb, satPos, sat.Variant, sat.Scale, sat.BodyRect, sat.ShadowRect,
                BodyAlphaForPart(sat.ToOcclusionPart(satPos), localPlayerPositions, entityRadius),
                camera, screenCenter, zoom);
        }
    }

    private static float BodyAlphaForPart(
        CloudOcclusionPart part,
        ReadOnlySpan<Vector2> localPlayerPositions,
        float entityRadius)
    {
        _ = entityRadius;
        for (var i = 0; i < localPlayerPositions.Length; i++)
        {
            if (!EntityUnderOcclusionPart(part, localPlayerPositions[i]))
                continue;
            return UnderCloudAlpha;
        }

        return 1f;
    }

    private static void DrawCloudPart(
        SpriteBatch sb,
        Vector2 anchorPos,
        int variant,
        float scale,
        Rectangle bodyRect,
        Rectangle shadowRect,
        float bodyAlpha,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom)
    {
        if (_texture == null) return;

        var drawScale = scale * zoom;
        var shadowOrigin = new Vector2(shadowRect.Width * 0.5f, shadowRect.Height);
        var shadowScreen = WorldToScreen(anchorPos, camera, screenCenter, zoom);
        sb.Draw(_texture, shadowScreen, shadowRect, Color.White, 0f, shadowOrigin, drawScale, SpriteEffects.None, 0f);

        var bodyBottomWorld = GetBodyBottomWorldY(anchorPos, shadowRect, scale);
        var bodyOrigin = new Vector2(bodyRect.Width * 0.5f, bodyRect.Height);
        var bodyScreen = WorldToScreen(new Vector2(anchorPos.X, bodyBottomWorld), camera, screenCenter, zoom);
        sb.Draw(_texture, bodyScreen, bodyRect, Color.White * bodyAlpha, 0f, bodyOrigin, drawScale, SpriteEffects.None, 0f);
    }

    private static readonly List<CloudInstance> VisibleScratch = [];

    public static void Draw(
        SpriteBatch sb,
        WorldMap map,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        ReadOnlySpan<Vector2> localPlayerPositions)
    {
        GetVisible(map, camera, screenCenter, zoom, VisibleScratch);
        foreach (var c in VisibleScratch)
            DrawInstance(sb, c, camera, screenCenter, zoom, localPlayerPositions);
    }

    private static readonly Color OcclusionDebugColor = new(240, 210, 48);

    /// <summary>Yellow opaque-pixel outlines — matches under-cloud ghost tint (F12).</summary>
    public static void DrawDebugOcclusionZones(
        SpriteBatch sb, WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        GetVisible(map, camera, screenCenter, zoom, VisibleScratch);
        foreach (var c in VisibleScratch)
        {
            OcclusionZone.DrawDebug(sb, c.ToOcclusionPart(), camera, screenCenter, zoom, OcclusionDebugColor);
            foreach (var sat in c.Satellites)
            {
                var satPos = c.Position + sat.Offset;
                OcclusionZone.DrawDebug(sb, sat.ToOcclusionPart(satPos), camera, screenCenter, zoom, OcclusionDebugColor);
            }
        }
    }

    // Overhead occluder: no Y-sort gate — see IOcclusionHost / IsOverheadOccluder.
    private static bool EntityUnderOcclusionPart(CloudOcclusionPart part, Vector2 feet) =>
        OcclusionZone.EntityEllipseOverlaps(
            part,
            feet,
            PlayerEntity.CollisionRadiusX,
            PlayerEntity.CollisionRadiusY);

    private static void GetBodyWorldBounds(
        int variant,
        Vector2 anchorPos,
        float scale,
        Rectangle bodyRect,
        Rectangle shadowRect,
        out float centerX,
        out float topY,
        out float halfW,
        out float bottomY)
    {
        _ = variant;
        centerX = anchorPos.X;
        bottomY = GetBodyBottomWorldY(anchorPos, shadowRect, scale);
        var bodyH = bodyRect.Height * scale;
        topY = bottomY - bodyH;
        halfW = bodyRect.Width * scale * 0.5f;
    }

    private static Vector2 WorldToScreen(Vector2 world, Vector2 camera, Vector2 screenCenter, float zoom) =>
        new((world.X - camera.X) * zoom + screenCenter.X, (world.Y - camera.Y) * zoom + screenCenter.Y);

    private static void Generate(WorldMap map)
    {
        var tw = map.TileWidth;
        var th = map.TileHeight;
        var townPad = map.TileSize * 3.5f;
        var spawnClear = map.TileSize * 10f;

        for (var ty = 4; ty < th - 4; ty += CloudStride)
        {
            for (var tx = 4; tx < tw - 4; tx += CloudStride)
            {
                if (!map.IsLand(tx, ty)) continue;
                if (map.HasElevation && map.GetElevation(tx, ty) < 0) continue;

                var roll = Hash(tx, ty, 1) % 1000;
                if (roll >= 220) continue;

                var pos = JitteredPosition(map, tx, ty);
                if (NearSpawn(map, pos, spawnClear) || InTown(pos, townPad)) continue;

                var variant = PrimaryVariants[(int)(Hash(tx, ty, 2) % PrimaryVariants.Length)];
                var template = VariantTemplate[variant];
                var scaleJitter = (Hash(tx, ty, 3) % 1000) / 1000f;
                var scale = (BaseSourceScale + scaleJitter * ScaleJitterRange) * VariantScaleMul[variant];
                scale = MathF.Min(scale, MaxSourceScale);
                var drift = VariantDrift[variant];
                var driftJitter = (Hash(tx, ty, 4) % 1000) / 1000f * 0.5f + 0.75f;
                drift *= driftJitter;
                if ((Hash(tx, ty, 5) & 1) == 0)
                    drift = -drift;
                if (MathF.Abs(drift) < MinDriftSpeed)
                    drift = drift >= 0f ? MinDriftSpeed : -MinDriftSpeed;

                var cloud = new CloudInstance
                {
                    Variant = variant,
                    Position = pos,
                    Scale = scale,
                    DriftSpeed = drift,
                    BodyRect = template.BodyRect,
                    ShadowRect = template.ShadowRect,
                };
                AttachSatellites(cloud, tx, ty);
                Instances.Add(cloud);
            }
        }
    }

    private static void AttachSatellites(CloudInstance parent, int tx, int ty)
    {
        var mode = Hash(tx, ty, 6) % 1000;
        if (mode >= 520) return;

        var tiny = VariantTemplate[TinyVariant];
        var tinyScale = parent.Scale;
        var parentHalfW = parent.BodyRect.Width * parent.Scale * 0.5f;
        var tinyHalfW = tiny.BodyRect.Width * tinyScale * 0.5f;
        var gap = parent.Scale * 1.6f;
        var side = (Hash(tx, ty, 7) & 1) == 0 ? -1f : 1f;
        var flankX = side * (parentHalfW + tinyHalfW + gap);
        var flankY = -parent.Scale * (1.2f + (Hash(tx, ty, 8) % 1000) / 1000f * 2.2f);

        parent.Satellites.Add(MakeSatellite(flankX, flankY, tinyScale));

        if (mode < 200) return;

        if (mode < 360)
        {
            parent.Satellites.Add(MakeSatellite(-flankX, flankY * 0.75f, tinyScale * 0.92f));
            return;
        }

        var overlapX = (Hash(tx, ty, 9) % 1000) / 1000f * parentHalfW * 0.55f - parentHalfW * 0.275f;
        var overlapY = -parent.BodyRect.Height * parent.Scale * 0.3f;
        parent.Satellites.Add(MakeSatellite(overlapX, overlapY, tinyScale * 0.9f));
    }

    private static CloudSatellite MakeSatellite(float offsetX, float offsetY, float scale)
    {
        var template = VariantTemplate[TinyVariant];
        return new CloudSatellite
        {
            Variant = TinyVariant,
            Offset = new Vector2(offsetX, offsetY),
            Scale = scale,
            BodyRect = template.BodyRect,
            ShadowRect = template.ShadowRect,
        };
    }

    private static Vector2 JitteredPosition(WorldMap map, int tx, int ty)
    {
        var ts = map.TileSize;
        var jx = (Hash(tx, ty, 20) % 1000) / 1000f * ts * 0.85f - ts * 0.425f;
        var jy = (Hash(tx, ty, 21) % 1000) / 1000f * ts * 0.85f - ts * 0.425f;
        return new Vector2((tx + 0.5f) * ts + jx, (ty + 0.5f) * ts + jy);
    }

    private static bool NearSpawn(WorldMap map, Vector2 pos, float radius) =>
        Vector2.DistanceSquared(pos, map.DefaultSpawn) < radius * radius;

    private static bool InTown(Vector2 world, float pad)
    {
        foreach (var zone in WorldZones.Towns)
        {
            if (world.X >= zone.Center.X - zone.HalfWidth - pad
                && world.X <= zone.Center.X + zone.HalfWidth + pad
                && world.Y >= zone.Center.Y - zone.HalfHeight - pad
                && world.Y <= zone.Center.Y + zone.HalfHeight + pad)
                return true;
        }
        return false;
    }

    private static uint Hash(int tx, int ty, int salt)
    {
        var h = Seed ^ (uint)(tx * 73856093) ^ (uint)(ty * 19349663) ^ (uint)(salt * 83492791);
        h ^= h >> 16;
        h *= 0x85EBCA6B;
        h ^= h >> 13;
        h *= 0xC2B2AE35;
        h ^= h >> 16;
        return h;
    }
}
