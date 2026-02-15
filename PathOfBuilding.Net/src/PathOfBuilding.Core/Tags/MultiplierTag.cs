namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Multiplies the modifier's value by a multiplier variable (e.g., "per Power Charge").
/// Lua: { type = "Multiplier", var = "PowerCharge", div = 1, limit = 10 }
/// </summary>
public sealed class MultiplierTag : ModTag
{
    public string? Var { get; init; }
    public IReadOnlyList<string>? VarList { get; init; }
    public double? Div { get; init; }
    public string? DivVar { get; init; }
    public double? Limit { get; init; }
    public bool LimitTotal { get; init; }
    public bool LimitNegTotal { get; init; }
    public double? Base { get; init; }
    public bool NoFloor { get; init; }
    public bool Invert { get; init; }
    public double? GlobalLimit { get; init; }
    public string? GlobalLimitKey { get; init; }
    public string? LimitActor { get; init; }
    public string? LimitVar { get; init; }

    public override bool Equals(ModTag? other) =>
        other is MultiplierTag mt &&
        Var == mt.Var &&
        Div == mt.Div &&
        Limit == mt.Limit &&
        LimitTotal == mt.LimitTotal &&
        Base == mt.Base &&
        GlobalLimit == mt.GlobalLimit &&
        GlobalLimitKey == mt.GlobalLimitKey &&
        Actor == mt.Actor;

    public override int GetHashCode() => HashCode.Combine("Multiplier", Var, Div, Limit);
    public override string ToString() => $"Multiplier({Var}{(Div.HasValue ? $"/div={Div}" : "")}{(Limit.HasValue ? $"/limit={Limit}" : "")})";
}
