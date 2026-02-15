using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Data;

/// <summary>
/// High-precision rounding rules for specific modifiers.
/// Some modifiers require more decimal places than the default floor() rounding.
/// Ported from data.highPrecisionMods and data.defaultHighPrecision in Data.lua.
/// </summary>
public static class PrecisionData
{
    /// <summary>
    /// Default number of decimal places for mods with fractional values.
    /// </summary>
    public const int DefaultHighPrecision = 1;

    /// <summary>
    /// Specific precision overrides keyed by (modName, modType).
    /// Will be populated as game data is ported.
    /// </summary>
    private static readonly Dictionary<(string Name, ModType Type), int> HighPrecisionMods = new();

    /// <summary>
    /// Gets the number of decimal places for a given mod, or null if default rounding applies.
    /// </summary>
    public static int? GetPrecision(string modName, ModType modType)
    {
        return HighPrecisionMods.TryGetValue((modName, modType), out int precision)
            ? precision
            : null;
    }

    /// <summary>
    /// Applies precision-aware rounding to a value.
    /// If the mod has a specific precision, uses that.
    /// If the value has decimals, uses DefaultHighPrecision.
    /// Otherwise, floors the value.
    /// </summary>
    public static double ApplyPrecision(double value, string modName, ModType modType)
    {
        int? precision = GetPrecision(modName, modType);

        if (precision.HasValue)
        {
            double power = Math.Pow(10, precision.Value);
            return Math.Floor(value * power) / power;
        }

        if (Math.Floor(value) != value)
        {
            double power = Math.Pow(10, DefaultHighPrecision);
            return Math.Floor(value * power) / power;
        }

        return Math.Floor(value);
    }

    /// <summary>
    /// Rounds a value to a specific number of decimal places (floor-based).
    /// </summary>
    public static double RoundToPlaces(double value, int places)
    {
        double power = Math.Pow(10, places);
        return Math.Floor(value * power) / power;
    }
}
