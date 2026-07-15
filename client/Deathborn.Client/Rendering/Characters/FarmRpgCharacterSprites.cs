using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering.Characters;

/// <summary>Loads Farm RPG modular layer sheets installed under Content/Characters/FarmRpg.</summary>
public static class FarmRpgCharacterSprites
{
    private static ContentManager? _content;
    private static readonly Dictionary<string, Texture2D?> Cache = new(StringComparer.Ordinal);

    public static void Load(ContentManager content) => _content = content;

    public static Texture2D? TryGetLayer(string? layerId, CharacterClip clip)
    {
        if (_content == null || string.IsNullOrEmpty(layerId))
            return null;

        var key = $"{layerId}:{clip}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var path = $"Characters/FarmRpg/layers/{layerId}/{ClipFileName(clip)}";
        try
        {
            var texture = _content.Load<Texture2D>(path);
            Cache[key] = texture;
            return texture;
        }
        catch (ContentLoadException)
        {
            Cache[key] = null;
            return null;
        }
    }

    public static string SkinLayerId(SkinTone tone) => tone switch
    {
        SkinTone.Fair => "skin-1",
        SkinTone.Tan => "skin-2",
        SkinTone.Olive => "skin-3",
        SkinTone.Dark => "skin-4",
        _ => "skin-1",
    };

    public static string EyesLayerId(EyeColor color) => color switch
    {
        EyeColor.Blue => "eyes-male-blue",
        EyeColor.Green => "eyes-male-green",
        EyeColor.Gray or EyeColor.Hazel => "eyes-male-black",
        _ => "eyes-male-brown",
    };

    private static string ClipFileName(CharacterClip clip) => clip switch
    {
        CharacterClip.Idle => "idle",
        CharacterClip.Cast => "cast",
        CharacterClip.Walk => "walk",
        CharacterClip.Run or CharacterClip.Roll => "run",
        CharacterClip.Attack => "attack",
        CharacterClip.Hurt => "hurt",
        CharacterClip.Death => "death",
        _ => "idle",
    };
}
