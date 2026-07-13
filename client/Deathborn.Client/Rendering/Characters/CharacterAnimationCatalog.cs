using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering.Characters;

public static class CharacterAnimationCatalog
{
    public const string SwordsmanV1 = "swordsman";
    public const string SwordsmanV2 = "swordsman_v2";

    public static AnimationSpecification GetSpec(string bodyTypeId, CharacterClip clip) =>
        bodyTypeId == SwordsmanV2
            ? SwordsmanV2AnimationSpecs.For(clip)
            : SwordsmanAnimationSpecs.For(clip);

    public static Texture2D GetTexture(string bodyTypeId, CharacterClip clip) =>
        CharacterSprites.GetTexture(bodyTypeId, clip);

    /// <summary>World draw scale so V2 (~100px) matches V1 (~32px) footprint.</summary>
    public static float GetDrawScale(string bodyTypeId) =>
        bodyTypeId == SwordsmanV2 ? 0.216f : PlayerEntity.SpriteDrawScale;
}
