using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering;

/// <summary>8-way facing: South first, then counter-clockwise through SW, W, NW, N, NE, E, SE.</summary>
public enum Facing8
{
    Down = 0,
    DownLeft = 1,
    Left = 2,
    UpLeft = 3,
    Up = 4,
    UpRight = 5,
    Right = 6,
    DownRight = 7,
}

public static class Facing8Resolver
{
    /// <summary>
    /// Resolves movement/aim direction to 8 sprite rows.
    /// Cardinals require a single axis; diagonals require both (e.g. W+D).
    /// </summary>
    public static Facing8 Resolve(Vector2 dir)
    {
        if (dir.LengthSquared() < 0.0001f)
            return Facing8.Down;

        var ax = MathF.Abs(dir.X);
        var ay = MathF.Abs(dir.Y);

        if (ax < 0.01f)
            return dir.Y > 0 ? Facing8.Down : Facing8.Up;
        if (ay < 0.01f)
            return dir.X > 0 ? Facing8.Right : Facing8.Left;

        // 8 sectors centered on cardinals/diagonals; sector 0 = East (+X).
        var angle = MathF.Atan2(dir.Y, dir.X);
        var sector = (int)MathF.Floor((angle + MathHelper.Pi / 8f) / (MathHelper.Pi / 4f));
        sector = ((sector % 8) + 8) % 8;

        return sector switch
        {
            0 => Facing8.Right,
            1 => Facing8.DownRight,
            2 => Facing8.Down,
            3 => Facing8.DownLeft,
            4 => Facing8.Left,
            5 => Facing8.UpLeft,
            6 => Facing8.Up,
            7 => Facing8.UpRight,
            _ => Facing8.Down,
        };
    }

    public static FacingDirection ToCardinal4(Facing8 facing) => facing switch
    {
        Facing8.Up or Facing8.UpLeft or Facing8.UpRight => FacingDirection.Up,
        Facing8.Left or Facing8.DownLeft => FacingDirection.Left,
        Facing8.Right or Facing8.DownRight => FacingDirection.Right,
        _ => FacingDirection.Down,
    };
}
