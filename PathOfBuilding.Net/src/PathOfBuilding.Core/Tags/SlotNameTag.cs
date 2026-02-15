namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on the equipment slot name.
/// Lua: { type = "SlotName", slotName = "Helmet" }
/// </summary>
public sealed class SlotNameTag : ModTag
{
    public string? SlotName { get; init; }
    public IReadOnlyList<string>? SlotNameList { get; init; }

    public override bool Equals(ModTag? other) =>
        other is SlotNameTag sn && SlotName == sn.SlotName;

    public override int GetHashCode() => HashCode.Combine("SlotName", SlotName);
    public override string ToString() => $"SlotName({SlotName ?? string.Join("|", SlotNameList ?? [])})";
}
