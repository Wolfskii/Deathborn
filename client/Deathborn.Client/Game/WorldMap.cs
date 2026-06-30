using System.Buffers.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Tile walkability grid for the Realik continent (generated from reference map art).</summary>
public sealed class WorldMap
{
    public static WorldMap Realik { get; } = Load();

    public int TileWidth { get; private init; }
    public int TileHeight { get; private init; }
    public float TileSize { get; private init; }
    public float WorldWidth => TileWidth * TileSize;
    public float WorldHeight => TileHeight * TileSize;
    public Vector2 DefaultSpawn { get; private init; }

    private bool[] _walkable = [];

    private static WorldMap Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Content", "World", "realik_collision.bin");
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
        return new WorldMap
        {
            TileWidth = tw,
            TileHeight = th,
            TileSize = tileSize,
            _walkable = walkable,
            DefaultSpawn = spawn,
        };
    }

    private static Vector2 FindSpawnTile(bool[] grid, int tw, int th, float tileSize)
    {
        // Prefer Dynas Expanse / central-north plains over deep forest or desert.
        var cx = tw / 2;
        var cy = (int)(th * 0.28f);
        for (var radius = 0; radius < Math.Max(tw, th); radius++)
        {
            for (var ty = Math.Max(0, cy - radius); ty <= Math.Min(th - 1, cy + radius); ty++)
            for (var tx = Math.Max(0, cx - radius); tx <= Math.Min(tw - 1, cx + radius); tx++)
            {
                if (!grid[ty * tw + tx]) continue;
                return TileCenter(tx, ty, tileSize);
            }
        }
        return TileCenter(tw / 2, th / 2, tileSize);
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
            var rect = TileScreenRect(tx, ty, camera, screenCenter, zoom);
            var walk = _walkable[ty * TileWidth + tx];
            var color = walk ? LandColor(tx, ty) : WaterColor(tx, ty);
            DrawPrimitives.FillRect(sb, rect, color);
        }
    }

    /// <summary>
    /// Pixel-snapped tile bounds so neighbours share edges with no sub-pixel gaps
    /// (prevents background showing through as shimmering black grid lines).
    /// </summary>
    private static Rectangle TileScreenRect(int tx, int ty, Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var left = MathF.Floor((tx * TileSize - camera.X) * zoom + screenCenter.X);
        var top = MathF.Floor((ty * TileSize - camera.Y) * zoom + screenCenter.Y);
        var right = MathF.Floor(((tx + 1) * TileSize - camera.X) * zoom + screenCenter.X);
        var bottom = MathF.Floor(((ty + 1) * TileSize - camera.Y) * zoom + screenCenter.Y);
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
}
