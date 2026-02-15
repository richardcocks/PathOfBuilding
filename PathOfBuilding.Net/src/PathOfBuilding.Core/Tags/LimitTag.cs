namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Caps the modifier's accumulated value at a maximum.
/// Lua: { type = "Limit", limit = 100 }
/// </summary>
public sealed class LimitTag : ModTag
{
    public double? Limit { get; init; }
    public string? LimitVar { get; init; }

    public override bool Equals(ModTag? other) =>
        other is LimitTag lt &&
        Limit == lt.Limit &&
        LimitVar == lt.LimitVar;

    public override int GetHashCode() => HashCode.Combine("Limit", Limit, LimitVar);
    public override string ToString() => $"Limit({Limit?.ToString() ?? LimitVar ?? "?"})";
}
