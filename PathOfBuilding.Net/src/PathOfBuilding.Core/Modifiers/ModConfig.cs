using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Configuration context passed to modifier queries (Sum, More, Flag, etc.).
/// Carries the skill/slot/flag context that determines which modifiers apply.
/// Ported from the Lua 'cfg' table used throughout the calculation engine.
/// </summary>
public class ModConfig
{
    public ModFlag Flags { get; set; }
    public KeywordFlag KeywordFlags { get; set; }
    public string? Source { get; set; }

    // Skill context
    public string? SkillName { get; set; }
    public string? SummonSkillName { get; set; }
    public int? SkillPart { get; set; }
    public int? SkillDist { get; set; }
    public string? SlotName { get; set; }
    public HashSet<SkillType>? SkillTypes { get; set; }
    public Dictionary<string, bool>? SkillCond { get; set; }
    public Dictionary<string, double>? SkillStats { get; set; }

    /// <summary>Empty config with no filtering. Matches all modifiers.</summary>
    public static readonly ModConfig Empty = new();
}
