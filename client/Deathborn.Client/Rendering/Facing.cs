using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering;

/// <summary>4-way cardinal facing — matches Farm RPG strip order (down, up, right, left).</summary>
public enum FacingDirection
{
    Down = 0,
    Up = 1,
    Right = 2,
    Left = 3,
}

public static class FacingResolver
{
    /// <summary>Resolves movement/aim to the dominant cardinal (diagonals snap to X or Y).</summary>
    public static FacingDirection Resolve(Vector2 dir)
    {
        if (dir.LengthSquared() < 0.0001f)
            return FacingDirection.Down;

        var ax = MathF.Abs(dir.X);
        var ay = MathF.Abs(dir.Y);
        if (ax >= ay)
            return dir.X >= 0 ? FacingDirection.Right : FacingDirection.Left;
        return dir.Y >= 0 ? FacingDirection.Down : FacingDirection.Up;
    }
}
