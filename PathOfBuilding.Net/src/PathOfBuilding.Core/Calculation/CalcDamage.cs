using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Recursive damage calculation and AoE radius utility.
/// Ported from CalcOffence.lua calcDamage (lines 68-138), calcAilmentSourceDamage (141-152),
/// and calcRadius (158-160).
/// </summary>
public static class CalcDamage
{
    /// <summary>
    /// Calculate min/max damage for a given damage type, recursively applying conversions.
    /// Ported from CalcOffence.lua calcDamage().
    /// </summary>
    /// <param name="convTable">The damage conversion table.</param>
    /// <param name="modList">Modifier store for INC/MORE queries.</param>
    /// <param name="cfg">Optional modifier config.</param>
    /// <param name="baseDamage">Dictionary of base damage values (e.g., "PhysicalMinBase", "PhysicalMaxBase").</param>
    /// <param name="damageType">The target damage type to calculate.</param>
    /// <param name="typeFlags">Accumulated type flags for modifier lookups.</param>
    /// <returns>(min, max) damage values.</returns>
    public static (double Min, double Max) CalcDamageMinMax(
        ConversionTable convTable,
        ModStore modList,
        ModConfig? cfg,
        Dictionary<string, double> baseDamage,
        string damageType,
        int typeFlags)
    {
        typeFlags |= DamageTypeFlags.GetFlag(damageType);

        // Recurse into prior types in the conversion chain
        double addMin = 0, addMax = 0;
        var dmgTypeList = DamageTypeFlags.DmgTypeList;
        foreach (string otherType in dmgTypeList)
        {
            if (otherType == damageType)
                break;

            double convMult = convTable[otherType].GetCombined(damageType);
            if (convMult > 0)
            {
                var (min, max) = CalcDamageMinMax(convTable, modList, cfg, baseDamage, otherType, typeFlags);
                addMin += min * convMult;
                addMax += max * convMult;
            }
        }

        if (addMin != 0 && addMax != 0)
        {
            addMin = CalcLib.Round(addMin);
            addMax = CalcLib.Round(addMax);
        }

        baseDamage.TryGetValue($"{damageType}MinBase", out double baseMin);
        baseDamage.TryGetValue($"{damageType}MaxBase", out double baseMax);

        if (baseMin == 0 && baseMax == 0)
            return (addMin, addMax);

        // Combine modifiers
        string[] modNames = DamageTypeFlags.GetModNames(typeFlags);
        double inc = 1 + modList.Sum(ModType.Inc, cfg, modNames) / 100;
        double more = modList.More(cfg, modNames);
        double genericMoreMinDamage = modList.More(cfg, "MinDamage");
        double genericMoreMaxDamage = modList.More(cfg, "MaxDamage");
        double moreMinDamage = modList.More(cfg, $"Min{damageType}Damage");
        double moreMaxDamage = modList.More(cfg, $"Max{damageType}Damage");

        return (
            CalcLib.Round(((baseMin * inc * more) * genericMoreMinDamage + addMin) * moreMinDamage),
            CalcLib.Round(((baseMax * inc * more) * genericMoreMaxDamage + addMax) * moreMaxDamage)
        );
    }

    /// <summary>
    /// Calculate ailment source damage: total damage * remaining fraction after conversions out.
    /// Ported from CalcOffence.lua calcAilmentSourceDamage().
    /// </summary>
    public static (double Min, double Max) CalcAilmentSourceDamage(
        ConversionTable convTable,
        ModStore modList,
        ModConfig? cfg,
        Dictionary<string, double> baseDamage,
        string damageType,
        int typeFlags)
    {
        var (min, max) = CalcDamageMinMax(convTable, modList, cfg, baseDamage, damageType, typeFlags);
        double convMult = convTable[damageType].Mult;
        return (min * convMult, max * convMult);
    }

    /// <summary>
    /// Calculate skill radius from base radius and area modifier.
    /// Ported from CalcOffence.lua calcRadius (lines 158-160).
    /// </summary>
    public static double CalcRadius(double baseRadius, double areaMod)
    {
        return Math.Floor(baseRadius * Math.Floor(100 * Math.Sqrt(areaMod)) / 100);
    }
}
