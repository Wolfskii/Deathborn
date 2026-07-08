using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>RuneScape-style floating combat text, XP splats, level-up sparkles, and banners.</summary>
public sealed class WorldFeedbackOverlay
{
    private sealed class Entry
    {
        public long? EntityId;
        public Vector2 WorldOffset;
        public Vector2? FixedWorld;
        public string Text = "";
        public Color Color;
        public float Age;
        public float Duration = 1.4f;
        public float FloatSpeed = 28f;
        public float Scale = 1f;
        public float RandomX;
        public bool Bold;
        public List<Sparkle>? Sparkles;
    }

    private sealed class Sparkle
    {
        public Vector2 Offset;
        public Vector2 Velocity;
        public float Age;
        public float Life = 0.9f;
        public float Size = 3f;
        public Color Color;
    }

    private sealed class Banner
    {
        public string Title = "";
        public string Subtitle = "";
        public Color Color;
        public float Age;
        public float Duration = 4.2f;
        public bool SparkleRing;
        public List<Sparkle>? Sparkles;
    }

    private readonly List<Entry> _entries = [];
    private readonly List<Banner> _banners = [];
    private int _spawnCounter;

    public void Clear()
    {
        _entries.Clear();
        _banners.Clear();
    }

    public void SpawnDamage(long targetId, int damage, bool blocked = false)
    {
        if (damage <= 0 && !blocked) return;
        var text = blocked ? "Block" : $"-{damage}";
        var color = blocked
            ? new Color(120, 170, 230)
            : new Color(255, 72, 58);
        var offset = targetId < 0 ? new Vector2(0, -58) : new Vector2(0, -48);
        AddEntity(targetId, text, color, duration: 1.15f, floatSpeed: 36f, scale: blocked ? 0.9f : 1.08f + Math.Min(damage, 30) * 0.012f, bold: true,
            worldOffset: offset);
    }

    public void SpawnHeal(long entityId, int amount)
    {
        if (amount <= 0) return;
        AddEntity(entityId, $"+{amount}", new Color(88, 235, 108), duration: 1.25f, floatSpeed: 28f, scale: 1.05f, bold: true,
            worldOffset: new Vector2(0, -48));
    }

    public void SpawnSkillXp(long entityId, string skillName, long amount, bool leveledUp, int level, Vector2? worldPos = null)
    {
        if (leveledUp)
        {
            SpawnLevelUp(entityId, skillName, level, worldPos);
            return;
        }

        if (amount <= 0) return;
        var text = $"+{amount} {skillName} XP";
        if (worldPos is { } pos)
            AddFixed(pos + new Vector2(0, -36), text, new Color(240, 200, 80), duration: 1.5f, floatSpeed: 22f, scale: 0.85f);
        else
            AddEntity(entityId, text, new Color(240, 200, 80), duration: 1.5f, floatSpeed: 22f, scale: 0.85f, bold: false);
    }

    public void SpawnLevelUp(long entityId, string skillName, int level, Vector2? worldPos = null)
    {
        var text = $"Level up! {skillName} {level}";
        var color = new Color(255, 220, 90);
        Entry entry;
        if (worldPos is { } pos)
            entry = AddFixed(pos + new Vector2(0, -48), text, color, duration: 2.6f, floatSpeed: 18f, scale: 1.15f, bold: true);
        else
            entry = AddEntity(entityId, text, color, duration: 2.6f, floatSpeed: 18f, scale: 1.15f, bold: true);

        entry.Sparkles = CreateSparkleBurst(16, new Color(255, 230, 120), 42f);
    }

    public void SpawnLocalLevelUpBanner(string skillName, int level)
    {
        _banners.Add(new Banner
        {
            Title = "Congratulations!",
            Subtitle = $"Your {skillName} level is now {level}.",
            Color = new Color(255, 215, 90),
            Duration = 4.5f,
            SparkleRing = true,
            Sparkles = CreateSparkleBurst(24, new Color(255, 240, 150), 55f),
        });
    }

    public void SpawnDeath(long entityId, string name, bool isLocal)
    {
        if (isLocal)
        {
            _banners.Add(new Banner
            {
                Title = "Oh dear, you are dead!",
                Subtitle = "Your items remain on your corpse.",
                Color = new Color(220, 80, 75),
                Duration = 5f,
            });
        }

        var label = isLocal ? "You died!" : $"{TrimName(name)} died";
        AddEntity(entityId, label, new Color(190, 190, 200), duration: 2.4f, floatSpeed: 14f, scale: 1f, bold: true,
            worldOffset: new Vector2(0, -58));
    }

    public void SpawnMiss(long entityId)
    {
        AddEntity(entityId, "Miss", new Color(170, 170, 185), duration: 0.9f, floatSpeed: 24f, scale: 0.85f, bold: false);
    }

    public void Update(float dt, IReadOnlyDictionary<long, PlayerEntity> players, IReadOnlyDictionary<long, BossEntity>? bosses = null)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var e = _entries[i];
            e.Age += dt;
            if (e.Age >= e.Duration)
            {
                _entries.RemoveAt(i);
                continue;
            }

            UpdateSparkles(e.Sparkles, dt);
        }

        for (var i = _banners.Count - 1; i >= 0; i--)
        {
            var b = _banners[i];
            b.Age += dt;
            if (b.Age >= b.Duration)
                _banners.RemoveAt(i);
            else
                UpdateSparkles(b.Sparkles, dt);
        }
    }

    public void SpawnBossDamage(long targetNpcId, int damage)
    {
        if (damage <= 0) return;
        SpawnDamage(targetNpcId, damage);
    }

    public void DrawWorld(
        SpriteBatch sb, SpriteFont font,
        Func<Vector2, Vector2> worldToScreen, float zoom,
        IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyDictionary<long, BossEntity>? bosses = null)
    {
        foreach (var e in _entries)
        {
            var world = ResolveWorldPos(e, players, bosses);
            var screen = worldToScreen(world);
            screen.X += e.RandomX;
            screen.Y -= e.Age * e.FloatSpeed;

            var t = e.Age / e.Duration;
            var alpha = t < 0.12f ? t / 0.12f : t > 0.72f ? (1f - t) / 0.28f : 1f;
            var scale = e.Scale * (1f + MathF.Sin(MathF.Min(t, 1f) * MathF.PI) * 0.08f);
            var filtered = SpriteFontSafe.Filter(e.Text);
            var textSize = SpriteFontSafe.MeasureString(font, filtered) * scale;
            var drawPos = new Vector2(screen.X - textSize.X / 2f, screen.Y - textSize.Y);
            DrawFloatingText(sb, font, e.Text, drawPos, e.Color * alpha, scale, e.Bold);
            DrawEntrySparkles(sb, worldToScreen, world, e.Sparkles, alpha);
        }
    }

    public void DrawScreen(SpriteBatch sb, SpriteFont font) => DrawBanners(sb, font);

    private void DrawBanners(SpriteBatch sb, SpriteFont font)
    {
        if (_banners.Count == 0) return;

        var cx = GameViewport.Width / 2f;
        var y = 72f;

        foreach (var b in _banners)
        {
            var t = b.Age / b.Duration;
            var fadeIn = MathF.Min(1f, b.Age / 0.25f);
            var fadeOut = t > 0.78f ? (1f - t) / 0.22f : 1f;
            var alpha = fadeIn * fadeOut;
            var pulse = 1f + MathF.Sin(b.Age * 6f) * 0.03f;

            if (TinySwordsUi.IsLoaded)
            {
                var titleSize = SpriteFontSafe.MeasureString(font, b.Title) * (1.05f * pulse);
                var subSize = string.IsNullOrEmpty(b.Subtitle)
                    ? Vector2.Zero
                    : SpriteFontSafe.MeasureString(font, b.Subtitle) * 0.92f;
                var bodyW = (int)MathF.Max(titleSize.X, subSize.X) + 56;
                var bodyH = (int)(titleSize.Y + (subSize.Y > 0 ? subSize.Y + 8 : 0) + 36);
                var ribbonH = 40;
                var totalW = Math.Clamp(bodyW, 280, 520);
                var totalH = ribbonH + bodyH;
                var panel = new Rectangle((int)(cx - totalW / 2f), (int)(y - 8), totalW, totalH);

                TinySwordsUi.DrawPanel(sb, panel, TinySwordsUi.PanelKind.Banner, alpha * 0.96f);
                var ribbon = new Rectangle(panel.X + 12, panel.Y + 10, panel.Width - 24, ribbonH);
                var ribbonKind = b.Color.R > b.Color.G + 40 ? TinySwordsUi.RibbonKind.Red
                    : b.Color.G > b.Color.R ? TinySwordsUi.RibbonKind.Teal
                    : TinySwordsUi.RibbonKind.Gold;
                TinySwordsUi.DrawBigRibbon(sb, ribbon, ribbonKind, pointed: true, alpha);

                var textArea = TinySwordsUi.MeasureRibbonTextArea(ribbon, 12);
                DrawFloatingText(sb, font, b.Title, new Vector2(textArea.X, textArea.Y + 4), b.Color * alpha, 1.05f * pulse, bold: true);

                if (!string.IsNullOrEmpty(b.Subtitle))
                {
                    var subPos = new Vector2(cx - subSize.X / 2f, panel.Y + ribbonH + 14);
                    DrawFloatingText(sb, font, b.Subtitle, subPos, new Color(230, 225, 210) * alpha, 0.92f, bold: false);
                }

                if (b.SparkleRing && b.Sparkles != null)
                {
                    var center = new Vector2(cx, y + 18);
                    foreach (var s in b.Sparkles)
                    {
                        var p = center + s.Offset;
                        var a = alpha * (1f - s.Age / s.Life);
                        DrawStar(sb, p, s.Size * pulse, s.Color * a);
                    }
                }

                y += totalH + 16;
                continue;
            }

            var legacyTitleSize = SpriteFontSafe.MeasureString(font, b.Title) * (1.05f * pulse);
            var legacySubSize = string.IsNullOrEmpty(b.Subtitle)
                ? Vector2.Zero
                : SpriteFontSafe.MeasureString(font, b.Subtitle) * 0.92f;
            var panelW = (int)MathF.Max(legacyTitleSize.X, legacySubSize.X) + 48;
            var panelH = (int)(legacyTitleSize.Y + (legacySubSize.Y > 0 ? legacySubSize.Y + 10 : 0) + 24);
            var legacyPanel = new Rectangle((int)(cx - panelW / 2f), (int)(y - 8), panelW, panelH);
            DrawPrimitives.FillRect(sb, legacyPanel, new Color(18, 14, 10, (int)(190 * alpha)));
            DrawBorder(sb, legacyPanel, b.Color * (alpha * 0.85f));

            var titlePos = new Vector2(cx - legacyTitleSize.X / 2f, y);
            DrawFloatingText(sb, font, b.Title, titlePos, b.Color * alpha, 1.05f * pulse, bold: true);

            if (!string.IsNullOrEmpty(b.Subtitle))
            {
                var subPos = new Vector2(cx - legacySubSize.X / 2f, y + legacyTitleSize.Y + 4);
                DrawFloatingText(sb, font, b.Subtitle, subPos, new Color(230, 225, 210) * alpha, 0.92f, bold: false);
            }

            if (b.SparkleRing && b.Sparkles != null)
            {
                var center = new Vector2(cx, y + 18);
                foreach (var s in b.Sparkles)
                {
                    var p = center + s.Offset;
                    var a = alpha * (1f - s.Age / s.Life);
                    DrawStar(sb, p, s.Size * pulse, s.Color * a);
                }
            }

            y += 72f;
        }
    }

    private static Vector2 ResolveWorldPos(
        Entry e,
        IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyDictionary<long, BossEntity>? bosses)
    {
        if (e.FixedWorld is { } fixedPos)
            return fixedPos;

        if (e.EntityId is long id)
        {
            if (id < 0 && bosses != null && bosses.TryGetValue(id, out var boss))
                return boss.Position + e.WorldOffset;
            if (players.TryGetValue(id, out var p))
                return p.Position + e.WorldOffset;
        }

        return e.WorldOffset;
    }

    private Entry AddEntity(
        long entityId, string text, Color color,
        float duration, float floatSpeed, float scale, bool bold,
        Vector2? worldOffset = null)
    {
        var entry = new Entry
        {
            EntityId = entityId,
            Text = text,
            Color = color,
            Duration = duration,
            FloatSpeed = floatSpeed,
            Scale = scale,
            Bold = bold,
            WorldOffset = worldOffset ?? new Vector2(0, -44),
            RandomX = NextJitter(),
        };
        _entries.Add(entry);
        return entry;
    }

    private Entry AddFixed(Vector2 worldPos, string text, Color color, float duration, float floatSpeed, float scale, bool bold = false)
    {
        var entry = new Entry
        {
            FixedWorld = worldPos,
            Text = text,
            Color = color,
            Duration = duration,
            FloatSpeed = floatSpeed,
            Scale = scale,
            Bold = bold,
            RandomX = NextJitter(),
        };
        _entries.Add(entry);
        return entry;
    }

    private float NextJitter()
    {
        _spawnCounter++;
        var n = (_spawnCounter * 1103515245 + 12345) & 0x7fffffff;
        return (n % 21 - 10) * 0.6f;
    }

    private static List<Sparkle> CreateSparkleBurst(int count, Color color, float speed)
    {
        var list = new List<Sparkle>(count);
        for (var i = 0; i < count; i++)
        {
            var angle = i / (float)count * MathHelper.TwoPi + (i * 0.31f);
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            list.Add(new Sparkle
            {
                Velocity = vel,
                Color = color,
                Size = 2.5f + (i % 3),
                Life = 0.75f + (i % 5) * 0.08f,
            });
        }
        return list;
    }

    private static void UpdateSparkles(List<Sparkle>? sparkles, float dt)
    {
        if (sparkles == null) return;
        for (var i = sparkles.Count - 1; i >= 0; i--)
        {
            var s = sparkles[i];
            s.Age += dt;
            s.Offset += s.Velocity * dt;
            s.Velocity *= 0.96f;
            if (s.Age >= s.Life)
                sparkles.RemoveAt(i);
        }
    }

    private static void DrawEntrySparkles(
        SpriteBatch sb, Func<Vector2, Vector2> worldToScreen, Vector2 world,
        List<Sparkle>? sparkles, float alpha)
    {
        if (sparkles == null || sparkles.Count == 0) return;
        var anchor = worldToScreen(world + new Vector2(0, -52));
        foreach (var s in sparkles)
        {
            var fade = alpha * (1f - s.Age / s.Life);
            DrawStar(sb, anchor + s.Offset, s.Size, s.Color * fade);
        }
    }

    private static void DrawFloatingText(
        SpriteBatch sb, SpriteFont font, string text, Vector2 pos, Color color, float scale, bool bold)
    {
        var filtered = SpriteFontSafe.Filter(text);
        if (filtered.Length == 0) return;

        var outline = new Color(12, 10, 8) * color.A;
        if (bold)
        {
            DrawStringOutline(sb, font, filtered, pos, color, outline, scale);
            return;
        }

        sb.DrawString(font, filtered, pos + new Vector2(1, 1), outline, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        sb.DrawString(font, filtered, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private static void DrawStringOutline(
        SpriteBatch sb, SpriteFont font, string text, Vector2 pos, Color fill, Color outline, float scale)
    {
        var o = scale;
        sb.DrawString(font, text, pos + new Vector2(-o, 0), outline, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        sb.DrawString(font, text, pos + new Vector2(o, 0), outline, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        sb.DrawString(font, text, pos + new Vector2(0, -o), outline, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        sb.DrawString(font, text, pos + new Vector2(0, o), outline, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        sb.DrawString(font, text, pos, fill, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private static void DrawStar(SpriteBatch sb, Vector2 center, float size, Color color)
    {
        if (color.A <= 2) return;
        var arm = size * 1.6f;
        DrawPrimitives.FillRect(sb, CenteredRect(center, size, size), color);
        DrawPrimitives.FillRect(sb, CenteredRect(center, arm, size * 0.45f), color);
        DrawPrimitives.FillRect(sb, CenteredRect(center, size * 0.45f, arm), color);
    }

    private static Rectangle CenteredRect(Vector2 center, float w, float h) =>
        new((int)(center.X - w / 2f), (int)(center.Y - h / 2f), (int)w, (int)h);

    private static string TrimName(string name)
    {
        var n = SpriteFontSafe.Filter(name);
        return n.Length > 14 ? n[..11] + "..." : n;
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
    }
}
