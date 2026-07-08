namespace Deathborn.Client.Rendering;

/// <summary>Packs tile coordinates for reuse across terrain draw passes.</summary>
internal static class TerrainTileBuckets
{
    private static readonly List<int>[] ByElevation =
    [
        new(512),
        new(512),
        new(512),
        new(512),
        new(512),
    ];

    public static void Clear()
    {
        for (var i = 0; i < ByElevation.Length; i++)
            ByElevation[i].Clear();
    }

    public static void Add(int elevation, int tx, int ty)
    {
        if ((uint)elevation >= (uint)ByElevation.Length)
            return;
        ByElevation[elevation].Add(Pack(tx, ty));
    }

    private static readonly List<int> EmptyTiles = [];

    public static List<int> ForElevation(int elevation) =>
        (uint)elevation < (uint)ByElevation.Length ? ByElevation[elevation] : EmptyTiles;

    public static void Unpack(int packed, out int tx, out int ty)
    {
        tx = packed & 0xFFFF;
        ty = packed >> 16;
    }

    private static int Pack(int tx, int ty) => (ty << 16) | (tx & 0xFFFF);
}
