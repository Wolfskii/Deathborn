namespace Deathborn.Client.Gameplay;

/// <summary>Wearable cosmetic collectibles (head slot).</summary>
public static class CosmeticCatalog
{
    public static readonly ItemInfo[] All =
    [
        Cosmetic("santa_hat", "Santa Hat", "A festive red hat. Somehow still cheerful in this cursed world."),
        Cosmetic("party_hat", "Party Hat", "Cone of denial. The apocalypse can wait five minutes."),
        Cosmetic("jester_cap", "Jester Cap", "Bell-tipped cap for laughing at your own permadeath."),
        Cosmetic("bucket_helmet", "Bucket Helmet", "Classic improvised head protection. Very fashionable."),
        Cosmetic("pirate_hat", "Pirate Hat", "Yo ho ho and a bottle of antidote."),
        Cosmetic("propeller_hat", "Propeller Beanie", "Spinning hope. Does not grant flight."),
        Cosmetic("bunny_ears", "Bunny Ears", "Floppy ears of questionable origin."),
        Cosmetic("top_hat", "Top Hat", "Formal wear for funerals you plan to survive."),
        Cosmetic("traffic_cone", "Traffic Cone", "High-visibility headwear. OSHA approved (probably)."),
        Cosmetic("beer_helm", "Beer Helm", "Dual tank helmet. Hydration is a lifestyle."),
        Cosmetic("wizard_hat_torn", "Torn Wizard Hat", "Mystical, frayed, smells like ash."),
        Cosmetic("crown_of_bones", "Crown of Bones", "Tiny ribcage tiara. Edgy but regal."),
        Cosmetic("rubber_chicken_hat", "Rubber Chicken Hat", "Squeaky crest of shame."),
        Cosmetic("grim_hood", "Grim Hood", "Deep hood for brooding professionals."),
        Cosmetic("gold_helm_rusty", "Rusty Gold Helm", "Once glorious. Now mostly tetanus."),
        Cosmetic("fedora_of_shame", "Fedora of Shame", "M'lady... we're all dying anyway."),
        Cosmetic("clown_nose_glasses", "Clown Disguise", "Glasses, nose, and despair."),
        Cosmetic("severed_elf_hat", "Severed Elf Hat", "Tiny green hat. The elf is fine. Probably."),
    ];

    private static ItemInfo Cosmetic(string id, string name, string description) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        Kind = ItemKind.Cosmetic,
        MaxStack = 1,
    };
}
