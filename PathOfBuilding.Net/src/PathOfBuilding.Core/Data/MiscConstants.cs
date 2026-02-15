namespace PathOfBuilding.Core.Data;

/// <summary>
/// Named constants from data.misc (Data.lua lines 163-231).
/// Magic numbers used throughout the calculation engine.
/// </summary>
public static class MiscConstants
{
    public const double ServerTickTime = 0.033;
    public static readonly double ServerTickRate = 1 / ServerTickTime;
    public const double AccuracyPerDexBase = 2;
    public const double LowPoolThreshold = 0.5;
    public const double TemporalChainsEffectCap = 75;
    public const double BuffExpirationSlowCap = 0.25;

    // Resolved from characterConstants / monsterConstants
    public static readonly double DamageReductionCap = GameConstants.Character["maximum_physical_damage_reduction_%"];
    public static readonly double EnemyPhysicalDamageReductionCap = GameConstants.Monster["maximum_physical_damage_reduction_%"];

    public const double ResistFloor = -200;
    public const double MaxResistCap = 90;
    public const double EvadeChanceCap = 95;
    public const double DodgeChanceCap = 75;
    public const double BlockChanceCap = 90;
    public const double SuppressionChanceCap = 100;
    public const double SuppressionEffect = 40;
    public const double AvoidChanceCap = 75;
    public const double FortifyBaseDuration = 6;

    public static readonly double ManaRegenBase = GameConstants.Character["mana_regeneration_rate_per_minute_%"] / 60.0 / 100.0;
    // Note: Lua source overrides calculated value with 0.33
    public const double EnergyShieldRechargeBase = 0.33;
    public const double EnergyShieldRechargeDelay = 2;
    public const double WardRechargeDelay = 2;
    public const double Transfiguration = 0.3;

    public static readonly double EnemyMaxResist = GameConstants.Monster["base_maximum_all_resistances_%"];

    public const double LeechRateBase = 0.02;
    public const double DotDpsCap = 35791394;
    public const double BleedPercentBase = 70;
    public const double BleedDurationBase = 5;
    public const double PoisonPercentBase = 0.30;
    public const double PoisonDurationBase = 2;
    public const double IgnitePercentBase = 0.9;
    public const double IgniteDurationBase = 4;
    public const double ImpaleStoredDamageBase = 0.1;
    public const double TrapTriggerRadiusBase = 10;
    public const double MineDetonationRadiusBase = 60;
    public const double MineAuraRadiusBase = 35;
    public const double BrandAttachmentRangeBase = 30;
    public const double ProjectileDistanceCap = 150;

    public static readonly double PlayerMovementSpeed = GameConstants.Character["base_speed"];

    public const double MinStunChanceNeeded = 20;
    public const double StunBaseMult = 200;
    public const double StunBaseDuration = 0.35;
    public const double StunNotMeleeDamageMult = 0.75;
    public const double MaxEnemyLevel = 85;
    public const double MaxExperiencePenaltyFreeAreaLevel = 70;
    public const double ExperiencePenaltyMultiplier = 0.06;

    // EHP helper constants
    public static readonly double StdBossDPSMult = 4 / 4.40;
    public static readonly double PinnacleBossDPSMult = 8 / 4.40;
    public static readonly double PinnacleBossPen = 15.0 / 5;
    public static readonly double UberBossDPSMult = 10 / 4.25;
    public static readonly double UberBossPen = 40.0 / 5;
    public const int EhpCalcSpeedUp = 8;
    public const int EhpCalcMaxDamage = 100000000;
    public const int EhpCalcMaxIterationsToCalc = 50;
    public const int MaxHitSmoothingPasses = 8;
    public const int MaxStatIncrease = 2;

    // PvP scaling
    public const double PvpElemental1 = 0.55;
    public const double PvpElemental2 = 150;
    public const double PvpNonElemental1 = 0.57;
    public const double PvpNonElemental2 = 90;
}
