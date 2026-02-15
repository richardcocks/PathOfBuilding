using System.Collections.Concurrent;

namespace PathOfBuilding.Core.Data;

/// <summary>
/// Damage type lists, bit flags, and modifier name caching for the calculation engine.
/// Ported from CalcOffence.lua lines 30-62.
/// </summary>
public static class DamageTypeFlags
{
    /// <summary>
    /// Damage types in conversion order (Physical → Lightning → Cold → Fire → Chaos).
    /// </summary>
    public static readonly string[] DmgTypeList =
        ["Physical", "Lightning", "Cold", "Fire", "Chaos"];

    /// <summary>Bit flag per damage type for modifier name lookup.</summary>
    public const int Physical = 0x01;
    public const int Lightning = 0x02;
    public const int Cold = 0x04;
    public const int Fire = 0x08;
    public const int Elemental = 0x0E; // Lightning | Cold | Fire
    public const int Chaos = 0x10;

    /// <summary>
    /// The order in which flags are checked when building modifier name lists.
    /// </summary>
    private static readonly (string Name, int Flag)[] FlagOrder =
    [
        ("Physical", Physical),
        ("Lightning", Lightning),
        ("Cold", Cold),
        ("Fire", Fire),
        ("Elemental", Elemental),
        ("Chaos", Chaos),
    ];

    /// <summary>Map from damage type name to its bit flag.</summary>
    public static readonly Dictionary<string, int> Flags = new()
    {
        ["Physical"] = Physical,
        ["Lightning"] = Lightning,
        ["Cold"] = Cold,
        ["Fire"] = Fire,
        ["Elemental"] = Elemental,
        ["Chaos"] = Chaos,
    };

    /// <summary>
    /// Cache of modifier name arrays keyed by combined type flags.
    /// Ports the damageStatsForTypes metatable from CalcOffence.lua lines 52-62.
    /// </summary>
    private static readonly ConcurrentDictionary<int, string[]> ModNameCache = new();

    /// <summary>
    /// Get the array of modifier stat names for a given combination of type flags.
    /// E.g., flags=0x01 (Physical) → ["Damage", "PhysicalDamage"]
    /// E.g., flags=0x09 (Physical|Fire) → ["Damage", "PhysicalDamage", "FireDamage", "ElementalDamage"]
    /// </summary>
    public static string[] GetModNames(int typeFlags)
    {
        return ModNameCache.GetOrAdd(typeFlags, BuildModNames);
    }

    private static string[] BuildModNames(int flags)
    {
        var names = new List<string> { "Damage" };
        foreach (var (name, flag) in FlagOrder)
        {
            if ((flags & flag) != 0)
                names.Add(name + "Damage");
        }
        return names.ToArray();
    }

    /// <summary>Whether a damage type name is elemental (Fire, Cold, or Lightning).</summary>
    public static bool IsElemental(string damageType)
        => damageType is "Fire" or "Cold" or "Lightning";

    /// <summary>Get the bit flag for a damage type name.</summary>
    public static int GetFlag(string damageType)
        => Flags.TryGetValue(damageType, out int flag) ? flag : 0;
}
