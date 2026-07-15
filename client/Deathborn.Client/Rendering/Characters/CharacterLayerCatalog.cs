using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Maps Farm RPG layer item ids to draw slots. Missing assets are skipped gracefully.</summary>
public static class CharacterLayerCatalog
{
    public readonly record struct LayerItem(CharacterLayerId LayerId, string? PaletteSlot);

    private static readonly Dictionary<string, LayerItem> Items = new(StringComparer.Ordinal)
    {
        ["farm-hair-josh-brown"] = new(CharacterLayerId.FrontHair, null),
        ["farm-hair-josh-black"] = new(CharacterLayerId.FrontHair, null),
        ["farm-hair-josh-blonde"] = new(CharacterLayerId.FrontHair, null),
        ["farm-hair-josh-ginger"] = new(CharacterLayerId.FrontHair, null),
        ["farm-outfit-blue"] = new(CharacterLayerId.Chest, null),
        ["farm-outfit-green"] = new(CharacterLayerId.Chest, null),
        ["farm-outfit-red"] = new(CharacterLayerId.Chest, null),
        ["farm-sword"] = new(CharacterLayerId.Weapon, null),
    };

    private static readonly Dictionary<string, string> FarmRpgLayerIds = new(StringComparer.Ordinal)
    {
        ["farm-hair-josh-brown"] = "hair-josh-brown",
        ["farm-hair-josh-black"] = "hair-josh-black",
        ["farm-hair-josh-blonde"] = "hair-josh-blonde",
        ["farm-hair-josh-ginger"] = "hair-josh-ginger",
        ["farm-outfit-blue"] = "outfit-farm-blue",
        ["farm-outfit-green"] = "outfit-farm-green",
        ["farm-outfit-red"] = "outfit-farm-red",
        ["farm-sword"] = "weapon-sword",
    };

    public static bool TryGetItem(string? itemId, out LayerItem item)
    {
        item = default;
        return !string.IsNullOrEmpty(itemId) && Items.TryGetValue(itemId, out item);
    }

    public static CharacterLayerId GetLayerId(string itemId) =>
        Items.TryGetValue(itemId, out var item) ? item.LayerId : CharacterLayerId.Chest;

    public static Texture2D? TryGetTexture(string? itemId, CharacterClip clip)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        if (FarmRpgLayerIds.TryGetValue(itemId, out var farmLayerId))
            return FarmRpgCharacterSprites.TryGetLayer(farmLayerId, clip);

        return null;
    }
}
