using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;
using Deathborn.Client.Rendering.Characters;

namespace Deathborn.Client.Gameplay;

/// <summary>Local ghost spirit that floats freely after death (Walk the veil spectator).</summary>
public sealed class GhostEntity
{
    public Vector2 Position;
    public Vector2 MoveDir;
    private readonly CharacterVisual _visual = new();
    private Vector2 _facing = new(0, 1);

    public const float FlySpeed = 120f;
    public const float FloatHeight = 42f;

    public CharacterVisual Visual => _visual;

    public void Update(float dt)
    {
        if (MoveDir.LengthSquared() > 0.01f)
        {
            Position += Vector2.Normalize(MoveDir) * FlySpeed * dt;
            _facing = MoveDir;
        }

        _visual.HoldIdlePose(_facing);
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var scale = CharacterAnimationCatalog.GetDrawScale(_visual.Appearance.BodyTypeId) * zoom;
        var ghostTint = new Color(0.78f, 0.82f, 0.95f, 0.72f);

        DrawPrimitives.FillCircle(sb, screenPos + new Vector2(0, 10f * zoom), 20f * zoom, new Color(0.75f, 0.85f, 1f, 0.12f));
        DrawPrimitives.FillCircle(sb, screenPos + new Vector2(0, 8f * zoom), 16f * zoom, new Color(1f, 1f, 1f, 0.1f));
        _visual.Draw(sb, screenPos, ghostTint, scale);
        DrawPrimitives.DrawCircleOutline(sb, screenPos + new Vector2(0, -6f * zoom), 16f * zoom,
            new Color(0.85f, 0.92f, 1f, 0.45f), 24, 2f);
    }
}
