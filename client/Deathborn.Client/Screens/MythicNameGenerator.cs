namespace Deathborn.Client.Screens;

/// <summary>Short, readable names inspired by myths and naming traditions.</summary>
internal static class MythicNameGenerator
{
    private readonly record struct NamePattern(string[] Starts, string[] Endings);

    private static readonly string[] MaleNames =
    [
        // Norse
        "Eirik", "Leif", "Harald", "Baldur", "Vidar", "Hakon", "Sten",
        // Slavic
        "Radomir", "Dragomir", "Miroslav", "Bogdan", "Vladimir", "Zoran",
        // Celtic
        "Bran", "Taliesin", "Cian", "Fintan", "Ronan", "Oisin",
        // Greek
        "Orion", "Dorian", "Theron", "Lysander", "Evander", "Nikos",
        // Japanese
        "Akira", "Ren", "Sora", "Daichi", "Hayato", "Riku",
        // West African
        "Ayo", "Sefu", "Kofi", "Jelani", "Tariq", "Bakari",
    ];

    private static readonly string[] FemaleNames =
    [
        // Norse
        "Astrid", "Freyja", "Sigrun", "Solveig", "Ragna", "Ingrid", "Yrsa",
        // Slavic
        "Vesna", "Zorya", "Mila", "Milena", "Danica", "Ljuba",
        // Celtic
        "Niamh", "Rhiannon", "Aine", "Maeve", "Branwen", "Eira",
        // Greek
        "Asteria", "Thalia", "Lyra", "Callista", "Daphne", "Ianthe",
        // Japanese
        "Kaede", "Hikari", "Yuki", "Aoi", "Miyu", "Kohana",
        // West African
        "Nia", "Zuri", "Amara", "Imani", "Asha", "Sade",
    ];

    // Generated names combine roots and endings from the same naming tradition.
    // This provides hundreds of short, pronounceable results without pretending
    // each generated combination is a historical personal name.
    private static readonly NamePattern[] MalePatterns =
    [
        new(["Ar", "Eir", "Hal", "Rag", "Sig", "Thor", "Ulf"], ["bjorn", "rik", "vald", "mund", "stein", "ulf"]),
        new(["Bogo", "Brati", "Dra", "Milo", "Rado", "Vele", "Zlato"], ["mir", "slav", "bor", "dan", "voj", "rad"]),
        new(["Aed", "Bren", "Cael", "Dun", "Fin", "Lugh", "Taran"], ["an", "an", "ric", "nan", "wyn", "os", "ach"]),
        new(["Alex", "Deme", "Heli", "Leand", "Theo", "Xan", "Zeph"], ["ios", "on", "dor", "ros", "dor", "tes", "yr"]),
        new(["Aki", "Haru", "Kazu", "Masa", "Rai", "Shin", "Taka"], ["to", "ki", "ya", "ru", "ji", "ro", "shi"]),
        new(["Ade", "Chi", "Jab", "Kam", "Olu", "Tem", "Zan"], ["bayo", "nedi", "ari", "au", "tunde", "ito", "le"]),
    ];

    private static readonly NamePattern[] FemalePatterns =
    [
        new(["Alf", "Astr", "Brynh", "Freyd", "Gud", "Hild", "Sigr"], ["dis", "hild", "run", "frid", "ny", "unn"]),
        new(["Bela", "Dani", "Lada", "Mila", "Rado", "Svet", "Zori"], ["na", "ka", "mira", "slava", "veta", "ana"]),
        new(["Ail", "Bran", "Ceri", "Eil", "Mair", "Rhi", "Teg"], ["wen", "wen", "dwen", "is", "ead", "annon", "wyn"]),
        new(["Ari", "Cali", "Deme", "Evad", "Kass", "Phae", "Theo"], ["adne", "sta", "tra", "ne", "ia", "dra", "dora"]),
        new(["Aka", "Emi", "Hina", "Kiyo", "Mina", "Noz", "Sayo"], ["ne", "ko", "mi", "ka", "ri", "omi", "ka"]),
        new(["Ade", "Ama", "Chi", "Im", "Nko", "Sade", "Zin"], ["ola", "ra", "ma", "ani", "si", "ya", "abu"]),
    ];

    public static string Next(string? gender)
    {
        var female = string.Equals(gender, "female", StringComparison.OrdinalIgnoreCase);
        var curated = female ? FemaleNames : MaleNames;
        if (Random.Shared.Next(3) == 0)
            return curated[Random.Shared.Next(curated.Length)];

        var patterns = female ? FemalePatterns : MalePatterns;
        var pattern = patterns[Random.Shared.Next(patterns.Length)];
        return pattern.Starts[Random.Shared.Next(pattern.Starts.Length)]
            + pattern.Endings[Random.Shared.Next(pattern.Endings.Length)];
    }
}
