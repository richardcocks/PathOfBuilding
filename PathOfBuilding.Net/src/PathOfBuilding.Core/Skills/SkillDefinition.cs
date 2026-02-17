using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Definition of a granted effect (skill or support) from Skills/*.lua.
/// Contains all static data needed for skill calculation.
/// </summary>
public sealed class SkillDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Color { get; init; }
    public double BaseEffectiveness { get; init; }
    public double IncrementalEffectiveness { get; init; }
    public string? Description { get; init; }
    public double CastTime { get; init; }
    public bool Support { get; init; }
    public List<SkillType> RequireSkillTypes { get; init; } = new();
    public List<SkillType> ExcludeSkillTypes { get; init; } = new();
    public List<SkillType> AddSkillTypes { get; init; } = new();
    public string? PlusVersionOf { get; init; }
    public Dictionary<SkillType, bool> SkillTypes { get; init; } = new();
    public Dictionary<string, bool> BaseFlags { get; init; } = new();
    public Dictionary<string, StatMapEntry> StatMap { get; init; } = new();
    public List<Mod> BaseMods { get; init; } = new();
    public List<string> Stats { get; init; } = new();
    public List<(string stat, double value)> ConstantStats { get; init; } = new();
    public Dictionary<string, List<(string stat, double value)>> QualityStats { get; init; } = new();
    public Dictionary<int, SkillLevelData> Levels { get; init; } = new();
    public List<SkillPartDef> Parts { get; init; } = new();
    public bool HasPreDamageFunc { get; init; }
}
