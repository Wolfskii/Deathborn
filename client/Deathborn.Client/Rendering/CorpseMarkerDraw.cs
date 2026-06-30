using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public static class CorpseMarkerDraw
{
    /// <summary>Tombstone with a cross — drawn above a corpse.</summary>
    public static void DrawCorpseMarker(SpriteBatch sb, Vector2 center, float zoom)
    {
        DrawTombstone(sb, center + new Vector2(0, 10f * zoom), zoom);
        DrawCross(sb, center + new Vector2(0, -16f * zoom), zoom * 1.15f);
    }

    public static void DrawCross(SpriteBatch sb, Vector2 center, float zoom)
    {
        var arm = 11f * zoom;
        var thick = 3.2f * zoom;
        var shadow = new Color(0, 0, 0, 0.55f);
        var wood = new Color(0.55f, 0.38f, 0.22f);
        var fill = new Color(0.96f, 0.92f, 0.86f);

        DrawPrimitives.FillCircle(sb, center + new Vector2(0, 2f * zoom), 6f * zoom, shadow);

        DrawCrossArms(sb, center + new Vector2(1.5f, 1.5f) * zoom, arm, thick + 1.2f, shadow);
        DrawCrossArms(sb, center, arm, thick, wood);
        DrawCrossArms(sb, center, arm * 0.82f, thick * 0.55f, fill);
    }

    private static void DrawTombstone(SpriteBatch sb, Vector2 baseCenter, float zoom)
    {
        var w = 22f * zoom;
        var h = 28f * zoom;
        var x = baseCenter.X - w / 2f;
        var y = baseCenter.Y - h;
        var stone = new Color(0.58f, 0.6f, 0.66f);
        var edge = new Color(0.38f, 0.4f, 0.46f);
        var highlight = new Color(0.78f, 0.8f, 0.86f);

        var body = new Rectangle((int)x, (int)(y + 6f * zoom), (int)w, (int)(h - 6f * zoom));
        DrawPrimitives.FillRect(sb, body, stone);
        DrawPrimitives.FillRect(sb, new Rectangle(body.X, body.Y, body.Width, (int)(2f * zoom)), highlight);
        DrawPrimitives.FillRect(sb, new Rectangle(body.X, body.Y, (int)(2f * zoom), body.Height), edge);
        DrawPrimitives.FillRect(sb, new Rectangle(body.Right - (int)(2f * zoom), body.Y, (int)(2f * zoom), body.Height), edge);

        var archCenter = new Vector2(baseCenter.X, y + 6f * zoom);
        DrawPrimitives.FillCircle(sb, archCenter, w * 0.48f, stone);
        DrawPrimitives.DrawCircleOutline(sb, archCenter, w * 0.48f, edge, 16, 1.5f);

        var shadow = new Rectangle((int)(x - 2f * zoom), body.Bottom - (int)(2f * zoom), (int)(w + 4f * zoom), (int)(4f * zoom));
        DrawPrimitives.FillRect(sb, shadow, new Color(0, 0, 0, 0.25f));
    }

    private static void DrawCrossArms(SpriteBatch sb, Vector2 center, float arm, float thick, Color color)
    {
        DrawPrimitives.DrawLine(sb, center + new Vector2(-arm, 0), center + new Vector2(arm, 0), color, thick);
        DrawPrimitives.DrawLine(sb, center + new Vector2(0, -arm * 0.85f), center + new Vector2(0, arm * 0.2f), color, thick);
    }
}
