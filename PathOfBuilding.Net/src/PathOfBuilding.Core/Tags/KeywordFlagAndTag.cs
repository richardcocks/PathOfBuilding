using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Requires ALL of the specified KeywordFlags to be present (AND logic).
/// Lua: { type = "KeywordFlagAnd", keywordFlags = bor(KeywordFlag.Fire, KeywordFlag.Spell) }
/// </summary>
public sealed class KeywordFlagAndTag : ModTag
{
    public KeywordFlag KeywordFlags { get; init; }

    public override bool Equals(ModTag? other) =>
        other is KeywordFlagAndTag kf && KeywordFlags == kf.KeywordFlags;

    public override int GetHashCode() => HashCode.Combine("KeywordFlagAnd", KeywordFlags);
    public override string ToString() => $"KeywordFlagAnd({KeywordFlags})";
}
