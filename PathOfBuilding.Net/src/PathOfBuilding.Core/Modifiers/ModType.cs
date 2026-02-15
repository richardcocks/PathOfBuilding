namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// The aggregation type of a modifier.
/// Maps to Lua: "BASE", "INC", "MORE", "FLAG", "OVERRIDE", "LIST", "MAX".
/// </summary>
public enum ModType
{
    /// <summary>Flat additive value (e.g., "+50 to Life").</summary>
    Base,

    /// <summary>Percentage additive increase/decrease (e.g., "25% increased Fire Damage").</summary>
    Inc,

    /// <summary>Percentage multiplicative more/less (e.g., "20% more Spell Damage").</summary>
    More,

    /// <summary>Boolean flag (e.g., "Your hits always Ignite").</summary>
    Flag,

    /// <summary>Replaces the calculated value entirely (e.g., "Your Maximum Resistances are 78%").</summary>
    Override,

    /// <summary>Collects multiple values into a list (e.g., keystones, extra skills).</summary>
    List,

    /// <summary>Finds the maximum value among all matching modifiers.</summary>
    Max,
}
