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

    public static bool TryGet(string? id, out Texture2D texture)
    {
        if (string.IsNullOrEmpty(id)) { texture = null!; return false; }
        return Icons.TryGetValue(id, out texture!);
    }

    /// <summary>Draws the icon centred on <paramref name="center"/> at <paramref name="scale"/> screen px per icon px (cursors).</summary>
    public static bool TryDrawAt(SpriteBatch sb, string? id, Vector2 center, float scale, float alpha = 1f)
    {
        if (string.IsNullOrEmpty(id) || !Icons.TryGetValue(id, out var tex))
            return false;
        var origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
        sb.Draw(tex, center, null, Color.White * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
        return true;
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
