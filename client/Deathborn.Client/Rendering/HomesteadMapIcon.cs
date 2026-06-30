using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Small house glyph for world map and minimap markers.</summary>
public static class HomesteadMapIcon
{
    private static readonly Color Body = new(186, 168, 142);
    private static readonly Color BodyDark = new(142, 124, 98);
    private static readonly Color Roof = new(168, 62, 48);
    private static readonly Color RoofLight = new(198, 88, 68);
    private static readonly Color Door = new(92, 58, 38);

    public static void Draw(SpriteBatch sb, Vector2 center, float scale)
    {
        var w = 10f * scale;
        var h = 9f * scale;

        DrawPrimitives.FillCircle(sb, center + new Vector2(0, h * 0.38f), w * 0.38f, new Color(0, 0, 0, 0.32f));

        var body = new Rectangle(
            (int)(center.X - w * 0.5f),
            (int)(center.Y - h * 0.15f),
            (int)w,
            (int)(h * 0.65f));
        DrawPrimitives.FillRect(sb, body, Body);
        DrawPrimitives.FillRect(sb, new Rectangle(body.X, body.Y, body.Width, Math.Max(1, (int)scale)), BodyDark);

        var roofH = h * 0.45f;
        var roofBase = new Vector2(body.Center.X, body.Y);
        DrawPrimitives.FillTriangle(sb,
            roofBase + new Vector2(-w * 0.58f, 0),
            roofBase + new Vector2(w * 0.58f, 0),
            roofBase + new Vector2(0, -roofH),
            Roof);
        DrawPrimitives.DrawLine(sb,
            roofBase + new Vector2(-w * 0.58f, 0),
            roofBase + new Vector2(0, -roofH),
            RoofLight,
            Math.Max(1f, scale));

        var doorW = w * 0.28f;
        var doorH = body.Height * 0.55f;
        DrawPrimitives.FillRect(sb, new Rectangle(
            (int)(body.Center.X - doorW * 0.5f),
            (int)(body.Bottom - doorH),
            (int)doorW,
            (int)doorH), Door);
    }
}
