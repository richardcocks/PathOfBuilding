using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Builds stat values from skill level data, handling quality bonuses,
/// stat interpolation, and constant stats.
/// Ported from CalcTools.lua buildSkillInstanceStats (lines 176-238).
/// </summary>
public static class SkillInstanceStats
{
    /// <summary>
    /// Build the stat dictionary for a skill instance at a given level/quality.
    /// </summary>
    public static Dictionary<string, double> Build(
        GemInstanceData gemInstance, SkillDefinition skill)
    {
        var stats = new Dictionary<string, double>();
        int level = gemInstance.Level;
        int quality = gemInstance.Quality;
        string qualityId = gemInstance.QualityId ?? "Default";

        // Add quality stats: stat[key] += Math.Truncate(qualityStat.value * quality)
        if (quality > 0 && skill.QualityStats.Count > 0)
        {
            List<(string stat, double value)>? qualityStats = null;

            if (skill.QualityStats.TryGetValue(qualityId, out var qs))
                qualityStats = qs;
            else if (skill.QualityStats.TryGetValue("Default", out var defaultQs))
                qualityStats = defaultQs;

            if (qualityStats != null)
            {
                foreach (var (stat, value) in qualityStats)
                {
                    stats.TryGetValue(stat, out double current);
                    stats[stat] = current + Math.Truncate(value * quality);
                }
            }
        }

        // Get level data
        if (!skill.Levels.TryGetValue(level, out var levelData))
        {
            // If exact level not found, return what we have (quality stats + constants)
            AddConstantStats(stats, skill);
            return stats;
        }

        // Get actor level for interpolation
        int actorLevel = levelData.LevelRequirement > 0 ? levelData.LevelRequirement : 1;

        double? availableEffectiveness = null;

        // Process each stat with interpolation
        for (int i = 0; i < skill.Stats.Count; i++)
        {
            string statName = skill.Stats[i];

            // Default value (statInterpolation == 1 or missing)
            double statValue = i < levelData.Values.Count ? levelData.Values[i] : 1;

            if (i < levelData.StatInterpolation.Count)
            {
                int interpType = levelData.StatInterpolation[i];

                if (interpType == 3)
                {
                    // Effectiveness interpolation
                    if (availableEffectiveness == null)
                    {
                        double baseEff = GameConstants.Game["SkillDamageBaseEffectiveness"];
                        double incrEff = GameConstants.Game["SkillDamageIncrementalEffectiveness"];
                        double skillBaseEff = skill.BaseEffectiveness > 0 ? skill.BaseEffectiveness : 1;
                        double skillIncrEff = skill.IncrementalEffectiveness;

                        availableEffectiveness =
                            (baseEff + incrEff * (actorLevel - 1)) *
                            skillBaseEff *
                            Math.Pow(1 + skillIncrEff, actorLevel - 1);
                    }

                    double rawValue = i < levelData.Values.Count ? levelData.Values[i] : 1;
                    statValue = CalcLib.Round(availableEffectiveness.Value * rawValue);
                }
                else if (interpType == 2)
                {
                    // Linear interpolation between gem levels
                    statValue = LinearInterpolate(skill, level, i, actorLevel);
                }
                // interpType == 1 (or default): use raw value (already set above)
            }

            stats.TryGetValue(statName, out double current);
            stats[statName] = current + statValue;
        }

        // Add constant stats
        AddConstantStats(stats, skill);

        return stats;
    }

    private static void AddConstantStats(Dictionary<string, double> stats, SkillDefinition skill)
    {
        foreach (var (stat, value) in skill.ConstantStats)
        {
            stats.TryGetValue(stat, out double current);
            stats[stat] = current + value;
        }
    }

    private static double LinearInterpolate(SkillDefinition skill, int gemLevel, int statIndex, int actorLevel)
    {
        // Order the levels
        var orderedLevels = skill.Levels.Keys.OrderBy(k => k).ToList();
        int currentLevelIdx = orderedLevels.IndexOf(gemLevel);
        if (currentLevelIdx < 0)
            currentLevelIdx = 0;

        if (orderedLevels.Count <= 1)
        {
            double val = statIndex < skill.Levels[orderedLevels[currentLevelIdx]].Values.Count
                ? skill.Levels[orderedLevels[currentLevelIdx]].Values[statIndex]
                : 1;
            return CalcLib.Round(val);
        }

        int nextLevelIdx = Math.Min(currentLevelIdx + 1, orderedLevels.Count - 1);
        int prevLevelIdx = nextLevelIdx - 1;
        if (prevLevelIdx < 0) prevLevelIdx = 0;

        var nextLevelData = skill.Levels[orderedLevels[nextLevelIdx]];
        var prevLevelData = skill.Levels[orderedLevels[prevLevelIdx]];

        double nextReq = nextLevelData.LevelRequirement;
        double prevReq = prevLevelData.LevelRequirement;
        double nextStat = statIndex < nextLevelData.Values.Count ? nextLevelData.Values[statIndex] : 1;
        double prevStat = statIndex < prevLevelData.Values.Count ? prevLevelData.Values[statIndex] : 1;

        if (Math.Abs(nextReq - prevReq) < 0.001)
            return CalcLib.Round(prevStat);

        return CalcLib.Round(prevStat + (nextStat - prevStat) * (actorLevel - prevReq) / (nextReq - prevReq));
    }
}
