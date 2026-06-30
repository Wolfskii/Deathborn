using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

/// <summary>Dead body left on the ground after the death animation finishes.</summary>
public sealed class PlayerCorpse
{
    public Vector2 Position;
    public Vector2 FacingDir = new(0, 1);
    private readonly FourDirectionDeathAnimation _anim;

    public PlayerCorpse(FourDirectionDeathAnimation anim, Vector2 position, Vector2 facingDir)
    {
        _anim = anim;
        Position = position;
        FacingDir = facingDir.LengthSquared() > 0.01f ? Vector2.Normalize(facingDir) : FacingDir;
        _anim.HoldCorpse(FacingDir);
    }

    public void Draw(SpriteBatch sb, Vector2 screenPos, float zoom)
    {
        var scale = PlayerEntity.SpriteDrawScale * zoom;
        _anim.Draw(sb, screenPos, Color.White, scale);
    }
}
