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

    public float SortY => UsesSprite
        ? (UsesFarmSlime
            ? FarmRpgSlimeSprites.GetFootSortY(SpriteId, Position, DisplayScale)
            : TinyRpgCharacterSprites.GetFootSortY(SpriteId, Position, DisplayScale))
        : Position.Y + Radius;
    public float Radius => FarmRpgSlimeSprites.TryGetVisuals(SpriteId, out var slime)
        ? slime.Radius
        : NpcCatalog.Get(DefId).Radius;
    public float DisplayScale => FarmRpgSlimeSprites.TryGetVisuals(SpriteId, out var slime)
        ? slime.DisplayScale
        : NpcCatalog.Get(DefId).DisplayScale;
    public bool IsAttackable => NpcCategoryRules.IsAttackable(Disposition);
    public bool UsesFarmSlime => !IsBoss && FarmRpgSlimeSprites.Get(SpriteId) != null;
    public bool UsesSprite => !IsBoss && !string.IsNullOrEmpty(SpriteId)
        && (UsesFarmSlime || TinyRpgCharacterSprites.Get(SpriteId) != null);

    private TinyRpgStripAnimation? _anim;
    private FarmRpgSlimeAnimation? _slimeAnim;
    private string _animSpriteId = "";
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
        var prevAction = Action;
        Action = s.Action ?? "";
        if (Action == "melee" && prevAction != "melee")
        {
            _anim?.BeginMeleeAttack();
            _slimeAnim?.BeginMeleeAttack();
        }
        if (MathF.Abs((float)s.DirX) > 0.01f || MathF.Abs((float)s.DirY) > 0.01f)
            Facing = Vector2.Normalize(new Vector2((float)s.DirX, (float)s.DirY));
        if (!string.Equals(_animSpriteId, SpriteId, StringComparison.OrdinalIgnoreCase))
        {
            _animSpriteId = SpriteId;
            _slimeAnim = FarmRpgSlimeSprites.Get(SpriteId)?.Clone();
            _anim = _slimeAnim == null ? TinyRpgCharacterSprites.Get(SpriteId)?.Clone() : null;
        }
    }

    public void Update(float dt)
    {
        var before = Position;
        var lerped = Vector2.Lerp(Position, Target, MathHelper.Clamp(dt * Config.PlayerLerpSpeed, 0f, 1f));
        Position = WorldMap.SwaroviaMainland.ResolveMove(lerped, Vector2.Zero, Radius);
        _moving = Vector2.DistanceSquared(before, Position) > 0.05f;
        var drawFacing = GetDrawFacing();
        _slimeAnim?.Update(dt, drawFacing, _moving, Config.WalkAnimSpeed);
        _anim?.Update(dt, drawFacing, _moving, Config.WalkAnimSpeed);
        if (AbilityFlash > 0) AbilityFlash -= dt;
        if (!string.IsNullOrEmpty(Action))
            AbilityFlash = MathF.Max(AbilityFlash, 0.35f);
    }

    private Vector2 GetDrawFacing()
    {
        if (_moving)
        {
            var delta = Target - Position;
            if (delta.LengthSquared() > 0.25f)
                return Vector2.Normalize(delta);
        }

        return Facing.LengthSquared() > 0.01f ? Facing : new Vector2(0, 1);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 screenPos, float zoom)
    {
        if (UsesSprite)
        {
            var scale = DisplayScale * zoom;
            var facing = GetDrawFacing();
            if (_slimeAnim != null)
                _slimeAnim.Draw(sb, screenPos, Color.White, scale, facing);
            else if (_anim != null)
                _anim.Draw(sb, screenPos, Color.White, scale, facing);
            else
                DrawBossProcedural(sb, screenPos, zoom);
        }
        else
        {
            DrawBossProcedural(sb, screenPos, zoom);
        }

        DrawOverheadUi(sb, font, screenPos, zoom);
    }

    private float GetHeadTopScreenY(Vector2 screenPos, float zoom)
    {
        if (UsesSprite)
        {
            var scale = DisplayScale * zoom;
            var headOffset = UsesFarmSlime
                ? FarmRpgSlimeSprites.GetHeadTopOffsetFromAnchor(SpriteId)
                : TinyRpgCharacterSprites.GetHeadTopOffsetFromAnchor(SpriteId);
            return screenPos.Y + headOffset * scale;
        }

        var r = Radius * zoom;
        return screenPos.Y - r * 1.15f;
    }

    private bool ShouldShowOverheadHp() => IsAttackable && !IsBoss && HpMax > 0 && Hp > 0;

    private void DrawOverheadUi(SpriteBatch sb, SpriteFont font, Vector2 screenPos, float zoom)
    {
        var label = SpriteFontSafe.Filter(Name);
        const float labelScale = 0.85f;
        var hasName = !string.IsNullOrWhiteSpace(label);
        var size = hasName ? SpriteFontSafe.MeasureString(font, label) * labelScale : Vector2.Zero;
        if (!hasName && !ShouldShowOverheadHp()) return;

        var headTopY = GetHeadTopScreenY(screenPos, zoom);
        var gap = 4f * zoom;
        var stackY = headTopY - gap;

        if (ShouldShowOverheadHp())
        {
            var barH = Math.Max(4f, 5f * zoom);
            var barW = Math.Max(size.X + 10f * zoom, 38f * zoom);
            stackY -= barH;
            var barRect = new Rectangle(
                (int)(screenPos.X - barW * 0.5f),
                (int)stackY,
                (int)barW,
                (int)barH);
            DrawOverheadHealthBar(sb, barRect);
            stackY -= gap;
        }

        if (!hasName) return;

        var nameY = stackY - size.Y;
        var namePos = new Vector2(screenPos.X - size.X * 0.5f, nameY);
        SpriteFontSafe.DrawOutlined(sb, font, label, namePos, Color.White, Color.Black, labelScale, 1f);
    }

    private void DrawOverheadHealthBar(SpriteBatch sb, Rectangle bar)
    {
        var pct = Math.Clamp(Hp / HpMax, 0f, 1f);
        var fill = new Color(235, 48, 48);

        if (TinySwordsUi.IsLoaded)
        {
            TinySwordsUi.DrawBar(sb, bar, pct, big: false, fill);
            return;
        }

        DrawPrimitives.FillRect(sb, bar, new Color(18, 14, 12, 210));
        var fillW = Math.Max(0, (int)((bar.Width - 2) * pct));
        if (fillW > 0)
            DrawPrimitives.FillRect(sb, new Rectangle(bar.X + 1, bar.Y + 1, fillW, bar.Height - 2), fill);
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
            DrawBossIcon(sb, new Vector2(screenPos.X, GetHeadTopScreenY(screenPos, zoom) - 14f * zoom), zoom * 0.9f);
    }

    public static void DrawBossIcon(SpriteBatch sb, Vector2 center, float scale) =>
        MonsterMapIcon.Draw(sb, center, scale);

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
