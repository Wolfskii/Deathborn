using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Circular minimap in the top-right showing nearby players.</summary>
public sealed class MinimapHud
{
    public Vector2 Center =>
        new(
            GameViewport.Width - Config.MinimapMargin - Config.MinimapScreenRadius,
            Config.MinimapMargin + Config.MinimapScreenRadius);

    public void Draw(
        SpriteBatch sb,
        Vector2 cameraWorld,
        long localId,
        IEnumerable<PlayerEntity> players)
    {
        var center = Center;
        var r = Config.MinimapScreenRadius;
        var worldR = Config.MinimapWorldRadius;

        DrawPrimitives.FillCircle(sb, center, r + 3f, new Color(0.08f, 0.08f, 0.1f, 0.92f));
        DrawPrimitives.FillCircle(sb, center, r, new Color(0.12f, 0.14f, 0.18f, 0.88f));
        DrawPrimitives.DrawCircleOutline(sb, center, r, new Color(0.75f, 0.62f, 0.38f, 0.9f), 48, 2.5f);

        // Subtle crosshair at local player (map center)
        DrawPrimitives.DrawLine(sb, center + new Vector2(-6, 0), center + new Vector2(6, 0), new Color(1f, 1f, 1f, 0.15f), 1f);
        DrawPrimitives.DrawLine(sb, center + new Vector2(0, -6), center + new Vector2(0, 6), new Color(1f, 1f, 1f, 0.15f), 1f);

        foreach (var player in players)
        {
            var offset = player.Position - cameraWorld;
            if (offset.LengthSquared() > worldR * worldR) continue;

            var mapPos = center + offset / worldR * r;
            var dist = Vector2.Distance(mapPos, center);
            if (dist > r - 4f)
                mapPos = center + Vector2.Normalize(mapPos - center) * (r - 4f);

            var isLocal = player.Id == localId;
            var dotR = isLocal ? 4f : 3f;
            var color = isLocal
                ? new Color(0.95f, 0.92f, 0.55f)
                : new Color(0.88f, 0.55f, 0.45f);

            DrawPrimitives.FillCircle(sb, mapPos, dotR + 1.5f, new Color(0, 0, 0, 0.45f));
            DrawPrimitives.FillCircle(sb, mapPos, dotR, color);
        }
    }
}
