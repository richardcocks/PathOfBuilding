using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on the active skill having a specific skill type.
/// Lua: { type = "SkillType", skillType = SkillType.Attack }
/// </summary>
public sealed class SkillTypeTag : ModTag
{
    public SkillType? SkillTypeValue { get; init; }
    public IReadOnlyList<SkillType>? SkillTypeList { get; init; }

    public override bool Equals(ModTag? other) =>
        other is SkillTypeTag st &&
        SkillTypeValue == st.SkillTypeValue &&
        Neg == st.Neg;

    public override int GetHashCode() => HashCode.Combine("SkillType", SkillTypeValue, Neg);
    public override string ToString() => $"SkillType({SkillTypeValue}{(Neg ? ",neg" : "")})";
}
