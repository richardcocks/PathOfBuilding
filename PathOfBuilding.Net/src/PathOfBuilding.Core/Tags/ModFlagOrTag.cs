using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Requires ANY of the specified ModFlags to be present (OR logic).
/// Unlike the default ModFlag matching (AND), this allows alternative matching.
/// Lua: { type = "ModFlagOr", modFlags = bor(ModFlag.Axe, ModFlag.Sword) }
/// </summary>
public sealed class ModFlagOrTag : ModTag
{
    public ModFlag ModFlags { get; init; }

    public override bool Equals(ModTag? other) =>
        other is ModFlagOrTag mf && ModFlags == mf.ModFlags;

    public override int GetHashCode() => HashCode.Combine("ModFlagOr", ModFlags);
    public override string ToString() => $"ModFlagOr({ModFlags})";
}
