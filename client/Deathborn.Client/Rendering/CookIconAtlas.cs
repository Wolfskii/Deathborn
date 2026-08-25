using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

public static class CookIconAtlas
{
    private static readonly Dictionary<string, Texture2D> Icons = new(StringComparer.Ordinal);

    public static void Load(ContentManager content)
    {
        Icons.Clear();
        foreach (var recipe in CookCatalog.Recipes)
        {
            try
            {
                Icons[recipe.Result] = content.Load<Texture2D>($"Icons/cook/{recipe.Result}");
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
}
