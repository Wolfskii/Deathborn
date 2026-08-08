using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public static class ProjectileSprites
{
    private const int LegacyCell = 32;

    /// <summary>
    /// Elemental ball sheets face left (bright head on −X). Add this to
    /// <c>Atan2(dir.Y, dir.X)</c> when drawing so the head leads the velocity.
    /// </summary>
    public const float BallArtFacingOffset = MathHelper.Pi;

    private static Texture2D? _arrow01;
    private static Texture2D? _arrow02;
    private static Texture2D? _arrow03;
    private static Texture2D? _magicSheet;

    private static BallSheet? _fire;
    private static BallSheet? _ice;
    private static BallSheet? _poison;

    public static bool IsLoaded => _arrow01 != null || _magicSheet != null || HasBallSheets;
    public static bool HasMagicSheet => _magicSheet != null;
    public static bool HasBallSheets => _fire != null || _ice != null || _poison != null;

    public static Texture2D? MagicSheet => _magicSheet;

    public static void Load(ContentManager content)
    {
        _arrow01 = TryLoad(content, "Characters/Rpg/Projectiles/arrow_01");
        _arrow02 = TryLoad(content, "Characters/Rpg/Projectiles/arrow_02");
        _arrow03 = TryLoad(content, "Characters/Rpg/Projectiles/arrow_03");
        _magicSheet = TryLoad(content, "Characters/FarmRpg/Effects/magic");

        _fire = TryLoadBall(content, "Effects/Projectiles/fireball", 68, 9, 6,
            "Effects/Projectiles/small_fireball", 10, 26);
        _ice = TryLoadBall(content, "Effects/Projectiles/iceball", 84, 9, 6,
            "Effects/Projectiles/small_iceball", 9, 24);
        _poison = TryLoadBall(content, "Effects/Projectiles/poisonball", 65, 9, 6,
            "Effects/Projectiles/small_poisonball", 9, 25);
    }

    private static Texture2D? TryLoad(ContentManager content, string path)
    {
        try { return content.Load<Texture2D>(path); }
        catch { return null; }
    }

    private static BallSheet? TryLoadBall(
        ContentManager content,
        string flyPath,
        int flyW,
        int flyH,
        int usableRows,
        string burstPath,
        int burstW,
        int burstH)
    {
        var fly = TryLoad(content, flyPath);
        if (fly == null) return null;

        var burst = TryLoad(content, burstPath);
        return new BallSheet(
            fly,
            flyW,
            flyH,
            Math.Max(1, fly.Width / flyW),
            Math.Clamp(usableRows, 1, Math.Max(1, fly.Height / flyH)),
            burst,
            burstW,
            burstH,
            burst != null ? Math.Max(1, burst.Width / burstW) : 0,
            burst != null ? Math.Max(1, burst.Height / burstH) : 0);
    }

    public static Texture2D? ForStyle(ProjectileStyle style) => style switch
    {
        ProjectileStyle.Ice => _arrow02,
        ProjectileStyle.Arrow => _arrow01,
        ProjectileStyle.Blood => _arrow03,
        _ => null,
    };

    public static bool TryGetBallFlyFrame(
        ProjectileStyle style,
        float anim,
        out Texture2D tex,
        out Rectangle source)
    {
        tex = null!;
        source = default;
        if (SheetFor(style) is not { } sheet)
            return false;

        var frame = Math.Max(0, (int)anim);
        var total = sheet.FlyCols * sheet.FlyRows;
        frame %= total;
        var col = frame % sheet.FlyCols;
        var row = frame / sheet.FlyCols;
        tex = sheet.Fly;
        source = new Rectangle(col * sheet.FlyFrameW, row * sheet.FlyFrameH, sheet.FlyFrameW, sheet.FlyFrameH);
        return true;
    }

    public static bool TryGetBallBurstFrame(
        ProjectileStyle style,
        float burstT01,
        out Texture2D tex,
        out Rectangle source)
    {
        tex = null!;
        source = default;
        if (SheetFor(style) is not { Burst: not null } sheet || sheet.BurstCols <= 0)
            return false;

        var total = sheet.BurstCols * sheet.BurstRows;
        var frame = Math.Clamp((int)(burstT01 * total), 0, total - 1);
        var col = frame % sheet.BurstCols;
        var row = frame / sheet.BurstCols;
        tex = sheet.Burst!;
        source = new Rectangle(
            col * sheet.BurstFrameW,
            row * sheet.BurstFrameH,
            sheet.BurstFrameW,
            sheet.BurstFrameH);
        return true;
    }

    /// <summary>Origin near the bright head (left side of left-facing fly frames).</summary>
    public static Vector2 BallFlyOrigin(Rectangle source) =>
        new(source.Width * 0.18f, source.Height * 0.5f);

    /// <summary>Farm RPG Magic.png — 2-frame flying streak per element (row 1).</summary>
    public static bool TryGetMagicFlyFrame(ProjectileStyle style, int frameIndex, out Rectangle source)
    {
        source = default;
        if (_magicSheet == null) return false;

        var col = frameIndex % 2;
        source = style switch
        {
            ProjectileStyle.Ice => LegacyCellRect(col, 1),
            ProjectileStyle.Fire => LegacyCellRect(3 + col, 1),
            ProjectileStyle.Blood => LegacyCellRect(3 + col, 1),
            ProjectileStyle.Poison => LegacyCellRect(col, 1),
            _ => default,
        };
        return source != default;
    }

    /// <summary>Farm RPG Magic.png — 3-frame impact burst (X + crescent). Unused for now — procedural burst looks cleaner.</summary>
    public static bool TryGetMagicBurstFrame(ProjectileStyle style, int frameIndex, out Rectangle source)
    {
        source = default;
        if (_magicSheet == null) return false;

        var col = Math.Clamp(frameIndex, 0, 2);
        source = style switch
        {
            ProjectileStyle.Ice => col switch
            {
                0 => LegacyCellRect(2, 0),
                1 => LegacyCellRect(2, 1),
                _ => LegacyCellRect(col, 3),
            },
            ProjectileStyle.Fire => col switch
            {
                0 => LegacyCellRect(5, 0),
                1 => LegacyCellRect(5, 1),
                _ => LegacyCellRect(4, 1),
            },
            ProjectileStyle.Blood => col switch
            {
                0 => LegacyCellRect(5, 0),
                1 => LegacyCellRect(5, 1),
                _ => LegacyCellRect(4, 1),
            },
            ProjectileStyle.Poison => col switch
            {
                0 => LegacyCellRect(2, 0),
                1 => LegacyCellRect(2, 1),
                _ => LegacyCellRect(col, 3),
            },
            _ => default,
        };
        return source != default;
    }

    public static Color TintForStyle(ProjectileStyle style) => style switch
    {
        ProjectileStyle.Blood => new Color(255, 180, 190),
        _ => Color.White,
    };

    private static BallSheet? SheetFor(ProjectileStyle style) => style switch
    {
        ProjectileStyle.Fire => _fire,
        ProjectileStyle.Ice => _ice,
        ProjectileStyle.Poison => _poison,
        _ => null,
    };

    private static Rectangle LegacyCellRect(int col, int row) =>
        new(col * LegacyCell, row * LegacyCell, LegacyCell, LegacyCell);

    private sealed class BallSheet(
        Texture2D fly,
        int flyFrameW,
        int flyFrameH,
        int flyCols,
        int flyRows,
        Texture2D? burst,
        int burstFrameW,
        int burstFrameH,
        int burstCols,
        int burstRows)
    {
        public Texture2D Fly { get; } = fly;
        public int FlyFrameW { get; } = flyFrameW;
        public int FlyFrameH { get; } = flyFrameH;
        public int FlyCols { get; } = flyCols;
        public int FlyRows { get; } = flyRows;
        public Texture2D? Burst { get; } = burst;
        public int BurstFrameW { get; } = burstFrameW;
        public int BurstFrameH { get; } = burstFrameH;
        public int BurstCols { get; } = burstCols;
        public int BurstRows { get; } = burstRows;
    }
}
