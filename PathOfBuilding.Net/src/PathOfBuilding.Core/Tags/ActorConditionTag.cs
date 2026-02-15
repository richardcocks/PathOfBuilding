namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on a condition checked against a specific actor (parent, enemy).
/// Lua: { type = "ActorCondition", actor = "enemy", var = "Frozen" }
/// </summary>
public sealed class ActorConditionTag : ModTag
{
    public string? Var { get; init; }
    public IReadOnlyList<string>? VarList { get; init; }

    public override bool Equals(ModTag? other) =>
        other is ActorConditionTag ct &&
        Var == ct.Var &&
        Neg == ct.Neg &&
        Actor == ct.Actor &&
        SequenceEqual(VarList, ct.VarList);

    public override int GetHashCode() => HashCode.Combine("ActorCondition", Var, Actor);
    public override string ToString() => $"ActorCondition({Actor},{Var ?? string.Join("|", VarList ?? [])})";

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
