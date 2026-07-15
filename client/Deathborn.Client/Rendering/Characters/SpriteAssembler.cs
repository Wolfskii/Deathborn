using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Resolves equipped Farm RPG layers for the current clip.</summary>
public sealed class SpriteAssembler
{
    private readonly SpriteLayer[] _buffer = new SpriteLayer[16];
    private int _layerCount;

    public ReadOnlySpan<SpriteLayer> Assemble(
        in CharacterAppearance appearance,
        in CharacterEquipment equipment,
        CharacterClip clip)
    {
        _layerCount = 0;

        TryAddFarmTexture(FarmRpgCharacterSprites.SkinLayerId(appearance.SkinTone), clip, CharacterLayerId.Body);
        TryAddFarmTexture(FarmRpgCharacterSprites.EyesLayerId(appearance.EyeColor), clip, CharacterLayerId.Eyes);
        TryAddEquipment(equipment.ChestId, clip);
        TryAddEquipment(appearance.HairStyleId, clip);
        TryAddEquipment(equipment.WeaponId, clip);

        if (clip == CharacterClip.Cast)
        {
            TryAddFarmTexture("weapon-staff", clip, CharacterLayerId.Weapon);
            TryAddFarmTexture("fx-cast", clip, CharacterLayerId.SpellEffect);
        }

        return _buffer.AsSpan(0, _layerCount);
    }

    private void TryAddFarmTexture(string? layerId, CharacterClip clip, CharacterLayerId slot)
    {
        var source = FarmRpgCharacterSprites.TryGetLayer(layerId, clip);
        if (source == null)
            return;
        _buffer[_layerCount++] = new SpriteLayer(source, slot);
    }

    private void TryAddEquipment(string? itemId, CharacterClip clip)
    {
        if (string.IsNullOrEmpty(itemId))
            return;
        if (!CharacterLayerCatalog.TryGetItem(itemId, out _))
            return;

        var source = CharacterLayerCatalog.TryGetTexture(itemId, clip);
        if (source == null)
            return;

        _buffer[_layerCount++] = new SpriteLayer(source, CharacterLayerCatalog.GetLayerId(itemId));
    }
}
