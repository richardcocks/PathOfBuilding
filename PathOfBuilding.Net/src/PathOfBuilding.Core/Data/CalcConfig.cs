namespace PathOfBuilding.Core.Data;

/// <summary>
/// Configuration record replacing Lua's env.configInput.
/// Holds combat mode flags, enemy damage config, and combat conditions.
/// Each Actor carries its own CalcConfig instance.
/// </summary>
public class CalcConfig
{
    // ─── Mode flags ───
    public bool ModeCombat { get; set; } = true;
    public bool ModeEffective { get; set; } = true;

    // ─── Charge flags ───
    public bool UsePowerCharges { get; set; }
    public bool UseFrenzyCharges { get; set; }
    public bool UseEnduranceCharges { get; set; }
    public int? OverridePowerCharges { get; set; }
    public int? OverrideFrenzyCharges { get; set; }
    public int? OverrideEnduranceCharges { get; set; }

    // ─── Buff flags ───
    public bool BuffOnslaught { get; set; }
    public bool BuffUnholyMight { get; set; }
    public bool BuffFortification { get; set; }
    public bool BuffTailwind { get; set; }
    public bool BuffPhasing { get; set; }

    // ─── Enemy config ───
    public string EnemyIsBoss { get; set; } = "None";
    public int EnemyLevel { get; set; } = 84;
    public string EnemyDamageType { get; set; } = "Average";

    public double? EnemyPhysicalDamage { get; set; }
    public double? EnemyLightningDamage { get; set; }
    public double? EnemyColdDamage { get; set; }
    public double? EnemyFireDamage { get; set; }
    public double? EnemyChaosDamage { get; set; }

    public double? EnemyPhysicalPen { get; set; }
    public double? EnemyLightningPen { get; set; }
    public double? EnemyColdPen { get; set; }
    public double? EnemyFirePen { get; set; }
    public double? EnemyChaosPen { get; set; }

    public double? EnemyPhysicalOverwhelm { get; set; }
    public double? EnemyLightningOverwhelm { get; set; }
    public double? EnemyColdOverwhelm { get; set; }
    public double? EnemyFireOverwhelm { get; set; }
    public double? EnemyChaosOverwhelm { get; set; }

    public double? EnemyCritChance { get; set; }
    public double? EnemyCritDamage { get; set; }
    public double? EnemySpeed { get; set; }

    public int EHPUnluckyWorstOf { get; set; } = 1;

    // ─── Combat condition booleans ───
    public bool AttackedRecently { get; set; }
    public bool CastSpellRecently { get; set; }
    public bool UsedMovementSkillRecently { get; set; }
    public bool UsedMinionSkillRecently { get; set; }
    public bool UsedVaalSkillRecently { get; set; }
    public bool Channelling { get; set; }
    public bool HitRecently { get; set; }
    public bool HitSpellRecently { get; set; }
    public bool HaveTotem { get; set; }
    public bool SummonedTotemRecently { get; set; }
    public bool TotemsHitRecently { get; set; }
    public bool TotemsSpellHitRecently { get; set; }
    public bool DetonatedMinesRecently { get; set; }
    public bool TriggeredTrapsRecently { get; set; }
    public bool ConditionLowLife { get; set; }
    public bool DisableEHPGainOnBlock { get; set; }

    // ─── Config multiplier stacks ───
    public int MultiplierManaBurnStacks { get; set; }
    public int MultiplierRageStack { get; set; }
    public int MultiplierSoulEater { get; set; }

    /// <summary>
    /// Get per-type enemy damage from config, returning 0 if null.
    /// </summary>
    public double GetEnemyDamage(string damageType) => damageType switch
    {
        "Physical" => EnemyPhysicalDamage ?? 0,
        "Lightning" => EnemyLightningDamage ?? 0,
        "Cold" => EnemyColdDamage ?? 0,
        "Fire" => EnemyFireDamage ?? 0,
        "Chaos" => EnemyChaosDamage ?? 0,
        _ => 0,
    };

    /// <summary>
    /// Get per-type enemy pen from config, returning 0 if null.
    /// </summary>
    public double GetEnemyPen(string damageType) => damageType switch
    {
        "Physical" => EnemyPhysicalPen ?? 0,
        "Lightning" => EnemyLightningPen ?? 0,
        "Cold" => EnemyColdPen ?? 0,
        "Fire" => EnemyFirePen ?? 0,
        "Chaos" => EnemyChaosPen ?? 0,
        _ => 0,
    };

    /// <summary>
    /// Get per-type enemy overwhelm from config, returning 0 if null.
    /// </summary>
    public double GetEnemyOverwhelm(string damageType) => damageType switch
    {
        "Physical" => EnemyPhysicalOverwhelm ?? 0,
        "Lightning" => EnemyLightningOverwhelm ?? 0,
        "Cold" => EnemyColdOverwhelm ?? 0,
        "Fire" => EnemyFireOverwhelm ?? 0,
        "Chaos" => EnemyChaosOverwhelm ?? 0,
        _ => 0,
    };
}
