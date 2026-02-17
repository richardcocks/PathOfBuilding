namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Tags a modifier as part of a named global effect (buff, debuff, aura, curse).
/// Lua: { type = "GlobalEffect", effectType = "Buff", effectName = "Onslaught" }
/// </summary>
public sealed class GlobalEffectTag : ModTag
{
    public string? EffectType { get; init; }
    public string? EffectName { get; init; }
    public string? EffectCond { get; init; }
    public bool Unscalable { get; init; }

    public override bool Equals(ModTag? other) =>
        other is GlobalEffectTag ge &&
        EffectType == ge.EffectType &&
        EffectName == ge.EffectName &&
        EffectCond == ge.EffectCond &&
        Unscalable == ge.Unscalable;

    public override int GetHashCode() => HashCode.Combine("GlobalEffect", EffectType, EffectName);
    public override string ToString() => $"GlobalEffect({EffectType},{EffectName ?? "?"})";
}
