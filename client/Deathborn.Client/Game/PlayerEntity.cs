using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public sealed class PlayerEntity
{
    public const float Radius = 12f;

    public long Id;
    public string Name = "";
    public Vector2 Position;
    public Vector2 Target;
    public bool IsLocal;
    public Vector2 MoveDir;

    public void SetTarget(Vector2 pos)
    {
        if (Vector2.DistanceSquared(pos, Target) > 0.01f)
            MoveDir = Vector2.Normalize(pos - Position);
        Target = pos;
    }

    public void Update(float dt)
    {
        Position = Vector2.Lerp(Position, Target, MathHelper.Clamp(dt * Config.PlayerLerpSpeed, 0, 1));
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Vector2 screenPos)
    {
        var col = IsLocal ? new Color(0.35f, 0.8f, 1f) : new Color(1f, 0.45f, 0.35f);
        DrawPrimitives.FillCircle(sb, screenPos, Radius, col);
        DrawPrimitives.DrawCircleOutline(sb, screenPos, Radius, new Color(0, 0, 0, 0.6f));

        if (IsLocal && MoveDir.LengthSquared() > 0.01f)
        {
            var tip = screenPos + MoveDir * (Radius + 10);
            DrawPrimitives.DrawLine(sb, screenPos, tip, Color.White, 3);
            DrawPrimitives.FillCircle(sb, tip, 3, Color.White);
        }

        var label = font.MeasureString(Name);
        sb.DrawString(font, Name, screenPos + new Vector2(-label.X / 2, -Radius - 22), Color.White);
    }

    public static PlayerEntity FromState(PlayerState s, bool isLocal) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Position = new Vector2((float)s.X, (float)s.Y),
        Target = new Vector2((float)s.X, (float)s.Y),
        IsLocal = isLocal,
    };
}
