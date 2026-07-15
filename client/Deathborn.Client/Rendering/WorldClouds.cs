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
}

/// <summary>
/// Overhead Farm RPG clouds — body and ground shadow drawn separately; ghost tint only on body overlap.
/// </summary>
public static class WorldClouds
{
    private const uint Seed = 0xC10D05;
    private const int CloudStride = 10;
    private const float UnderCloudAlpha = 0.42f;
    private const float BodyShadowGap = 3f;
    private const int VariantCount = 4;
    private const byte OpaqueAlpha = 48;
    // Keep a consistent world-px : source-px ratio so tiny cloud art is not upscaled with chunky borders.
    private const float BaseSourceScale = 3.6f;
    private const float MaxSourceScale = 4.5f;

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
    private static readonly byte?[][] BodyAlphaMasks = new byte?[VariantCount][];
    private static Texture2D? _texture;
    private static bool _initialized;

    public static bool IsLoaded => _texture != null;

    public static void Load(ContentManager content)
    {
        _texture = content.Load<Texture2D>("Decorations/FarmRpg/clouds");
        var pixels = new Color[_texture.Width * _texture.Height];
        _texture.GetData(pixels);

        for (var i = 0; i < VariantCount; i++)
        {
            var body = VariantTemplate[i].BodyRect;
            var mask = new byte[body.Width * body.Height];
            for (var y = 0; y < body.Height; y++)
            for (var x = 0; x < body.Width; x++)
            {
                var px = pixels[(body.Y + y) * _texture.Width + body.X + x];
                mask[y * body.Width + x] = px.A;
            }
            BodyAlphaMasks[i] = mask;
        }
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
        var headY = pos.Y - entityRadius * 3.1f;
        var chestY = pos.Y - entityRadius * 1.6f;
        return SampleBodyOpaque(c, pos.X, headY)
            || SampleBodyOpaque(c, pos.X, chestY)
            || SampleBodyOpaque(c, pos.X, (headY + chestY) * 0.5f);
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
            GetBodyWorldBounds(c, out _, out var bodyTop, out var bodyHalfW, out var bodyBottom);
            var shadowHalfW = c.ShadowRect.Width * c.Scale * 0.5f;
            var halfW = MathF.Max(bodyHalfW, shadowHalfW);
            if (c.Position.X + halfW < minX || c.Position.X - halfW > maxX)
                continue;
            if (c.Position.Y < minY || bodyTop > maxY)
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
        ReadOnlySpan<Vector2> entityPositions,
        float entityRadius = PlayerEntity.Radius)
    {
        if (_texture == null) return;

        var drawScale = c.Scale * zoom;
        var shadowOrigin = new Vector2(c.ShadowRect.Width * 0.5f, c.ShadowRect.Height);
        var shadowScreen = WorldToScreen(c.Position, camera, screenCenter, zoom);
        sb.Draw(_texture, shadowScreen, c.ShadowRect, Color.White, 0f, shadowOrigin, drawScale, SpriteEffects.None, 0f);

        var bodyBottomWorld = GetBodyBottomWorldY(c);
        var bodyOrigin = new Vector2(c.BodyRect.Width * 0.5f, c.BodyRect.Height);
        var bodyScreen = WorldToScreen(new Vector2(c.Position.X, bodyBottomWorld), camera, screenCenter, zoom);

        var alpha = 1f;
        for (var i = 0; i < entityPositions.Length; i++)
        {
            if (!EntityUnderCloud(c, entityPositions[i], entityRadius)) continue;
            alpha = UnderCloudAlpha;
            break;
        }

        sb.Draw(_texture, bodyScreen, c.BodyRect, Color.White * alpha, 0f, bodyOrigin, drawScale, SpriteEffects.None, 0f);
    }

    private static readonly List<CloudInstance> VisibleScratch = [];

    public static void Draw(
        SpriteBatch sb,
        WorldMap map,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        ReadOnlySpan<Vector2> entityPositions)
    {
        GetVisible(map, camera, screenCenter, zoom, VisibleScratch);
        foreach (var c in VisibleScratch)
            DrawInstance(sb, c, camera, screenCenter, zoom, entityPositions);
    }

    private static float GetBodyBottomWorldY(CloudInstance c)
    {
        var shadowTop = c.Position.Y - c.ShadowRect.Height * c.Scale;
        return shadowTop - BodyShadowGap;
    }

    private static void GetBodyWorldBounds(
        CloudInstance c, out float centerX, out float topY, out float halfW, out float bottomY)
    {
        centerX = c.Position.X;
        bottomY = GetBodyBottomWorldY(c);
        var bodyH = c.BodyRect.Height * c.Scale;
        topY = bottomY - bodyH;
        halfW = c.BodyRect.Width * c.Scale * 0.5f;
    }

    private static bool SampleBodyOpaque(CloudInstance c, float worldX, float worldY)
    {
        var mask = BodyAlphaMasks[c.Variant];
        if (mask == null) return false;

        GetBodyWorldBounds(c, out var centerX, out var topY, out var halfW, out var bottomY);
        if (worldX < centerX - halfW || worldX > centerX + halfW || worldY < topY || worldY > bottomY)
            return false;

        var scale = c.Scale;
        var localX = (int)((worldX - centerX) / scale + c.BodyRect.Width * 0.5f);
        var localY = (int)((worldY - topY) / scale);
        if (localX < 0 || localY < 0 || localX >= c.BodyRect.Width || localY >= c.BodyRect.Height)
            return false;

        return mask[localY * c.BodyRect.Width + localX] >= OpaqueAlpha;
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

                var variant = (int)(Hash(tx, ty, 2) % VariantCount);
                var template = VariantTemplate[variant];
                var scaleJitter = (Hash(tx, ty, 3) % 1000) / 1000f;
                var scale = (BaseSourceScale + scaleJitter * 0.9f) * VariantScaleMul[variant];
                scale = MathF.Min(scale, MaxSourceScale);
                var drift = VariantDrift[variant];
                var driftJitter = (Hash(tx, ty, 4) % 1000) / 1000f * 0.5f + 0.75f;
                drift *= driftJitter;
                if ((Hash(tx, ty, 5) & 1) == 0)
                    drift = -drift;
                if (MathF.Abs(drift) < MinDriftSpeed)
                    drift = drift >= 0f ? MinDriftSpeed : -MinDriftSpeed;

                Instances.Add(new CloudInstance
                {
                    Variant = variant,
                    Position = pos,
                    Scale = scale,
                    DriftSpeed = drift,
                    BodyRect = template.BodyRect,
                    ShadowRect = template.ShadowRect,
                });
            }
        }
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
