using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>
/// Immutable animation contract shared by every sprite layer for a given clip.
/// Body, hair, armor, weapons, and VFX sheets must all conform to the same spec.
/// </summary>
public readonly struct AnimationSpecification
{
    public int FrameWidth { get; init; }
    public int FrameHeight { get; init; }
    public int FramesPerDirection { get; init; }
    public int Directions { get; init; }
    public Vector2 Pivot { get; init; }
    public Vector2 Origin { get; init; }
    public float FrameDuration { get; init; }
    public AnimationSheetLayout Layout { get; init; }

    /// <summary>Column spacing in the sheet (Craftpix layout).</summary>
    public int FrameStride { get; init; }

    /// <summary>Left margin before the first frame column (Craftpix layout).</summary>
    public int FrameStartX { get; init; }

    /// <summary>Y offset of each facing row indexed by <see cref="FacingDirection"/>.</summary>
    public int[] DirectionRowTops { get; init; }

    /// <summary>When set, overrides <see cref="FramesPerDirection"/> per facing (idle up row).</summary>
    public int[]? PerDirectionFrameCounts { get; init; }

    /// <summary>Frame column offset per facing (idle).</summary>
    public int[]? PerDirectionFrameOffsets { get; init; }

    /// <summary>Physical row index per facing for grid layout (death, hurt).</summary>
    public int[]? DirectionRows { get; init; }

    /// <summary>Maps logical facing index to physical sheet row (walk has 6 rows).</summary>
    public int[]? DirectionRowMap { get; init; }

    /// <summary>Draw scale per physical sheet row (normalizes diagonal vs cardinal height).</summary>
    public float[]? DirectionRowDrawScale { get; init; }

    /// <summary>Left edge of each frame column (length = frames + 1; last entry is sheet width).</summary>
    public int[]? ColumnStarts { get; init; }

    /// <summary>Top edge of each direction row (length = rows + 1; last entry is sheet height).</summary>
    public int[]? RowStarts { get; init; }

    /// <summary>Left-facing attack swings extend into the cell's left margin.</summary>
    public int AttackLeftSourcePad { get; init; }

    public int FrameCountForFacing(FacingDirection facing)
    {
        var index = (int)facing;
        if (PerDirectionFrameCounts is { Length: > 0 } counts && index < counts.Length)
            return counts[index];
        return FramesPerDirection;
    }
}
