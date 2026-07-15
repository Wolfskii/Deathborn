using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering.Characters;

public static class CharacterAnimationCatalog
{
    public const string FarmRpg = "farm_rpg";

    public static AnimationSpecification GetSpec(string bodyTypeId, CharacterClip clip) =>
        FarmRpgAnimationSpecs.For(clip);

    /// <summary>World draw scale — Farm RPG is native 32×32.</summary>
    public static float GetDrawScale(string bodyTypeId) =>
        PlayerEntity.SpriteDrawScale * 1.35f;
}
