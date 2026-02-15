using PathOfBuilding.Core.Config;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Orchestrates building from imported BuildData to fully calculated Actor pair.
/// Single entry point from XML import to calculation results.
/// </summary>
public static class BuildPipeline
{
    /// <summary>
    /// Create configured Actor pair from BuildData without running calculations.
    /// Sets up ModDBs with class stats, level scaling, config-driven mods, etc.
    /// </summary>
    public static (Actor player, Actor enemy) CreateActors(BuildData build)
    {
        var player = new Actor();
        var enemy = new Actor();
        player.Enemy = enemy;
        enemy.Enemy = player;

        var meta = build.Metadata;

        // Resolve class name (ascendancy → base class)
        var className = ClassBaseStats.GetBaseClassName(meta.AscendClassName);
        if (string.IsNullOrEmpty(className))
            className = meta.ClassName;

        // Get resistance penalty from config (default -60)
        int resPenalty = GetResistancePenalty(build);

        // Get enemy level from config or default
        int enemyLevel = GetEnemyLevel(build, meta.Level);

        // Initialize ModDBs
        CalcSetup.InitPlayerModDB(player.ModDB, className, meta.Level,
            meta.Bandit, resPenalty);
        CalcSetup.InitEnemyModDB(enemy.ModDB, enemyLevel);

        // Apply config inputs → mods + CalcConfig
        var inputs = build.ConfigSets.SelectMany(s => s.Inputs).ToList();
        var config = ConfigApplicator.Apply(inputs, player.ModDB, enemy.ModDB);
        player.Config = config;
        enemy.Config = config;

        return (player, enemy);
    }

    /// <summary>
    /// Create actors and run the full calculation pipeline.
    /// Returns the actor pair with populated Output dictionaries.
    /// </summary>
    public static (Actor player, Actor enemy) Calculate(BuildData build)
    {
        var (player, enemy) = CreateActors(build);

        // Run calculation pipeline in order
        CalcPerform.DoActorAttributes(player);
        CalcConditions.CombatConditions(player);
        CalcConditions.ShrineBuffs(player);
        CalcPerform.DoActorLifeMana(player);
        CalcPerform.DoActorLifeManaReservation(player);
        CalcPerform.DoActorCharges(player);
        CalcMisc.DoActorMisc(player);
        CalcDefence.Defence(player);
        CalcEHP.BuildDefenceEstimations(player);

        return (player, enemy);
    }

    /// <summary>
    /// Extract resistance penalty from config inputs. Defaults to -60.
    /// </summary>
    internal static int GetResistancePenalty(BuildData build)
    {
        foreach (var set in build.ConfigSets)
        {
            foreach (var input in set.Inputs)
            {
                if (input.Name == "resistancePenalty" && input.Kind == ConfigInputKind.Number)
                    return (int)input.NumberValue;
            }
        }
        return -60;
    }

    /// <summary>
    /// Determine enemy level from config or boss defaults.
    /// </summary>
    internal static int GetEnemyLevel(BuildData build, int playerLevel)
    {
        // Check for explicit enemy level in config
        foreach (var set in build.ConfigSets)
        {
            foreach (var input in set.Inputs)
            {
                if (input.Name == "enemyLevel" && input.Kind == ConfigInputKind.Number && input.NumberValue > 0)
                    return (int)input.NumberValue;
            }
        }

        // Check for boss type to determine default level
        string bossType = "None";
        foreach (var set in build.ConfigSets)
        {
            foreach (var input in set.Inputs)
            {
                if (input.Name == "enemyIsBoss" && input.Kind == ConfigInputKind.String)
                    bossType = input.StringValue;
            }
        }

        return BossData.GetDefaultEnemyLevel(bossType, playerLevel);
    }
}
