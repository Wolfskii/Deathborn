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
    /// <summary>Unscaled pixels from sprite bottom to the visual foot (opaque base).</summary>
    public float FootInset;
    /// <summary>Unscaled pixels from sprite bottom to canopy/foliage top row.</summary>
    public float CanopyTopInset;
    /// <summary>Unscaled pixels from sprite bottom to where canopy ends (trunk begins on trees).</summary>
    public float CanopyBottomInset;
    /// <summary>Unscaled half-width of the overlap / transparency region.</summary>
    public float CanopyHalfWidth;
    public bool BlocksMovement;
    public byte ColliderMaskId = FoliagePixelCollider.NoMaskId;
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
    private const float BaseTownPad = 56f;
    private const float BaseSpawnClearRadius = 160f;
    private static float _townPad = BaseTownPad;
    private static float _spawnClearRadius = BaseSpawnClearRadius;
    private const float UnderFoliageAlpha = 0.42f;
    /// <summary>Extra depth (unscaled px) into the ground shadow while still treated as behind.</summary>
    private const float TreeShadowDepthInset = 3f;

    private static readonly List<FoliageInstance> Instances = [];
    private static Texture2D?[] _textures = new Texture2D[13];
    private static float _animTime;
    private static bool _initialized;

    public static bool IsLoaded => _textures[0] != null;
    public static IReadOnlyList<FoliageInstance> All => Instances;

    public static void Load(ContentManager content)
    {
        _textures[0] = content.Load<Texture2D>("Decorations/FarmRpg/bush_1");
        _textures[1] = content.Load<Texture2D>("Decorations/FarmRpg/bush_2");
        _textures[2] = content.Load<Texture2D>("Decorations/FarmRpg/bush_3");
        _textures[3] = content.Load<Texture2D>("Decorations/FarmRpg/tree_pine");
        _textures[4] = content.Load<Texture2D>("Decorations/FarmRpg/tree_maple");
        _textures[5] = content.Load<Texture2D>("Decorations/FarmRpg/rock_1");
        _textures[6] = content.Load<Texture2D>("Decorations/FarmRpg/rock_2");
        _textures[7] = content.Load<Texture2D>("Decorations/FarmRpg/rock_1");
        _textures[8] = content.Load<Texture2D>("Decorations/FarmRpg/rock_2");
        _textures[9] = content.Load<Texture2D>("Decorations/FarmRpg/water_rock_1");
        _textures[10] = content.Load<Texture2D>("Decorations/FarmRpg/water_rock_1");
        _textures[11] = content.Load<Texture2D>("Decorations/FarmRpg/water_rock_2");
        _textures[12] = content.Load<Texture2D>("Decorations/FarmRpg/water_rock_2");
        FoliagePixelCollider.Build(_textures[5], _textures[6], _textures[3], _textures[4]);
    }

    public static void Initialize(WorldMap map)
    {
        if (_initialized) return;
        var scale = map.TileSize / 16f;
        _townPad = BaseTownPad * scale;
        _spawnClearRadius = BaseSpawnClearRadius * scale;
        _initialized = true;
        Instances.Clear();
        Generate(map);
    }

    public static void Update(float dt) => _animTime += dt;

    // Lift the circle by its radius so its bottom edge sits at the visual foot;
    // otherwise half the collider sticks out below the sprite.
    public static Vector2 ColliderCenter(FoliageInstance f) =>
        f.Kind == FoliageKind.Tree ? TreeStemColliderCenter(f) :
        new(f.Position.X, f.Position.Y - f.FootInset * f.Scale - f.CollisionRadius);

    private static Vector2 TreeStemColliderCenter(FoliageInstance f)
    {
        var stemTopY = SortY(f);
        var stemBottomY = FoliageBottomY(f);
        return new Vector2(f.Position.X, (stemTopY + stemBottomY) * 0.5f);
    }

    private static bool InstanceBlocksCircle(FoliageInstance f, Vector2 collisionCenter, float entityRadius)
    {
        if (!f.BlocksMovement) return false;

        if (f.Kind == FoliageKind.Tree)
        {
            var feetY = collisionCenter.Y - PlayerEntity.CollisionCenterYOffset;
            if (feetY < SortY(f))
                return false;
        }

        if (FoliagePixelCollider.TryGetMask(f.ColliderMaskId, out var mask) && mask != null)
            return FoliagePixelCollider.CircleOverlaps(f, mask, collisionCenter, entityRadius);

        var center = f.Kind == FoliageKind.Tree ? TreeStemColliderCenter(f) : ColliderCenter(f);
        var hit = f.CollisionRadius + entityRadius;
        var dx = collisionCenter.X - center.X;
        var dy = collisionCenter.Y - center.Y;
        return dx * dx + dy * dy <= hit * hit;
    }

    private static Vector2 PushOutOfInstance(FoliageInstance f, Vector2 center, float entityRadius)
    {
        if (FoliagePixelCollider.TryGetMask(f.ColliderMaskId, out var mask) && mask != null)
            return FoliagePixelCollider.PushOut(f, mask, center, entityRadius);

        var colliderCenter = f.Kind == FoliageKind.Tree ? TreeStemColliderCenter(f) : ColliderCenter(f);
        var dx = center.X - colliderCenter.X;
        var dy = center.Y - colliderCenter.Y;
        var minDist = f.CollisionRadius + entityRadius;
        var distSq = dx * dx + dy * dy;
        if (distSq >= minDist * minDist || distSq < 0.0001f)
            return center;

        var dist = MathF.Sqrt(distSq);
        var push = (minDist - dist) / dist;
        return center + new Vector2(dx * push, dy * push);
    }

    /// <summary>Ground contact / trunk base (north of texture bottom).</summary>
    public static float SortY(FoliageInstance f) =>
        f.Position.Y - f.FootInset * f.Scale;

    /// <summary>
    /// World Y of the object's bottom edge for depth — compare to player feet.
    /// Whichever bottom is higher on screen (smaller Y) is behind.
    /// </summary>
    public static float FoliageBottomY(FoliageInstance f) =>
        f.Kind == FoliageKind.Tree
            ? f.Position.Y + TreeShadowDepthInset * f.Scale
            : f.Position.Y;

    public static bool EntityIsBehind(FoliageInstance f, float entityFeetY) =>
        entityFeetY < FoliageBottomY(f);

    public static bool BlocksCircle(Vector2 pos, float radius)
    {
        foreach (var f in Instances)
        {
            if (InstanceBlocksCircle(f, pos, radius))
                return true;
        }
        return false;
    }

    public static bool BlocksFeet(Vector2 feet, float entityRadius) =>
        FeetWouldCollide(feet, entityRadius);

    public static Vector2 ResolvePosition(Vector2 feet, float entityRadius)
    {
        var center = PlayerEntity.CollisionCenter(feet);
        var resolved = ResolveCollisionCenter(center, entityRadius);
        return feet + (resolved - center);
    }

    /// <summary>
    /// Move with push-out sliding so the player glides around pixel-accurate rock/tree stems.
    /// </summary>
    public static Vector2 ResolveMoveBlock(Vector2 fromFeet, Vector2 toFeet, float entityRadius)
    {
        var fromCenter = PlayerEntity.CollisionCenter(fromFeet);
        var toCenter = PlayerEntity.CollisionCenter(toFeet);

        if (!CenterWouldCollide(toCenter, entityRadius))
            return toFeet;

        var resolved = ResolveCollisionCenter(toCenter, entityRadius);
        if (!CenterWouldCollide(resolved, entityRadius))
            return FeetFromCenter(fromFeet, resolved);

        var slideX = ResolveCollisionCenter(new Vector2(toCenter.X, fromCenter.Y), entityRadius);
        if (!CenterWouldCollide(slideX, entityRadius))
            return FeetFromCenter(fromFeet, slideX);

        var slideY = ResolveCollisionCenter(new Vector2(fromCenter.X, toCenter.Y), entityRadius);
        if (!CenterWouldCollide(slideY, entityRadius))
            return FeetFromCenter(fromFeet, slideY);

        var delta = toCenter - fromCenter;
        var len = delta.Length();
        if (len > 0.001f)
        {
            var dir = delta / len;
            var best = fromCenter;
            var lo = 0f;
            var hi = 1f;
            for (var i = 0; i < 7; i++)
            {
                var mid = (lo + hi) * 0.5f;
                var tryCenter = ResolveCollisionCenter(fromCenter + dir * (len * mid), entityRadius);
                if (!CenterWouldCollide(tryCenter, entityRadius))
                {
                    best = tryCenter;
                    lo = mid;
                }
                else hi = mid;
            }

            if (Vector2.DistanceSquared(best, fromCenter) > 0.01f)
                return FeetFromCenter(fromFeet, best);
        }

        return fromFeet;
    }

    private static Vector2 FeetFromCenter(Vector2 fromFeet, Vector2 resolvedCenter) =>
        fromFeet + (resolvedCenter - PlayerEntity.CollisionCenter(fromFeet));

    private static bool CenterWouldCollide(Vector2 center, float entityRadius) =>
        FeetWouldCollide(PlayerEntity.CollisionCenterToFeet(center), entityRadius);

    private static bool FeetWouldCollide(Vector2 feet, float entityRadius)
    {
        var center = PlayerEntity.CollisionCenter(feet);
        foreach (var f in Instances)
        {
            if (InstanceBlocksCircle(f, center, entityRadius))
                return true;
        }
        return false;
    }

    private static Vector2 ResolveCollisionCenter(Vector2 pos, float entityRadius)
    {
        for (var iter = 0; iter < 6; iter++)
        {
            var pushed = false;
            foreach (var f in Instances)
            {
                if (!InstanceBlocksCircle(f, pos, entityRadius)) continue;
                pos = PushOutOfInstance(f, pos, entityRadius);
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
            next = ResolveMoveBlock(pos, next, entityRadius);
            if (Vector2.DistanceSquared(next, pos) < 0.01f)
                return pos;
            pos = next;
        }
        return pos;
    }

    public static bool EntityUnderFoliage(FoliageInstance f, Vector2 pos, float entityRadius)
    {
        if (f.Kind is FoliageKind.Rock or FoliageKind.WaterRock) return false;
        if (!EntityIsBehind(f, pos.Y)) return false;

        var scale = f.Scale;
        var canopyTop = f.Position.Y - f.CanopyTopInset * scale;
        if (pos.Y < canopyTop) return false;

        var halfW = f.CanopyHalfWidth * scale;
        return MathF.Abs(pos.X - f.Position.X) <= halfW;
    }

    public static void GetVisible(
        WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom, List<FoliageInstance> visible)
    {
        visible.Clear();
        if (!IsLoaded || Instances.Count == 0) return;

        var margin = map.TileSize * 4f;
        var halfViewW = screenCenter.X / zoom + margin;
        var halfViewH = screenCenter.Y / zoom + margin;
        var minX = camera.X - halfViewW;
        var maxX = camera.X + halfViewW;
        var minY = camera.Y - halfViewH;
        var maxY = camera.Y + halfViewH;

        foreach (var f in Instances)
        {
            if (f.Position.X < minX || f.Position.X > maxX || f.Position.Y < minY || f.Position.Y > maxY)
                continue;
            visible.Add(f);
        }
    }

    public static void DrawInstance(
        SpriteBatch sb,
        FoliageInstance f,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        ReadOnlySpan<Vector2> localPlayerPositions,
        float entityRadius = PlayerEntity.Radius)
    {
        var tex = TextureFor(f);
        if (tex == null) return;

        var (frameW, frameH, frameCount, fps) = FrameSpec(f);
        var frame = AnimFrame(f, frameCount, fps);
        var src = SourceRect(f, frame, frameW, frameH);
        if (src.Right > tex.Width) src.Width = Math.Max(1, tex.Width - src.X);
        if (src.Bottom > tex.Height) src.Height = Math.Max(1, tex.Height - src.Y);

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

        var alpha = 1f;
        for (var i = 0; i < localPlayerPositions.Length; i++)
        {
            if (!EntityUnderFoliage(f, localPlayerPositions[i], entityRadius)) continue;
            alpha = UnderFoliageAlpha;
            break;
        }

        var color = Color.White * alpha;
        sb.Draw(tex, dest, src, color);
    }

    private static readonly List<FoliageInstance> VisibleScratch = [];

    public static void Draw(SpriteBatch sb, WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        GetVisible(map, camera, screenCenter, zoom, VisibleScratch);
        VisibleScratch.Sort(static (a, b) => a.Position.Y.CompareTo(b.Position.Y));
        Span<Vector2> empty = [];
        foreach (var f in VisibleScratch)
            DrawInstance(sb, f, camera, screenCenter, zoom, empty);
    }

    private static Texture2D? TextureFor(FoliageInstance f) => f.Kind switch
    {
        FoliageKind.Bush => _textures[f.Variant switch { 0 => 0, 1 => 1, _ => 2 }],
        FoliageKind.Tree => _textures[f.Variant == 0 ? 3 : 4],
        FoliageKind.Rock => _textures[5 + f.Variant],
        FoliageKind.WaterRock => _textures[9 + f.Variant],
        _ => null,
    };

    private static (int frameW, int frameH, int frameCount, float fps) FrameSpec(FoliageInstance f) => f.Kind switch
    {
        FoliageKind.Bush => (48, 32, 1, 1f),
        FoliageKind.Tree => (32, 48, 1, 1f),
        FoliageKind.Rock => (16, 16, 1, 1f),
        FoliageKind.WaterRock => (32, 32, 1, 1f),
        _ => (16, 16, 1, 1f),
    };

    private static Rectangle SourceRect(FoliageInstance f, int frame, int frameW, int frameH)
    {
        switch (f.Kind)
        {
            case FoliageKind.Bush:
                return new Rectangle((f.Variant % 3) * 48, 0, frameW, frameH);
            case FoliageKind.Tree when f.Variant == 0:
                // Pine sheet (256×96): 32 px cols; col 2 row 0–1 = one green tree (not 64 px col with mask/beehive).
                return new Rectangle(64, 0, frameW, frameH);
            case FoliageKind.Tree:
                // Maple sheet (288×192): row y=48 holds mature 32 px trees; y≥96 = stumps, y=144 = white masks.
                var mapleX = (f.AnimPhase & 1) * 64;
                return new Rectangle(mapleX, 48, frameW, frameH);
            case FoliageKind.Rock:
                return new Rectangle((f.Variant % 8) * 16, 0, frameW, frameH);
            case FoliageKind.WaterRock:
                return new Rectangle((3 + f.Variant % 2) * 32, 0, frameW, frameH);
            default:
                return new Rectangle(0, 0, frameW, frameH);
        }
    }

    /// <summary>
    /// Trees and bushes hold still during calm spells, then sway with eased gusts.
    /// Water rocks ripple continuously with gentle speed variation.
    /// </summary>
    private static int AnimFrame(FoliageInstance f, int frameCount, float baseFps)
    {
        if (frameCount <= 1) return 0;
        return 0;
    }

    private static int ModFrame(float floatFrame, int frameCount)
    {
        var mod = floatFrame % frameCount;
        if (mod < 0f) mod += frameCount;
        return Math.Clamp((int)mod, 0, frameCount - 1);
    }

    private static void Generate(WorldMap map)
    {
        var tw = map.TileWidth;
        var th = map.TileHeight;

        for (var ty = 0; ty < th; ty += LandStride)
        {
            for (var tx = 0; tx < tw; tx += LandStride)
            {
                if (!map.IsLand(tx, ty)) continue;
                if (map.HasElevation && map.GetElevation(tx, ty) < 1) continue;

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
        scale *= kind switch
        {
            FoliageKind.Tree => 2.75f,
            FoliageKind.Bush => 1.65f,
            FoliageKind.Rock => 1.15f,
            _ => 1f,
        };
        var variant = VariantFor(kind, tx, ty);
        var instance = new FoliageInstance
        {
            Kind = kind,
            Position = pos,
            Variant = variant,
            Scale = scale,
            AnimPhase = (int)(Hash(tx, ty, 11) % 100),
        };
        ConfigureMetrics(instance);
        Instances.Add(instance);
    }

    private static void ConfigureMetrics(FoliageInstance f)
    {
        switch (f.Kind)
        {
            case FoliageKind.Tree when f.Variant == 0:
                f.FootInset = 6f;
                f.CanopyTopInset = 34f;
                f.CanopyBottomInset = 11f;
                f.CanopyHalfWidth = 16f;
                f.CollisionRadius = 0f;
                f.ColliderMaskId = FoliagePixelCollider.MaskIdFor(f);
                f.BlocksMovement = true;
                break;
            case FoliageKind.Tree:
                f.FootInset = 8f;
                f.CanopyTopInset = 45f;
                f.CanopyBottomInset = 11f;
                f.CanopyHalfWidth = 16f;
                f.CollisionRadius = 0f;
                f.ColliderMaskId = FoliagePixelCollider.MaskIdFor(f);
                f.BlocksMovement = true;
                break;
            case FoliageKind.Bush:
                f.FootInset = 6f;
                f.CanopyTopInset = 26f;
                f.CanopyBottomInset = 4f;
                f.CanopyHalfWidth = 24f;
                f.CollisionRadius = 0f;
                f.BlocksMovement = false;
                break;
            case FoliageKind.Rock:
                f.FootInset = 4f;
                f.CanopyTopInset = 0f;
                f.CanopyBottomInset = 0f;
                f.CanopyHalfWidth = 0f;
                f.CollisionRadius = 0f;
                f.ColliderMaskId = FoliagePixelCollider.MaskIdFor(f);
                f.BlocksMovement = true;
                break;
            case FoliageKind.WaterRock:
                f.FootInset = 8f;
                f.CanopyTopInset = 0f;
                f.CanopyBottomInset = 0f;
                f.CanopyHalfWidth = 0f;
                f.CollisionRadius = 10f * f.Scale;
                f.BlocksMovement = true;
                break;
        }
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
        Vector2.DistanceSquared(pos, map.DefaultSpawn) < _spawnClearRadius * _spawnClearRadius;

    private static bool InTown(Vector2 world)
    {
        foreach (var zone in WorldZones.Towns)
        {
            if (world.X >= zone.Center.X - zone.HalfWidth - _townPad
                && world.X <= zone.Center.X + zone.HalfWidth + _townPad
                && world.Y >= zone.Center.Y - zone.HalfHeight - _townPad
                && world.Y <= zone.Center.Y + zone.HalfHeight + _townPad)
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
