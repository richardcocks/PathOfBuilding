namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on a multiplier variable meeting a threshold.
/// Lua: { type = "MultiplierThreshold", var = "PowerCharge", threshold = 3 }
/// Fails if multiplier &lt; threshold (or &gt; threshold if Upper, or != threshold if Equals).
/// </summary>
public sealed class MultiplierThresholdTag : ModTag
{
    public string? Var { get; init; }
    public double Threshold { get; init; }
    public string? ThresholdVar { get; init; }
    public string? ThresholdActor { get; init; }
    public bool Upper { get; init; }
    public bool Equals_ { get; init; }

    public override bool Equals(ModTag? other) =>
        other is MultiplierThresholdTag mt &&
        Var == mt.Var &&
        Threshold == mt.Threshold &&
        Upper == mt.Upper &&
        Equals_ == mt.Equals_ &&
        Actor == mt.Actor;

    public override int GetHashCode() => HashCode.Combine("MultiplierThreshold", Var, Threshold);
    public override string ToString() => $"MultiplierThreshold({Var},{(Upper ? "upper=" : "")}{Threshold})";
}
