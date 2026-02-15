using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Helper functions for the calculation engine.
/// Ported from CalcTools.lua: calcLib.mod(), calcLib.val(), round().
/// </summary>
public static class CalcLib
{
    /// <summary>
    /// Calculate combined INC/MORE modifier for the given stat names.
    /// Returns (1 + Sum(INC)/100) * More().
    /// Ported from calcLib.mod().
    /// </summary>
    public static double Mod(ModStore modStore, ModConfig? cfg, params string[] statNames)
    {
        return (1 + modStore.Sum(ModType.Inc, cfg, statNames) / 100.0) * modStore.More(cfg, statNames);
    }

    /// <summary>
    /// Calculate the value of a stat: base * mod().
    /// Returns 0 if base is 0.
    /// Ported from calcLib.val().
    /// </summary>
    public static double Val(ModStore modStore, string name, ModConfig? cfg = null)
    {
        double baseVal = modStore.Sum(ModType.Base, cfg, name);
        if (baseVal != 0)
            return baseVal * Mod(modStore, cfg, name);
        return 0;
    }

    /// <summary>
    /// Lua-style rounding: Math.Floor(val + 0.5).
    /// This is NOT the same as C# Math.Round which uses banker's rounding.
    /// </summary>
    public static double Round(double val)
    {
        return Math.Floor(val + 0.5);
    }

    /// <summary>
    /// Lua-style rounding with decimal precision.
    /// </summary>
    public static double Round(double val, int decimals)
    {
        double mult = Math.Pow(10, decimals);
        return Math.Floor(val * mult + 0.5) / mult;
    }
}
