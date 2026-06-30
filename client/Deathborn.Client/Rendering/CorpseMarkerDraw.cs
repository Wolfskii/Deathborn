using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public static class CorpseMarkerDraw
{
    public static void DrawCross(SpriteBatch sb, Vector2 center, float zoom)
    {
        var arm = 9f * zoom;
        var thick = 2.8f * zoom;
        var shadow = new Color(0, 0, 0, 0.45f);
        var wood = new Color(0.55f, 0.38f, 0.22f);
        var fill = new Color(0.92f, 0.88f, 0.82f);

        DrawPrimitives.FillCircle(sb, center + new Vector2(0, 2f * zoom), 5f * zoom, shadow);

        DrawCrossArms(sb, center + new Vector2(1f, 1f) * zoom, arm, thick + 1f, shadow);
        DrawCrossArms(sb, center, arm, thick, wood);
        DrawCrossArms(sb, center, arm * 0.82f, thick * 0.55f, fill);
    }

    private static void DrawCrossArms(SpriteBatch sb, Vector2 center, float arm, float thick, Color color)
    {
        DrawPrimitives.DrawLine(sb, center + new Vector2(-arm, 0), center + new Vector2(arm, 0), color, thick);
        DrawPrimitives.DrawLine(sb, center + new Vector2(0, -arm * 0.85f), center + new Vector2(0, arm * 0.2f), color, thick);
    }
}
