using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Skill cooldown, warcry cast time, and skill duration calculations.
/// Ported from CalcOffence.lua: calcSkillCooldown (277-290), calcWarcryCastTime (292-301),
/// calcSkillDuration (303-316).
/// </summary>
public static class CalcSpeed
{
    /// <summary>
    /// Calculate skill cooldown, optionally rounding to server ticks.
    /// </summary>
    /// <param name="modList">Modifier store.</param>
    /// <param name="cfg">Optional modifier config.</param>
    /// <param name="baseCooldown">Base cooldown in seconds.</param>
    /// <param name="storedUses">Number of stored uses (>1 disables tick rounding).</param>
    /// <param name="additionalCooldownUses">Additional cooldown uses from mods.</param>
    /// <returns>(cooldown, rounded) — cooldown in seconds and whether it was rounded to server ticks.</returns>
    public static (double Cooldown, bool Rounded) CalcSkillCooldown(
        ModStore modList,
        ModConfig? cfg,
        double baseCooldown,
        int storedUses = 1,
        double additionalCooldownUses = 0)
    {
        var cooldownOverride = modList.Override(cfg, "CooldownRecovery");
        double addedCooldown = modList.Sum(ModType.Base, cfg, "CooldownRecovery");
        double cooldown = cooldownOverride?.AsNumber()
            ?? (baseCooldown + addedCooldown) / Math.Max(0.001, CalcLib.Mod(modList, cfg, "CooldownRecovery"));

        if (storedUses > 1 || additionalCooldownUses > 0)
        {
            return (cooldown, false);
        }

        cooldown = Math.Ceiling(cooldown * MiscConstants.ServerTickRate) / MiscConstants.ServerTickRate;
        return (cooldown, true);
    }

    /// <summary>
    /// Calculate warcry cast time.
    /// Returns 0 for instant warcries.
    /// </summary>
    /// <param name="modList">Modifier store.</param>
    /// <param name="cfg">Optional modifier config.</param>
    /// <param name="actionSpeedMod">Action speed modifier from CalcPerform.</param>
    /// <param name="instant">Whether the warcry is instant.</param>
    /// <returns>Cast time in seconds (0 = instant).</returns>
    public static double CalcWarcryCastTime(
        ModStore modList,
        ModConfig? cfg,
        double actionSpeedMod,
        bool instant = false)
    {
        if (instant)
            return 0;

        double baseTime = modList.Sum(ModType.Base, cfg, "WarcryCastTime");
        if (baseTime <= 0)
            return 0;

        double baseSpeed = 1.0 / baseTime;
        double warcryCastTime = baseSpeed * CalcLib.Mod(modList, cfg, "WarcrySpeed") * actionSpeedMod;
        warcryCastTime = Math.Min(warcryCastTime, MiscConstants.ServerTickRate);
        return 1.0 / warcryCastTime;
    }

    /// <summary>
    /// Calculate skill duration.
    /// </summary>
    /// <param name="modList">Modifier store.</param>
    /// <param name="cfg">Optional modifier config.</param>
    /// <param name="baseDuration">Base duration in seconds.</param>
    /// <param name="enemyDB">Optional enemy ModStore for debuff expiration.</param>
    /// <param name="isDebuff">Whether the skill is a debuff (applies enemy expiration).</param>
    /// <param name="useEffective">Whether to apply effective mode (enemy buff expiration).</param>
    /// <returns>Duration in seconds.</returns>
    public static double CalcSkillDuration(
        ModStore modList,
        ModConfig? cfg,
        double baseDuration,
        ModStore? enemyDB = null,
        bool isDebuff = false,
        bool useEffective = false)
    {
        double durationMod = Math.Max(CalcLib.Mod(modList, cfg, "Duration", "PrimaryDuration"), 0);
        double durationBase = baseDuration + modList.Sum(ModType.Base, cfg, "Duration", "PrimaryDuration");
        double duration = durationBase * durationMod;

        if (isDebuff && useEffective && enemyDB != null)
        {
            double debuffDurationMult = 1.0 / Math.Max(
                MiscConstants.BuffExpirationSlowCap,
                CalcLib.Mod(enemyDB, cfg, "BuffExpireFaster"));
            duration *= debuffDurationMult;
        }

        return duration;
    }
}
