using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Skull/monster glyph for boss markers on the minimap and world map.</summary>
public static class MonsterMapIcon
{
    public static void Draw(SpriteBatch sb, Vector2 center, float scale)
    {
        var s = 8f * scale;
        DrawPrimitives.FillCircle(sb, center + new Vector2(0, s * 0.42f), s * 0.62f, new Color(0, 0, 0, 0.28f));
        DrawPrimitives.FillCircle(sb, center, s * 0.55f, new Color(180, 40, 40));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(center.X - s * 0.15f), (int)(center.Y - s * 0.55f), (int)(s * 0.3f), (int)(s * 0.35f)), new Color(230, 220, 210));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(center.X - s * 0.55f), (int)(center.Y + s * 0.05f), (int)(s * 0.35f), (int)(s * 0.12f)), new Color(230, 220, 210));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(center.X + s * 0.2f), (int)(center.Y + s * 0.05f), (int)(s * 0.35f), (int)(s * 0.12f)), new Color(230, 220, 210));
    }
}
