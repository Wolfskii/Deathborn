namespace Deathborn.Client.Rendering.Characters;

/// <summary>How frame rectangles are addressed inside a sprite sheet.</summary>
public enum AnimationSheetLayout
{
    /// <summary>Craftpix-style rows with column stride (run, idle, attack).</summary>
    CraftpixBody,

    /// <summary>Uniform 64×64 grid with remapped direction rows (death, hurt).</summary>
    Grid64,

    /// <summary>Uniform row-per-direction, column-per-frame grid (Swordsman V2).</summary>
    UniformGrid,
}
