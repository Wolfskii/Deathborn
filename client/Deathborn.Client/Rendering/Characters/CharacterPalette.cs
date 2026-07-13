using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

public static class CharacterPalette
{
    public const int DefaultTolerance = 18;

    public static bool ColorsMatch(Color a, Color b, int tolerance)
    {
        if (a.A == 0 && b.A == 0) return true;
        return Math.Abs(a.R - b.R) <= tolerance
            && Math.Abs(a.G - b.G) <= tolerance
            && Math.Abs(a.B - b.B) <= tolerance;
    }

    public static Texture2D Remap(
        Texture2D source,
        GraphicsDevice device,
        IReadOnlyList<(Color From, Color To)> map,
        int tolerance = DefaultTolerance)
    {
        if (map.Count == 0)
            return source;

        var data = new Color[source.Width * source.Height];
        source.GetData(data);
        for (var i = 0; i < data.Length; i++)
        {
            var px = data[i];
            if (px.A == 0) continue;
            foreach (var (from, to) in map)
            {
                if (!ColorsMatch(px, from, tolerance)) continue;
                data[i] = new Color(to.R, to.G, to.B, px.A);
                break;
            }
        }

        var result = new Texture2D(device, source.Width, source.Height);
        result.SetData(data);
        return result;
    }
}
