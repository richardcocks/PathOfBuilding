namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Multiplies the modifier's value by a percentage of a stat (e.g., "10% of maximum Mana").
/// Lua: { type = "PercentStat", stat = "Mana", percent = 10 }
/// </summary>
public sealed class PercentStatTag : ModTag
{
    public string? Stat { get; init; }
    public double Percent { get; init; }
    public string? PercentVar { get; init; }
    public bool Floor { get; init; }
    public double? Limit { get; init; }
    public bool LimitTotal { get; init; }

    public override bool Equals(ModTag? other) =>
        other is PercentStatTag pt &&
        Stat == pt.Stat &&
        Percent == pt.Percent &&
        Floor == pt.Floor &&
        Actor == pt.Actor;

    public override int GetHashCode() => HashCode.Combine("PercentStat", Stat, Percent);
    public override string ToString() => $"PercentStat({Stat},{Percent}%)";
}
