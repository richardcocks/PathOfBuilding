namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Scales the modifier based on melee proximity distance.
/// 0-15 units: full effect (Ramp.Near), 16-39: linear fade, 40+: zero.
/// Lua: { type = "MeleeProximity", ramp = {1, 0} }
/// </summary>
public sealed class MeleeProximityTag : ModTag
{
    /// <summary>Value at close range (0-15 units).</summary>
    public double Near { get; init; }

    /// <summary>Value at far range (40+ units).</summary>
    public double Far { get; init; }

    public override bool Equals(ModTag? other) =>
        other is MeleeProximityTag mt &&
        Near == mt.Near &&
        Far == mt.Far;

    public override int GetHashCode() => HashCode.Combine("MeleeProximity", Near, Far);
    public override string ToString() => $"MeleeProximity({Near},{Far})";
}
