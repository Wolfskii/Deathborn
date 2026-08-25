using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>16×16 inventory icons for farm tools, seeds, produce, and animals.</summary>
public static class FarmIconAtlas
{
    private static readonly Dictionary<string, Texture2D> Icons = new(StringComparer.Ordinal);

    public static void Load(ContentManager content)
    {
        Icons.Clear();
        foreach (var name in IconIds())
        {
            try
            {
                Icons[name] = content.Load<Texture2D>($"Icons/farm/{name}");
            }
            catch (ContentLoadException)
            {
            }
        }
    }

    public static bool TryDraw(SpriteBatch sb, string? id, Rectangle dest)
    {
        if (string.IsNullOrEmpty(id) || !Icons.TryGetValue(id, out var tex))
            return false;
        sb.Draw(tex, dest, Color.White);
        return true;
    }

    private static IEnumerable<string> IconIds()
    {
        yield return "hoe";
        yield return "watering_can";
        yield return "animal_feed";
        yield return "chicken_egg";
        yield return "milk";
        yield return "wool";
        yield return "chicken";
        yield return "cow";
        yield return "sheep";
        yield return "pig";
        foreach (var crop in Gameplay.FarmCatalog.Crops.Keys)
        {
            yield return crop;
            yield return crop + "_seeds";
        }
    }
}
