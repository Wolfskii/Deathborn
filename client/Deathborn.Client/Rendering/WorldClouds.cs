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
    public Rectangle SourceRect;
    public float CanopyTopInset;
    public float CanopyBottomInset;
    public float CanopyHalfWidth;
}

/// <summary>
/// Overhead Tiny Swords clouds with slow per-instance drift and canopy transparency.
/// </summary>
public static class WorldClouds
{
    private const uint Seed = 0xC10D05;
    private const int CloudStride = 10;
    private const float UnderCloudAlpha = 0.42f;

    private static readonly CloudInstance[] VariantTemplate =
    [
        new() { SourceRect = new(38, 42, 495, 157), CanopyTopInset = 149, CanopyBottomInset = 30, CanopyHalfWidth = 228 },
        new() { SourceRect = new(133, 85, 307, 110), CanopyTopInset = 102, CanopyBottomInset = 22, CanopyHalfWidth = 142 },
        new() { SourceRect = new(205, 102, 165, 75), CanopyTopInset = 68, CanopyBottomInset = 16, CanopyHalfWidth = 76 },
        new() { SourceRect = new(234, 115, 106, 57), CanopyTopInset = 50, CanopyBottomInset = 12, CanopyHalfWidth = 48 },
        new() { SourceRect = new(76, 49, 416, 152), CanopyTopInset = 144, CanopyBottomInset = 28, CanopyHalfWidth = 192 },
        new() { SourceRect = new(141, 90, 306, 99), CanopyTopInset = 92, CanopyBottomInset = 20, CanopyHalfWidth = 140 },
        new() { SourceRect = new(212, 92, 156, 83), CanopyTopInset = 76, CanopyBottomInset = 16, CanopyHalfWidth = 72 },
        new() { SourceRect = new(238, 109, 88, 55), CanopyTopInset = 48, CanopyBottomInset = 10, CanopyHalfWidth = 40 },
    ];

    private const float MinDriftSpeed = 0.4f;

    // px/s at world scale; every variant drifts (slowest ~0.4).
    private static readonly float[] VariantDrift =
        [-0.9f, -3.5f, 2.2f, -1.2f, 0.65f, -2f, 1.4f, 0.75f];

    private static readonly List<CloudInstance> Instances = [];
    private static Texture2D?[] _textures = new Texture2D[8];
    private static bool _initialized;

    public static bool IsLoaded => _textures[0] != null;

    public static void Load(ContentManager content)
    {
        for (var i = 0; i < 8; i++)
            _textures[i] = content.Load<Texture2D>($"Decorations/cloud_{i + 1:D2}");
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
        var scale = c.Scale;
        var topY = c.Position.Y - c.CanopyTopInset * scale;
        var bottomY = c.Position.Y - c.CanopyBottomInset * scale;
        if (pos.Y + entityRadius < topY || pos.Y - entityRadius > bottomY)
            return false;

        var halfW = c.CanopyHalfWidth * scale + entityRadius;
        return MathF.Abs(pos.X - c.Position.X) <= halfW;
    }

    public static List<CloudInstance> GetVisible(WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var visible = new List<CloudInstance>();
        if (!IsLoaded || Instances.Count == 0) return visible;

        var margin = map.TileSize * 14f;
        var halfViewW = screenCenter.X / zoom + margin;
        var halfViewH = screenCenter.Y / zoom + margin;
        var minX = camera.X - halfViewW;
        var maxX = camera.X + halfViewW;
        var minY = camera.Y - halfViewH;
        var maxY = camera.Y + halfViewH;

        foreach (var c in Instances)
        {
            var halfW = c.SourceRect.Width * c.Scale * 0.5f;
            var topY = c.Position.Y - c.SourceRect.Height * c.Scale;
            if (c.Position.X + halfW < minX || c.Position.X - halfW > maxX)
                continue;
            if (c.Position.Y < minY || topY > maxY)
                continue;
            visible.Add(c);
        }

        return visible;
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
        var tex = _textures[c.Variant];
        if (tex == null) return;

        var drawScale = c.Scale * zoom;
        var screenPos = new Vector2(
            (c.Position.X - camera.X) * zoom + screenCenter.X,
            (c.Position.Y - camera.Y) * zoom + screenCenter.Y);
        var origin = new Vector2(c.SourceRect.Width * 0.5f, c.SourceRect.Height);

        var alpha = 1f;
        for (var i = 0; i < entityPositions.Length; i++)
        {
            if (!EntityUnderCloud(c, entityPositions[i], entityRadius)) continue;
            alpha = UnderCloudAlpha;
            break;
        }

        sb.Draw(tex, screenPos, c.SourceRect, Color.White * alpha, 0f, origin, drawScale, SpriteEffects.None, 0f);
    }

    public static void Draw(
        SpriteBatch sb,
        WorldMap map,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        ReadOnlySpan<Vector2> entityPositions)
    {
        foreach (var c in GetVisible(map, camera, screenCenter, zoom))
            DrawInstance(sb, c, camera, screenCenter, zoom, entityPositions);
    }

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

                var variant = (int)(Hash(tx, ty, 2) % 8);
                var template = VariantTemplate[variant];
                var scaleJitter = (Hash(tx, ty, 3) % 1000) / 1000f;
                var tilesWide = 5.2f + variant * 0.22f + scaleJitter * 2.1f;
                if (variant >= 5)
                    tilesWide *= 1f + (variant - 4) * 0.2f;
                var targetWorldW = map.TileSize * tilesWide;
                var scale = targetWorldW / template.SourceRect.Width;
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
                    SourceRect = template.SourceRect,
                    CanopyTopInset = template.CanopyTopInset,
                    CanopyBottomInset = template.CanopyBottomInset,
                    CanopyHalfWidth = template.CanopyHalfWidth,
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
