namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Per-level data for a skill definition.
/// Mixed table with positional stat values and named properties.
/// </summary>
public sealed class SkillLevelData
{
    public int Level { get; init; }
    public List<double> Values { get; init; } = new();
    public double CritChance { get; init; }
    public double DamageEffectiveness { get; init; }
    public int LevelRequirement { get; init; }
    public double ManaMultiplier { get; init; }
    public double ManaCost { get; init; }
    public Dictionary<string, int> Cost { get; init; } = new();
    public List<int> StatInterpolation { get; init; } = new();
    public double BaseMultiplier { get; init; }
    public double AttackSpeedMultiplier { get; init; }
    public double ManaReservationFlat { get; init; }
    public double ManaReservationPercent { get; init; }
    public double LifeReservationFlat { get; init; }
    public double LifeReservationPercent { get; init; }
    public int SoulCost { get; init; }
    public int StoredUses { get; init; }
    public double Cooldown { get; init; }
    public double Duration { get; init; }
    public int VaalSoulGainPreventionDuration { get; init; }
}
