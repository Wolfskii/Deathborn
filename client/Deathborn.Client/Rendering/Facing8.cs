using Microsoft.Xna.Framework;

namespace Deathborn.Client.Rendering;

/// <summary>8-way facing matching Swordsman V2 sprite sheet row order (down first, clockwise).</summary>
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
    /// <summary>Resolves movement/aim direction to the nearest of 8 sprite rows.</summary>
    public static Facing8 Resolve(Microsoft.Xna.Framework.Vector2 dir)
    {
        if (dir.LengthSquared() < 0.0001f)
            return Facing8.Down;

        var angle = MathF.Atan2(dir.Y, dir.X);
        var sector = (int)MathF.Floor((angle + MathHelper.PiOver4) / MathHelper.PiOver2);
        sector = ((sector % 8) + 8) % 8;
        return (Facing8)((sector + 6) % 8);
    }

    public static FacingDirection ToCardinal4(Facing8 facing) => facing switch
    {
        Facing8.Up or Facing8.UpLeft or Facing8.UpRight => FacingDirection.Up,
        Facing8.Left or Facing8.DownLeft => FacingDirection.Left,
        Facing8.Right or Facing8.DownRight => FacingDirection.Right,
        _ => FacingDirection.Down,
    };
}
