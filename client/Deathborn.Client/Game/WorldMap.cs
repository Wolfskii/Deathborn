using System.Buffers.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Tile walkability grid for the Swarovia mainland overworld.</summary>
public sealed class WorldMap
{
    private static WorldMap? _swaroviaMainland;
    private static DateTime _sourceWriteTime;
    private static DateTime _elevationSourceWriteTime;

    public static WorldMap SwaroviaMainland => GetOrLoad();

    public int TileWidth { get; private init; }
    public int TileHeight { get; private init; }
    public float TileSize { get; private init; }
    public float WorldWidth => TileWidth * TileSize;
    public float WorldHeight => TileHeight * TileSize;
    public Vector2 DefaultSpawn { get; private init; }

    private bool[] _walkable = [];
    private sbyte[] _elevation = [];
    private byte[] _ramps = [];
    private int _maxElevation;
    private Texture2D? _mapColorTexture;
    private Color[] _mapTileColors = [];
    private DateTime _collisionWriteTime;
    private DateTime _elevationWriteTime;
    private DateTime _mapColorSourceWriteTime;

    public bool IsLand(int tx, int ty) =>
        (uint)tx < (uint)TileWidth && (uint)ty < (uint)TileHeight && _walkable[ty * TileWidth + tx];

    public bool HasElevation => _elevation.Length > 0;
    public int MaxElevation => _maxElevation;

    public int GetElevation(int tx, int ty)
    {
        if ((uint)tx >= (uint)TileWidth || (uint)ty >= (uint)TileHeight)
            return -1;
        if (!HasElevation)
            return IsLand(tx, ty) ? 0 : -1;
        return _elevation[ty * TileWidth + tx];
    }

    /// <summary>0 = none, 1 = left ramp landing, 2 = right ramp landing (bottom cell).</summary>
    public int GetRamp(int tx, int ty)
    {
        if (_ramps.Length == 0 || (uint)tx >= (uint)TileWidth || (uint)ty >= (uint)TileHeight)
            return 0;
        return _ramps[ty * TileWidth + tx];
    }

    public bool IsRampLanding(int tx, int ty) => GetRamp(tx, ty) != 0;

    public bool IsRampTop(int tx, int ty) =>
        (uint)ty + 1 < (uint)TileHeight && GetRamp(tx, ty + 1) != 0;

    /// <summary>Whether an orthogonal step between two land tiles is allowed by elevation.</summary>
    public bool CanStepElevation(int fx, int fy, int tx, int ty)
    {
        if (!HasElevation) return true;

        var fe = GetElevation(fx, fy);
        var te = GetElevation(tx, ty);
        if (fe < 0 || te < 0) return false;
        if (fe == te) return true;
        if (Math.Abs(fe - te) != 1) return false;

        var dx = tx - fx;
        var dy = ty - fy;

        if (dx == 1 && dy == 0 && te == fe + 1 && GetRamp(fx, fy + 1) == 1)
            return true;
        if (dx == -1 && dy == 0 && fe == te + 1 && GetRamp(tx, ty + 1) == 1)
            return true;
        if (dx == -1 && dy == 0 && te == fe + 1 && GetRamp(fx, fy + 1) == 2)
            return true;
        if (dx == 1 && dy == 0 && fe == te + 1 && GetRamp(tx, ty + 1) == 2)
            return true;

        if (dx == 0 && dy == -1 && te == fe + 1)
        {
            if (GetRamp(tx, ty) != 0 && fx == tx && fy == ty + 1) return true;
            var (rx1, ry1, _, ok1) = RampTreadCellId(fx, fy);
            var (rx2, ry2, _, ok2) = RampTreadCellId(tx, ty);
            if (ok1 && ok2 && rx1 == rx2 && ry1 == ry2) return true;
            // Shoreline → plateau on walkable land (reverse of south descent).
            if (IsLand(tx, ty)) return true;
        }
        if (dx == 0 && dy == 1 && fe == te + 1)
        {
            if (GetRamp(fx, fy) != 0 && tx == fx && ty == fy + 1) return true;
            var (rx1, ry1, _, ok1) = RampTreadCellId(fx, fy);
            var (rx2, ry2, _, ok2) = RampTreadCellId(tx, ty);
            if (ok1 && ok2 && rx1 == rx2 && ry1 == ry2) return true;
            // Plateau → shoreline (or other −1 elevation band) on walkable land.
            if (IsLand(tx, ty)) return true;
        }

        if (dx != 0 && dy != 0)
        {
            var (rx1, ry1, _, ok1) = RampTreadCellId(fx, fy);
            var (rx2, ry2, _, ok2) = RampTreadCellId(tx, ty);
            if (ok1 && ok2 && rx1 == rx2 && ry1 == ry2)
            {
                var onTread = static (int x, int y, int rx, int ry) =>
                    (x == rx && y == ry) || (x == rx && y == ry - 1);
                if (onTread(fx, fy, rx1, ry1) && onTread(tx, ty, rx1, ry1))
                    return true;
            }
        }

        return false;
    }

    private (int rx, int ry, int kind, bool ok) RampTreadCellId(int tx, int ty)
    {
        for (var ry = 0; ry < TileHeight; ry++)
        for (var rx = 0; rx < TileWidth; rx++)
        {
            var ramp = GetRamp(rx, ry);
            if (ramp == 0) continue;
            if (tx == rx && (ty == ry || ty == ry - 1))
                return (rx, ry, ramp, true);
        }
        return (0, 0, 0, false);
    }

    private (int rx, int ry, int kind, bool ok) RampEngagedAtWorld(float worldX, float worldY)
    {
        var (rx, ry, kind, hit) = RampTreadAtWorld(worldX, worldY);
        if (hit) return (rx, ry, kind, true);

        var tx = (int)(worldX / TileSize);
        var ty = (int)(worldY / TileSize);
        var lx = (worldX - tx * TileSize) / TileSize;

        for (var ry2 = 0; ry2 < TileHeight; ry2++)
        for (var rx2 = 0; rx2 < TileWidth; rx2++)
        {
            var ramp = GetRamp(rx2, ry2);
            if (ramp == 0) continue;

            if (ramp == 1)
            {
                if (tx == rx2 - 1 && ty == ry2) return (rx2, ry2, 1, true);
                if (tx == rx2 && ty == ry2 + 1) return (rx2, ry2, 1, true);
                if (tx == rx2 && ty == ry2 && lx < 0.50f) return (rx2, ry2, 1, true);
                if (tx == rx2 && ty == ry2 - 1 && lx <= 0.50f) return (rx2, ry2, 1, true);
            }
            else
            {
                var lxr = tx == rx2 ? 1f - lx : lx;
                if (tx == rx2 + 1 && ty == ry2) return (rx2, ry2, 2, true);
                if (tx == rx2 && ty == ry2 + 1) return (rx2, ry2, 2, true);
                if (tx == rx2 && ty == ry2 && lxr < 0.50f) return (rx2, ry2, 2, true);
                if (tx == rx2 && ty == ry2 - 1 && lxr <= 0.50f) return (rx2, ry2, 2, true);
            }
        }
        return (0, 0, 0, false);
    }

    private (int rx, int ry, int kind, bool ok) RampTreadAtWorld(float worldX, float worldY)
    {
        for (var ry = 0; ry < TileHeight; ry++)
        for (var rx = 0; rx < TileWidth; rx++)
        {
            var ramp = GetRamp(rx, ry);
            if (ramp == 1 && OnLeftRampTread(worldX, worldY, rx, ry))
                return (rx, ry, 1, true);
            if (ramp == 2 && OnRightRampTread(worldX, worldY, rx, ry))
                return (rx, ry, 2, true);
        }
        return (0, 0, 0, false);
    }

    /// <summary>Green tread band for left stair pieces 29 (bottom) and 25 (top).</summary>
    private bool OnLeftRampTread(float worldX, float worldY, int rx, int ry)
    {
        var tx = (int)(worldX / TileSize);
        var ty = (int)(worldY / TileSize);
        var lx = (worldX - tx * TileSize) / TileSize;
        var ly = (worldY - ty * TileSize) / TileSize;

        if (tx == rx && ty == ry)
        {
            if (lx < 0.42f) return false;
            return RampBandHit(lx, ly, 0.46f, 0.56f, 0.93f, 0.14f, 0.11f);
        }
        if (tx == rx && ty == ry - 1)
        {
            if (lx > 0.50f) return false;
            return RampBandHit(lx, ly, 0.10f, 0.66f, 0.48f, 0.34f, 0.10f);
        }
        return false;
    }

    /// <summary>Green tread band for right stair pieces 32 (bottom) and 28 (top).</summary>
    private bool OnRightRampTread(float worldX, float worldY, int rx, int ry)
    {
        var tx = (int)(worldX / TileSize);
        var ty = (int)(worldY / TileSize);
        var lx = 1f - (worldX - tx * TileSize) / TileSize;
        var ly = (worldY - ty * TileSize) / TileSize;

        if (tx == rx && ty == ry)
        {
            if (lx < 0.42f) return false;
            return RampBandHit(lx, ly, 0.46f, 0.56f, 0.93f, 0.14f, 0.11f);
        }
        if (tx == rx && ty == ry - 1)
        {
            if (lx > 0.50f) return false;
            return RampBandHit(lx, ly, 0.10f, 0.66f, 0.48f, 0.34f, 0.10f);
        }
        return false;
    }

    private static bool RampBandHit(float lx, float ly, float ax, float ay, float bx, float by, float halfW)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var len2 = dx * dx + dy * dy;
        if (len2 < 0.0001f) return false;
        var t = ((lx - ax) * dx + (ly - ay) * dy) / len2;
        t = MathHelper.Clamp(t, 0f, 1f);
        var px = ax + t * dx;
        var py = ay + t * dy;
        var ddx = lx - px;
        var ddy = ly - py;
        return ddx * ddx + ddy * ddy <= halfW * halfW;
    }

    private bool TryGetRampSlopeWorld(float worldX, float worldY, out float dyPerDx)
    {
        dyPerDx = 0;
        if (!HasElevation) return false;

        var (_, _, kind, hit) = RampEngagedAtWorld(worldX, worldY);
        if (!hit) return false;
        dyPerDx = kind == 1 ? -1f : 1f;
        return true;
    }

    private bool CanTraverseWorld(float fromX, float fromY, float toX, float toY)
    {
        var sampleFromY = fromY;
        var sampleToY = toY;
        // Feet anchor sits below the ellipse — sample the relevant ellipse edge for vertical moves.
        if (toY > fromY + 0.001f)
        {
            sampleFromY = PlayerEntity.CollisionBottomY(fromY);
            sampleToY = PlayerEntity.CollisionBottomY(toY);
        }
        else if (toY < fromY - 0.001f)
        {
            sampleFromY = PlayerEntity.CollisionTopY(fromY);
            sampleToY = PlayerEntity.CollisionTopY(toY);
        }

        var fx = (int)(fromX / TileSize);
        var fy = (int)(sampleFromY / TileSize);
        var tx = (int)(toX / TileSize);
        var ty = (int)(sampleToY / TileSize);
        if (fx == tx && fy == ty) return true;

        if (HasElevation)
        {
            var (rx1, ry1, k1, ok1) = RampEngagedAtWorld(fromX, fromY);
            var (rx2, ry2, k2, ok2) = RampEngagedAtWorld(toX, toY);
            if (ok1 && ok2 && rx1 == rx2 && ry1 == ry2 && k1 == k2)
            {
                var fe = GetElevation(fx, fy);
                var te = GetElevation(tx, ty);
                if (fe >= 0 && te >= 0 && Math.Abs(fe - te) <= 1)
                    return true;
            }
        }

        if (CanStepElevation(fx, fy, tx, fy) && CanStepElevation(tx, fy, tx, ty))
            return true;
        if (fx != tx && fy != ty
            && CanStepElevation(fx, fy, fx, ty) && CanStepElevation(fx, ty, tx, ty))
            return true;
        return false;
    }

    private Vector2 AdjustRampDelta(float x, float y, Vector2 delta)
    {
        if (!HasElevation || MathF.Abs(delta.X) < 0.0001f)
            return delta;
        if (!TryGetRampSlopeWorld(x, y, out var dyPerDx))
            return delta;
        if (MathF.Abs(delta.Y) > MathF.Abs(delta.X) * 1.25f)
            return delta;

        return new Vector2(delta.X, dyPerDx * delta.X);
    }

    /// <summary>Authoritative terrain slide (elevation + foliage), matching server ResolveMove.</summary>
    public Vector2 ResolveMove(Vector2 feet, Vector2 delta, float entityRadius)
    {
        var fromX = feet.X;
        var fromY = feet.Y;
        var x = fromX;
        var y = fromY;
        delta = AdjustRampDelta(x, y, delta);

        var nx = x + delta.X;
        var ny = y + delta.Y;
        if (IsWalkable(nx, ny, entityRadius) && CanTraverseWorld(x, y, nx, ny))
        {
            x = nx;
            y = ny;
        }
        else
        {
            if (IsWalkable(nx, y, entityRadius) && CanTraverseWorld(x, y, nx, y))
                x = nx;
            if (IsWalkable(x, ny, entityRadius) && CanTraverseWorld(x, y, x, ny))
                y = ny;
        }

        return WorldFoliage.ResolveMoveBlock(feet, new Vector2(x, y), entityRadius);
    }

    private static string CollisionPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "World", "swarovia_mainland_collision.bin");

    private static string ElevationPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "World", "swarovia_mainland_elevation.bin");

    private static WorldMap GetOrLoad()
    {
        var path = CollisionPath;
        var writeTime = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        var elevWriteTime = File.Exists(ElevationPath) ? File.GetLastWriteTimeUtc(ElevationPath) : DateTime.MinValue;
        if (_swaroviaMainland != null && writeTime == _sourceWriteTime && elevWriteTime == _elevationSourceWriteTime)
            return _swaroviaMainland;

        _swaroviaMainland?._mapColorTexture?.Dispose();
        _swaroviaMainland = Load(path);
        _sourceWriteTime = writeTime;
        _elevationSourceWriteTime = elevWriteTime;
        return _swaroviaMainland;
    }

    private static WorldMap Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("World collision data missing. Run scripts/generate_world_collision.py.", path);

        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 13 || bytes[0] != (byte)'R' || bytes[1] != (byte)'E' || bytes[2] != (byte)'A' || bytes[3] != (byte)'L')
            throw new InvalidDataException("Invalid world collision header.");

        var version = bytes[4];
        if (version != 1)
            throw new InvalidDataException($"Unsupported world collision version {version}.");

        var tw = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(5, 2));
        var th = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(7, 2));
        var tileSize = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(9, 4));
        var expected = 13 + tw * th;
        if (bytes.Length < expected)
            throw new InvalidDataException($"World collision truncated (expected {expected} bytes).");

        var walkable = new bool[tw * th];
        for (var i = 0; i < walkable.Length; i++)
            walkable[i] = bytes[13 + i] != 0;

        var spawn = FindSpawnTile(walkable, tw, th, tileSize);
        var writeTime = File.GetLastWriteTimeUtc(path);
        var elevWriteTime = File.Exists(ElevationPath) ? File.GetLastWriteTimeUtc(ElevationPath) : DateTime.MinValue;
        var (elevation, ramps, maxElev) = TryLoadElevation(tw, th);
        return new WorldMap
        {
            TileWidth = tw,
            TileHeight = th,
            TileSize = tileSize,
            _walkable = walkable,
            _elevation = elevation,
            _ramps = ramps,
            _maxElevation = maxElev,
            DefaultSpawn = spawn,
            _collisionWriteTime = writeTime,
            _elevationWriteTime = elevWriteTime,
        };
    }

    private static (sbyte[] elevation, byte[] ramps, int maxElev) TryLoadElevation(int tw, int th)
    {
        var path = ElevationPath;
        if (!File.Exists(path))
            return ([], [], 0);

        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 9 || bytes[0] != (byte)'E' || bytes[1] != (byte)'L' || bytes[2] != (byte)'E' || bytes[3] != (byte)'V')
            return ([], [], 0);

        var version = bytes[4];
        if (version is not (1 or 2))
            return ([], [], 0);

        var etw = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(5, 2));
        var eth = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(7, 2));
        var expected = version == 2 ? 9 + tw * th * 2 : 9 + tw * th;
        if (etw != tw || eth != th || bytes.Length < expected)
            return ([], [], 0);

        var elev = new sbyte[tw * th];
        var max = 0;
        for (var i = 0; i < elev.Length; i++)
        {
            elev[i] = (sbyte)bytes[9 + i];
            if (elev[i] > max) max = elev[i];
        }

        var ramps = new byte[tw * th];
        if (version == 2)
        {
            var rampOff = 9 + tw * th;
            for (var i = 0; i < ramps.Length; i++)
                ramps[i] = bytes[rampOff + i];
        }

        return (elev, ramps, max);
    }

    private static Vector2 FindSpawnTile(bool[] grid, int tw, int th, float tileSize)
    {
        const int clearance = 10;
        var minTx = (int)(tw * 0.32f);
        var maxTx = (int)(tw * 0.68f);
        var minTy = (int)(th * 0.18f);
        var maxTy = (int)(th * 0.38f);

        var bestScore = -1;
        var bestTx = tw / 2;
        var bestTy = th / 2;

        for (var ty = minTy; ty <= maxTy; ty++)
        for (var tx = minTx; tx <= maxTx; tx++)
        {
            if (!IsLand(grid, tw, th, tx, ty) || !HasClearance(grid, tw, th, tx, ty, clearance))
                continue;

            var score = LandCount(grid, tw, th, tx, ty, 14);
            if (score <= bestScore) continue;
            bestScore = score;
            bestTx = tx;
            bestTy = ty;
        }

        return TileCenter(bestTx, bestTy, tileSize);
    }

    private static bool IsLand(bool[] grid, int tw, int th, int tx, int ty) =>
        (uint)tx < (uint)tw && (uint)ty < (uint)th && grid[ty * tw + tx];

    private static bool HasClearance(bool[] grid, int tw, int th, int tx, int ty, int radius)
    {
        for (var dy = -radius; dy <= radius; dy++)
        for (var dx = -radius; dx <= radius; dx++)
        {
            if (dx * dx + dy * dy > radius * radius) continue;
            if (!IsLand(grid, tw, th, tx + dx, ty + dy)) return false;
        }
        return true;
    }

    private static int LandCount(bool[] grid, int tw, int th, int tx, int ty, int radius)
    {
        var n = 0;
        for (var dy = -radius; dy <= radius; dy++)
        for (var dx = -radius; dx <= radius; dx++)
        {
            if (IsLand(grid, tw, th, tx + dx, ty + dy)) n++;
        }
        return n;
    }

    private static Vector2 TileCenter(int tx, int ty, float tileSize) =>
        new((tx + 0.5f) * tileSize, (ty + 0.5f) * tileSize);

    public bool IsWalkable(float worldX, float worldY, float radius = 0f)
    {
        var feet = new Vector2(worldX, worldY);
        var center = PlayerEntity.CollisionCenter(feet);
        var rx = PlayerEntity.CollisionRadiusX;
        var ry = PlayerEntity.CollisionRadiusY;
        var boundsR = MathF.Max(rx, ry);

        if (center.X < boundsR || center.Y < boundsR
            || center.X > WorldWidth - boundsR || center.Y > WorldHeight - boundsR)
            return false;

        if (radius <= 0f)
            return IsWalkableTile(center.X, center.Y) && !WorldFoliage.BlocksFeet(feet, 0f);

        return IsWalkableTile(center.X, center.Y)
            && IsWalkableTile(center.X + rx, center.Y)
            && IsWalkableTile(center.X - rx, center.Y)
            && IsWalkableTile(center.X, center.Y + ry)
            && IsWalkableTile(center.X, center.Y - ry)
            && !WorldFoliage.BlocksFeet(feet, radius);
    }

    private bool IsWalkableTile(float worldX, float worldY)
    {
        var tx = (int)(worldX / TileSize);
        var ty = (int)(worldY / TileSize);
        if ((uint)tx >= (uint)TileWidth || (uint)ty >= (uint)TileHeight)
            return false;
        return _walkable[ty * TileWidth + tx];
    }

    public void Draw(SpriteBatch sb, VisibleTileRegion region)
    {
        var tilePx = TileSize * region.Zoom;
        if (tilePx < 1f) return;

        var minTx = region.MinTx;
        var maxTx = region.MaxTx;
        var minTy = region.MinTy;
        var maxTy = region.MaxTy;

        for (var ty = minTy; ty <= maxTy; ty++)
        for (var tx = minTx; tx <= maxTx; tx++)
        {
            if (_walkable[ty * TileWidth + tx]) continue;
            var rect = region.Rect(tx, ty);
            if (!WaterTiles.TryDraw(sb, this, tx, ty, rect))
                DrawPrimitives.FillRect(sb, rect, WaterColor(tx, ty));
        }

        if (HasElevation && FarmRpgTerrain.IsLoaded)
        {
            FarmRpgTerrain.Draw(sb, this, region);
            FarmRpgGrassProps.Draw(sb, this, region);
        }
        else
        {
            for (var ty = minTy; ty <= maxTy; ty++)
            for (var tx = minTx; tx <= maxTx; tx++)
            {
                if (!_walkable[ty * TileWidth + tx]) continue;
                var rect = region.Rect(tx, ty);
                if (!TerrainLandTiles.TryDrawLand(sb, this, tx, ty, rect))
                    DrawPrimitives.FillRect(sb, rect, LandColor(tx, ty));
            }
        }
    }

    public static Rectangle GetTileScreenRect(
        int tx, int ty, Vector2 camera, Vector2 screenCenter, float zoom, float tileSize) =>
        TileScreenRect(tx, ty, camera, screenCenter, zoom, tileSize);

    /// <summary>Red edge lines on walkable tiles bordering blocked cells (F12 debug).</summary>
    public void DrawDebugTerrainBorders(SpriteBatch sb, VisibleTileRegion region)
    {
        var thickness = MathF.Max(2f, 2f * region.Zoom);
        var color = new Color(220, 48, 48);

        region.ForEachTile((tx, ty) =>
        {
            if (!IsLand(tx, ty))
                return;

            var rect = region.Rect(tx, ty);
            var topLeft = new Vector2(rect.Left, rect.Top);
            var topRight = new Vector2(rect.Right, rect.Top);
            var bottomLeft = new Vector2(rect.Left, rect.Bottom);
            var bottomRight = new Vector2(rect.Right, rect.Bottom);

            if (IsBlocked(tx, ty - 1))
                DrawPrimitives.DrawLine(sb, topLeft, topRight, color, thickness);
            if (IsBlocked(tx + 1, ty))
                DrawPrimitives.DrawLine(sb, topRight, bottomRight, color, thickness);
            if (IsBlocked(tx, ty + 1))
                DrawPrimitives.DrawLine(sb, bottomLeft, bottomRight, color, thickness);
            if (IsBlocked(tx - 1, ty))
                DrawPrimitives.DrawLine(sb, topLeft, bottomLeft, color, thickness);
        });
    }

    private bool IsBlocked(int tx, int ty) =>
        (uint)tx >= (uint)TileWidth || (uint)ty >= (uint)TileHeight || !_walkable[ty * TileWidth + tx];

    /// <summary>
    /// Pixel-snapped tile bounds so neighbours share edges with no sub-pixel gaps
    /// (prevents background showing through as shimmering black grid lines).
    /// </summary>
    private static Rectangle TileScreenRect(int tx, int ty, Vector2 camera, Vector2 screenCenter, float zoom, float tileSize)
    {
        var left = MathF.Floor((tx * tileSize - camera.X) * zoom + screenCenter.X);
        var top = MathF.Floor((ty * tileSize - camera.Y) * zoom + screenCenter.Y);
        var right = MathF.Floor(((tx + 1) * tileSize - camera.X) * zoom + screenCenter.X);
        var bottom = MathF.Floor(((ty + 1) * tileSize - camera.Y) * zoom + screenCenter.Y);
        return new Rectangle(
            (int)left,
            (int)top,
            Math.Max(1, (int)(right - left)),
            Math.Max(1, (int)(bottom - top)));
    }

    private static Color LandColor(int tx, int ty)
    {
        var v = 110 + ((tx * 7 + ty * 13) % 5) * 8;
        return new Color((byte)v, (byte)v, (byte)v);
    }

    private static Color WaterColor(int tx, int ty)
    {
        var v = 40 + ((tx * 3 + ty * 5) % 4) * 12;
        return new Color(20, 50 + v, 140 + v / 2);
    }

    private DateTime MapColorSourceTime =>
        _collisionWriteTime > _elevationWriteTime ? _collisionWriteTime : _elevationWriteTime;

    public void EnsureMapColorTexture(GraphicsDevice device)
    {
        var sourceTime = MapColorSourceTime;
        if (_mapColorTexture != null && _mapColorSourceWriteTime == sourceTime)
            return;

        _mapColorTexture?.Dispose();

        var data = new Color[TileWidth * TileHeight];
        for (var ty = 0; ty < TileHeight; ty++)
        for (var tx = 0; tx < TileWidth; tx++)
        {
            var i = ty * TileWidth + tx;
            data[i] = TerrainMapColors.ForTile(this, tx, ty);
        }

        _mapTileColors = data;
        var tex = new Texture2D(device, TileWidth, TileHeight);
        tex.SetData(data);
        _mapColorTexture = tex;
        _mapColorSourceWriteTime = sourceTime;
    }

    /// <summary>Local terrain fill for the circular minimap (elevation + water colors).</summary>
    public void DrawLocalMinimap(
        SpriteBatch sb,
        Vector2 minimapCenter,
        float minimapRadius,
        Vector2 worldCenter,
        float worldRadius)
    {
        if (worldRadius <= 0f || minimapRadius <= 0f) return;

        EnsureMapColorTexture(sb.GraphicsDevice);

        var scale = minimapRadius / worldRadius;

        var minTx = Math.Max(0, (int)((worldCenter.X - worldRadius) / TileSize) - 1);
        var maxTx = Math.Min(TileWidth - 1, (int)((worldCenter.X + worldRadius) / TileSize) + 1);
        var minTy = Math.Max(0, (int)((worldCenter.Y - worldRadius) / TileSize) - 1);
        var maxTy = Math.Min(TileHeight - 1, (int)((worldCenter.Y + worldRadius) / TileSize) + 1);

        var r2 = minimapRadius * minimapRadius;

        for (var ty = minTy; ty <= maxTy; ty++)
        for (var tx = minTx; tx <= maxTx; tx++)
        {
            var left = minimapCenter.X + (tx * TileSize - worldCenter.X) * scale;
            var top = minimapCenter.Y + (ty * TileSize - worldCenter.Y) * scale;
            var right = minimapCenter.X + ((tx + 1) * TileSize - worldCenter.X) * scale;
            var bottom = minimapCenter.Y + ((ty + 1) * TileSize - worldCenter.Y) * scale;

            var cx = (left + right) * 0.5f;
            var cy = (top + bottom) * 0.5f;
            var dx = cx - minimapCenter.X;
            var dy = cy - minimapCenter.Y;
            if (dx * dx + dy * dy > r2) continue;

            var rect = new Rectangle(
                (int)MathF.Floor(left),
                (int)MathF.Floor(top),
                Math.Max(1, (int)MathF.Ceiling(right) - (int)MathF.Floor(left)),
                Math.Max(1, (int)MathF.Ceiling(bottom) - (int)MathF.Floor(top)));

            DrawPrimitives.FillRect(sb, rect, _mapTileColors[ty * TileWidth + tx]);
        }
    }

    /// <summary>Elevation-colored continent for the world map overlay (M).</summary>
    public void DrawOverlay(SpriteBatch sb, Rectangle bounds, Vector2 playerWorldPos)
    {
        EnsureMapColorTexture(sb.GraphicsDevice);
        sb.Draw(_mapColorTexture!, bounds, Color.White);

        var px = bounds.X + playerWorldPos.X / WorldWidth * bounds.Width;
        var py = bounds.Y + playerWorldPos.Y / WorldHeight * bounds.Height;
        var center = new Vector2(px, py);
        DrawPrimitives.DrawCircleOutline(sb, center, 7f, new Color(0.12f, 0.1f, 0.08f, 0.9f), 24, 2.5f);
        DrawPrimitives.FillCircle(sb, center, 5f, new Color(1f, 0.88f, 0.35f));
        DrawPrimitives.DrawCircleOutline(sb, center, 5f, new Color(1f, 1f, 1f, 0.9f), 24, 1.5f);
    }
}
