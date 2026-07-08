using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public enum FoliageKind { Bush, Tree, Rock, WaterRock }

public sealed class FoliageInstance
{
    public FoliageKind Kind;
    public Vector2 Position;
    public int Variant;
    public float Scale;
    public float CollisionRadius;
    public int AnimPhase;
}

/// <summary>
/// Deterministic wilderness foliage (bushes, trees, rocks, water rocks) with circle colliders.
/// Generation matches server/internal/worldmap/foliage.go.
/// </summary>
public static class WorldFoliage
{
    private const uint Seed = 0xB00B5;
    private const int LandStride = 2;
    private const int WaterStride = 3;
    private const float TownPad = 56f;
    private const float SpawnClearRadius = 160f;

    private static readonly List<FoliageInstance> Instances = [];
    private static Texture2D?[] _textures = new Texture2D[13];
    private static float _animTime;
    private static bool _initialized;

    public static bool IsLoaded => _textures[0] != null;
    public static IReadOnlyList<FoliageInstance> All => Instances;

    public static void Load(ContentManager content)
    {
        _textures[0] = content.Load<Texture2D>("Decorations/bush_1");
        _textures[1] = content.Load<Texture2D>("Decorations/bush_2");
        _textures[2] = content.Load<Texture2D>("Decorations/bush_4");
        _textures[3] = content.Load<Texture2D>("Decorations/tree_3");
        _textures[4] = content.Load<Texture2D>("Decorations/tree_4");
        _textures[5] = content.Load<Texture2D>("Decorations/rock_1");
        _textures[6] = content.Load<Texture2D>("Decorations/rock_2");
        _textures[7] = content.Load<Texture2D>("Decorations/rock_3");
        _textures[8] = content.Load<Texture2D>("Decorations/rock_4");
        _textures[9] = content.Load<Texture2D>("Decorations/water_rock_1");
        _textures[10] = content.Load<Texture2D>("Decorations/water_rock_2");
        _textures[11] = content.Load<Texture2D>("Decorations/water_rock_3");
        _textures[12] = content.Load<Texture2D>("Decorations/water_rock_4");
    }

    public static void Initialize(WorldMap map)
    {
        if (_initialized) return;
        _initialized = true;
        Instances.Clear();
        Generate(map);
    }

    public static void Update(float dt) => _animTime += dt;

    public static bool BlocksCircle(Vector2 pos, float radius)
    {
        foreach (var f in Instances)
        {
            var hit = f.CollisionRadius + radius;
            if (Vector2.DistanceSquared(pos, f.Position) <= hit * hit)
                return true;
        }
        return false;
    }

    public static Vector2 ResolvePosition(Vector2 pos, float entityRadius)
    {
        for (var iter = 0; iter < 4; iter++)
        {
            var pushed = false;
            foreach (var f in Instances)
            {
                var dx = pos.X - f.Position.X;
                var dy = pos.Y - f.Position.Y;
                var minDist = f.CollisionRadius + entityRadius;
                var distSq = dx * dx + dy * dy;
                if (distSq >= minDist * minDist || distSq < 0.0001f) continue;
                var dist = MathF.Sqrt(distSq);
                var push = (minDist - dist) / dist;
                pos.X += dx * push;
                pos.Y += dy * push;
                pushed = true;
            }
            if (!pushed) break;
        }
        return pos;
    }

    public static Vector2 ClipSegment(Vector2 from, Vector2 to, float entityRadius)
    {
        var delta = to - from;
        var len = delta.Length();
        if (len < 0.01f) return from;

        var dir = delta / len;
        const int steps = 12;
        var stepLen = len / steps;
        var pos = from;
        for (var i = 0; i < steps; i++)
        {
            var next = pos + dir * stepLen;
            var resolved = ResolvePosition(next, entityRadius);
            if (Vector2.DistanceSquared(resolved, next) > 0.25f)
                return pos;
            pos = resolved;
        }
        return pos;
    }

    public static void Draw(SpriteBatch sb, WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        if (!IsLoaded || Instances.Count == 0) return;

        var tileSize = map.TileSize;
        var margin = tileSize * 4f;
        var halfViewW = screenCenter.X / zoom + margin;
        var halfViewH = screenCenter.Y / zoom + margin;
        var minX = camera.X - halfViewW;
        var maxX = camera.X + halfViewW;
        var minY = camera.Y - halfViewH;
        var maxY = camera.Y + halfViewH;

        var visible = new List<FoliageInstance>();
        foreach (var f in Instances)
        {
            if (f.Position.X < minX || f.Position.X > maxX || f.Position.Y < minY || f.Position.Y > maxY)
                continue;
            visible.Add(f);
        }
        visible.Sort((a, b) => a.Position.Y.CompareTo(b.Position.Y));

        foreach (var f in visible)
            DrawInstance(sb, f, camera, screenCenter, zoom);
    }

    private static void DrawInstance(SpriteBatch sb, FoliageInstance f, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var tex = TextureFor(f);
        if (tex == null) return;

        var (frameW, frameH, frameCount, fps) = FrameSpec(f.Kind);
        var frame = (int)((_animTime * fps + f.AnimPhase) % frameCount);
        var src = new Rectangle(frame * frameW, 0, frameW, frameH);

        var drawW = frameW * f.Scale * zoom;
        var drawH = frameH * f.Scale * zoom;
        var screenPos = new Vector2(
            (f.Position.X - camera.X) * zoom + screenCenter.X,
            (f.Position.Y - camera.Y) * zoom + screenCenter.Y);
        var dest = new Rectangle(
            (int)MathF.Floor(screenPos.X - drawW * 0.5f),
            (int)MathF.Floor(screenPos.Y - drawH),
            Math.Max(1, (int)MathF.Ceiling(drawW)),
            Math.Max(1, (int)MathF.Ceiling(drawH)));

        sb.Draw(tex, dest, src, Color.White);
    }

    private static Texture2D? TextureFor(FoliageInstance f) => f.Kind switch
    {
        FoliageKind.Bush => _textures[f.Variant switch { 0 => 0, 1 => 1, _ => 2 }],
        FoliageKind.Tree => _textures[f.Variant == 0 ? 3 : 4],
        FoliageKind.Rock => _textures[5 + f.Variant],
        FoliageKind.WaterRock => _textures[9 + f.Variant],
        _ => null,
    };

    private static (int frameW, int frameH, int frameCount, float fps) FrameSpec(FoliageKind kind) => kind switch
    {
        FoliageKind.Bush => (128, 128, 8, 8f),
        FoliageKind.Tree => (192, 192, 8, 6f),
        FoliageKind.Rock => (64, 64, 1, 1f),
        FoliageKind.WaterRock => (64, 64, 16, 10f),
        _ => (64, 64, 1, 1f),
    };

    private static void Generate(WorldMap map)
    {
        var tw = map.TileWidth;
        var th = map.TileHeight;
        var ts = map.TileSize;

        for (var ty = 0; ty < th; ty += LandStride)
        {
            for (var tx = 0; tx < tw; tx += LandStride)
            {
                if (!map.IsLand(tx, ty)) continue;

                var pos = JitteredPosition(map, tx, ty);
                if (InTown(pos) || NearSpawn(map, pos)) continue;

                var treeRoll = Hash(tx, ty, 1) % 1000;
                var rockRoll = Hash(tx, ty, 2) % 1000;
                var bushRoll = Hash(tx, ty, 3) % 1000;

                if (treeRoll < 16 && IsInland(map, tx, ty))
                    Add(FoliageKind.Tree, pos, tx, ty);
                else if (rockRoll < 12)
                    Add(FoliageKind.Rock, pos, tx, ty);
                else if (bushRoll < 28)
                    Add(FoliageKind.Bush, pos, tx, ty);
            }
        }

        for (var ty = 0; ty < th; ty += WaterStride)
        {
            for (var tx = 0; tx < tw; tx += WaterStride)
            {
                if (map.IsLand(tx, ty)) continue;

                var pos = JitteredPosition(map, tx, ty);
                if (InTown(pos)) continue;
                if (Hash(tx, ty, 4) % 1000 >= 22) continue;

                Add(FoliageKind.WaterRock, pos, tx, ty);
            }
        }
    }

    private static void Add(FoliageKind kind, Vector2 pos, int tx, int ty)
    {
        var scale = 0.78f + (Hash(tx, ty, 10) % 1000) / 1000f * 0.38f;
        var variant = VariantFor(kind, tx, ty);
        var radius = kind switch
        {
            FoliageKind.Tree => 20f * scale,
            FoliageKind.Rock => 15f * scale,
            FoliageKind.Bush => 12f * scale,
            FoliageKind.WaterRock => 12f * scale,
            _ => 12f,
        };

        Instances.Add(new FoliageInstance
        {
            Kind = kind,
            Position = pos,
            Variant = variant,
            Scale = scale,
            CollisionRadius = radius,
            AnimPhase = (int)(Hash(tx, ty, 11) % 100),
        });
    }

    private static int VariantFor(FoliageKind kind, int tx, int ty)
    {
        var roll = Hash(tx, ty, 12);
        return kind switch
        {
            FoliageKind.Bush => (int)(roll % 3),
            FoliageKind.Tree => (int)(roll % 2),
            FoliageKind.Rock => (int)(roll % 4),
            FoliageKind.WaterRock => (int)(roll % 4),
            _ => 0,
        };
    }

    private static Vector2 JitteredPosition(WorldMap map, int tx, int ty)
    {
        var ts = map.TileSize;
        var jx = (Hash(tx, ty, 20) % 1000) / 1000f * ts * 0.7f - ts * 0.35f;
        var jy = (Hash(tx, ty, 21) % 1000) / 1000f * ts * 0.7f - ts * 0.35f;
        return new Vector2((tx + 0.5f) * ts + jx, (ty + 0.5f) * ts + jy);
    }

    private static bool IsInland(WorldMap map, int tx, int ty) =>
        map.IsLand(tx, ty - 1) && map.IsLand(tx + 1, ty)
        && map.IsLand(tx, ty + 1) && map.IsLand(tx - 1, ty);

    private static bool NearSpawn(WorldMap map, Vector2 pos) =>
        Vector2.DistanceSquared(pos, map.DefaultSpawn) < SpawnClearRadius * SpawnClearRadius;

    private static bool InTown(Vector2 world)
    {
        foreach (var zone in WorldZones.Towns)
        {
            if (world.X >= zone.Center.X - zone.HalfWidth - TownPad
                && world.X <= zone.Center.X + zone.HalfWidth + TownPad
                && world.Y >= zone.Center.Y - zone.HalfHeight - TownPad
                && world.Y <= zone.Center.Y + zone.HalfHeight + TownPad)
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
