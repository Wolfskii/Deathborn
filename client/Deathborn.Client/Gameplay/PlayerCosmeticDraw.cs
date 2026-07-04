using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Draws equipped head cosmetics above the player sprite.</summary>
public static class PlayerCosmeticDraw
{
    public static void DrawHead(SpriteBatch sb, string? cosmeticId, Vector2 screenPos, Vector2 facing, float zoom)
    {
        if (string.IsNullOrEmpty(cosmeticId)) return;

        var head = screenPos + new Vector2(0, (-PlayerEntity.Radius - 10f) * zoom);
        var size = (int)(22f * zoom);
        var icon = new Rectangle((int)(head.X - size / 2f), (int)(head.Y - size), size, size);

        if (CosmeticIconAtlas.TryDraw(sb, cosmeticId, icon))
            return;

        DrawProcedural(sb, cosmeticId, head, facing, zoom);
    }

    private static void DrawProcedural(SpriteBatch sb, string id, Vector2 head, Vector2 facing, float z)
    {
        var flip = facing.X < -0.1f ? -1 : 1;
        switch (id)
        {
            case "santa_hat": DrawSantaHat(sb, head, z); break;
            case "party_hat": DrawPartyHat(sb, head, z); break;
            case "bucket_helmet": DrawBucket(sb, head, z); break;
            case "bunny_ears": DrawBunnyEars(sb, head, z); break;
            case "traffic_cone": DrawCone(sb, head, z); break;
            case "propeller_hat": DrawPropeller(sb, head, z, flip); break;
            case "pirate_hat": DrawPirateHat(sb, head, z, flip); break;
            case "top_hat": DrawTopHat(sb, head, z); break;
            case "jester_cap": DrawJester(sb, head, z); break;
            case "beer_helm": DrawBeerHelm(sb, head, z); break;
            case "crown_of_bones": DrawBoneCrown(sb, head, z); break;
            case "rubber_chicken_hat": DrawChicken(sb, head, z, flip); break;
            case "grim_hood": DrawHood(sb, head, z); break;
            case "clown_nose_glasses": DrawClownFace(sb, head, z); break;
            default: DrawGenericHat(sb, head, z, new Color(120, 90, 70)); break;
        }
    }

    private static void DrawSantaHat(SpriteBatch sb, Vector2 head, float z)
    {
        var brim = new Rectangle((int)(head.X - 11 * z), (int)(head.Y - 4 * z), (int)(22 * z), (int)(4 * z));
        DrawPrimitives.FillRect(sb, brim, new Color(220, 40, 40));
        DrawTriangle(sb, head + new Vector2(0, -4 * z), 10 * z, 14 * z, new Color(210, 35, 35));
        DrawPrimitives.FillCircle(sb, head + new Vector2(8 * z, -16 * z), 3 * z, Color.White);
    }

    private static void DrawPartyHat(SpriteBatch sb, Vector2 head, float z)
    {
        DrawTriangle(sb, head, 9 * z, 18 * z, new Color(240, 200, 40));
        DrawPrimitives.FillCircle(sb, head + new Vector2(6 * z, -16 * z), 2.5f * z, new Color(240, 80, 120));
    }

    private static void DrawBucket(SpriteBatch sb, Vector2 head, float z)
    {
        var r = new Rectangle((int)(head.X - 12 * z), (int)(head.Y - 14 * z), (int)(24 * z), (int)(14 * z));
        DrawPrimitives.FillRect(sb, r, new Color(160, 165, 175));
        DrawPrimitives.FillRect(sb, new Rectangle(r.X, r.Y, r.Width, (int)(3 * z)), new Color(130, 135, 145));
    }

    private static void DrawBunnyEars(SpriteBatch sb, Vector2 head, float z)
    {
        DrawEar(sb, head + new Vector2(-7 * z, -8 * z), z);
        DrawEar(sb, head + new Vector2(7 * z, -8 * z), z);
    }

    private static void DrawEar(SpriteBatch sb, Vector2 basePos, float z) =>
        DrawPrimitives.FillRect(sb, CenteredRect(basePos + new Vector2(0, -6 * z), 5 * z, 12 * z), new Color(240, 210, 220));

    private static void DrawCone(SpriteBatch sb, Vector2 head, float z)
    {
        DrawTriangle(sb, head + new Vector2(0, 2 * z), 11 * z, 16 * z, new Color(240, 120, 30));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(head.X - 11 * z), (int)(head.Y + 1 * z), (int)(22 * z), (int)(3 * z)),
            new Color(220, 100, 20));
    }

    private static void DrawPropeller(SpriteBatch sb, Vector2 head, float z, int flip)
    {
        DrawGenericHat(sb, head, z, new Color(50, 90, 180));
        var c = head + new Vector2(0, -14 * z);
        DrawPrimitives.DrawLine(sb, c + new Vector2(-8 * z * flip, 0), c + new Vector2(8 * z * flip, 0), new Color(180, 50, 50), 2f * z);
        DrawPrimitives.DrawLine(sb, c + new Vector2(0, -6 * z), c + new Vector2(0, 6 * z), new Color(180, 50, 50), 2f * z);
    }

    private static void DrawPirateHat(SpriteBatch sb, Vector2 head, float z, int flip)
    {
        DrawPrimitives.FillRect(sb, new Rectangle((int)(head.X - 14 * z), (int)(head.Y - 2 * z), (int)(28 * z), (int)(4 * z)), new Color(30, 30, 36));
        DrawTriangle(sb, head + new Vector2(-4 * z * flip, -2 * z), 8 * z, 10 * z, new Color(35, 35, 42));
    }

    private static void DrawTopHat(SpriteBatch sb, Vector2 head, float z)
    {
        DrawPrimitives.FillRect(sb, new Rectangle((int)(head.X - 12 * z), (int)(head.Y - 2 * z), (int)(24 * z), (int)(3 * z)), new Color(30, 28, 34));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(head.X - 8 * z), (int)(head.Y - 16 * z), (int)(16 * z), (int)(14 * z)), new Color(38, 36, 44));
    }

    private static void DrawJester(SpriteBatch sb, Vector2 head, float z)
    {
        DrawTriangle(sb, head + new Vector2(-6 * z, 0), 6 * z, 12 * z, new Color(120, 50, 160));
        DrawTriangle(sb, head + new Vector2(6 * z, 0), 6 * z, 12 * z, new Color(200, 180, 40));
        DrawTriangle(sb, head, 7 * z, 14 * z, new Color(180, 40, 60));
    }

    private static void DrawBeerHelm(SpriteBatch sb, Vector2 head, float z)
    {
        DrawPrimitives.FillRect(sb, new Rectangle((int)(head.X - 14 * z), (int)(head.Y - 10 * z), (int)(10 * z), (int)(12 * z)), new Color(180, 140, 60));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(head.X + 4 * z), (int)(head.Y - 10 * z), (int)(10 * z), (int)(12 * z)), new Color(180, 140, 60));
    }

    private static void DrawBoneCrown(SpriteBatch sb, Vector2 head, float z)
    {
        DrawPrimitives.DrawCircleOutline(sb, head + new Vector2(0, -8 * z), 9 * z, new Color(220, 210, 190), 12, 2f);
        for (var i = 0; i < 5; i++)
        {
            var a = i / 5f * MathHelper.TwoPi - MathHelper.PiOver2;
            var p = head + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 8 * z + new Vector2(0, -8 * z);
            DrawPrimitives.FillRect(sb, CenteredRect(p, 3 * z, 5 * z), new Color(230, 225, 210));
        }
    }

    private static void DrawChicken(SpriteBatch sb, Vector2 head, float z, int flip)
    {
        DrawPrimitives.FillCircle(sb, head + new Vector2(0, -6 * z), 7 * z, new Color(240, 220, 50));
        DrawPrimitives.FillCircle(sb, head + new Vector2(5 * z * flip, -8 * z), 2.5f * z, new Color(220, 60, 50));
    }

    private static void DrawHood(SpriteBatch sb, Vector2 head, float z) =>
        DrawPrimitives.FillCircle(sb, head + new Vector2(0, -6 * z), 12 * z, new Color(40, 38, 48, 220));

    private static void DrawClownFace(SpriteBatch sb, Vector2 head, float z)
    {
        DrawPrimitives.DrawLine(sb, head + new Vector2(-8 * z, -4 * z), head + new Vector2(8 * z, -4 * z), new Color(40, 40, 48), 2f * z);
        DrawPrimitives.FillCircle(sb, head + new Vector2(0, -2 * z), 3 * z, new Color(220, 50, 60));
    }

    private static void DrawGenericHat(SpriteBatch sb, Vector2 head, float z, Color color)
    {
        DrawPrimitives.FillRect(sb, new Rectangle((int)(head.X - 10 * z), (int)(head.Y - 2 * z), (int)(20 * z), (int)(3 * z)), color * 0.8f);
        DrawTriangle(sb, head, 8 * z, 12 * z, color);
    }

    private static void DrawTriangle(SpriteBatch sb, Vector2 baseCenter, float halfW, float h, Color color)
    {
        var a = baseCenter + new Vector2(-halfW, 0);
        var b = baseCenter + new Vector2(halfW, 0);
        var c = baseCenter + new Vector2(0, -h);
        DrawPrimitives.FillTriangle(sb, a, b, c, color);
    }

    private static Rectangle CenteredRect(Vector2 center, float w, float h) =>
        new((int)(center.X - w / 2), (int)(center.Y - h / 2), (int)w, (int)h);
}
