using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Runtime skill instance created from a socket group gem.
/// Holds resolved types, flags, support list, configs, and mod list for calculation.
/// Ported from CalcActiveSkill.lua createActiveSkill / buildActiveSkillModList.
/// </summary>
public sealed class ActiveSkill
{
    // Identity
    public required SkillDefinition GrantedEffect { get; init; }
    public required GemInstanceData GemInstance { get; init; }
    public GemDefinition? GemData { get; init; }

    // Resolved types and flags (mutable — supports can add to these)
    public Dictionary<SkillType, bool> SkillTypes { get; set; } = new();
    public Dictionary<string, bool> SkillFlags { get; set; } = new();

    // Effect list: first element is the active effect, rest are supports
    public List<ActiveSkillEffect> EffectList { get; } = new();

    // Skill output data (populated by SkillModListBuilder)
    public Dictionary<string, double> SkillData { get; } = new();

    // Mod configs (populated by SkillModListBuilder)
    public ModConfig? SkillCfg { get; set; }
    public ModList SkillModList { get; set; } = new();

    // State
    public int SkillPart { get; set; } = 1;
    public string? SkillPartName { get; set; }
    public bool Disabled { get; set; }
    public string? DisableReason { get; set; }

    // References
    public Actor? Actor { get; set; }
    public SocketGroupData? SocketGroup { get; set; }
}

/// <summary>
/// Represents a single effect (active or support) applied to an active skill.
/// Each effect references a SkillDefinition and the gem instance that provides it.
/// </summary>
public sealed class ActiveSkillEffect
{
    public required SkillDefinition GrantedEffect { get; init; }
    public required GemInstanceData GemInstance { get; init; }
    public GemDefinition? GemData { get; init; }
}
