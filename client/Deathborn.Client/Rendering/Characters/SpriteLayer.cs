using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

public readonly struct SpriteLayer(Texture2D texture, CharacterLayerId layerId, Color? tint = null)
{
    public Texture2D Texture { get; } = texture;
    public CharacterLayerId LayerId { get; } = layerId;
    public Color Tint { get; } = tint ?? Color.White;
}
