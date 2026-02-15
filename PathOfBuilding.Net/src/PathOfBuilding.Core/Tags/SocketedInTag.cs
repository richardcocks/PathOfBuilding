namespace PathOfBuilding.Core.Tags;

/// <summary>
/// Gates the modifier on properties of gems socketed in a specific slot.
/// Lua: { type = "SocketedIn", slotName = "Weapon 1", keyword = "Aura" }
/// </summary>
public sealed class SocketedInTag : ModTag
{
    public string? SlotName { get; init; }
    public string? Keyword { get; init; }
    public string? SocketColor { get; init; }

    /// <summary>
    /// Socket constraint: null = any, "all" = all sockets, int = max count, int[] = specific positions.
    /// Stored as object to handle the polymorphic Lua semantics.
    /// </summary>
    public object? Sockets { get; init; }

    public override bool Equals(ModTag? other) =>
        other is SocketedInTag si &&
        SlotName == si.SlotName &&
        Keyword == si.Keyword &&
        SocketColor == si.SocketColor;

    public override int GetHashCode() => HashCode.Combine("SocketedIn", SlotName, Keyword, SocketColor);
    public override string ToString() => $"SocketedIn({SlotName}{(Keyword != null ? $",keyword={Keyword}" : "")}{(SocketColor != null ? $",color={SocketColor}" : "")})";
}
