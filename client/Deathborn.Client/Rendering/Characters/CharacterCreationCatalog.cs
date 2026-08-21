namespace Deathborn.Client.Rendering.Characters;

public readonly record struct CharacterRaceOption(string Id, string Name, string Description);

public static class CharacterCreationCatalog
{
    public static readonly CharacterRaceOption[] Races =
    [
        new("human", "Human", "Versatile and ambitious."),
        new("elf", "Elf", "Graceful and insightful."),
        new("dwarf", "Dwarf", "Stout and resilient."),
        new("orc", "Orc", "Strong and fearless."),
        new("halfling", "Halfling", "Small and quick-witted."),
        new("undead", "Undead", "Risen and relentless."),
    ];

    public static readonly string[] HairStyleIds =
    [
        "farm-hair-josh-brown",
        "farm-hair-josh-black",
        "farm-hair-josh-blonde",
        "farm-hair-josh-ginger",
        "farm-hair-lyria-brown",
        "farm-hair-lyria-black",
        "farm-hair-lyria-blonde",
        "farm-hair-lyria-ginger",
    ];

    public static readonly string[] HairStyleNames = ["Josh", "Lyria"];
    private static readonly string[] HairColors = ["brown", "black", "blonde", "ginger"];

    public static string NormalizeRace(string? value) =>
        Races.Any(r => string.Equals(r.Id, value, StringComparison.OrdinalIgnoreCase))
            ? value!.ToLowerInvariant()
            : "human";

    public static string NormalizeHairStyle(string? value) =>
        HairStyleIds.Contains(value, StringComparer.Ordinal) ? value! : HairStyleIds[0];

    public static string HairStyleId(int styleIndex, int colorIndex)
    {
        styleIndex = Math.Clamp(styleIndex, 0, HairStyleNames.Length - 1);
        colorIndex = Math.Clamp(colorIndex, 0, HairColors.Length - 1);
        return $"farm-hair-{HairStyleNames[styleIndex].ToLowerInvariant()}-{HairColors[colorIndex]}";
    }

    public static string NormalizeGender(string? value) =>
        string.Equals(value, "female", StringComparison.OrdinalIgnoreCase) ? "female" : "male";
}
