using Deathborn.Client.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public static class ProjectileSprites
{
    private const int Cell = 32;

    private static Texture2D? _arrow01;
    private static Texture2D? _arrow02;
    private static Texture2D? _arrow03;
    private static Texture2D? _magicSheet;

    public static bool IsLoaded => _arrow01 != null || _magicSheet != null;
    public static bool HasMagicSheet => _magicSheet != null;

    public static Texture2D? MagicSheet => _magicSheet;

    public static void Load(ContentManager content)
    {
        _arrow01 = TryLoad(content, "Characters/Rpg/Projectiles/arrow_01");
        _arrow02 = TryLoad(content, "Characters/Rpg/Projectiles/arrow_02");
        _arrow03 = TryLoad(content, "Characters/Rpg/Projectiles/arrow_03");
        _magicSheet = TryLoad(content, "Characters/FarmRpg/Effects/magic");
    }

    private static Texture2D? TryLoad(ContentManager content, string path)
    {
        try { return content.Load<Texture2D>(path); }
        catch { return null; }
    }

    public static Texture2D? ForStyle(ProjectileStyle style) => style switch
    {
        ProjectileStyle.Ice => _arrow02,
        ProjectileStyle.Arrow => _arrow01,
        ProjectileStyle.Blood => _arrow03,
        _ => null,
    };

    /// <summary>Farm RPG Magic.png — 2-frame flying streak per element (row 1).</summary>
    public static bool TryGetMagicFlyFrame(ProjectileStyle style, int frameIndex, out Rectangle source)
    {
        source = default;
        if (_magicSheet == null) return false;

        var col = frameIndex % 2;
        source = style switch
        {
            ProjectileStyle.Ice => CellRect(col, 1),
            ProjectileStyle.Fire => CellRect(3 + col, 1),
            ProjectileStyle.Blood => CellRect(3 + col, 1),
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
            // Blue X (col 2) + crescent; green row-3 crescents as fade-out.
            ProjectileStyle.Ice => col switch
            {
                0 => CellRect(2, 0),
                1 => CellRect(2, 1),
                _ => CellRect(col, 3),
            },
            // Orange X (col 5) + crescent on row 1.
            ProjectileStyle.Fire => col switch
            {
                0 => CellRect(5, 0),
                1 => CellRect(5, 1),
                _ => CellRect(4, 1),
            },
            ProjectileStyle.Blood => col switch
            {
                0 => CellRect(5, 0),
                1 => CellRect(5, 1),
                _ => CellRect(4, 1),
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

    private static Rectangle CellRect(int col, int row) =>
        new(col * Cell, row * Cell, Cell, Cell);
}
