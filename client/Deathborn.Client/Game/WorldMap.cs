using System.Buffers.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Tile walkability grid for the Realik continent (generated from reference map art).</summary>
public sealed class WorldMap
{
    private static WorldMap? _realik;
    private static DateTime _sourceWriteTime;

    public static WorldMap Realik => GetOrLoad();

    public int TileWidth { get; private init; }
    public int TileHeight { get; private init; }
    public float TileSize { get; private init; }
    public float WorldWidth => TileWidth * TileSize;
    public float WorldHeight => TileHeight * TileSize;
    public Vector2 DefaultSpawn { get; private init; }

    private bool[] _walkable = [];
    private Texture2D? _landOverlayTexture;
    private DateTime _collisionWriteTime;
    private DateTime _overlaySourceWriteTime;

    public bool IsLand(int tx, int ty) =>
        (uint)tx < (uint)TileWidth && (uint)ty < (uint)TileHeight && _walkable[ty * TileWidth + tx];

    private static string CollisionPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "World", "realik_collision.bin");

    private static WorldMap GetOrLoad()
    {
        var path = CollisionPath;
        var writeTime = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        if (_realik != null && writeTime == _sourceWriteTime)
            return _realik;

        _realik?._landOverlayTexture?.Dispose();
        _realik = Load(path);
        _sourceWriteTime = writeTime;
        return _realik;
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
        return new WorldMap
        {
            TileWidth = tw,
            TileHeight = th,
            TileSize = tileSize,
            _walkable = walkable,
            DefaultSpawn = spawn,
            _collisionWriteTime = writeTime,
        };
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
        if (worldX < radius || worldY < radius
            || worldX > WorldWidth - radius || worldY > WorldHeight - radius)
            return false;

        if (radius <= 0f)
            return IsWalkableTile(worldX, worldY);

        return IsWalkableTile(worldX, worldY)
            && IsWalkableTile(worldX + radius, worldY)
            && IsWalkableTile(worldX - radius, worldY)
            && IsWalkableTile(worldX, worldY + radius)
            && IsWalkableTile(worldX, worldY - radius);
    }

    private bool IsWalkableTile(float worldX, float worldY)
    {
        var tx = (int)(worldX / TileSize);
        var ty = (int)(worldY / TileSize);
        if ((uint)tx >= (uint)TileWidth || (uint)ty >= (uint)TileHeight)
            return false;
        return _walkable[ty * TileWidth + tx];
    }

    public void Draw(SpriteBatch sb, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var tilePx = TileSize * zoom;
        if (tilePx < 1f) return;

        var halfViewW = screenCenter.X / zoom + TileSize * 2;
        var halfViewH = screenCenter.Y / zoom + TileSize * 2;

        var minTx = Math.Clamp((int)((camera.X - halfViewW) / TileSize), 0, TileWidth - 1);
        var maxTx = Math.Clamp((int)((camera.X + halfViewW) / TileSize), 0, TileWidth - 1);
        var minTy = Math.Clamp((int)((camera.Y - halfViewH) / TileSize), 0, TileHeight - 1);
        var maxTy = Math.Clamp((int)((camera.Y + halfViewH) / TileSize), 0, TileHeight - 1);

        for (var ty = minTy; ty <= maxTy; ty++)
        for (var tx = minTx; tx <= maxTx; tx++)
        {
            var rect = TileScreenRect(tx, ty, camera, screenCenter, zoom, TileSize);
            var walk = _walkable[ty * TileWidth + tx];
            if (walk)
            {
                if (!DungeonFloorTiles.TryDrawFloor(sb, tx, ty, rect))
                    DrawPrimitives.FillRect(sb, rect, LandColor(tx, ty));
            }
            else if (!WaterTiles.TryDraw(sb, tx, ty, rect))
                DrawPrimitives.FillRect(sb, rect, WaterColor(tx, ty));
        }
    }

    public static Rectangle GetTileScreenRect(
        int tx, int ty, Vector2 camera, Vector2 screenCenter, float zoom, float tileSize) =>
        TileScreenRect(tx, ty, camera, screenCenter, zoom, tileSize);

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

    private static readonly Color OverlayLand = new(138, 134, 128);

    public void EnsureOverlayTexture(GraphicsDevice device)
    {
        if (_landOverlayTexture != null && _overlaySourceWriteTime == _collisionWriteTime)
            return;

        _landOverlayTexture?.Dispose();

        var tex = new Texture2D(device, TileWidth, TileHeight);
        var data = new Color[TileWidth * TileHeight];
        for (var ty = 0; ty < TileHeight; ty++)
        for (var tx = 0; tx < TileWidth; tx++)
        {
            var i = ty * TileWidth + tx;
            data[i] = _walkable[i] ? OverlayLand : Color.Transparent;
        }
        tex.SetData(data);
        _landOverlayTexture = tex;
        _overlaySourceWriteTime = _collisionWriteTime;
    }

    /// <summary>Local area land tiles for the circular minimap (centered on worldCenter).</summary>
    public void DrawLocalMinimap(
        SpriteBatch sb,
        Vector2 minimapCenter,
        float minimapRadius,
        Vector2 worldCenter,
        float worldRadius)
    {
        if (worldRadius <= 0f || minimapRadius <= 0f) return;

        var scale = minimapRadius / worldRadius;
        var tileDraw = MathF.Max(1f, TileSize * scale);

        var minTx = Math.Max(0, (int)((worldCenter.X - worldRadius) / TileSize) - 1);
        var maxTx = Math.Min(TileWidth - 1, (int)((worldCenter.X + worldRadius) / TileSize) + 1);
        var minTy = Math.Max(0, (int)((worldCenter.Y - worldRadius) / TileSize) - 1);
        var maxTy = Math.Min(TileHeight - 1, (int)((worldCenter.Y + worldRadius) / TileSize) + 1);

        var r2 = minimapRadius * minimapRadius;

        for (var ty = minTy; ty <= maxTy; ty++)
        for (var tx = minTx; tx <= maxTx; tx++)
        {
            if (!_walkable[ty * TileWidth + tx]) continue;

            var world = new Vector2((tx + 0.5f) * TileSize, (ty + 0.5f) * TileSize);
            var screen = minimapCenter + (world - worldCenter) * scale;
            var dx = screen.X - minimapCenter.X;
            var dy = screen.Y - minimapCenter.Y;
            if (dx * dx + dy * dy > r2) continue;

            var half = tileDraw * 0.5f;
            var rect = new Rectangle(
                (int)MathF.Floor(screen.X - half),
                (int)MathF.Floor(screen.Y - half),
                Math.Max(1, (int)MathF.Ceiling(tileDraw)),
                Math.Max(1, (int)MathF.Ceiling(tileDraw)));

            if (!DungeonFloorTiles.TryDrawFloor(sb, tx, ty, rect))
            {
                var land = new Color(0.82f, 0.8f, 0.76f);
                DrawPrimitives.FillRect(sb, rect, land);
            }
        }
    }

    /// <summary>Scaled land silhouette for the circular minimap background.</summary>
    public void DrawMinimapLand(SpriteBatch sb, Rectangle bounds)
    {
        EnsureOverlayTexture(sb.GraphicsDevice);
        sb.Draw(_landOverlayTexture!, bounds, new Color(0.82f, 0.8f, 0.76f, 1f));
    }

    /// <summary>Land-only continent silhouette for the world map overlay.</summary>
    public void DrawOverlay(SpriteBatch sb, Rectangle bounds, Vector2 playerWorldPos)
    {
        EnsureOverlayTexture(sb.GraphicsDevice);
        sb.Draw(_landOverlayTexture!, bounds, Color.White);

        var px = bounds.X + playerWorldPos.X / WorldWidth * bounds.Width;
        var py = bounds.Y + playerWorldPos.Y / WorldHeight * bounds.Height;
        var center = new Vector2(px, py);
        DrawPrimitives.DrawCircleOutline(sb, center, 7f, new Color(0.12f, 0.1f, 0.08f, 0.9f), 24, 2.5f);
        DrawPrimitives.FillCircle(sb, center, 5f, new Color(1f, 0.88f, 0.35f));
        DrawPrimitives.DrawCircleOutline(sb, center, 5f, new Color(1f, 1f, 1f, 0.9f), 24, 1.5f);
    }
}
