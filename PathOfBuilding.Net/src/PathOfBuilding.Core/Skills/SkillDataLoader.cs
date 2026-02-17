using System.Globalization;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Loads skill definitions from Skills/*.lua files.
/// </summary>
public static class SkillDataLoader
{
    public static Dictionary<string, SkillDefinition> LoadFromFile(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return LoadFromText(text);
    }

    public static Dictionary<string, SkillDefinition> LoadFromText(string lua)
    {
        var parsed = LuaSkillParser.ParseSkillFile(lua);
        var result = new Dictionary<string, SkillDefinition>(parsed.Count);

        foreach (var (id, entry) in parsed)
        {
            if (entry is not Dictionary<string, object?> skillData)
                continue;

            var skill = ParseSkill(id, skillData);
            result[id] = skill;
        }

        return result;
    }

    public static Dictionary<string, SkillDefinition> LoadAllFromDirectory(string skillsDir)
    {
        var result = new Dictionary<string, SkillDefinition>();

        foreach (var file in Directory.GetFiles(skillsDir, "*.lua"))
        {
            var skills = LoadFromFile(file);
            foreach (var (id, skill) in skills)
                result[id] = skill;
        }

        return result;
    }

    private static SkillDefinition ParseSkill(string id, Dictionary<string, object?> d)
    {
        return new SkillDefinition
        {
            Id = id,
            Name = GetString(d, "name") ?? id,
            Color = GetInt(d, "color"),
            BaseEffectiveness = GetDouble(d, "baseEffectiveness"),
            IncrementalEffectiveness = GetDouble(d, "incrementalEffectiveness"),
            Description = GetString(d, "description"),
            CastTime = GetDouble(d, "castTime"),
            Support = GetBool(d, "support"),
            RequireSkillTypes = ParseSkillTypeList(d, "requireSkillTypes"),
            ExcludeSkillTypes = ParseSkillTypeList(d, "excludeSkillTypes"),
            AddSkillTypes = ParseSkillTypeList(d, "addSkillTypes"),
            PlusVersionOf = GetString(d, "plusVersionOf"),
            SkillTypes = ParseSkillTypeDict(d, "skillTypes"),
            BaseFlags = ParseBoolDict(d, "baseFlags"),
            StatMap = ParseStatMap(d),
            BaseMods = ParseBaseMods(d),
            Stats = ParseStringList(d, "stats"),
            ConstantStats = ParseStatPairs(d, "constantStats"),
            QualityStats = ParseQualityStats(d),
            Levels = ParseLevels(d),
            Parts = ParseParts(d),
            HasPreDamageFunc = d.ContainsKey("preDamageFunc"),
        };
    }

    private static List<SkillType> ParseSkillTypeList(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return new();

        if (v is List<object?> list)
        {
            var result = new List<SkillType>();
            foreach (var item in list)
            {
                var resolved = LuaDslInterpreter.Resolve(item);
                if (resolved is double dv)
                    result.Add((SkillType)(int)dv);
            }
            return result;
        }

        return new();
    }

    private static Dictionary<SkillType, bool> ParseSkillTypeDict(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return new();
        if (v is not Dictionary<string, object?> dict) return new();

        var result = new Dictionary<SkillType, bool>();
        foreach (var (k, val) in dict)
        {
            if (val is not true) continue;

            // Keys can be "SkillType.Spell" (from identifier keys) or numeric strings
            if (k.StartsWith("SkillType.") && Enum.TryParse<SkillType>(k.Substring("SkillType.".Length), out var st))
                result[st] = true;
            else if (double.TryParse(k, NumberStyles.Any, CultureInfo.InvariantCulture, out var numKey))
                result[(SkillType)(int)numKey] = true;
        }
        return result;
    }

    private static Dictionary<string, bool> ParseBoolDict(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return new();
        if (v is not Dictionary<string, object?> dict) return new();

        var result = new Dictionary<string, bool>();
        foreach (var (k, val) in dict)
        {
            if (val is true)
                result[k] = true;
        }
        return result;
    }

    private static Dictionary<string, StatMapEntry> ParseStatMap(Dictionary<string, object?> d)
    {
        if (!d.TryGetValue("statMap", out var v)) return new();
        if (v is not Dictionary<string, object?> mapDict) return new();

        var result = new Dictionary<string, StatMapEntry>();
        foreach (var (stat, entry) in mapDict)
        {
            if (entry is Dictionary<string, object?> entryDict)
            {
                result[stat] = ParseStatMapEntry(entryDict);
            }
            else if (entry is List<object?> entryList)
            {
                // Pure list: only mod calls, no div/mult/base
                var mods = new List<Mod>();
                foreach (var item in entryList)
                {
                    var resolved = LuaDslInterpreter.Resolve(item);
                    if (resolved is Mod mod)
                        mods.Add(mod);
                }
                result[stat] = new StatMapEntry { Mods = mods };
            }
        }
        return result;
    }

    internal static StatMapEntry ParseStatMapEntry(Dictionary<string, object?> d)
    {
        var mods = new List<Mod>();
        double? div = null;
        double? mult = null;
        double? baseMod = null;

        foreach (var (key, value) in d)
        {
            if (key == "div" && value is double dDiv) { div = dDiv; continue; }
            if (key == "mult" && value is double dMult) { mult = dMult; continue; }
            if (key == "base" && value is double dBase) { baseMod = dBase; continue; }

            // Numeric keys are array entries (mods)
            if (int.TryParse(key, out _))
            {
                var resolved = LuaDslInterpreter.Resolve(value);
                if (resolved is Mod mod)
                    mods.Add(mod);
            }
        }

        // Also check for list-style entries (if the entry was parsed as a mixed table)
        return new StatMapEntry
        {
            Mods = mods,
            Div = div,
            Mult = mult,
            Base = baseMod,
        };
    }

    private static List<Mod> ParseBaseMods(Dictionary<string, object?> d)
    {
        if (!d.TryGetValue("baseMods", out var v)) return new();
        if (v is not List<object?> list) return new();

        var result = new List<Mod>();
        foreach (var item in list)
        {
            var resolved = LuaDslInterpreter.Resolve(item);
            if (resolved is Mod mod)
                result.Add(mod);
        }
        return result;
    }

    private static List<string> ParseStringList(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return new();
        if (v is not List<object?> list) return new();

        var result = new List<string>();
        foreach (var item in list)
        {
            if (item is string s)
                result.Add(s);
        }
        return result;
    }

    private static List<(string stat, double value)> ParseStatPairs(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return new();
        if (v is not List<object?> list) return new();

        var result = new List<(string stat, double value)>();
        foreach (var item in list)
        {
            if (item is List<object?> pair && pair.Count >= 2 && pair[0] is string stat && pair[1] is double val)
                result.Add((stat, val));
        }
        return result;
    }

    private static Dictionary<string, List<(string stat, double value)>> ParseQualityStats(Dictionary<string, object?> d)
    {
        if (!d.TryGetValue("qualityStats", out var v)) return new();
        if (v is not Dictionary<string, object?> qDict) return new();

        var result = new Dictionary<string, List<(string stat, double value)>>();
        foreach (var (qualType, entries) in qDict)
        {
            if (entries is not List<object?> list) continue;
            var pairs = new List<(string stat, double value)>();
            foreach (var item in list)
            {
                if (item is List<object?> pair && pair.Count >= 2 && pair[0] is string stat && pair[1] is double val)
                    pairs.Add((stat, val));
            }
            result[qualType] = pairs;
        }
        return result;
    }

    private static Dictionary<int, SkillLevelData> ParseLevels(Dictionary<string, object?> d)
    {
        if (!d.TryGetValue("levels", out var v)) return new();
        if (v is not Dictionary<string, object?> levelsDict) return new();

        var result = new Dictionary<int, SkillLevelData>();
        foreach (var (key, entry) in levelsDict)
        {
            if (!int.TryParse(key, out var level)) continue;
            if (entry is not Dictionary<string, object?> levelData) continue;

            result[level] = ParseLevelData(level, levelData);
        }
        return result;
    }

    private static SkillLevelData ParseLevelData(int level, Dictionary<string, object?> d)
    {
        // Extract positional values (numeric keys "1", "2", "3", ...)
        var values = new List<double>();
        for (int i = 1; ; i++)
        {
            if (!d.TryGetValue(i.ToString(), out var v) || v == null)
                break;
            values.Add(v is double dv ? dv : 0);
        }

        // Extract cost dict
        var cost = new Dictionary<string, int>();
        if (d.TryGetValue("cost", out var costVal) && costVal is Dictionary<string, object?> costDict)
        {
            foreach (var (k, cv) in costDict)
            {
                if (cv is double cdv)
                    cost[k] = (int)cdv;
            }
        }

        // Extract stat interpolation list
        var statInterp = new List<int>();
        if (d.TryGetValue("statInterpolation", out var siVal) && siVal is List<object?> siList)
        {
            foreach (var item in siList)
            {
                if (item is double siv)
                    statInterp.Add((int)siv);
            }
        }

        return new SkillLevelData
        {
            Level = level,
            Values = values,
            CritChance = GetDouble(d, "critChance"),
            DamageEffectiveness = GetDouble(d, "damageEffectiveness"),
            LevelRequirement = GetInt(d, "levelRequirement"),
            ManaMultiplier = GetDouble(d, "manaMultiplier"),
            ManaCost = GetDouble(d, "manaCost"),
            Cost = cost,
            StatInterpolation = statInterp,
            BaseMultiplier = GetDouble(d, "baseMultiplier"),
            AttackSpeedMultiplier = GetDouble(d, "attackSpeedMultiplier"),
            ManaReservationFlat = GetDouble(d, "manaReservationFlat"),
            ManaReservationPercent = GetDouble(d, "manaReservationPercent"),
            LifeReservationFlat = GetDouble(d, "lifeReservationFlat"),
            LifeReservationPercent = GetDouble(d, "lifeReservationPercent"),
            SoulCost = GetInt(d, "soulCost"),
            StoredUses = GetInt(d, "storedUses"),
            Cooldown = GetDouble(d, "cooldown"),
            Duration = GetDouble(d, "duration"),
            VaalSoulGainPreventionDuration = GetInt(d, "vaalSoulGainPreventionDuration"),
        };
    }

    private static List<SkillPartDef> ParseParts(Dictionary<string, object?> d)
    {
        if (!d.TryGetValue("parts", out var v)) return new();
        if (v is not List<object?> list) return new();

        var result = new List<SkillPartDef>();
        foreach (var item in list)
        {
            if (item is not Dictionary<string, object?> partDict) continue;

            var name = GetString(partDict, "name") ?? "Unknown";
            var flags = new Dictionary<string, bool>();
            foreach (var (k, pv) in partDict)
            {
                if (k == "name") continue;
                if (pv is true)
                    flags[k] = true;
                else if (pv is false)
                    flags[k] = false;
            }

            result.Add(new SkillPartDef { Name = name, Flags = flags });
        }
        return result;
    }

    // --- Helper methods ---

    private static string? GetString(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;
        if (v is string s) return s;
        if (v is DottedIdentifier di && string.IsNullOrEmpty(di.Namespace)) return di.Name;
        return v.ToString();
    }

    private static bool GetBool(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return false;
        return v is true;
    }

    private static int GetInt(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return 0;
        if (v is double dv) return (int)dv;
        return 0;
    }

    private static double GetDouble(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return 0;
        if (v is double dv) return dv;
        return 0;
    }
}
