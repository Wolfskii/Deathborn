using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>
/// Farm RPG speech bubbles (Ui/FarmRpg/emotes.png, installed by scripts/install_farm_rpg_ui.py).
/// Bubbles are 16 x 16 cells with the tail at the bottom centre.
/// </summary>
public static class EmoteSprites
{
    private static Texture2D? _sheet;

    /// <summary>Speech bubble with a red "!" (16 x 16, tail included).</summary>
    public static readonly Rectangle Exclamation = new(48, 192, 16, 16);

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content)
    {
        try
        {
            _sheet = content.Load<Texture2D>("Ui/FarmRpg/emotes");
        }
        catch (ContentLoadException)
        {
            _sheet = null;
        }
    }

    /// <summary>Draws a bubble with its tail tip at <paramref name="tailTip"/>, <paramref name="scale"/> screen px per art px.</summary>
    public static bool TryDraw(SpriteBatch sb, Rectangle source, Vector2 tailTip, float scale, Color tint)
    {
        if (_sheet == null) return false;
        var origin = new Vector2(source.Width * 0.5f, source.Height);
        sb.Draw(_sheet, tailTip, source, tint, 0f, origin, scale, SpriteEffects.None, 0f);
        return true;
    }
}
