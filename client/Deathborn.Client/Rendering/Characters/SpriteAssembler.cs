using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Resolves equipped layers for the current clip and applies palette remaps.</summary>
public sealed class SpriteAssembler
{
    private readonly SpriteLayer[] _buffer = new SpriteLayer[16];
    private readonly Dictionary<string, Texture2D> _remapCache = new(StringComparer.Ordinal);
    private int _layerCount;
    private GraphicsDevice? _device;

    private GraphicsDevice Device => _device ??= DeathbornGame.Instance.GraphicsDevice;

    public ReadOnlySpan<SpriteLayer> Assemble(
        in CharacterAppearance appearance,
        in CharacterEquipment equipment,
        CharacterClip clip)
    {
        _layerCount = 0;

        TryAddEquipment(equipment.CapeId, clip);
        TryAddHair(appearance, clip, CharacterLayerId.BackHair);
        TryAddEquipment(equipment.LegsId, clip);
        AddBody(appearance, clip);
        TryAddEquipment(equipment.ChestId, clip);
        TryAddEquipment(equipment.GlovesId, clip);
        TryAddEquipment(equipment.BootsId, clip);
        TryAddHair(appearance, clip, CharacterLayerId.FrontHair);
        TryAddEquipment(equipment.HelmetId, clip);
        TryAddEquipment(equipment.WeaponId, clip);
        TryAddEquipment(equipment.ShieldId, clip);

        return _buffer.AsSpan(0, _layerCount);
    }

    private void AddBody(in CharacterAppearance appearance, CharacterClip clip)
    {
        var source = CharacterAnimationCatalog.GetTexture(appearance.BodyTypeId, clip);
        var texture = RemapTexture(
            source,
            $"body:{appearance.BodyTypeId}:{clip}:{appearance.SkinTone}:{appearance.EyeColor}",
            CharacterColorPresets.BodyRemap(appearance.SkinTone, appearance.EyeColor));
        _buffer[_layerCount++] = new SpriteLayer(texture, CharacterLayerId.Body);
    }

    private void TryAddHair(in CharacterAppearance appearance, CharacterClip clip, CharacterLayerId slot)
    {
        if (string.IsNullOrEmpty(appearance.HairStyleId))
            return;
        if (!CharacterLayerCatalog.TryGetItem(appearance.HairStyleId, out var item))
            return;
        if (item.LayerId != slot)
            return;

        var source = CharacterLayerCatalog.TryGetTexture(appearance.HairStyleId, clip);
        if (source == null)
            return;

        var texture = item.PaletteSlot == "hair"
            ? RemapTexture(
                source,
                $"hair:{appearance.HairStyleId}:{clip}:{appearance.HairColor}",
                CharacterColorPresets.HairRemap(appearance.HairColor))
            : source;

        _buffer[_layerCount++] = new SpriteLayer(texture, item.LayerId);
    }

    private void TryAddEquipment(string? itemId, CharacterClip clip)
    {
        if (string.IsNullOrEmpty(itemId))
            return;
        if (!CharacterLayerCatalog.TryGetItem(itemId, out var item))
            return;

        var source = CharacterLayerCatalog.TryGetTexture(itemId, clip);
        if (source == null)
            return;

        _buffer[_layerCount++] = new SpriteLayer(source, item.LayerId);
    }

    private Texture2D RemapTexture(
        Texture2D source,
        string cacheKey,
        IReadOnlyList<(Microsoft.Xna.Framework.Color From, Microsoft.Xna.Framework.Color To)> map)
    {
        if (_remapCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var remapped = CharacterPalette.Remap(source, Device, map);
        _remapCache[cacheKey] = remapped;
        return remapped;
    }
}
