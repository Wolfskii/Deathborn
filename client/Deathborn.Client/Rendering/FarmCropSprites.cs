using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Growth strips for homestead crops (16×N cells, last cell is harvest-ready).</summary>
public static class FarmCropSprites
{
    private static readonly Dictionary<string, Texture2D> Sheets = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int[]> TopOpaqueRows = new(StringComparer.Ordinal);

    public static void Load(ContentManager content)
    {
        Sheets.Clear();
        TopOpaqueRows.Clear();
        foreach (var id in Gameplay.FarmCatalog.Crops.Keys)
        {
            try
            {
                var sheet = content.Load<Texture2D>($"Crops/{id}");
                Sheets[id] = sheet;
                TopOpaqueRows[id] = MeasureTopOpaqueRows(sheet);
            }
            catch (ContentLoadException)
            {
                // Missing crop art is skipped; FarmRenderer falls back to a rectangle.
            }
        }
    }

    public static Texture2D? Get(string? cropId) =>
        cropId != null && Sheets.TryGetValue(cropId, out var tex) ? tex : null;

    public static int StageCount(Texture2D sheet) => Math.Max(1, sheet.Width / 16);

    /// <summary>
    /// Height in sprite pixels of the visible part of one growth stage: from the lowest-to-top
    /// opaque pixel down to the base. Transparent space above a short seedling is not counted.
    /// </summary>
    public static int VisibleHeight(string? cropId, int stage, int sheetHeight)
    {
        if (cropId == null || !TopOpaqueRows.TryGetValue(cropId, out var tops) || tops.Length == 0)
            return sheetHeight;
        var index = Math.Clamp(stage, 0, tops.Length - 1);
        return sheetHeight - tops[index];
    }

    private static int[] MeasureTopOpaqueRows(Texture2D sheet)
    {
        var pixels = new Color[sheet.Width * sheet.Height];
        sheet.GetData(pixels);
        var stages = StageCount(sheet);
        var tops = new int[stages];
        for (var stage = 0; stage < stages; stage++)
        {
            tops[stage] = sheet.Height;
            for (var y = 0; y < sheet.Height; y++)
            {
                var opaque = false;
                for (var x = stage * 16; x < stage * 16 + 16 && !opaque; x++)
                    opaque = pixels[y * sheet.Width + x].A > 0;
                if (!opaque) continue;
                tops[stage] = y;
                break;
            }
        }
        return tops;
    }
}
