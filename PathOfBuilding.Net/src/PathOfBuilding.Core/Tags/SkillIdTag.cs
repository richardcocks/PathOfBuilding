namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on the active skill's granted effect ID.
/// Lua: { type = "SkillId", skillId = "SomeGrantedEffectId" }
/// </summary>
public sealed class SkillIdTag : ModTag
{
    public required string SkillId { get; init; }

    public override bool Equals(ModTag? other) =>
        other is SkillIdTag si && SkillId == si.SkillId;

    public override int GetHashCode() => HashCode.Combine("SkillId", SkillId);
    public override string ToString() => $"SkillId({SkillId})";
}
