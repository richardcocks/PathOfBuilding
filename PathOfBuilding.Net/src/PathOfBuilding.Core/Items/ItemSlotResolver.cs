using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Items;

/// <summary>
/// Resolves active ItemSet slot assignments to ParsedItem instances.
/// </summary>
public static class ItemSlotResolver
{
    public static readonly string[] EquipmentSlots =
    {
        "Weapon 1", "Weapon 2", "Helmet", "Body Armour",
        "Gloves", "Boots", "Amulet", "Ring 1", "Ring 2", "Belt"
    };

    public static readonly string[] FlaskSlots =
    {
        "Flask 1", "Flask 2", "Flask 3", "Flask 4", "Flask 5"
    };

    private static readonly HashSet<string> EquipmentSlotSet = new(EquipmentSlots);

    public static Dictionary<string, ParsedItem> Resolve(
        BuildData build,
        Dictionary<int, ParsedItem> parsedItems)
    {
        var result = new Dictionary<string, ParsedItem>();

        // Find slot assignments: prefer active ItemSet, fall back to DefaultSlots
        var slots = GetActiveSlots(build);

        foreach (var slot in slots)
        {
            // Skip non-equipment slots (flasks, jewels, abyssal sockets, swaps)
            if (!EquipmentSlotSet.Contains(slot.SlotName))
                continue;

            if (slot.ItemId <= 0)
                continue;

            if (parsedItems.TryGetValue(slot.ItemId, out var item))
                result[slot.SlotName] = item;
        }

        return result;
    }

    private static List<SlotAssignment> GetActiveSlots(BuildData build)
    {
        // Try to find the ItemSet matching ActiveItemSet
        foreach (var itemSet in build.ItemSets)
        {
            if (itemSet.Id == build.ActiveItemSet)
                return itemSet.Slots;
        }

        // Fall back to default slots
        return build.DefaultSlots;
    }
}
