using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public enum FoliageKind { Bush, Tree, Rock, WaterRock }

public sealed class FoliageInstance : IOcclusionHost
{
    public FoliageKind Kind;
    public Vector2 Position;
    public int Variant;
    public float Scale;
    public float CollisionRadius;
    public int AnimPhase;
    /// <summary>Unscaled pixels from sprite bottom to the visual foot (opaque base).</summary>
    public float FootInset;
    public bool BlocksMovement;
    public byte ColliderMaskId = 255;
    public byte OcclusionMaskId = OcclusionZone.NoMaskId;
    public OcclusionColliderOverride OcclusionOverride;
    /// <summary>
    /// Set true for aerial foliage that should ghost on overlap alone (like clouds).
    /// Leave false (default) for trees/bushes so standing south/in front of them does not ghost.
    /// </summary>
    public bool IsOverheadOccluder;

    Vector2 IOcclusionHost.OcclusionAnchor => Position;
    float IOcclusionHost.OcclusionScale => Scale;
    byte IOcclusionHost.OcclusionMaskId => OcclusionMaskId;
    OcclusionColliderOverride IOcclusionHost.OcclusionOverride => OcclusionOverride;
    bool IOcclusionHost.IsOverheadOccluder => IsOverheadOccluder;
    /// <summary>Sprite / visual feet for Y-sort depth (see <see cref="WorldFoliage.OcclusionDepthBottomY"/>).</summary>
    float IOcclusionHost.OcclusionDepthBottomY => WorldFoliage.OcclusionDepthBottomY(this);

    /// <summary>Replace sprite-derived ghost zone with a custom unscaled rect from bottom-center.</summary>
    public void SetOcclusionOverride(float left, float right, float top, float bottom) =>
        OcclusionOverride = OcclusionColliderOverride.FromLocalRect(left, right, top, bottom);
}

/// <summary>
/// Deterministic wilderness foliage (bushes, trees, rocks, water rocks) with circle colliders.
/// Generation matches server/internal/worldmap/foliage.go.
/// </summary>
public static class WorldFoliage
{
    private const uint Seed = 0xB00B5;
    private const int LandStride = 2;
    private const float BaseTownPad = 56f;
    private const float BaseSpawnClearRadius = 160f;
    private static float _townPad = BaseTownPad;
    private static float _spawnClearRadius = BaseSpawnClearRadius;
    private const float UnderFoliageAlpha = 0.42f;
    /// <summary>Unscaled stem rows used for the square trunk collider (matches old pixel-mask height).</summary>
    private const float TreeStemColliderRows = 13f;
    /// <summary>Trim this many unscaled px off the collider bottom (above sprite anchor).</summary>
    private const float TreeStemColliderBottomInsetPx = 5f;
    /// <summary>
    /// Extra unscaled px the stem may extend north of <see cref="SortY"/>.
    /// Feet north of SortY pass through the canopy; the solid box must not reach deep into
    /// that band or walking down from behind traps the player inside the trunk.
    /// </summary>
    private const float TreeStemSortPadPx = 2f;

    private static readonly List<FoliageInstance> Instances = [];
    /// <summary>Sparse grid: cell key → instances whose feet sit in that cell.</summary>
    private static readonly Dictionary<long, List<FoliageInstance>> Cells = new();
    private const float CellSize = 128f;
    /// <summary>Largest collision/occlusion extent seen at last rebuild (query padding).</summary>
    private static float _queryExtent = 48f;
    private static Texture2D?[] _textures = new Texture2D[13];
    private static float _animTime;
    private static bool _initialized;

    public static bool IsLoaded => _textures[0] != null;
    public static IReadOnlyList<FoliageInstance> All => Instances;

    private static long CellKey(int cx, int cy) => ((long)cx << 32) ^ (uint)cy;

    private static void RebuildSpatialIndex()
    {
        Cells.Clear();
        _queryExtent = 48f;
        foreach (var f in Instances)
        {
            var ext = Math.Max(f.CollisionRadius, 40f * f.Scale);
            if (ext > _queryExtent) _queryExtent = ext;

            var cx = (int)MathF.Floor(f.Position.X / CellSize);
            var cy = (int)MathF.Floor(f.Position.Y / CellSize);
            var key = CellKey(cx, cy);
            if (!Cells.TryGetValue(key, out var list))
            {
                list = new List<FoliageInstance>(4);
                Cells[key] = list;
            }
            list.Add(f);
        }
    }

    private static bool AnyNear(Vector2 pos, float radius, Func<FoliageInstance, bool> predicate)
    {
        if (Cells.Count == 0) return false;
        var minCx = (int)MathF.Floor((pos.X - radius) / CellSize);
        var maxCx = (int)MathF.Floor((pos.X + radius) / CellSize);
        var minCy = (int)MathF.Floor((pos.Y - radius) / CellSize);
        var maxCy = (int)MathF.Floor((pos.Y + radius) / CellSize);
        for (var cy = minCy; cy <= maxCy; cy++)
        {
            for (var cx = minCx; cx <= maxCx; cx++)
            {
                if (!Cells.TryGetValue(CellKey(cx, cy), out var list)) continue;
                foreach (var f in list)
                {
                    if (predicate(f)) return true;
                }
            }
        }
        return false;
    }

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
        OcclusionMaskCache.Build(_textures[0], _textures[1], _textures[2], _textures[3], _textures[4]);
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
        RebuildSpatialIndex();
    }

    public static void Update(float dt) => _animTime += dt;

    // Lift the circle by its radius so its bottom edge sits at the visual foot;
    // otherwise half the collider sticks out below the sprite.
    public static Vector2 ColliderCenter(FoliageInstance f) =>
        new(f.Position.X, f.Position.Y - f.FootInset * f.Scale - f.CollisionRadius);

    private static void TreeStemColliderBounds(FoliageInstance f, out float left, out float right, out float top, out float bottom)
    {
        var half = f.CollisionRadius;
        left = f.Position.X - half;
        right = f.Position.X + half;
        bottom = f.Position.Y - TreeStemColliderBottomInsetPx * f.Scale;
        top = f.Position.Y - TreeStemColliderRows * f.Scale;
        // Keep the solid trunk out of the canopy pass-through band (feet north of SortY).
        var solidTop = SortY(f) - TreeStemSortPadPx * f.Scale;
        if (top < solidTop)
            top = solidTop;
        if (bottom < top + 0.5f * f.Scale)
            bottom = top + 0.5f * f.Scale;
        // Shorten from the bottom only — top stays put (40% less height).
        bottom = top + (bottom - top) * 0.6f;
    }

    private static bool CircleOverlapsTreeStem(FoliageInstance f, Vector2 center, float rx, float ry)
    {
        TreeStemColliderBounds(f, out var left, out var right, out var top, out var bottom);
        return PlayerEntity.EllipseOverlapsRect(center, rx, ry, left, right, top, bottom);
    }

    private static Vector2 PushOutOfTreeStem(FoliageInstance f, Vector2 center, float rx, float ry)
    {
        TreeStemColliderBounds(f, out var left, out var right, out var top, out var bottom);
        return PlayerEntity.PushEllipseOutOfRect(center, rx, ry, left, right, top, bottom);
    }

    private static bool InstanceBlocksEntity(FoliageInstance f, Vector2 collisionCenter, float rx, float ry)
    {
        if (!f.BlocksMovement) return false;

        if (f.Kind == FoliageKind.Tree)
        {
            var feetY = collisionCenter.Y - PlayerEntity.CollisionCenterYOffset;
            if (feetY < SortY(f))
                return false;
            return CircleOverlapsTreeStem(f, collisionCenter, rx, ry);
        }

        if (FoliagePixelCollider.TryGetMask(f.ColliderMaskId, out var mask) && mask != null)
            return FoliagePixelCollider.EllipseOverlaps(f, mask, collisionCenter, rx, ry);

        var center = ColliderCenter(f);
        return PlayerEntity.EllipseOverlapsCircle(collisionCenter, rx, ry, center, f.CollisionRadius);
    }

    private static Vector2 PushOutOfInstance(FoliageInstance f, Vector2 center, float rx, float ry)
    {
        if (f.Kind == FoliageKind.Tree)
            return PushOutOfTreeStem(f, center, rx, ry);

        if (FoliagePixelCollider.TryGetMask(f.ColliderMaskId, out var mask) && mask != null)
            return FoliagePixelCollider.PushOutEllipse(f, mask, center, rx, ry);

        var colliderCenter = ColliderCenter(f);
        var dx = center.X - colliderCenter.X;
        var dy = center.Y - colliderCenter.Y;
        var minDist = f.CollisionRadius + MathF.Max(rx, ry);
        var distSq = dx * dx + dy * dy;
        if (distSq >= minDist * minDist || distSq < 0.0001f)
            return center;

        var dist = MathF.Sqrt(distSq);
        var push = (minDist - dist + 0.35f) / dist;
        return center + new Vector2(dx * push, dy * push);
    }

    /// <summary>Ground contact / trunk base (north of texture bottom).</summary>
    public static float SortY(FoliageInstance f) =>
        f.Position.Y - f.FootInset * f.Scale;

    /// <summary>
    /// Exterior draw-order Y vs players — visual foot only.
    /// Do not push south into the shadow: that made trunks paint over players standing in front.
    /// Bushes use <see cref="OcclusionDepthBottomY"/> in the exterior sort list instead.
    /// </summary>
    public static float FoliageBottomY(FoliageInstance f) => SortY(f);

    /// <summary>
    /// Depth for ghosting + exterior Y-sort. Trees use visual feet; bushes use the yellow
    /// occlusion collider's southern tip so transparency starts when the player ellipse
    /// crosses that line — not several pixels later at FootInset.
    /// </summary>
    public static float OcclusionDepthBottomY(FoliageInstance f)
    {
        if (f.Kind == FoliageKind.Bush
            && OcclusionMaskCache.TryGetMask(f.OcclusionMaskId, out var mask)
            && mask != null)
        {
            if (mask.UsesSimplifiedCollider
                && mask.ColliderShape is OcclusionColliderShape.Ellipse or OcclusionColliderShape.Circle)
            {
                mask.GetWorldEllipse(f.Position, f.Scale, out var center, out _, out var radiusY);
                return center.Y + radiusY;
            }

            if (OcclusionZone.TryGetWorldBounds(f, out _, out _, out _, out var bottom))
                return bottom;
        }

        return FoliageBottomY(f);
    }

    /// <summary>True when the player would be drawn behind this foliage (same test as ghost depth).</summary>
    public static bool EntityIsBehind(FoliageInstance f, float entityFeetY) =>
        PlayerEntity.SortYFromFeet(new Vector2(0, entityFeetY)) < OcclusionDepthBottomY(f);

    public static bool BlocksCircle(Vector2 pos, float radius)
    {
        var pad = radius + _queryExtent + 4f;
        return AnyNear(pos, pad, f => InstanceBlocksEntity(f, pos, radius, radius));
    }

    public static bool BlocksFeet(Vector2 feet, float entityRadius) =>
        FeetWouldCollide(feet);

    public static Vector2 ResolvePosition(Vector2 feet, float entityRadius) => feet;

    /// <summary>
    /// Move with axis slide + segment clamp. Stops at contact — never push-out (no bounceback).
    /// Tries both slide orders so tree/rock corners do not wedge the ellipse.
    /// Always sweeps the path: thin stem AABBs are shorter than a frame of movement, so
    /// endpoint-only tests teleport through (from north of trunk → south in one step).
    /// </summary>
    public static Vector2 ResolveMoveBlock(Vector2 fromFeet, Vector2 toFeet, float entityRadius)
    {
        _ = entityRadius;
        var fromCenter = PlayerEntity.CollisionCenter(fromFeet);
        var toCenter = PlayerEntity.CollisionCenter(toFeet);
        var rx = PlayerEntity.CollisionRadiusX;
        var ry = PlayerEntity.CollisionRadiusY;

        // Soft depenetrate if already inside (e.g. spawned in trunk). Cap distance — never
        // fling the player to the far side of a thin stem.
        if (CenterWouldCollide(fromCenter))
        {
            var pushed = PushOutOfOverlappingFoliage(fromCenter, rx, ry);
            var delta = pushed - fromCenter;
            if (delta.LengthSquared() > 0.0001f)
            {
                // Keep push small; prefer not amplifying the intended move.
                var maxPush = MathF.Max(rx, ry) + 4f;
                if (delta.LengthSquared() > maxPush * maxPush)
                    delta = Vector2.Normalize(delta) * maxPush;
                fromCenter += delta;
                fromFeet = PlayerEntity.CollisionCenterToFeet(fromCenter);
                // Do not shift toCenter by the same delta — that recreated south-side teleports.
            }
        }

        if (PathClear(fromCenter, toCenter))
            return toFeet;

        TryFoliageSlide(fromCenter, toCenter, xFirst: true, out var a);
        TryFoliageSlide(fromCenter, toCenter, xFirst: false, out var b);
        var pathClamp = BinaryClampPath(fromCenter, toCenter);
        var da = (a - fromCenter).LengthSquared();
        var db = (b - fromCenter).LengthSquared();
        var dp = (pathClamp - fromCenter).LengthSquared();
        var best = pathClamp;
        var bestD = dp;
        if (da > bestD) { best = a; bestD = da; }
        if (db > bestD) { best = b; bestD = db; }

        if (bestD > 0.0001f)
            return FeetFromCenter(fromFeet, best);

        return fromFeet;
    }

    /// <summary>
    /// True when every sample along from→to is free. Step size ≤2px so thin tree stems cannot be skipped.
    /// </summary>
    private static bool PathClear(Vector2 from, Vector2 to)
    {
        if (CenterWouldCollide(from) || CenterWouldCollide(to))
            return false;

        var dist = Vector2.Distance(from, to);
        if (dist < 0.001f)
            return true;

        var steps = Math.Max(2, (int)MathF.Ceiling(dist / 2f));
        for (var i = 1; i < steps; i++)
        {
            var p = Vector2.Lerp(from, to, i / (float)steps);
            if (CenterWouldCollide(p))
                return false;
        }

        return true;
    }

    private static Vector2 BinaryClampPath(Vector2 from, Vector2 to)
    {
        if (!CenterWouldCollide(from) && PathClear(from, to))
            return to;

        var best = from;
        var lo = 0f;
        var hi = 1f;
        for (var i = 0; i < 8; i++)
        {
            var mid = (lo + hi) * 0.5f;
            var tryCenter = Vector2.Lerp(from, to, mid);
            if (!CenterWouldCollide(tryCenter) && PathClear(from, tryCenter))
            {
                best = tryCenter;
                lo = mid;
            }
            else hi = mid;
        }

        return best;
    }

    private static void TryFoliageSlide(Vector2 fromCenter, Vector2 toCenter, bool xFirst, out Vector2 result)
    {
        result = fromCenter;
        if (xFirst)
        {
            result = BinaryClampPath(fromCenter, new Vector2(toCenter.X, fromCenter.Y));
            result = BinaryClampPath(result, new Vector2(result.X, toCenter.Y));
        }
        else
        {
            result = BinaryClampPath(fromCenter, new Vector2(fromCenter.X, toCenter.Y));
            result = BinaryClampPath(result, new Vector2(toCenter.X, result.Y));
        }
    }

    private static Vector2 PushOutOfOverlappingFoliage(Vector2 center, float rx, float ry)
    {
        var pad = Math.Max(rx, ry) + _queryExtent + 4f;
        for (var iter = 0; iter < 4; iter++)
        {
            var moved = false;
            var minCx = (int)MathF.Floor((center.X - pad) / CellSize);
            var maxCx = (int)MathF.Floor((center.X + pad) / CellSize);
            var minCy = (int)MathF.Floor((center.Y - pad) / CellSize);
            var maxCy = (int)MathF.Floor((center.Y + pad) / CellSize);
            for (var cy = minCy; cy <= maxCy; cy++)
            {
                for (var cx = minCx; cx <= maxCx; cx++)
                {
                    if (!Cells.TryGetValue(CellKey(cx, cy), out var list)) continue;
                    foreach (var f in list)
                    {
                        if (!InstanceBlocksEntity(f, center, rx, ry)) continue;
                        var next = PushOutOfInstance(f, center, rx, ry);
                        if (next == center) continue;
                        center = next;
                        moved = true;
                    }
                }
            }
            if (!moved) break;
        }
        return center;
    }

    private static Vector2 FeetFromCenter(Vector2 fromFeet, Vector2 resolvedCenter) =>
        fromFeet + (resolvedCenter - PlayerEntity.CollisionCenter(fromFeet));

    private static bool CenterWouldCollide(Vector2 center) =>
        FeetWouldCollide(PlayerEntity.CollisionCenterToFeet(center));

    private static bool FeetWouldCollide(Vector2 feet)
    {
        var center = PlayerEntity.CollisionCenter(feet);
        var rx = PlayerEntity.CollisionRadiusX;
        var ry = PlayerEntity.CollisionRadiusY;
        var pad = Math.Max(rx, ry) + _queryExtent + 4f;
        return AnyNear(center, pad, f => InstanceBlocksEntity(f, center, rx, ry));
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

    /// <summary>
    /// Ghost under foliage when the occlusion collider overlaps and the player is behind
    /// the sprite feet (unless <see cref="FoliageInstance.IsOverheadOccluder"/>).
    /// </summary>
    public static bool EntityUnderFoliage(FoliageInstance f, Vector2 feet, float entityRadius)
    {
        _ = entityRadius;
        if (f.Kind is FoliageKind.Rock or FoliageKind.WaterRock) return false;
        if (f.OcclusionMaskId == OcclusionZone.NoMaskId && !f.OcclusionOverride.IsSet) return false;

        return OcclusionZone.EntityEllipseOverlaps(
            f,
            feet,
            PlayerEntity.CollisionRadiusX,
            PlayerEntity.CollisionRadiusY);
    }

    public static bool IsEntityUnderBush(Vector2 feet, float entityRadius = PlayerEntity.Radius)
    {
        var pad = entityRadius + _queryExtent + 8f;
        return AnyNear(feet, pad, f =>
            f.Kind == FoliageKind.Bush && EntityUnderFoliage(f, feet, entityRadius));
    }

    public static void GetVisible(
        WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom, List<FoliageInstance> visible)
    {
        visible.Clear();
        if (!IsLoaded || Instances.Count == 0 || Cells.Count == 0) return;

        var margin = map.TileSize * 4f;
        var halfViewW = screenCenter.X / zoom + margin;
        var halfViewH = screenCenter.Y / zoom + margin;
        var minX = camera.X - halfViewW;
        var maxX = camera.X + halfViewW;
        var minY = camera.Y - halfViewH;
        var maxY = camera.Y + halfViewH;

        var minCx = (int)MathF.Floor(minX / CellSize);
        var maxCx = (int)MathF.Floor(maxX / CellSize);
        var minCy = (int)MathF.Floor(minY / CellSize);
        var maxCy = (int)MathF.Floor(maxY / CellSize);
        for (var cy = minCy; cy <= maxCy; cy++)
        {
            for (var cx = minCx; cx <= maxCx; cx++)
            {
                if (!Cells.TryGetValue(CellKey(cx, cy), out var list)) continue;
                foreach (var f in list)
                {
                    if (f.Position.X < minX || f.Position.X > maxX || f.Position.Y < minY || f.Position.Y > maxY)
                        continue;
                    visible.Add(f);
                }
            }
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

    /// <summary>Chroma-key green collider outlines — call when F12 debug HUD is on.</summary>
    public static void DrawDebugColliders(SpriteBatch sb, WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        GetVisible(map, camera, screenCenter, zoom, VisibleScratch);
        foreach (var f in VisibleScratch)
        {
            if (!f.BlocksMovement) continue;

            if (f.Kind == FoliageKind.Tree)
            {
                TreeStemColliderBounds(f, out var left, out var right, out var top, out var bottom);
                var thickness = MathF.Max(2f, 2f * zoom);
                var screenLeft = (left - camera.X) * zoom + screenCenter.X;
                var screenTop = (top - camera.Y) * zoom + screenCenter.Y;
                var screenRight = (right - camera.X) * zoom + screenCenter.X;
                var screenBottom = (bottom - camera.Y) * zoom + screenCenter.Y;
                DrawPrimitives.DrawRectOutline(
                    sb,
                    new Rectangle(
                        (int)screenLeft,
                        (int)screenTop,
                        Math.Max(1, (int)MathF.Ceiling(screenRight - screenLeft)),
                        Math.Max(1, (int)MathF.Ceiling(screenBottom - screenTop))),
                    Color.Lime,
                    thickness);
                continue;
            }

            if (FoliagePixelCollider.TryGetMask(f.ColliderMaskId, out var mask) && mask != null)
            {
                FoliagePixelCollider.DrawDebugMask(sb, f, mask, camera, screenCenter, zoom);
                continue;
            }

            if (f.CollisionRadius <= 0f) continue;
            FoliagePixelCollider.DrawDebugCircle(sb, ColliderCenter(f), f.CollisionRadius, camera, screenCenter, zoom);
        }
    }

    private static readonly Color OcclusionDebugColor = new(240, 210, 48);

    /// <summary>Yellow canopy/bush fade zones — where the player walks under and art ghosts (F12).</summary>
    public static void DrawDebugOcclusionZones(SpriteBatch sb, WorldMap map, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        GetVisible(map, camera, screenCenter, zoom, VisibleScratch);
        foreach (var f in VisibleScratch)
        {
            if (f.Kind is FoliageKind.Rock or FoliageKind.WaterRock) continue;
            if (f.OcclusionMaskId == OcclusionZone.NoMaskId && !f.OcclusionOverride.IsSet) continue;
            OcclusionZone.DrawDebug(sb, f, camera, screenCenter, zoom, OcclusionDebugColor);
        }
    }

    public static void DrawDebugPlayerCollider(
        SpriteBatch sb, Vector2 feet, float entityRadius, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var center = PlayerEntity.CollisionCenter(feet);
        var screen = new Vector2(
            (center.X - camera.X) * zoom + screenCenter.X,
            (center.Y - camera.Y) * zoom + screenCenter.Y);
        DrawPrimitives.DrawEllipseOutline(
            sb,
            screen,
            PlayerEntity.CollisionRadiusX * zoom,
            PlayerEntity.CollisionRadiusY * zoom,
            Color.Lime,
            32,
            MathF.Max(2f, 2f * zoom));
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
                var bushRoll = Hash(tx, ty, 3) % 1000;

                if (treeRoll < 16 && IsInland(map, tx, ty))
                    Add(FoliageKind.Tree, pos, tx, ty);
                else if (bushRoll < 28)
                    Add(FoliageKind.Bush, pos, tx, ty);
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
                f.CollisionRadius = 5f * f.Scale;
                f.ColliderMaskId = FoliagePixelCollider.NoMaskId;
                f.OcclusionMaskId = OcclusionMaskCache.MaskIdFor(f);
                f.BlocksMovement = true;
                break;
            case FoliageKind.Tree:
                f.FootInset = 8f;
                f.CollisionRadius = 6f * f.Scale;
                f.ColliderMaskId = FoliagePixelCollider.NoMaskId;
                f.OcclusionMaskId = OcclusionMaskCache.MaskIdFor(f);
                f.BlocksMovement = true;
                break;
            case FoliageKind.Bush:
                f.FootInset = 6f;
                f.CollisionRadius = 0f;
                f.OcclusionMaskId = OcclusionMaskCache.MaskIdFor(f);
                f.BlocksMovement = false;
                break;
            case FoliageKind.Rock:
                f.FootInset = 4f;
                f.CollisionRadius = 0f;
                f.ColliderMaskId = FoliagePixelCollider.MaskIdFor(f);
                f.OcclusionMaskId = OcclusionZone.NoMaskId;
                f.BlocksMovement = true;
                break;
            case FoliageKind.WaterRock:
                f.FootInset = 8f;
                f.CollisionRadius = 10f * f.Scale;
                f.OcclusionMaskId = OcclusionZone.NoMaskId;
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
