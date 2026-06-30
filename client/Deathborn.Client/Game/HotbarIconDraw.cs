using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Procedural hotbar spell icons drawn without external assets.</summary>
public static class HotbarIconDraw
{
    public static void Draw(SpriteBatch sb, string? spellId, Rectangle bounds)
    {
        if (string.IsNullOrEmpty(spellId))
        {
            DrawPrimitives.FillRect(sb, bounds, new Color(30, 30, 36));
            return;
        }

        switch (spellId)
        {
            case "fireball":
                DrawFireball(sb, bounds);
                break;
            case "ice_shard":
                DrawIceShard(sb, bounds);
                break;
            case "arc_bolt":
                DrawArcBolt(sb, bounds);
                break;
            case "blood_bolt":
                DrawBloodBolt(sb, bounds);
                break;
            case "bandage":
                DrawBandage(sb, bounds);
                break;
            case "poison_cloud":
                DrawPoisonCloud(sb, bounds);
                break;
            case "shield_bash":
                DrawShieldBash(sb, bounds);
                break;
            case "whirlwind":
                DrawWhirlwind(sb, bounds);
                break;
            case "warrior_dash":
                DrawWarriorDash(sb, bounds);
                break;
            case "battle_shout":
                DrawBattleShout(sb, bounds);
                break;
            case "iron_skin":
                DrawIronSkin(sb, bounds);
                break;
            case "hunter_mark":
                DrawHunterMark(sb, bounds);
                break;
            case "second_wind":
                DrawSecondWind(sb, bounds);
                break;
            case "slash":
                DrawSlash(sb, bounds);
                break;
            case "health_potion":
                DrawHealthPotion(sb, bounds);
                break;
            case "mana_potion":
                DrawManaPotion(sb, bounds);
                break;
            case "stamina_potion":
                DrawStaminaPotion(sb, bounds);
                break;
            case "antidote":
                DrawAntidote(sb, bounds);
                break;
            default:
                DrawPrimitives.FillRect(sb, bounds, new Color(40, 42, 50));
                break;
        }
    }

    private static void DrawFireball(SpriteBatch sb, Rectangle bounds)
    {
        var c = new Vector2(bounds.Center.X, bounds.Center.Y);
        var r = bounds.Width * 0.28f;
        DrawPrimitives.FillRect(sb, bounds, new Color(28, 14, 8));
        DrawPrimitives.FillCircle(sb, c, r * 1.5f, new Color(1f, 0.4f, 0.08f, 0.45f));
        DrawPrimitives.FillCircle(sb, c, r, new Color(1f, 0.75f, 0.2f));
        DrawPrimitives.FillCircle(sb, c, r * 0.4f, new Color(1f, 1f, 0.85f));
    }

    private static void DrawIceShard(SpriteBatch sb, Rectangle bounds)
    {
        var c = new Vector2(bounds.Center.X, bounds.Center.Y);
        DrawPrimitives.FillRect(sb, bounds, new Color(10, 22, 38));
        DrawIceDiamond(sb, c, bounds.Width * 0.32f, 0f);
        DrawIceDiamond(sb, c + new Vector2(-8, 4), bounds.Width * 0.18f, 0.5f);
        DrawIceDiamond(sb, c + new Vector2(9, -3), bounds.Width * 0.15f, -0.3f);
    }

    private static void DrawIceDiamond(SpriteBatch sb, Vector2 center, float size, float rot)
    {
        var pts = new Vector2[4];
        for (var i = 0; i < 4; i++)
        {
            var a = rot + i / 4f * MathHelper.TwoPi + MathHelper.PiOver4;
            pts[i] = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * size;
        }
        for (var i = 0; i < 4; i++)
            DrawPrimitives.DrawLine(sb, pts[i], pts[(i + 1) % 4], new Color(0.7f, 0.92f, 1f), 2f);
    }

    private static void DrawArcBolt(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(12, 16, 32));
        DrawLightningBolt(sb, bounds, new Color(0.5f, 0.75f, 1f, 0.5f), new Color(0.9f, 0.95f, 1f));
    }

    private static void DrawBloodBolt(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(28, 8, 10));
        DrawLightningBolt(sb, bounds, new Color(0.85f, 0.15f, 0.2f, 0.55f), new Color(1f, 0.45f, 0.5f));
    }

    private static void DrawLightningBolt(SpriteBatch sb, Rectangle bounds, Color glow, Color core)
    {
        var a = new Vector2(bounds.X + 8, bounds.Bottom - 10);
        var b = new Vector2(bounds.Right - 8, bounds.Y + 10);
        var mid = (a + b) * 0.5f + new Vector2(6, 0);
        DrawPrimitives.DrawLine(sb, a, mid, glow, 4f);
        DrawPrimitives.DrawLine(sb, mid, b, glow, 4f);
        DrawPrimitives.DrawLine(sb, a, mid, core, 2f);
        DrawPrimitives.DrawLine(sb, mid, b, core, 2f);
    }

    private static void DrawBandage(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(32, 28, 18));
        var pad = 6;
        var roll = new Rectangle(bounds.X + pad, bounds.Y + pad + 4, bounds.Width - pad * 2, bounds.Height - pad * 2 - 6);
        DrawPrimitives.FillRect(sb, roll, new Color(0.92f, 0.88f, 0.72f));
        DrawPrimitives.FillRect(sb, new Rectangle(roll.X, roll.Y, roll.Width, 3), new Color(0.75f, 0.65f, 0.45f));
        var cross = new Vector2(roll.Center.X, roll.Center.Y);
        DrawPrimitives.DrawLine(sb, cross + new Vector2(-10, 0), cross + new Vector2(10, 0), new Color(0.85f, 0.25f, 0.2f), 3f);
        DrawPrimitives.DrawLine(sb, cross + new Vector2(0, -8), cross + new Vector2(0, 8), new Color(0.85f, 0.25f, 0.2f), 3f);
    }

    private static void DrawPoisonCloud(SpriteBatch sb, Rectangle bounds)
    {
        var c = new Vector2(bounds.Center.X, bounds.Center.Y + 2);
        DrawPrimitives.FillRect(sb, bounds, new Color(14, 24, 10));
        DrawPrimitives.FillCircle(sb, c, bounds.Width * 0.3f, new Color(0.3f, 0.7f, 0.15f, 0.55f));
        DrawPrimitives.FillCircle(sb, c + new Vector2(-10, -4), bounds.Width * 0.18f, new Color(0.4f, 0.8f, 0.2f, 0.5f));
        DrawPrimitives.FillCircle(sb, c + new Vector2(11, -2), bounds.Width * 0.16f, new Color(0.35f, 0.75f, 0.18f, 0.5f));
        DrawPrimitives.DrawCircleOutline(sb, c, bounds.Width * 0.32f, new Color(0.55f, 0.95f, 0.3f, 0.7f), 20, 1.5f);
    }

    private static void DrawShieldBash(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(22, 24, 32));
        var c = new Vector2(bounds.Center.X - 6, bounds.Center.Y);
        DrawPrimitives.FillRect(sb, new Rectangle((int)c.X - 10, (int)c.Y - 14, 14, 28), new Color(0.55f, 0.6f, 0.7f));
        DrawPrimitives.FillRect(sb, new Rectangle((int)c.X + 4, (int)c.Y - 10, 16, 20), new Color(0.75f, 0.78f, 0.85f));
        DrawPrimitives.DrawLine(sb, c + new Vector2(18, -8), c + new Vector2(28, 0), new Color(0.9f, 0.92f, 1f), 3f);
    }

    private static void DrawWhirlwind(SpriteBatch sb, Rectangle bounds)
    {
        var c = new Vector2(bounds.Center.X, bounds.Center.Y);
        DrawPrimitives.FillRect(sb, bounds, new Color(18, 20, 30));
        for (var i = 0; i < 3; i++)
        {
            var a = i / 3f * MathHelper.TwoPi;
            var tip = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * bounds.Width * 0.28f;
            DrawPrimitives.DrawLine(sb, c, tip, new Color(0.75f, 0.8f, 0.95f), 2f);
        }
    }

    private static void DrawWarriorDash(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(16, 18, 28));
        var a = new Vector2(bounds.X + 10, bounds.Center.Y);
        var b = new Vector2(bounds.Right - 8, bounds.Center.Y);
        DrawPrimitives.DrawLine(sb, a, b, new Color(0.5f, 0.6f, 0.95f, 0.5f), 6f);
        DrawPrimitives.DrawLine(sb, a, b, new Color(0.85f, 0.9f, 1f), 2f);
        DrawPrimitives.FillCircle(sb, b, 5f, new Color(0.9f, 0.95f, 1f));
    }

    private static void DrawBattleShout(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(32, 18, 10));
        var c = new Vector2(bounds.Center.X, bounds.Center.Y + 2);
        for (var i = 0; i < 3; i++)
            DrawPrimitives.DrawCircleOutline(sb, c, 8f + i * 5f, new Color(0.95f, 0.55f, 0.2f, 0.7f - i * 0.15f), 12, 1.5f);
    }

    private static void DrawIronSkin(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(20, 24, 30));
        var plate = new Rectangle(bounds.X + 12, bounds.Y + 10, bounds.Width - 24, bounds.Height - 18);
        DrawPrimitives.FillRect(sb, plate, new Color(0.55f, 0.62f, 0.72f));
        DrawPrimitives.FillRect(sb, new Rectangle(plate.X, plate.Y, plate.Width, 3), new Color(0.75f, 0.8f, 0.9f));
    }

    private static void DrawHunterMark(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(24, 12, 14));
        var c = new Vector2(bounds.Center.X, bounds.Center.Y);
        DrawPrimitives.DrawCircleOutline(sb, c, bounds.Width * 0.22f, new Color(0.95f, 0.35f, 0.4f), 14, 2f);
        DrawPrimitives.FillCircle(sb, c, 4f, new Color(0.95f, 0.25f, 0.3f));
    }

    private static void DrawSecondWind(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(12, 28, 22));
        var c = new Vector2(bounds.Center.X, bounds.Center.Y);
        DrawPrimitives.FillCircle(sb, c, bounds.Width * 0.22f, new Color(0.35f, 0.9f, 0.55f, 0.45f));
        DrawPrimitives.DrawLine(sb, c + new Vector2(-8, 0), c + new Vector2(8, 0), new Color(0.5f, 0.95f, 0.65f), 3f);
        DrawPrimitives.DrawLine(sb, c + new Vector2(0, -8), c + new Vector2(0, 8), new Color(0.5f, 0.95f, 0.65f), 3f);
    }

    private static void DrawSlash(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(24, 20, 18));
        var c = new Vector2(bounds.Center.X, bounds.Center.Y);
        DrawPrimitives.DrawLine(sb, c + new Vector2(-12, 8), c + new Vector2(14, -10), new Color(0.85f, 0.88f, 0.95f), 3f);
        DrawPrimitives.DrawLine(sb, c + new Vector2(-10, 10), c + new Vector2(12, -8), new Color(0.55f, 0.58f, 0.65f), 5f);
    }

    private static void DrawHealthPotion(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(18, 12, 14));
        var cx = bounds.Center.X;
        var flask = new Rectangle(cx - 10, bounds.Y + 14, 20, 28);
        DrawPrimitives.FillRect(sb, flask, new Color(0.75f, 0.2f, 0.22f, 0.5f));
        DrawPrimitives.FillRect(sb, new Rectangle(flask.X + 4, flask.Y + 6, flask.Width - 8, flask.Height - 10),
            new Color(0.95f, 0.25f, 0.28f, 0.85f));
        DrawPrimitives.FillRect(sb, new Rectangle(cx - 5, flask.Y - 4, 10, 6), new Color(0.55f, 0.5f, 0.48f));
    }

    private static void DrawManaPotion(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(10, 14, 28));
        var cx = bounds.Center.X;
        var flask = new Rectangle(cx - 10, bounds.Y + 14, 20, 28);
        DrawPrimitives.FillRect(sb, flask, new Color(0.2f, 0.35f, 0.85f, 0.5f));
        DrawPrimitives.FillRect(sb, new Rectangle(flask.X + 4, flask.Y + 6, flask.Width - 8, flask.Height - 10),
            new Color(0.35f, 0.55f, 0.98f, 0.85f));
        DrawPrimitives.FillRect(sb, new Rectangle(cx - 5, flask.Y - 4, 10, 6), new Color(0.55f, 0.5f, 0.48f));
    }

    private static void DrawStaminaPotion(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(20, 18, 8));
        var cx = bounds.Center.X;
        var flask = new Rectangle(cx - 10, bounds.Y + 14, 20, 28);
        DrawPrimitives.FillRect(sb, flask, new Color(0.75f, 0.65f, 0.15f, 0.5f));
        DrawPrimitives.FillRect(sb, new Rectangle(flask.X + 4, flask.Y + 6, flask.Width - 8, flask.Height - 10),
            new Color(0.95f, 0.82f, 0.2f, 0.85f));
        DrawPrimitives.FillRect(sb, new Rectangle(cx - 5, flask.Y - 4, 10, 6), new Color(0.55f, 0.5f, 0.48f));
    }

    private static void DrawAntidote(SpriteBatch sb, Rectangle bounds)
    {
        DrawPrimitives.FillRect(sb, bounds, new Color(12, 22, 14));
        var c = new Vector2(bounds.Center.X, bounds.Center.Y + 2);
        DrawPrimitives.FillCircle(sb, c, bounds.Width * 0.2f, new Color(0.45f, 0.9f, 0.45f, 0.75f));
        DrawPrimitives.DrawLine(sb, c + new Vector2(-6, 0), c + new Vector2(6, 0), new Color(0.3f, 0.65f, 0.35f), 2f);
    }
}
