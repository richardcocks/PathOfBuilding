namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on a stat meeting a threshold (e.g., "while you have at least 200 Strength").
/// Lua: { type = "StatThreshold", stat = "Str", threshold = 200 }
/// </summary>
public sealed class StatThresholdTag : ModTag
{
    public string? Stat { get; init; }
    public double Threshold { get; init; }
    public string? ThresholdStat { get; init; }
    public double? ThresholdPercent { get; init; }
    public bool Upper { get; init; }

    public override bool Equals(ModTag? other) =>
        other is StatThresholdTag st &&
        Stat == st.Stat &&
        Threshold == st.Threshold &&
        Upper == st.Upper &&
        Actor == st.Actor;

    public override int GetHashCode() => HashCode.Combine("StatThreshold", Stat, Threshold);
    public override string ToString() => $"StatThreshold({Stat},{(Upper ? "upper=" : "")}{Threshold})";
}
