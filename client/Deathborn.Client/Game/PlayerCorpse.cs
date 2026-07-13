using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;
using Deathborn.Client.Rendering.Characters;

namespace Deathborn.Client.Gameplay;

/// <summary>Dead body left on the ground after the death animation finishes.</summary>
public sealed class PlayerCorpse
{
    public Vector2 Position;
    public Vector2 FacingDir = new(0, 1);
    private readonly CharacterVisual _visual;

    public PlayerCorpse(Vector2 position, Vector2 facingDir)
    {
        Position = position;
        FacingDir = facingDir.LengthSquared() > 0.01f ? Vector2.Normalize(facingDir) : FacingDir;
        _visual = CharacterVisual.CreateCorpse(FacingDir);
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var scale = PlayerEntity.SpriteDrawScale * zoom;
        _visual.Draw(sb, screenPos, Color.White, scale);

        var markerY = screenPos.Y + (-PlayerEntity.Radius - 28f) * zoom;
        CorpseMarkerDraw.DrawCorpseMarker(sb, new Vector2(screenPos.X, markerY), zoom);
    }
}
