namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Multiplies the modifier's value by a stat value (e.g., "per 10 Strength").
/// Lua: { type = "PerStat", stat = "Str", div = 10 }
/// </summary>
public sealed class PerStatTag : ModTag
{
    public string? Stat { get; init; }
    public IReadOnlyList<string>? StatList { get; init; }
    public double? Div { get; init; }
    public double? Limit { get; init; }
    public bool LimitTotal { get; init; }
    public double? Base { get; init; }

    public override bool Equals(ModTag? other) =>
        other is PerStatTag pt &&
        Stat == pt.Stat &&
        Div == pt.Div &&
        Limit == pt.Limit &&
        Actor == pt.Actor;

    public override int GetHashCode() => HashCode.Combine("PerStat", Stat, Div);
    public override string ToString() => $"PerStat({Stat}{(Div.HasValue ? $"/div={Div}" : "")})";
}
