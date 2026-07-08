using Deathborn.Client.Net;
using Deathborn.Client.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Gameplay;

public sealed class WorldNpcEntity
{
    public const float DefaultRadius = 14f;

    public long Id;
    public string DefId = "";
    public string Name = "";
    public string SpriteId = "";
    public NpcCategory Category = NpcCategory.Monster;
    public NpcDisposition Disposition = NpcDisposition.Hostile;
    public Vector2 Position;
    public Vector2 Target;
    public float Hp;
    public float HpMax;
    public string Action = "";
    public Vector2 Facing = new(0, 1);
    public float AbilityFlash;
    public bool IsBoss;

  public float Radius => NpcCatalog.Get(DefId).Radius;
    public float DisplayScale => NpcCatalog.Get(DefId).DisplayScale;
    public bool IsAttackable => NpcCategoryRules.IsAttackable(Disposition);
    public bool UsesSprite => !IsBoss && !string.IsNullOrEmpty(SpriteId) && TinyRpgCharacterSprites.Get(SpriteId) != null;

    private TinyRpgStripAnimation? _anim;
    private bool _moving;

    public void SetTarget(Vector2 pos) => Target = pos;

    public void Sync(NpcState s)
    {
        DefId = s.DefId;
        Name = s.Name;
        SpriteId = s.SpriteId ?? "";
        Category = NpcCatalog.ParseCategory(s.Category, s.IsBoss);
        Disposition = NpcCatalog.ParseDisposition(s.Disposition);
        IsBoss = s.IsBoss || Category == NpcCategory.Boss;
        Target = new Vector2((float)s.X, (float)s.Y);
        Hp = (float)s.Hp;
        HpMax = (float)s.HpMax;
        Action = s.Action ?? "";
        if (MathF.Abs((float)s.DirX) > 0.01f || MathF.Abs((float)s.DirY) > 0.01f)
            Facing = Vector2.Normalize(new Vector2((float)s.DirX, (float)s.DirY));
        _anim ??= TinyRpgCharacterSprites.Get(SpriteId);
    }

    public void Update(float dt)
    {
        var before = Position;
        var lerped = Vector2.Lerp(Position, Target, MathHelper.Clamp(dt * Config.PlayerLerpSpeed, 0f, 1f));
        Position = WorldFoliage.ResolvePosition(lerped, Radius);
        _moving = Vector2.DistanceSquared(before, Position) > 0.05f;
        _anim?.Update(dt, Facing, _moving, Config.WalkAnimSpeed);
        if (AbilityFlash > 0) AbilityFlash -= dt;
        if (!string.IsNullOrEmpty(Action))
            AbilityFlash = MathF.Max(AbilityFlash, 0.35f);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 screenPos, float zoom)
    {
        if (UsesSprite && _anim != null)
        {
            var scale = DisplayScale * zoom;
            _anim.Draw(sb, screenPos, Color.White, scale, Facing);
        }
        else
        {
            DrawBossProcedural(sb, screenPos, zoom);
        }

        DrawNameplate(sb, font, screenPos, zoom);
    }

    private void DrawBossProcedural(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var r = Radius * zoom;
        var body = BodyColor();
        var core = CoreColor();

        switch (DefId)
        {
            case "iron_colossus":
                DrawColossus(sb, screenPos, r, body, core);
                break;
            case "storm_wyrm":
                DrawWyrm(sb, screenPos, r, body, core);
                break;
            case "blight_herald":
                DrawHerald(sb, screenPos, r, body, core);
                break;
            default:
                DrawPrimitives.FillCircle(sb, screenPos, r, body);
                break;
        }

        if (AbilityFlash > 0)
        {
            var pulse = AbilityFlash / 0.35f;
            DrawPrimitives.FillCircle(sb, screenPos, r * (1.2f + pulse * 0.3f),
                AbilityColor() * (0.35f * pulse));
        }

        if (IsBoss)
            DrawBossIcon(sb, new Vector2(screenPos.X, screenPos.Y - r - 18f * zoom), zoom * 0.9f);
    }

    private void DrawNameplate(SpriteBatch sb, SpriteFont font, Vector2 screenPos, float zoom)
    {
        var r = Radius * zoom;
        var label = SpriteFontSafe.Filter(Name);
        var size = SpriteFontSafe.MeasureString(font, label) * 0.75f;
        var nameY = screenPos.Y - r - (UsesSprite ? 52f * zoom : 18f * zoom);
        var nameColor = Disposition switch
        {
            NpcDisposition.Friendly => new Color(140, 210, 255),
            NpcDisposition.Neutral => new Color(220, 210, 140),
            _ => new Color(255, 160, 140),
        };
        if (IsBoss) nameColor = new Color(255, 210, 120);
        SpriteFontSafe.DrawString(sb, font, label,
            new Vector2(screenPos.X - size.X / 2f, nameY), nameColor,
            0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
    }

    public static void DrawBossIcon(SpriteBatch sb, Vector2 center, float scale)
    {
        var s = 8f * scale;
        DrawPrimitives.FillCircle(sb, center, s * 0.55f, new Color(180, 40, 40));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(center.X - s * 0.15f), (int)(center.Y - s * 0.55f), (int)(s * 0.3f), (int)(s * 0.35f)), new Color(230, 220, 210));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(center.X - s * 0.55f), (int)(center.Y + s * 0.05f), (int)(s * 0.35f), (int)(s * 0.12f)), new Color(230, 220, 210));
        DrawPrimitives.FillRect(sb, new Rectangle((int)(center.X + s * 0.2f), (int)(center.Y + s * 0.05f), (int)(s * 0.35f), (int)(s * 0.12f)), new Color(230, 220, 210));
    }

    private void DrawColossus(SpriteBatch sb, Vector2 c, float r, Color body, Color core)
    {
        DrawPrimitives.FillRect(sb, Centered(c, r * 1.6f, r * 1.9f), body * 0.85f);
        DrawPrimitives.FillRect(sb, Centered(c + new Vector2(0, -r * 0.15f), r * 1.2f, r * 1.3f), body);
        DrawPrimitives.FillCircle(sb, c + new Vector2(0, -r * 0.55f), r * 0.45f, core);
        DrawPrimitives.FillRect(sb, Centered(c + new Vector2(-r * 0.9f, r * 0.2f), r * 0.35f, r * 0.9f), body * 0.9f);
        DrawPrimitives.FillRect(sb, Centered(c + new Vector2(r * 0.9f, r * 0.2f), r * 0.35f, r * 0.9f), body * 0.9f);
    }

    private void DrawWyrm(SpriteBatch sb, Vector2 c, float r, Color body, Color core)
    {
        var dir = Facing.LengthSquared() > 0.01f ? Vector2.Normalize(Facing) : new Vector2(0, 1);
        var tail = c - dir * r * 1.2f;
        var head = c + dir * r * 0.8f;
        DrawPrimitives.FillCircle(sb, tail, r * 0.55f, body * 0.8f);
        DrawPrimitives.FillCircle(sb, c, r * 0.75f, body);
        DrawPrimitives.FillCircle(sb, head, r * 0.65f, core);
        DrawPrimitives.FillCircle(sb, head + new Vector2(-dir.Y, dir.X) * r * 0.35f, r * 0.18f, new Color(120, 200, 255));
        DrawPrimitives.FillCircle(sb, head + new Vector2(dir.Y, -dir.X) * r * 0.35f, r * 0.18f, new Color(120, 200, 255));
    }

    private void DrawHerald(SpriteBatch sb, Vector2 c, float r, Color body, Color core)
    {
        DrawPrimitives.FillCircle(sb, c, r * 0.95f, body);
        DrawPrimitives.FillCircle(sb, c + new Vector2(0, -r * 0.35f), r * 0.55f, core);
        for (var i = 0; i < 6; i++)
        {
            var a = i / 6f * MathHelper.TwoPi;
            var p = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r * 1.1f;
            DrawPrimitives.FillCircle(sb, p, r * 0.22f, new Color(80, 180, 60, 180));
        }
    }

    private Color BodyColor() => DefId switch
    {
        "iron_colossus" => new Color(90, 88, 96),
        "storm_wyrm" => new Color(55, 85, 130),
        "blight_herald" => new Color(58, 92, 48),
        _ => new Color(120, 60, 60),
    };

    private Color CoreColor() => DefId switch
    {
        "iron_colossus" => new Color(200, 120, 60),
        "storm_wyrm" => new Color(140, 210, 255),
        "blight_herald" => new Color(170, 240, 90),
        _ => Color.White,
    };

    private Color AbilityColor() => Action switch
    {
        "ground_slam" => new Color(220, 120, 40),
        "lightning_bolt" => new Color(100, 180, 255),
        "poison_nova" => new Color(90, 220, 70),
        _ => new Color(255, 200, 80),
    };

    private static Rectangle Centered(Vector2 c, float w, float h) =>
        new((int)(c.X - w / 2f), (int)(c.Y - h / 2f), (int)w, (int)h);
}
