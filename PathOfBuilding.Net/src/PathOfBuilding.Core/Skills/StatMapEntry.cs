using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// A stat map entry mapping a stat name to one or more mods, with optional div/mult/base.
/// Used in both per-skill statMap and the global SkillStatMap.
/// </summary>
public sealed class StatMapEntry
{
    public List<Mod> Mods { get; init; } = new();
    public double? Div { get; init; }
    public double? Mult { get; init; }
    public double? Base { get; init; }
}
