using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Config;

/// <summary>
/// Data-driven table of config var → handler mappings.
/// Organized by section matching ConfigOptions.lua.
/// </summary>
public static class ConfigVarRegistry
{
    private static readonly Dictionary<string, ConfigVarHandler> Registry;

    static ConfigVarRegistry()
    {
        var handlers = new List<ConfigVarHandler>();

        // ─── General section ───
        handlers.Add(new ConfigVarHandler { VarName = "resistancePenalty", Pattern = ConfigPattern.Custom,
            CustomApply = (val, p, e) => { /* handled directly in CalcSetup */ } });
        handlers.Add(Condition("conditionStationary", "Stationary"));
        handlers.Add(Condition("conditionMoving", "Moving"));
        handlers.Add(Condition("conditionFullLife", "FullLife"));
        handlers.Add(Condition("conditionLowLife", "LowLife"));
        handlers.Add(Condition("conditionFullMana", "FullMana"));
        handlers.Add(Condition("conditionLowMana", "LowMana"));
        handlers.Add(Condition("conditionFullEnergyShield", "FullEnergyShield"));
        handlers.Add(Condition("conditionLowEnergyShield", "LowEnergyShield"));
        handlers.Add(Condition("conditionHaveEnergyShield", "HaveEnergyShield"));

        // ─── Combat section ───
        handlers.Add(CombatCondition("usePowerCharges", "UsePowerCharges"));
        handlers.Add(CombatCondition("useFrenzyCharges", "UseFrenzyCharges"));
        handlers.Add(CombatCondition("useEnduranceCharges", "UseEnduranceCharges"));
        handlers.Add(CombatMultiplier("overridePowerCharges", "PowerChargesOverride"));
        handlers.Add(CombatMultiplier("overrideFrenzyCharges", "FrenzyChargesOverride"));
        handlers.Add(CombatMultiplier("overrideEnduranceCharges", "EnduranceChargesOverride"));

        handlers.Add(CombatCondition("conditionLeeching", "Leeching"));
        handlers.Add(CombatCondition("conditionLeechingLife", "LeechingLife"));
        handlers.Add(CombatCondition("conditionLeechingMana", "LeechingMana"));
        handlers.Add(CombatCondition("conditionLeechingEnergyShield", "LeechingEnergyShield"));
        handlers.Add(CombatCondition("conditionUsingFlask", "UsingFlask"));
        handlers.Add(CombatCondition("conditionHaveTotem", "HaveTotem"));
        handlers.Add(CombatCondition("conditionSummonedTotemRecently", "SummonedTotemRecently"));
        handlers.Add(CombatCondition("conditionTotemsHitRecently", "TotemsHitRecently"));
        handlers.Add(CombatCondition("conditionTotemsSpellHitRecently", "TotemsSpellHitRecently"));

        handlers.Add(CombatCondition("buffOnslaught", "Onslaught"));
        handlers.Add(CombatCondition("buffUnholyMight", "UnholyMight"));
        handlers.Add(CombatCondition("buffPhasing", "Phasing"));
        handlers.Add(CombatCondition("buffFortification", "Fortified"));
        handlers.Add(CombatCondition("buffTailwind", "Tailwind"));
        handlers.Add(CombatCondition("buffAdrenaline", "Adrenaline"));
        handlers.Add(CombatCondition("buffDivinity", "Divinity"));
        handlers.Add(CombatCondition("buffArcaneSurge", "ArcaneSurge"));
        handlers.Add(CombatCondition("buffConvergence", "Convergence"));

        handlers.Add(CombatCondition("conditionAttackedRecently", "AttackedRecently",
            implied: new[] { "SkillAttackedRecently" }));
        handlers.Add(CombatCondition("conditionCastSpellRecently", "CastSpellRecently",
            implied: new[] { "SkillCastSpellRecently" }));
        handlers.Add(CombatCondition("conditionUsedMovementSkillRecently", "UsedMovementSkillRecently"));
        handlers.Add(CombatCondition("conditionUsedMinionSkillRecently", "UsedMinionSkillRecently"));
        handlers.Add(CombatCondition("conditionUsedVaalSkillRecently", "UsedVaalSkillRecently"));
        handlers.Add(CombatCondition("conditionChannelling", "Channelling"));
        handlers.Add(CombatCondition("conditionHitRecently", "HitRecently",
            implied: new[] { "SkillHitRecently" }));
        handlers.Add(CombatCondition("conditionHitSpellRecently", "HitSpellRecently"));
        handlers.Add(CombatCondition("conditionCritRecently", "CritRecently",
            implied: new[] { "SkillCritRecently" }));
        handlers.Add(CombatCondition("conditionNonCritRecently", "NonCritRecently"));
        handlers.Add(CombatCondition("conditionKilledRecently", "KilledRecently"));
        handlers.Add(CombatCondition("conditionTotemsKilledRecently", "TotemsKilledRecently"));
        handlers.Add(CombatCondition("conditionMinionKilledRecently", "MinionKilledRecently"));
        handlers.Add(CombatCondition("conditionKilledAffectedByDoT", "KilledAffectedByDoT"));
        handlers.Add(CombatCondition("conditionFrozenEnemyRecently", "FrozenEnemyRecently"));
        handlers.Add(CombatCondition("conditionShatteredEnemyRecently", "ShatteredEnemyRecently"));
        handlers.Add(CombatCondition("conditionIgnitedEnemyRecently", "IgnitedEnemyRecently"));
        handlers.Add(CombatCondition("conditionShockedEnemyRecently", "ShockedEnemyRecently"));
        handlers.Add(CombatCondition("conditionBlockedRecently", "BlockedRecently"));
        handlers.Add(CombatCondition("conditionBlockedAttackRecently", "BlockedAttackRecently"));
        handlers.Add(CombatCondition("conditionBlockedSpellRecently", "BlockedSpellRecently"));
        handlers.Add(CombatCondition("conditionEnergyShieldRechargeRecently", "EnergyShieldRechargeRecently"));
        handlers.Add(CombatCondition("conditionDetonatedMinesRecently", "DetonatedMinesRecently"));
        handlers.Add(CombatCondition("conditionTriggeredTrapsRecently", "TriggeredTrapsRecently"));

        // Combat multipliers
        handlers.Add(CombatMultiplier("multiplierRageStack", "RageStack"));
        handlers.Add(CombatMultiplier("multiplierSoulEater", "SoulEater"));
        handlers.Add(CombatMultiplier("multiplierManaBurnStacks", "ManaBurnStacks"));
        handlers.Add(CombatMultiplier("multiplierNearbyEnemies", "NearbyEnemies"));
        handlers.Add(CombatMultiplier("multiplierNearbyAlly", "NearbyAlly"));
        handlers.Add(CombatMultiplier("multiplierNearbyCorpse", "NearbyCorpse"));
        handlers.Add(CombatMultiplier("multiplierNearbyRareOrUnique", "NearbyRareOrUniqueEnemies"));
        handlers.Add(CombatMultiplier("multiplierPoisonStack", "PoisonStack"));
        handlers.Add(CombatMultiplier("multiplierWitheredStack", "WitheredStack"));
        handlers.Add(CombatMultiplier("multiplierBleedStack", "BleedStack"));
        handlers.Add(CombatMultiplier("multiplierRampage", "Rampage"));
        handlers.Add(CombatMultiplier("multiplierDefiance", "Defiance"));

        // ─── Effective DPS section ───
        handlers.Add(EffectiveEnemyCondition("conditionEnemyMoving", "Moving"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyFullLife", "FullLife"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyLowLife", "LowLife"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyBleeding", "Bleeding"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyPoisoned", "Poisoned"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyBurning", "Burning"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyIgnited", "Ignited"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyChilled", "Chilled"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyFrozen", "Frozen"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyShocked", "Shocked"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyBlinded", "Blinded"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyIntimidated", "Intimidated"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyUnnerved", "Unnerved"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyCoveredInAsh", "CoveredInAsh"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyCoveredInFrost", "CoveredInFrost"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyMaimed", "Maimed"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyHindered", "Hindered"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyTaunted", "Taunted"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyCrushed", "Crushed"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyOnConsecratedGround", "OnConsecratedGround"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyOnProfaneGround", "OnProfaneGround"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyCursed", "Cursed"));
        handlers.Add(EffectiveEnemyCondition("conditionEnemyHexproof", "Hexproof"));

        // Effective multipliers on enemy
        handlers.Add(EffectiveEnemyMultiplier("multiplierEnemyWitheredStack", "WitheredStack"));
        handlers.Add(EffectiveEnemyMultiplier("multiplierEnemyPoisonStack", "PoisonStack"));

        // ─── Enemy stat section ───
        handlers.Add(new ConfigVarHandler { VarName = "enemyIsBoss", Pattern = ConfigPattern.Custom,
            CustomApply = (val, playerDB, enemyDB) =>
            {
                var bossType = val as string ?? "None";
                BossData.ApplyBoss(bossType, playerDB, enemyDB);
            }
        });
        handlers.Add(new ConfigVarHandler { VarName = "enemyLevel", Pattern = ConfigPattern.Custom,
            CustomApply = (val, p, e) => { /* handled in BuildPipeline */ } });
        handlers.Add(EnemyOverride("enemyPhysicalDamage", "PhysicalDamage"));
        handlers.Add(EnemyOverride("enemyFireDamage", "FireDamage"));
        handlers.Add(EnemyOverride("enemyColdDamage", "ColdDamage"));
        handlers.Add(EnemyOverride("enemyLightningDamage", "LightningDamage"));
        handlers.Add(EnemyOverride("enemyChaosDamage", "ChaosDamage"));
        handlers.Add(EnemyOverride("enemyCritChance", "CritChance"));
        handlers.Add(EnemyOverride("enemyCritDamage", "CritDamage"));
        handlers.Add(EnemyOverride("enemySpeed", "Speed"));
        handlers.Add(EnemyOverride("enemyPhysicalOverwhelm", "PhysicalOverwhelm"));

        // EHP section
        handlers.Add(new ConfigVarHandler { VarName = "EHPUnluckyWorstOf", Pattern = ConfigPattern.Custom,
            CustomApply = (val, p, e) => { /* handled via CalcConfig */ } });
        handlers.Add(new ConfigVarHandler { VarName = "DisableEHPGainOnBlock", Pattern = ConfigPattern.Custom,
            CustomApply = (val, p, e) => { /* handled via CalcConfig */ } });

        // Build registry dictionary
        Registry = new Dictionary<string, ConfigVarHandler>(handlers.Count, StringComparer.Ordinal);
        foreach (var h in handlers)
            Registry[h.VarName] = h;
    }

    public static ConfigVarHandler? Find(string varName)
    {
        return Registry.TryGetValue(varName, out var handler) ? handler : null;
    }

    public static int Count => Registry.Count;

    public static IEnumerable<string> AllVarNames => Registry.Keys;

    // ─── Factory helpers ───

    private static ConfigVarHandler Condition(string varName, string conditionName) => new()
    {
        VarName = varName,
        Pattern = ConfigPattern.ConditionFlag,
        ModName = conditionName,
        Target = ConfigTarget.Player,
    };

    private static ConfigVarHandler CombatCondition(string varName, string conditionName,
        string[]? implied = null) => new()
    {
        VarName = varName,
        Pattern = ConfigPattern.ConditionFlag,
        ModName = conditionName,
        Target = ConfigTarget.Player,
        HasCombatTag = true,
        ImpliedConditions = implied,
    };

    private static ConfigVarHandler CombatMultiplier(string varName, string multiplierName) => new()
    {
        VarName = varName,
        Pattern = ConfigPattern.Multiplier,
        ModName = multiplierName,
        Target = ConfigTarget.Player,
        HasCombatTag = true,
    };

    private static ConfigVarHandler EffectiveEnemyCondition(string varName, string conditionName) => new()
    {
        VarName = varName,
        Pattern = ConfigPattern.ConditionFlag,
        ModName = conditionName,
        Target = ConfigTarget.Enemy,
        HasEffectiveTag = true,
    };

    private static ConfigVarHandler EffectiveEnemyMultiplier(string varName, string multiplierName) => new()
    {
        VarName = varName,
        Pattern = ConfigPattern.Multiplier,
        ModName = multiplierName,
        Target = ConfigTarget.Enemy,
        HasEffectiveTag = true,
    };

    private static ConfigVarHandler EnemyOverride(string varName, string modName) => new()
    {
        VarName = varName,
        Pattern = ConfigPattern.Override,
        ModName = modName,
        Target = ConfigTarget.Enemy,
    };
}
