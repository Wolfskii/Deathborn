using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;
using Deathborn.Client.Rendering.Characters;

namespace Deathborn.Client.Gameplay;

/// <summary>Local ghost spirit that floats above a corpse after death.</summary>
public sealed class GhostEntity
{
    public Vector2 Position;
    public Vector2 MoveDir;
    private readonly CharacterVisual _visual = new();
    private float _bobTimer;

    public const float FlySpeed = 95f;
    public const float FloatHeight = 42f;

    public void Update(float dt)
    {
        _bobTimer += dt;
        if (MoveDir.LengthSquared() > 0.01f)
            Position += Vector2.Normalize(MoveDir) * FlySpeed * dt;

        var face = MoveDir.LengthSquared() > 0.01f ? MoveDir : new Vector2(0, 1);
        _visual.UpdateAnimation(dt, new AnimationInput { FacingDir = face });
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var bob = MathF.Sin(_bobTimer * 3f) * 4f * zoom;
        var drawPos = screenPos + new Vector2(0, bob);
        var scale = PlayerEntity.SpriteDrawScale * zoom;
        var ghostTint = new Color(0.78f, 0.82f, 0.95f, 0.72f);

        DrawPrimitives.FillCircle(sb, drawPos + new Vector2(0, 10f * zoom), 20f * zoom, new Color(0.75f, 0.85f, 1f, 0.12f));
        DrawPrimitives.FillCircle(sb, drawPos + new Vector2(0, 8f * zoom), 16f * zoom, new Color(1f, 1f, 1f, 0.1f));
        _visual.Draw(sb, drawPos, ghostTint, scale);
        DrawPrimitives.DrawCircleOutline(sb, drawPos + new Vector2(0, -6f * zoom), 16f * zoom,
            new Color(0.85f, 0.92f, 1f, 0.45f), 24, 2f);
    }
}
