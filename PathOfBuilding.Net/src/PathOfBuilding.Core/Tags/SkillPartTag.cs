namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on the selected skill part (for multi-part skills).
/// Lua: { type = "SkillPart", skillPart = 2 }
/// </summary>
public sealed class SkillPartTag : ModTag
{
    public int? SkillPart { get; init; }
    public IReadOnlyList<int>? SkillPartList { get; init; }

    public override bool Equals(ModTag? other) =>
        other is SkillPartTag sp && SkillPart == sp.SkillPart;

    public override int GetHashCode() => HashCode.Combine("SkillPart", SkillPart);
    public override string ToString() => $"SkillPart({SkillPart})";
}
