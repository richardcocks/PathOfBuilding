using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Data;

/// <summary>
/// Boss stat presets from ConfigOptions.lua enemyIsBoss apply function.
/// Applies boss-specific mods to player and enemy ModDBs.
/// </summary>
public static class BossData
{
    // Monster accuracy table (level 1-100), from Data/Misc.lua
    internal static readonly int[] MonsterAccuracyTable =
    {
        14, 15, 15, 16, 17, 18, 19, 20, 21, 23,
        24, 25, 26, 28, 29, 31, 32, 34, 35, 37,
        39, 41, 43, 45, 47, 49, 52, 54, 57, 59,
        62, 65, 68, 71, 74, 77, 81, 84, 88, 92,
        96, 100, 105, 109, 114, 119, 124, 129, 135, 140,
        146, 152, 159, 165, 172, 179, 187, 195, 203, 211,
        220, 229, 238, 247, 257, 268, 279, 290, 301, 314,
        326, 339, 352, 366, 381, 396, 412, 428, 444, 462,
        480, 499, 518, 538, 559, 580, 603, 626, 650, 675,
        701, 728, 755, 784, 814, 845, 877, 910, 945, 980,
    };

    // Monster damage table (level 1-100), from Data/Misc.lua
    internal static readonly double[] MonsterDamageTable =
    {
        4.99, 5.56, 6.16, 6.81, 7.5, 8.23, 9, 9.82, 10.7, 11.62,
        12.6, 13.64, 14.74, 15.91, 17.14, 18.45, 19.83, 21.29, 22.84, 24.47,
        26.19, 28.01, 29.94, 31.96, 34.11, 36.36, 38.75, 41.26, 43.91, 46.7,
        49.65, 52.75, 56.01, 59.45, 63.08, 66.89, 70.91, 75.13, 79.58, 84.26,
        89.18, 94.35, 99.8, 105.52, 111.53, 117.86, 124.5, 131.49, 138.83, 146.53,
        154.63, 163.14, 172.07, 181.45, 191.3, 201.63, 212.48, 223.87, 235.83, 248.37,
        261.53, 275.33, 289.82, 305.01, 320.94, 337.65, 355.18, 373.55, 392.81, 413.01,
        434.18, 456.37, 479.62, 504, 529.54, 556.3, 584.35, 613.73, 644.5, 676.75,
        710.52, 745.89, 782.94, 821.73, 862.36, 904.9, 949.44, 996.07, 1044.89, 1096,
        1149.5, 1205.5, 1264.11, 1325.45, 1389.64, 1456.82, 1527.12, 1600.68, 1677.64, 1758.17,
    };

    /// <summary>
    /// Get monster accuracy for a given level (1-100).
    /// </summary>
    public static int GetMonsterAccuracy(int level)
    {
        int idx = Math.Clamp(level, 1, 100) - 1;
        return MonsterAccuracyTable[idx];
    }

    /// <summary>
    /// Get monster base damage for a given level (1-100).
    /// </summary>
    public static double GetMonsterDamage(int level)
    {
        int idx = Math.Clamp(level, 1, 100) - 1;
        return MonsterDamageTable[idx];
    }

    /// <summary>
    /// Apply boss-type mods to player and enemy ModDBs.
    /// Boss types: "None", "Boss", "Shaper" (legacy alias for Pinnacle), "Pinnacle", "Uber"
    /// </summary>
    public static void ApplyBoss(string bossType, ModDB playerModDB, ModDB enemyModDB)
    {
        if (string.IsNullOrEmpty(bossType) || bossType == "None")
            return;

        var effectiveTag = new ConditionTag { Var = "Effective" };

        switch (bossType)
        {
            case "Boss":
                enemyModDB.NewMod("Condition:RareOrUnique", ModType.Flag, true, "Config", tags: effectiveTag);
                enemyModDB.NewMod("AilmentThreshold", ModType.More, 488, "Boss");
                playerModDB.NewMod("WarcryPower", ModType.Base, 20, "Boss");
                playerModDB.NewMod("Multiplier:EnemyPower", ModType.Base, 20, "Boss");
                break;

            case "Shaper":
            case "Pinnacle":
                enemyModDB.NewMod("Condition:RareOrUnique", ModType.Flag, true, "Config", tags: effectiveTag);
                enemyModDB.NewMod("Condition:PinnacleBoss", ModType.Flag, true, "Config", tags: effectiveTag);
                enemyModDB.NewMod("AilmentThreshold", ModType.More, 404, "Boss");
                playerModDB.NewMod("WarcryPower", ModType.Base, 20, "Boss");
                playerModDB.NewMod("Multiplier:EnemyPower", ModType.Base, 20, "Boss");
                break;

            case "Uber":
                enemyModDB.NewMod("Condition:RareOrUnique", ModType.Flag, true, "Config", tags: effectiveTag);
                enemyModDB.NewMod("Condition:PinnacleBoss", ModType.Flag, true, "Config", tags: effectiveTag);
                enemyModDB.NewMod("DamageTaken", ModType.More, -70, "Boss");
                enemyModDB.NewMod("AilmentThreshold", ModType.More, 404, "Boss");
                playerModDB.NewMod("WarcryPower", ModType.Base, 20, "Boss");
                playerModDB.NewMod("Multiplier:EnemyPower", ModType.Base, 20, "Boss");
                break;
        }
    }

    /// <summary>
    /// Get the default enemy level for a boss type.
    /// </summary>
    public static int GetDefaultEnemyLevel(string bossType, int playerLevel)
    {
        return bossType switch
        {
            "Pinnacle" or "Shaper" => 84,
            "Uber" => 85,
            "Boss" => Math.Min(83, playerLevel),
            _ => Math.Min(83, playerLevel),
        };
    }
}
