namespace Deathborn.Client.Rendering.Characters;

/// <summary>Resolves equipped layers for the current clip. Phase 0: single baked body layer.</summary>
public sealed class SpriteAssembler
{
  private readonly SpriteLayer[] _buffer = new SpriteLayer[16];
  private int _layerCount;

  public ReadOnlySpan<SpriteLayer> Assemble(
    in CharacterAppearance appearance,
    in CharacterEquipment equipment,
    CharacterClip clip)
  {
    _ = appearance;
    _ = equipment;

    var texture = CharacterAnimationCatalog.GetTexture(appearance.BodyTypeId, clip);
    _buffer[0] = new SpriteLayer(texture, CharacterLayerId.Body);
    _layerCount = 1;
    return _buffer.AsSpan(0, _layerCount);
  }
}
