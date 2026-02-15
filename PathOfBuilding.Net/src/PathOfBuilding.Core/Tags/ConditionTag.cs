namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on a boolean condition (e.g., "while on Low Life").
/// Lua: { type = "Condition", var = "LowLife" }
/// With varList, matches if ANY condition is true (OR logic).
/// </summary>
public sealed class ConditionTag : ModTag
{
    public string? Var { get; init; }
    public IReadOnlyList<string>? VarList { get; init; }

    public override bool Equals(ModTag? other) =>
        other is ConditionTag ct &&
        Var == ct.Var &&
        Neg == ct.Neg &&
        Actor == ct.Actor &&
        SequenceEqual(VarList, ct.VarList);

    public override int GetHashCode() => HashCode.Combine("Condition", Var, Neg);
    public override string ToString() => $"Condition({Var ?? string.Join("|", VarList ?? [])}{(Neg ? ",neg" : "")})";

    private static bool SequenceEqual(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
