using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Local ghost spirit that floats above a corpse after death.</summary>
public sealed class GhostEntity
{
    public Vector2 Position;
    public Vector2 MoveDir;
    private readonly FourDirectionIdleAnimation _idle = CharacterSprites.CreateSwordsmanIdle();
    private float _bobTimer;

    public const float FlySpeed = 95f;
    public const float FloatHeight = 42f;

    public void Update(float dt)
    {
        _bobTimer += dt;
        if (MoveDir.LengthSquared() > 0.01f)
            Position += Vector2.Normalize(MoveDir) * FlySpeed * dt;

        _idle.Update(dt, MoveDir.LengthSquared() > 0.01f ? MoveDir : new Vector2(0, 1));
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var bob = MathF.Sin(_bobTimer * 3f) * 4f * zoom;
        var drawPos = screenPos + new Vector2(0, bob);
        var scale = PlayerEntity.SpriteDrawScale * zoom;
        var ghostTint = new Color(0.82f, 0.86f, 0.92f, 0.62f);

        DrawPrimitives.FillCircle(sb, drawPos + new Vector2(0, 8f * zoom), 16f * zoom, new Color(1f, 1f, 1f, 0.08f));
        _idle.Draw(sb, drawPos, ghostTint, scale);
        DrawPrimitives.DrawCircleOutline(sb, drawPos + new Vector2(0, -6f * zoom), 14f * zoom,
            new Color(0.9f, 0.95f, 1f, 0.28f), 24, 1.5f);
    }
}
