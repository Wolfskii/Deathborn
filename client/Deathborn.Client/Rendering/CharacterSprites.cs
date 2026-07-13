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

    private static Texture2D _v2Idle = null!;
    private static Texture2D _v2Walk = null!;
    private static Texture2D _v2Attack = null!;

    public static void Load(ContentManager content)
    {
        _swordsmanRun = content.Load<Texture2D>("Characters/Swordsman/Run");
        _swordsmanIdle = content.Load<Texture2D>("Characters/Swordsman/Idle");
        _swordsmanAttack = content.Load<Texture2D>("Characters/Swordsman/Attack");
        _swordsmanDeath = content.Load<Texture2D>("Characters/Swordsman/Death");
        _swordsmanHurt = content.Load<Texture2D>("Characters/Swordsman/Hurt");

        _v2Idle = content.Load<Texture2D>("Characters/Swordsman V2/idle");
        _v2Walk = content.Load<Texture2D>("Characters/Swordsman V2/walk");
        _v2Attack = content.Load<Texture2D>("Characters/Swordsman V2/one-handed-attack");
    }

    public static FourDirectionRunAnimation CreateSwordsmanRun() => new(_swordsmanRun);

    public static FourDirectionIdleAnimation CreateSwordsmanIdle() => new(_swordsmanIdle);

    public static FourDirectionAttackAnimation CreateSwordsmanAttack() => new(_swordsmanAttack);

    public static FourDirectionDeathAnimation CreateSwordsmanDeath() => new(_swordsmanDeath);

    public static FourDirectionHurtAnimation CreateSwordsmanHurt() => new(_swordsmanHurt);

    public static FourDirectionDeathAnimation CreateCorpseDeathAnim() => new(_swordsmanDeath);

    public static Texture2D GetTexture(string bodyTypeId, Characters.CharacterClip clip)
    {
        if (bodyTypeId == Characters.CharacterAnimationCatalog.SwordsmanV2)
        {
            return clip switch
            {
                Characters.CharacterClip.Idle or Characters.CharacterClip.Cast => _v2Idle,
                Characters.CharacterClip.Run or Characters.CharacterClip.Roll => _v2Walk,
                Characters.CharacterClip.Attack => _v2Attack,
                Characters.CharacterClip.Hurt => _swordsmanHurt,
                Characters.CharacterClip.Death => _swordsmanDeath,
                _ => _v2Idle,
            };
        }

        return clip switch
        {
            Characters.CharacterClip.Idle or Characters.CharacterClip.Cast => _swordsmanIdle,
            Characters.CharacterClip.Run or Characters.CharacterClip.Roll => _swordsmanRun,
            Characters.CharacterClip.Attack => _swordsmanAttack,
            Characters.CharacterClip.Hurt => _swordsmanHurt,
            Characters.CharacterClip.Death => _swordsmanDeath,
            _ => _swordsmanIdle,
        };
    }

    public static Texture2D GetTexture(Characters.CharacterClip clip) =>
        GetTexture(Characters.CharacterAnimationCatalog.SwordsmanV2, clip);
}
