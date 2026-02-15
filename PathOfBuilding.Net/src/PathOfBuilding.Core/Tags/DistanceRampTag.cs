namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Linearly interpolates the modifier's value based on distance.
/// Lua: { type = "DistanceRamp", ramp = {{35, 0}, {70, 1}} }
/// </summary>
public sealed class DistanceRampTag : ModTag
{
    /// <summary>
    /// Sorted list of (distance, multiplier) points for linear interpolation.
    /// </summary>
    public required IReadOnlyList<(double Distance, double Multiplier)> Ramp { get; init; }

    public override bool Equals(ModTag? other) =>
        other is DistanceRampTag dt &&
        Ramp.Count == dt.Ramp.Count &&
        Ramp.SequenceEqual(dt.Ramp);

    public override int GetHashCode() => HashCode.Combine("DistanceRamp", Ramp.Count);
    public override string ToString() => $"DistanceRamp({string.Join(",", Ramp.Select(r => $"{r.Distance}:{r.Multiplier}"))})";
}
