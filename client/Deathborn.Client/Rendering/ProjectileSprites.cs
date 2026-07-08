using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public static class ProjectileSprites
{
    private static Texture2D? _arrow01;
    private static Texture2D? _arrow02;
    private static Texture2D? _arrow03;

    public static bool IsLoaded => _arrow01 != null;

    public static void Load(ContentManager content)
    {
        _arrow01 = content.Load<Texture2D>("Characters/Rpg/Projectiles/arrow_01");
        _arrow02 = content.Load<Texture2D>("Characters/Rpg/Projectiles/arrow_02");
        _arrow03 = content.Load<Texture2D>("Characters/Rpg/Projectiles/arrow_03");
    }

    public static Texture2D? ForStyle(ProjectileStyle style) => style switch
    {
        ProjectileStyle.Ice => _arrow02,
        ProjectileStyle.Arrow => _arrow01,
        ProjectileStyle.Blood => _arrow03,
        _ => null,
    };
}
