using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Config;

/// <summary>Target ModDB for config var application.</summary>
public enum ConfigTarget { Player, Enemy }

/// <summary>Pattern for how a config var maps to mods.</summary>
public enum ConfigPattern
{
    ConditionFlag,   // Boolean → sets Condition:{ModName} flag
    Multiplier,      // Number → sets Multiplier[ModName] value
    Override,        // Number → sets Override mod on stat
    FlagMod,         // Boolean → sets a specific ModType.Flag mod
    Custom,          // Custom handler function
}

/// <summary>
/// Describes how a config variable maps to modifier database operations.
/// Data-driven table entry matching Lua ConfigOptions.lua structure.
/// </summary>
public class ConfigVarHandler
{
    public string VarName { get; init; } = "";
    public ConfigPattern Pattern { get; init; }
    public ConfigTarget Target { get; init; } = ConfigTarget.Player;

    // Pattern-specific fields
    public string? ModName { get; init; }
    public ModType ModType { get; init; }
    public string Source { get; init; } = "Config";
    public bool HasCombatTag { get; init; }
    public bool HasEffectiveTag { get; init; }

    // For Custom pattern
    public Action<object, ModDB, ModDB>? CustomApply { get; init; }

    // Implied conditions (e.g., CritRecently → SkillCritRecently)
    public string[]? ImpliedConditions { get; init; }
}
