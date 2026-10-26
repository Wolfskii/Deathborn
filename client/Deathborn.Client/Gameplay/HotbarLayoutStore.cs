namespace Deathborn.Client.Gameplay;

/// <summary>
/// Per-character hotbar layout in local app data, so slot placements survive a restart.
/// Each slot is saved as a token: <c>ability:&lt;id&gt;</c>, <c>item:&lt;itemId&gt;</c> or empty.
/// Item slots are re-linked to wherever that item sits in the inventory on load.
/// </summary>
public static class HotbarLayoutStore
{
    private const string AbilityPrefix = "ability:";
    private const string ItemPrefix = "item:";

    private static string PathFor(long characterId) => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Deathborn",
        "hotbar",
        $"character_{characterId}.cfg");

    /// <summary>Token for one hotbar entry, or null for an empty slot.</summary>
    public static string? Token(Dictionary<string, object>? entry)
    {
        if (entry == null) return null;
        if (entry.GetValueOrDefault("fromInventory") is true
            && entry.GetValueOrDefault("itemId") is string itemId && itemId.Length > 0)
            return ItemPrefix + itemId;
        return entry.GetValueOrDefault(HotbarEntry.IdKey) is string id && id.Length > 0
            ? AbilityPrefix + id
            : null;
    }

    public static void Save(long characterId, Hotbar hotbar)
    {
        if (characterId <= 0) return;
        try
        {
            var path = PathFor(characterId);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var lines = new List<string>(hotbar.Slots.Length + 1) { $"selected={hotbar.SelectedIndex}" };
            for (var i = 0; i < hotbar.Slots.Length; i++)
                lines.Add($"slot{i}={Token(hotbar.Slots[i].Entry) ?? ""}");
            File.WriteAllLines(path, lines);
        }
        catch (IOException)
        {
            // A locked or read-only file must never break gameplay; the layout just is not saved.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Applies the saved layout. Returns false when this character has none yet, so the caller
    /// seeds defaults instead.
    /// </summary>
    public static bool TryRestore(long characterId, Hotbar hotbar, PlayerInventory inventory)
    {
        if (characterId <= 0) return false;
        string[] lines;
        try
        {
            var path = PathFor(characterId);
            if (!File.Exists(path)) return false;
            lines = File.ReadAllLines(path);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        var selected = 0;
        for (var i = 0; i < hotbar.Slots.Length; i++)
            hotbar.AssignSlot(i, null);

        foreach (var line in lines)
        {
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (key == "selected")
            {
                _ = int.TryParse(value, out selected);
                continue;
            }
            if (!key.StartsWith("slot", StringComparison.Ordinal)
                || !int.TryParse(key[4..], out var index)
                || index < 0 || index >= hotbar.Slots.Length)
                continue;
            hotbar.AssignSlot(index, EntryFor(value, inventory));
        }

        hotbar.SelectSlot(Math.Clamp(selected, 0, hotbar.Slots.Length - 1));
        return true;
    }

    private static Dictionary<string, object>? EntryFor(string token, PlayerInventory inventory)
    {
        if (token.StartsWith(AbilityPrefix, StringComparison.Ordinal))
        {
            var id = token[AbilityPrefix.Length..];
            return AbilityCatalog.Get(id) != null ? AbilityCatalog.ToHotbarEntry(id) : null;
        }
        if (token.StartsWith(ItemPrefix, StringComparison.Ordinal))
        {
            var itemId = token[ItemPrefix.Length..];
            if (ItemCatalog.Get(itemId) == null) return null;
            var slot = inventory.FindSlot(itemId);
            return slot >= 0 ? ItemCatalog.ToHotbarEntry(itemId, slot) : null;
        }
        return null;
    }
}
