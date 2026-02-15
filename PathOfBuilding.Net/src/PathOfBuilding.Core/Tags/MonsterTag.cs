namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on the minion's monster tags (e.g., "demon", "undead").
/// Lua: { type = "MonsterTag", monsterTag = "demon" }
/// </summary>
public sealed class MonsterTag : ModTag
{
    public string? MonsterTagValue { get; init; }
    public IReadOnlyList<string>? MonsterTagList { get; init; }

    public override bool Equals(ModTag? other) =>
        other is MonsterTag mt && MonsterTagValue == mt.MonsterTagValue;

    public override int GetHashCode() => HashCode.Combine("MonsterTag", MonsterTagValue);
    public override string ToString() => $"MonsterTag({MonsterTagValue ?? string.Join("|", MonsterTagList ?? [])})";
}
