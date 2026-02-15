namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Hit damage formula utilities: resistance/penetration application, crit averaging,
/// lucky hits, and double/triple damage effects.
/// Extracted from CalcOffence.lua lines 3090-3506 as standalone static methods.
/// </summary>
public static class CalcHitDamage
{
    /// <summary>
    /// Apply resistance and penetration to base damage.
    /// effMult = (1 - (resist - pen) / 100) * (1 + takenInc / 100) * takenMore
    /// </summary>
    /// <param name="baseDamage">Pre-resistance damage.</param>
    /// <param name="resist">Enemy resistance (0-100+, can be negative).</param>
    /// <param name="pen">Penetration value (reduces effective resistance).</param>
    /// <param name="takenInc">Increased damage taken on enemy (additive %).</param>
    /// <param name="takenMore">More damage taken multiplier on enemy.</param>
    /// <returns>Damage after resistance/pen/taken modifiers.</returns>
    public static double ApplyResistAndPen(double baseDamage, double resist, double pen, double takenInc = 0, double takenMore = 1)
    {
        double effectiveResist = resist - pen;
        double effMult = (1 - effectiveResist / 100) * (1 + takenInc / 100) * takenMore;
        return baseDamage * effMult;
    }

    /// <summary>
    /// Average hit damage combining critical and non-critical hits.
    /// result = nonCritAvg * (1 - critChance/100) + critAvg * (critChance/100)
    /// </summary>
    /// <param name="nonCritAvg">Average non-critical hit damage.</param>
    /// <param name="critAvg">Average critical hit damage.</param>
    /// <param name="critChance">Crit chance as a percentage (0-100).</param>
    /// <returns>Weighted average damage.</returns>
    public static double AverageCritDamage(double nonCritAvg, double critAvg, double critChance)
    {
        return nonCritAvg * (1 - critChance / 100) + critAvg * (critChance / 100);
    }

    /// <summary>
    /// Calculate effective critical strike chance.
    /// result = min(baseCritChance * (1 + inc/100) * more, cap)
    /// </summary>
    /// <param name="baseCritChance">Base crit chance (%).</param>
    /// <param name="critChanceInc">Total increased crit chance (%).</param>
    /// <param name="critChanceMore">Total more crit chance multiplier (1.0 = no change).</param>
    /// <param name="critChanceCap">Maximum crit chance (default 100).</param>
    /// <returns>Effective crit chance (%).</returns>
    public static double EffectiveCritChance(double baseCritChance, double critChanceInc = 0, double critChanceMore = 1, double critChanceCap = 100)
    {
        return Math.Min(baseCritChance * (1 + critChanceInc / 100) * critChanceMore, critChanceCap);
    }

    /// <summary>
    /// Calculate effective crit multiplier.
    /// In PoE, crit multi is additive: baseCritMulti + inc.
    /// </summary>
    /// <param name="baseCritMulti">Base crit multiplier (e.g., 150 for 150%).</param>
    /// <param name="inc">Additional crit multiplier (additive, e.g., 50 for +50%).</param>
    /// <returns>Total crit multiplier (%).</returns>
    public static double CritMultiplier(double baseCritMulti, double inc = 0)
    {
        return baseCritMulti + inc;
    }

    /// <summary>
    /// Calculate average damage with lucky/unlucky rolls.
    /// Lucky (luckyChance >= 0): biased toward max (best of N+2 rolls).
    /// Unlucky (luckyChance &lt; 0): biased toward min (worst of N+2 rolls).
    /// luckyChance is a fraction (0-1 for lucky, -1-0 for unlucky).
    /// </summary>
    /// <param name="hitMin">Minimum hit damage.</param>
    /// <param name="hitMax">Maximum hit damage.</param>
    /// <param name="luckyChance">Lucky fraction: positive=lucky, negative=unlucky.</param>
    /// <returns>Averaged damage considering luck.</returns>
    public static double LuckyDamage(double hitMin, double hitMax, double luckyChance)
    {
        double rolls = Math.Abs(luckyChance) + 2;
        double avgNotLucky = hitMin / rolls + hitMax / rolls;
        double avgLucky = hitMin / rolls + (rolls - 1) * hitMax / rolls;
        double avgUnlucky = (rolls - 1) * hitMin / rolls + hitMax / rolls;

        if (luckyChance >= 0)
            return avgNotLucky * (1 - luckyChance) + avgLucky * luckyChance;
        else
            return avgNotLucky * (1 - Math.Abs(luckyChance)) + avgUnlucky * Math.Abs(luckyChance);
    }

    /// <summary>
    /// Calculate the effective damage multiplier from double/triple damage chances.
    /// Each proc adds 100% of base damage.
    /// </summary>
    /// <param name="doubleDmgChance">Chance to deal double damage (0-100%).</param>
    /// <param name="tripleDmgChance">Chance to deal triple damage (0-100%).</param>
    /// <returns>Multiplier to apply (1.0 = no bonus).</returns>
    public static double DoubleDamageEffect(double doubleDmgChance, double tripleDmgChance = 0)
    {
        return 1 + doubleDmgChance / 100 + tripleDmgChance / 100;
    }
}
