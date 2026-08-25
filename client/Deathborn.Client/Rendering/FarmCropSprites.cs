using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Growth strips for homestead crops (16×N cells, last cell is harvest-ready).</summary>
public static class FarmCropSprites
{
    private static readonly Dictionary<string, Texture2D> Sheets = new(StringComparer.Ordinal);

    public static void Load(ContentManager content)
    {
        Sheets.Clear();
        foreach (var id in Gameplay.FarmCatalog.Crops.Keys)
        {
            try
            {
                Sheets[id] = content.Load<Texture2D>($"Crops/{id}");
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
}
