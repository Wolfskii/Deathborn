using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Maps layer item ids to content paths and draw slots. Missing assets are skipped gracefully.</summary>
public static class CharacterLayerCatalog
{
    public readonly record struct LayerItem(CharacterLayerId LayerId, string? PaletteSlot);

    private static readonly Dictionary<string, LayerItem> Items = new(StringComparer.Ordinal)
    {
        ["hair-short"] = new(CharacterLayerId.FrontHair, "hair"),
        ["hair-long"] = new(CharacterLayerId.BackHair, "hair"),
        ["starter-legs"] = new(CharacterLayerId.Legs, null),
        ["starter-chest"] = new(CharacterLayerId.Chest, null),
        ["starter-boots"] = new(CharacterLayerId.Boots, null),
        ["iron-sword"] = new(CharacterLayerId.Weapon, null),
        ["wooden-shield"] = new(CharacterLayerId.Shield, null),
    };

    private static readonly Dictionary<(string ItemId, CharacterClip Clip), string> ContentPaths = new()
    {
        [("hair-short", CharacterClip.Idle)] = "Characters/Swordsman V2/layers/hair-short/idle",
        [("hair-short", CharacterClip.Run)] = "Characters/Swordsman V2/layers/hair-short/walk",
        [("hair-short", CharacterClip.Roll)] = "Characters/Swordsman V2/layers/hair-short/walk",
        [("hair-short", CharacterClip.Attack)] = "Characters/Swordsman V2/layers/hair-short/one-handed-attack",
        [("hair-long", CharacterClip.Idle)] = "Characters/Swordsman V2/layers/hair-long/idle",
        [("hair-long", CharacterClip.Run)] = "Characters/Swordsman V2/layers/hair-long/walk",
        [("hair-long", CharacterClip.Roll)] = "Characters/Swordsman V2/layers/hair-long/walk",
        [("hair-long", CharacterClip.Attack)] = "Characters/Swordsman V2/layers/hair-long/one-handed-attack",
        [("starter-legs", CharacterClip.Idle)] = "Characters/Swordsman V2/layers/starter-legs/idle",
        [("starter-legs", CharacterClip.Run)] = "Characters/Swordsman V2/layers/starter-legs/walk",
        [("starter-legs", CharacterClip.Roll)] = "Characters/Swordsman V2/layers/starter-legs/walk",
        [("starter-legs", CharacterClip.Attack)] = "Characters/Swordsman V2/layers/starter-legs/one-handed-attack",
        [("starter-chest", CharacterClip.Idle)] = "Characters/Swordsman V2/layers/starter-chest/idle",
        [("starter-chest", CharacterClip.Run)] = "Characters/Swordsman V2/layers/starter-chest/walk",
        [("starter-chest", CharacterClip.Roll)] = "Characters/Swordsman V2/layers/starter-chest/walk",
        [("starter-chest", CharacterClip.Attack)] = "Characters/Swordsman V2/layers/starter-chest/one-handed-attack",
        [("starter-boots", CharacterClip.Idle)] = "Characters/Swordsman V2/layers/starter-boots/idle",
        [("starter-boots", CharacterClip.Run)] = "Characters/Swordsman V2/layers/starter-boots/walk",
        [("starter-boots", CharacterClip.Roll)] = "Characters/Swordsman V2/layers/starter-boots/walk",
        [("starter-boots", CharacterClip.Attack)] = "Characters/Swordsman V2/layers/starter-boots/one-handed-attack",
        [("iron-sword", CharacterClip.Idle)] = "Characters/Swordsman V2/layers/iron-sword/idle",
        [("iron-sword", CharacterClip.Run)] = "Characters/Swordsman V2/layers/iron-sword/walk",
        [("iron-sword", CharacterClip.Roll)] = "Characters/Swordsman V2/layers/iron-sword/walk",
        [("iron-sword", CharacterClip.Attack)] = "Characters/Swordsman V2/layers/iron-sword/one-handed-attack",
        [("wooden-shield", CharacterClip.Idle)] = "Characters/Swordsman V2/layers/wooden-shield/idle",
        [("wooden-shield", CharacterClip.Run)] = "Characters/Swordsman V2/layers/wooden-shield/walk",
        [("wooden-shield", CharacterClip.Roll)] = "Characters/Swordsman V2/layers/wooden-shield/walk",
        [("wooden-shield", CharacterClip.Attack)] = "Characters/Swordsman V2/layers/wooden-shield/one-handed-shield-attack",
    };

    private static ContentManager? _content;
    private static readonly Dictionary<string, Texture2D?> _cache = new(StringComparer.Ordinal);

    public static void Load(ContentManager content) => _content = content;

    public static bool TryGetItem(string? itemId, out LayerItem item)
    {
        item = default;
        return !string.IsNullOrEmpty(itemId) && Items.TryGetValue(itemId, out item);
    }

    public static Texture2D? TryGetTexture(string? itemId, CharacterClip clip)
    {
        if (_content == null || string.IsNullOrEmpty(itemId))
            return null;

        var key = $"{itemId}:{clip}";
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        if (!ContentPaths.TryGetValue((itemId, clip), out var path))
        {
            _cache[key] = null;
            return null;
        }

        try
        {
            var texture = _content.Load<Texture2D>(path);
            _cache[key] = texture;
            return texture;
        }
        catch (ContentLoadException)
        {
            _cache[key] = null;
            return null;
        }
    }
}
