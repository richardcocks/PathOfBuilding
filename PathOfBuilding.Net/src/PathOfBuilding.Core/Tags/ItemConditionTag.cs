namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on properties of an equipped item.
/// Lua: { type = "ItemCondition", itemSlot = "Weapon 1", searchCond = "increased physical damage" }
/// </summary>
public sealed class ItemConditionTag : ModTag
{
    public string? ItemSlot { get; init; }
    public string? SearchCond { get; init; }
    public string? RarityCond { get; init; }
    public bool? CorruptedCond { get; init; }
    public bool? ShaperCond { get; init; }
    public bool? ElderCond { get; init; }
    public string? NameCond { get; init; }
    public bool AllSlots { get; init; }
    public bool BothSlots { get; init; }
    public bool ExcludeSelf { get; init; } = true;

    public override bool Equals(ModTag? other) =>
        other is ItemConditionTag ic &&
        ItemSlot == ic.ItemSlot &&
        SearchCond == ic.SearchCond &&
        RarityCond == ic.RarityCond &&
        AllSlots == ic.AllSlots;

    public override int GetHashCode() => HashCode.Combine("ItemCondition", ItemSlot, SearchCond);
    public override string ToString() => $"ItemCondition({ItemSlot ?? "all"},{SearchCond ?? RarityCond ?? "?"})";
}
