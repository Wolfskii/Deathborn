using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>16×16 icons for the fishing rod, bait, and fish.</summary>
public static class FishIconAtlas
{
    private static readonly Dictionary<string, Texture2D> Icons = new(StringComparer.Ordinal);

    public static void Load(ContentManager content)
    {
        Icons.Clear();
        foreach (var name in IconIds())
        {
            try
            {
                Icons[name] = content.Load<Texture2D>($"Icons/fish/{name}");
            }
            catch (ContentLoadException)
            {
            }
        }
    }

    public static bool TryGet(string? id, out Texture2D texture)
    {
        texture = null!;
        return !string.IsNullOrEmpty(id) && Icons.TryGetValue(id, out texture!);
    }

    public static bool TryDraw(SpriteBatch sb, string? id, Rectangle dest)
    {
        if (!TryGet(id, out var tex))
            return false;
        sb.Draw(tex, dest, Color.White);
        return true;
    }

    private static IEnumerable<string> IconIds()
    {
        yield return "fishing_rod";
        yield return "worm_bait";
        foreach (var fish in FishCatalog.All)
            yield return fish.Id;
    }
}
