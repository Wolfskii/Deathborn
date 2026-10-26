namespace Deathborn.Client.Rendering.Characters;

/// <summary>Shared animation clips — every layer sheet for a clip uses the same <see cref="AnimationSpecification"/>.</summary>
public enum CharacterClip
{
    Idle,
    Walk,
    Run,
    Attack,
    ShieldBash,
    Cast,
    Hurt,
    Death,
    Roll,
    FishWait,
    FishReel,
    /// <summary>Farm swing (Farm RPG "Pickaxe, Hoe" strip) with the hoe in hand.</summary>
    Hoe,
    /// <summary>Same body strip as <see cref="Hoe"/> but empty-handed (planting, feeding).</summary>
    Plant,
    Water,
}
