namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Base class for all modifier tags. Tags provide conditional application,
/// multipliers, thresholds, and other contextual modifiers.
/// Each tag is evaluated during ModStore.EvalMod() and can modify or gate
/// the modifier's effective value.
/// </summary>
public abstract class ModTag : IEquatable<ModTag>
{
    /// <summary>
    /// Optional actor redirection (e.g., "parent", "enemy").
    /// When set, lookups are redirected to a different actor's ModStore.
    /// </summary>
    public string? Actor { get; init; }

    /// <summary>
    /// When true, inverts the tag's boolean result (for Condition/SkillType/SkillName tags).
    /// </summary>
    public bool Neg { get; init; }

    public abstract bool Equals(ModTag? other);
    public override bool Equals(object? obj) => obj is ModTag tag && Equals(tag);
    public abstract override int GetHashCode();
    public abstract override string ToString();
}
