using System.Text;

namespace Deathborn.Client.Rendering;

/// <summary>SpriteFont only includes printable ASCII; other chars crash DrawString.</summary>
public static class SpriteFontSafe
{
    public static string Filter(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is >= (char)32 and <= (char)126)
                sb.Append(c);
            else if (c is '\n' or '\r' or '\t')
                sb.Append(' ');
        }
        return sb.ToString();
    }
}
