using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public static class CharacterSprites
{
    private static Texture2D _swordsmanRun = null!;
    private static Texture2D _swordsmanIdle = null!;
    private static Texture2D _swordsmanAttack = null!;
    private static Texture2D _swordsmanDeath = null!;
    private static Texture2D _swordsmanHurt = null!;

    public static void Load(ContentManager content)
    {
        _swordsmanRun = content.Load<Texture2D>("Characters/Swordsman/Run");
        _swordsmanIdle = content.Load<Texture2D>("Characters/Swordsman/Idle");
        _swordsmanAttack = content.Load<Texture2D>("Characters/Swordsman/Attack");
        _swordsmanDeath = content.Load<Texture2D>("Characters/Swordsman/Death");
        _swordsmanHurt = content.Load<Texture2D>("Characters/Swordsman/Hurt");
    }

    public static FourDirectionRunAnimation CreateSwordsmanRun() => new(_swordsmanRun);

    public static FourDirectionIdleAnimation CreateSwordsmanIdle() => new(_swordsmanIdle);

    public static FourDirectionAttackAnimation CreateSwordsmanAttack() => new(_swordsmanAttack);

    public static FourDirectionDeathAnimation CreateSwordsmanDeath() => new(_swordsmanDeath);

    public static FourDirectionHurtAnimation CreateSwordsmanHurt() => new(_swordsmanHurt);

    public static FourDirectionDeathAnimation CreateCorpseDeathAnim() => new(_swordsmanDeath);
}
