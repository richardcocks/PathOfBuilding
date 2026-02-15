using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Config;

/// <summary>
/// Applies config inputs to player and enemy ModDBs and populates CalcConfig.
/// Bridge between BuildData config and calculation engine.
/// </summary>
public static class ConfigApplicator
{
    /// <summary>
    /// Apply all config inputs to player and enemy ModDBs, returning a populated CalcConfig.
    /// </summary>
    public static CalcConfig Apply(List<ConfigInput> inputs, ModDB playerModDB, ModDB enemyModDB)
    {
        var config = new CalcConfig();

        foreach (var input in inputs)
        {
            // Skip inactive boolean inputs
            if (input.Kind == ConfigInputKind.Boolean && !input.BooleanValue)
                continue;

            // Look up handler
            var handler = ConfigVarRegistry.Find(input.Name);
            if (handler != null)
                ApplyHandler(handler, input, playerModDB, enemyModDB, config);

            // Populate CalcConfig fields directly for known names
            PopulateCalcConfig(input, config);
        }

        return config;
    }

    internal static void ApplyHandler(ConfigVarHandler handler, ConfigInput input,
        ModDB playerModDB, ModDB enemyModDB, CalcConfig config)
    {
        var targetDB = handler.Target == ConfigTarget.Player ? playerModDB : enemyModDB;

        switch (handler.Pattern)
        {
            case ConfigPattern.ConditionFlag:
                if (input.Kind == ConfigInputKind.Boolean && input.BooleanValue)
                {
                    targetDB.Conditions[$"Condition:{handler.ModName}"] = true;

                    // Apply implied conditions
                    if (handler.ImpliedConditions != null)
                    {
                        foreach (var implied in handler.ImpliedConditions)
                            targetDB.Conditions[$"Condition:{implied}"] = true;
                    }
                }
                break;

            case ConfigPattern.Multiplier:
                if (input.Kind == ConfigInputKind.Number && input.NumberValue != 0)
                {
                    targetDB.Multipliers[handler.ModName!] = input.NumberValue;
                }
                break;

            case ConfigPattern.Override:
                if (input.Kind == ConfigInputKind.Number && input.NumberValue != 0)
                {
                    targetDB.NewMod(handler.ModName!, ModType.Override, input.NumberValue, handler.Source);
                }
                break;

            case ConfigPattern.FlagMod:
                if (input.Kind == ConfigInputKind.Boolean && input.BooleanValue)
                {
                    targetDB.NewMod(handler.ModName!, ModType.Flag, true, handler.Source);
                }
                break;

            case ConfigPattern.Custom:
                if (handler.CustomApply != null)
                {
                    object val = input.Kind switch
                    {
                        ConfigInputKind.Boolean => input.BooleanValue,
                        ConfigInputKind.Number => input.NumberValue,
                        ConfigInputKind.String => input.StringValue,
                        _ => input.StringValue,
                    };
                    handler.CustomApply(val, playerModDB, enemyModDB);
                }
                break;
        }
    }

    internal static void PopulateCalcConfig(ConfigInput input, CalcConfig config)
    {
        switch (input.Name)
        {
            // Mode flags
            case "mode_combat":
                config.ModeCombat = input.BooleanValue;
                break;
            case "mode_effective":
                config.ModeEffective = input.BooleanValue;
                break;

            // Charge flags
            case "usePowerCharges":
                config.UsePowerCharges = input.BooleanValue;
                break;
            case "useFrenzyCharges":
                config.UseFrenzyCharges = input.BooleanValue;
                break;
            case "useEnduranceCharges":
                config.UseEnduranceCharges = input.BooleanValue;
                break;
            case "overridePowerCharges":
                if (input.Kind == ConfigInputKind.Number && input.NumberValue > 0)
                    config.OverridePowerCharges = (int)input.NumberValue;
                break;
            case "overrideFrenzyCharges":
                if (input.Kind == ConfigInputKind.Number && input.NumberValue > 0)
                    config.OverrideFrenzyCharges = (int)input.NumberValue;
                break;
            case "overrideEnduranceCharges":
                if (input.Kind == ConfigInputKind.Number && input.NumberValue > 0)
                    config.OverrideEnduranceCharges = (int)input.NumberValue;
                break;

            // Buff flags
            case "buffOnslaught":
                config.BuffOnslaught = input.BooleanValue;
                break;
            case "buffUnholyMight":
                config.BuffUnholyMight = input.BooleanValue;
                break;
            case "buffFortification":
                config.BuffFortification = input.BooleanValue;
                break;
            case "buffTailwind":
                config.BuffTailwind = input.BooleanValue;
                break;
            case "buffPhasing":
                config.BuffPhasing = input.BooleanValue;
                break;

            // Combat conditions
            case "conditionAttackedRecently":
                config.AttackedRecently = input.BooleanValue;
                break;
            case "conditionCastSpellRecently":
                config.CastSpellRecently = input.BooleanValue;
                break;
            case "conditionUsedMovementSkillRecently":
                config.UsedMovementSkillRecently = input.BooleanValue;
                break;
            case "conditionUsedMinionSkillRecently":
                config.UsedMinionSkillRecently = input.BooleanValue;
                break;
            case "conditionUsedVaalSkillRecently":
                config.UsedVaalSkillRecently = input.BooleanValue;
                break;
            case "conditionChannelling":
                config.Channelling = input.BooleanValue;
                break;
            case "conditionHitRecently":
                config.HitRecently = input.BooleanValue;
                break;
            case "conditionHitSpellRecently":
                config.HitSpellRecently = input.BooleanValue;
                break;
            case "conditionHaveTotem":
                config.HaveTotem = input.BooleanValue;
                break;
            case "conditionSummonedTotemRecently":
                config.SummonedTotemRecently = input.BooleanValue;
                break;
            case "conditionTotemsHitRecently":
                config.TotemsHitRecently = input.BooleanValue;
                break;
            case "conditionTotemsSpellHitRecently":
                config.TotemsSpellHitRecently = input.BooleanValue;
                break;
            case "conditionDetonatedMinesRecently":
                config.DetonatedMinesRecently = input.BooleanValue;
                break;
            case "conditionTriggeredTrapsRecently":
                config.TriggeredTrapsRecently = input.BooleanValue;
                break;
            case "conditionLowLife":
                config.ConditionLowLife = input.BooleanValue;
                break;

            // Multiplier stacks
            case "multiplierRageStack":
                config.MultiplierRageStack = (int)input.NumberValue;
                break;
            case "multiplierManaBurnStacks":
                config.MultiplierManaBurnStacks = (int)input.NumberValue;
                break;
            case "multiplierSoulEater":
                config.MultiplierSoulEater = (int)input.NumberValue;
                break;

            // Enemy config
            case "enemyIsBoss":
                config.EnemyIsBoss = input.StringValue;
                break;
            case "enemyLevel":
                config.EnemyLevel = (int)input.NumberValue;
                break;

            // Enemy damage
            case "enemyPhysicalDamage":
                if (input.NumberValue > 0) config.EnemyPhysicalDamage = input.NumberValue;
                break;
            case "enemyLightningDamage":
                if (input.NumberValue > 0) config.EnemyLightningDamage = input.NumberValue;
                break;
            case "enemyColdDamage":
                if (input.NumberValue > 0) config.EnemyColdDamage = input.NumberValue;
                break;
            case "enemyFireDamage":
                if (input.NumberValue > 0) config.EnemyFireDamage = input.NumberValue;
                break;
            case "enemyChaosDamage":
                if (input.NumberValue > 0) config.EnemyChaosDamage = input.NumberValue;
                break;
            case "enemyCritChance":
                if (input.NumberValue > 0) config.EnemyCritChance = input.NumberValue;
                break;
            case "enemyCritDamage":
                if (input.NumberValue > 0) config.EnemyCritDamage = input.NumberValue;
                break;
            case "enemySpeed":
                if (input.NumberValue > 0) config.EnemySpeed = input.NumberValue;
                break;

            // EHP
            case "EHPUnluckyWorstOf":
                if (input.NumberValue > 0) config.EHPUnluckyWorstOf = (int)input.NumberValue;
                break;
            case "DisableEHPGainOnBlock":
                config.DisableEHPGainOnBlock = input.BooleanValue;
                break;
        }
    }
}
