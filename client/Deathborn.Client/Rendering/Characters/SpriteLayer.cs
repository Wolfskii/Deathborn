using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

public readonly struct SpriteLayer(Texture2D texture, CharacterLayerId layerId)
{
    public Texture2D Texture { get; } = texture;
    public CharacterLayerId LayerId { get; } = layerId;
}
