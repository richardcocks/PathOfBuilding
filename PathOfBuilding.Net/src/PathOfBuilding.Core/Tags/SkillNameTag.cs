namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on the active skill's name.
/// Lua: { type = "SkillName", skillName = "Fireball" }
/// </summary>
public sealed class SkillNameTag : ModTag
{
    public string? SkillName { get; init; }
    public IReadOnlyList<string>? SkillNameList { get; init; }
    public bool IncludeTransfigured { get; init; }
    public bool SummonSkill { get; init; }

    public override bool Equals(ModTag? other) =>
        other is SkillNameTag sn &&
        SkillName == sn.SkillName &&
        IncludeTransfigured == sn.IncludeTransfigured &&
        SummonSkill == sn.SummonSkill &&
        Neg == sn.Neg;

    public override int GetHashCode() => HashCode.Combine("SkillName", SkillName, Neg);
    public override string ToString() => $"SkillName({SkillName ?? string.Join("|", SkillNameList ?? [])}{(Neg ? ",neg" : "")})";
}
