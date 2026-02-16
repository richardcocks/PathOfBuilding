using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Items;

/// <summary>
/// Applies parsed item modifier lines to a player's ModDB.
/// Sets source tracking and item count multipliers.
/// </summary>
public static class ItemModApplicator
{
    public static void Apply(
        Dictionary<string, ParsedItem> equippedItems,
        ModDB playerModDB)
    {
        foreach (var (slotName, item) in equippedItems)
        {
            // Add all mods from all mod lines
            foreach (var modLine in item.AllModLines)
            {
                if (modLine.ParseFailed)
                    continue;

                foreach (var mod in modLine.Mods)
                {
                    var itemMod = new Mod
                    {
                        Name = mod.Name,
                        Type = mod.Type,
                        Value = mod.Value,
                        Flags = mod.Flags,
                        KeywordFlags = mod.KeywordFlags,
                        Source = $"Item:{slotName}",
                        Tags = mod.Tags,
                    };
                    playerModDB.AddMod(itemMod);
                }
            }

            SetItemMultipliers(slotName, item, playerModDB);
        }
    }

    private static void SetItemMultipliers(string slotName, ParsedItem item, ModDB modDB)
    {
        // Rarity multipliers
        var rarityKey = item.Rarity switch
        {
            ItemRarity.Unique or ItemRarity.Relic => "UniqueItem",
            ItemRarity.Rare => "RareItem",
            ItemRarity.Magic => "MagicItem",
            _ => "NormalItem",
        };
        modDB.Multipliers[rarityKey] = modDB.Multipliers.GetValueOrDefault(rarityKey) + 1;

        // Slot-type multipliers
        var slotTypeKey = SlotToTypeKey(slotName);
        if (slotTypeKey != null)
            modDB.Multipliers[slotTypeKey] = modDB.Multipliers.GetValueOrDefault(slotTypeKey) + 1;

        // Corrupted item multiplier
        if (item.Corrupted)
            modDB.Multipliers["CorruptedItem"] = modDB.Multipliers.GetValueOrDefault("CorruptedItem") + 1;
    }

    private static string? SlotToTypeKey(string slotName) => slotName switch
    {
        "Helmet" => "HelmetItem",
        "Body Armour" => "BodyArmourItem",
        "Gloves" => "GlovesItem",
        "Boots" => "BootsItem",
        "Amulet" => "AmuletItem",
        "Ring 1" or "Ring 2" => "RingItem",
        "Belt" => "BeltItem",
        "Weapon 1" or "Weapon 2" => "WeaponItem",
        _ => null,
    };
}
