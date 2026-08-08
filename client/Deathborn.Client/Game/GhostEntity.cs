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
        var moving = MoveDir.LengthSquared() > 0.01f;
        if (moving)
        {
            Position += Vector2.Normalize(MoveDir) * FlySpeed * dt;
            _facing = MoveDir;
        }

        // Soft idle loop (breathing) while the spirit drifts — not a frozen pose.
        _visual.UpdateAnimation(dt, new AnimationInput
        {
            IsMoving = false,
            FacingDir = _facing,
            AnimSpeed = Config.WalkAnimSpeed,
        });
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var scale = CharacterAnimationCatalog.GetDrawScale(_visual.Appearance.BodyTypeId) * zoom;
        var ghostTint = new Color(0.78f, 0.82f, 0.95f, 0.72f);
        var cloudTint = new Color(0.92f, 0.95f, 1f, 0.88f);

        // screenPos is the frame-bottom origin; opaque feet sit above that (Farm RPG inset).
        var feetScreenPos = screenPos - new Vector2(0f, FarmRpgAnimationSpecs.FootBottomInsetPx * scale);
        WorldClouds.DrawVeilSupportClouds(sb, feetScreenPos, zoom, cloudTint);
        _visual.Draw(sb, screenPos, ghostTint, scale);
    }
}
