using System.Globalization;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Resolves parsed Lua DSL constructs (from LuaSkillParser) into typed C# objects.
/// Handles: DottedIdentifier → enum values, FunctionCall → Mod objects, tag tables → ModTag objects.
/// </summary>
internal static class LuaDslInterpreter
{
    /// <summary>
    /// Recursively resolve all DSL constructs in a parsed object tree.
    /// </summary>
    public static object? Resolve(object? parsed)
    {
        return parsed switch
        {
            FunctionCall fc => ResolveFunction(fc),
            DottedIdentifier di => ResolveIdentifier(di),
            Dictionary<string, object?> dict => ResolveDict(dict),
            List<object?> list => ResolveList(list),
            _ => parsed, // primitives pass through
        };
    }

    private static object? ResolveFunction(FunctionCall fc)
    {
        return fc.Name switch
        {
            "mod" => InterpretMod(fc.Args),
            "skill" => InterpretSkill(fc.Args),
            "flag" => InterpretFlag(fc.Args),
            "bit.bor" => InterpretBitBor(fc.Args),
            _ => fc, // Unknown functions preserved as-is
        };
    }

    private static object? ResolveIdentifier(DottedIdentifier di)
    {
        if (string.IsNullOrEmpty(di.Namespace))
            return di; // bare identifier, keep as-is

        return di.Namespace switch
        {
            "SkillType" => (double)(int)Enum.Parse<SkillType>(di.Name),
            "ModFlag" => (double)(uint)Enum.Parse<ModFlag>(di.Name),
            "KeywordFlag" => (double)(uint)Enum.Parse<KeywordFlag>(di.Name),
            "ModType" => di.Name, // Return as string for mod type parsing
            _ => di, // Unknown namespace, preserve
        };
    }

    private static Dictionary<string, object?> ResolveDict(Dictionary<string, object?> dict)
    {
        var result = new Dictionary<string, object?>(dict.Count);
        foreach (var (key, value) in dict)
            result[key] = Resolve(value);
        return result;
    }

    private static List<object?> ResolveList(List<object?> list)
    {
        var result = new List<object?>(list.Count);
        foreach (var item in list)
            result.Add(Resolve(item));
        return result;
    }

    /// <summary>
    /// Interpret a mod() function call: mod(name, type, value, flags, keywordFlags, ...tags)
    /// </summary>
    public static Mod InterpretMod(List<object?> args)
    {
        if (args.Count < 2)
            throw new FormatException($"mod() requires at least 2 arguments, got {args.Count}");

        var name = ResolveToString(args[0]);
        var type = ResolveToModType(args[1]);
        var value = args.Count > 2 ? ResolveModValue(args[2], type) : (ModValue)0;
        var flags = args.Count > 3 ? ResolveToModFlag(args[3]) : ModFlag.None;
        var keywordFlags = args.Count > 4 ? ResolveToKeywordFlag(args[4]) : KeywordFlag.None;

        var tags = new List<ModTag>();
        for (int i = 5; i < args.Count; i++)
        {
            var tag = ResolveToTag(args[i]);
            if (tag != null)
                tags.Add(tag);
        }

        return new Mod
        {
            Name = name,
            Type = type,
            Value = value,
            Flags = flags,
            KeywordFlags = keywordFlags,
            Tags = tags,
        };
    }

    /// <summary>
    /// Interpret a skill() function call: skill(key, value, ...tags)
    /// → mod("SkillData", "LIST", {key, value}, 0, 0, ...tags)
    /// </summary>
    public static Mod InterpretSkill(List<object?> args)
    {
        if (args.Count < 1)
            throw new FormatException($"skill() requires at least 1 argument, got {args.Count}");

        var key = ResolveToString(args[0]);
        var rawValue = args.Count > 1 ? Resolve(args[1]) : null;

        var tags = new List<ModTag>();
        for (int i = 2; i < args.Count; i++)
        {
            var tag = ResolveToTag(args[i]);
            if (tag != null)
                tags.Add(tag);
        }

        return new Mod
        {
            Name = "SkillData",
            Type = ModType.List,
            Value = ModValue.FromComplex(new Dictionary<string, object?>
            {
                ["key"] = key,
                ["value"] = rawValue,
            }),
            Flags = ModFlag.None,
            KeywordFlags = KeywordFlag.None,
            Tags = tags,
        };
    }

    /// <summary>
    /// Interpret a flag() function call: flag(key, ...tags)
    /// → mod(key, "FLAG", true, 0, 0, ...tags)
    /// </summary>
    public static Mod InterpretFlag(List<object?> args)
    {
        if (args.Count < 1)
            throw new FormatException("flag() requires at least 1 argument");

        var name = ResolveToString(args[0]);

        var tags = new List<ModTag>();
        for (int i = 1; i < args.Count; i++)
        {
            var tag = ResolveToTag(args[i]);
            if (tag != null)
                tags.Add(tag);
        }

        return new Mod
        {
            Name = name,
            Type = ModType.Flag,
            Value = true,
            Flags = ModFlag.None,
            KeywordFlags = KeywordFlag.None,
            Tags = tags,
        };
    }

    private static object? InterpretBitBor(List<object?> args)
    {
        long result = 0;
        foreach (var arg in args)
        {
            var resolved = Resolve(arg);
            result |= ResolveToLong(resolved);
        }
        return (double)result;
    }

    /// <summary>
    /// Build a ModTag from a parsed tag table like { type = "Condition", var = "LowLife" }.
    /// </summary>
    public static ModTag? BuildTag(Dictionary<string, object?> tagTable)
    {
        // Resolve all values first
        var resolved = new Dictionary<string, object?>(tagTable.Count);
        foreach (var (k, v) in tagTable)
            resolved[k] = Resolve(v);

        var type = GetString(resolved, "type");
        if (type == null) return null;

        ModTag? tag = type switch
        {
            "Condition" => BuildConditionTag(resolved),
            "ActorCondition" => BuildActorConditionTag(resolved),
            "Multiplier" => BuildMultiplierTag(resolved),
            "MultiplierThreshold" => BuildMultiplierThresholdTag(resolved),
            "PerStat" => BuildPerStatTag(resolved),
            "PercentStat" => BuildPercentStatTag(resolved),
            "StatThreshold" => BuildStatThresholdTag(resolved),
            "DistanceRamp" => BuildDistanceRampTag(resolved),
            "MeleeProximity" => BuildMeleeProximityTag(resolved),
            "Limit" => BuildLimitTag(resolved),
            "SkillType" => BuildSkillTypeTag(resolved),
            "SkillName" => BuildSkillNameTag(resolved),
            "SkillId" => BuildSkillIdTag(resolved),
            "SkillPart" => BuildSkillPartTag(resolved),
            "SlotName" => BuildSlotNameTag(resolved),
            "SocketedIn" => BuildSocketedInTag(resolved),
            "ItemCondition" => BuildItemConditionTag(resolved),
            "ModFlagOr" => BuildModFlagOrTag(resolved),
            "KeywordFlagAnd" => BuildKeywordFlagAndTag(resolved),
            "GlobalEffect" => BuildGlobalEffectTag(resolved),
            "MonsterTag" => BuildMonsterTagTag(resolved),
            _ => null, // Unknown tag types are silently ignored
        };

        return tag;
    }

    // --- Tag builders ---

    private static ConditionTag BuildConditionTag(Dictionary<string, object?> d) => new()
    {
        Var = GetString(d, "var"),
        VarList = GetStringList(d, "varList"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static ActorConditionTag BuildActorConditionTag(Dictionary<string, object?> d) => new()
    {
        Var = GetString(d, "var"),
        VarList = GetStringList(d, "varList"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static MultiplierTag BuildMultiplierTag(Dictionary<string, object?> d) => new()
    {
        Var = GetString(d, "var"),
        VarList = GetStringList(d, "varList"),
        Div = GetNullableDouble(d, "div"),
        DivVar = GetString(d, "divVar"),
        Limit = GetNullableDouble(d, "limit"),
        LimitTotal = GetBool(d, "limitTotal"),
        LimitNegTotal = GetBool(d, "limitNegTotal"),
        Base = GetNullableDouble(d, "base"),
        NoFloor = GetBool(d, "noFloor"),
        Invert = GetBool(d, "invert"),
        GlobalLimit = GetNullableDouble(d, "globalLimit"),
        GlobalLimitKey = GetString(d, "globalLimitKey"),
        LimitActor = GetString(d, "limitActor"),
        LimitVar = GetString(d, "limitVar"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static MultiplierThresholdTag BuildMultiplierThresholdTag(Dictionary<string, object?> d) => new()
    {
        Var = GetString(d, "var"),
        Threshold = GetDouble(d, "threshold"),
        ThresholdVar = GetString(d, "thresholdVar"),
        ThresholdActor = GetString(d, "thresholdActor"),
        Upper = GetBool(d, "upper"),
        Equals_ = GetBool(d, "equals"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static PerStatTag BuildPerStatTag(Dictionary<string, object?> d) => new()
    {
        Stat = GetString(d, "stat"),
        StatList = GetStringList(d, "statList"),
        Div = GetNullableDouble(d, "div"),
        Limit = GetNullableDouble(d, "limit"),
        LimitTotal = GetBool(d, "limitTotal"),
        Base = GetNullableDouble(d, "base"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static PercentStatTag BuildPercentStatTag(Dictionary<string, object?> d) => new()
    {
        Stat = GetString(d, "stat"),
        Percent = GetDouble(d, "percent"),
        PercentVar = GetString(d, "percentVar"),
        Floor = GetBool(d, "floor"),
        Limit = GetNullableDouble(d, "limit"),
        LimitTotal = GetBool(d, "limitTotal"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static StatThresholdTag BuildStatThresholdTag(Dictionary<string, object?> d) => new()
    {
        Stat = GetString(d, "stat"),
        Threshold = GetDouble(d, "threshold"),
        ThresholdStat = GetString(d, "thresholdStat"),
        ThresholdPercent = GetNullableDouble(d, "thresholdPercent"),
        Upper = GetBool(d, "upper"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static DistanceRampTag BuildDistanceRampTag(Dictionary<string, object?> d)
    {
        var rampData = d.GetValueOrDefault("ramp");
        var ramp = new List<(double Distance, double Multiplier)>();

        if (rampData is List<object?> rampList)
        {
            foreach (var item in rampList)
            {
                if (item is List<object?> pair && pair.Count >= 2)
                    ramp.Add((ToDouble(pair[0]), ToDouble(pair[1])));
            }
        }

        return new DistanceRampTag
        {
            Ramp = ramp,
            Neg = GetBool(d, "neg"),
            Actor = GetString(d, "actor"),
        };
    }

    private static MeleeProximityTag BuildMeleeProximityTag(Dictionary<string, object?> d)
    {
        var rampData = d.GetValueOrDefault("ramp");
        double near = 1, far = 0;
        if (rampData is List<object?> list && list.Count >= 2)
        {
            near = ToDouble(list[0]);
            far = ToDouble(list[1]);
        }

        return new MeleeProximityTag
        {
            Near = near,
            Far = far,
            Neg = GetBool(d, "neg"),
            Actor = GetString(d, "actor"),
        };
    }

    private static LimitTag BuildLimitTag(Dictionary<string, object?> d) => new()
    {
        Limit = GetNullableDouble(d, "limit"),
        LimitVar = GetString(d, "limitVar"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static SkillTypeTag BuildSkillTypeTag(Dictionary<string, object?> d) => new()
    {
        SkillTypeValue = GetSkillType(d, "skillType"),
        SkillTypeList = GetSkillTypeList(d, "skillTypeList"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static SkillNameTag BuildSkillNameTag(Dictionary<string, object?> d) => new()
    {
        SkillName = GetString(d, "skillName"),
        SkillNameList = GetStringList(d, "skillNameList"),
        IncludeTransfigured = GetBool(d, "includeTransfigured"),
        SummonSkill = GetBool(d, "summonSkill"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static SkillIdTag BuildSkillIdTag(Dictionary<string, object?> d) => new()
    {
        SkillId = GetString(d, "skillId") ?? "",
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static SkillPartTag BuildSkillPartTag(Dictionary<string, object?> d) => new()
    {
        SkillPart = GetNullableInt(d, "skillPart"),
        SkillPartList = GetIntList(d, "skillPartList"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static SlotNameTag BuildSlotNameTag(Dictionary<string, object?> d) => new()
    {
        SlotName = GetString(d, "slotName"),
        SlotNameList = GetStringList(d, "slotNameList"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static SocketedInTag BuildSocketedInTag(Dictionary<string, object?> d) => new()
    {
        SlotName = GetString(d, "slotName"),
        Keyword = GetString(d, "keyword"),
        SocketColor = GetString(d, "socketColor"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static ItemConditionTag BuildItemConditionTag(Dictionary<string, object?> d) => new()
    {
        ItemSlot = GetString(d, "itemSlot"),
        SearchCond = GetString(d, "searchCond"),
        RarityCond = GetString(d, "rarityCond"),
        CorruptedCond = GetNullableBool(d, "corruptedCond"),
        ShaperCond = GetNullableBool(d, "shaperCond"),
        ElderCond = GetNullableBool(d, "elderCond"),
        NameCond = GetString(d, "nameCond"),
        AllSlots = GetBool(d, "allSlots"),
        BothSlots = GetBool(d, "bothSlots"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static ModFlagOrTag BuildModFlagOrTag(Dictionary<string, object?> d) => new()
    {
        ModFlags = GetModFlag(d, "modFlags"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static KeywordFlagAndTag BuildKeywordFlagAndTag(Dictionary<string, object?> d) => new()
    {
        KeywordFlags = GetKeywordFlag(d, "keywordFlags"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static GlobalEffectTag BuildGlobalEffectTag(Dictionary<string, object?> d) => new()
    {
        EffectType = GetString(d, "effectType"),
        EffectName = GetString(d, "effectName"),
        EffectCond = GetString(d, "effectCond"),
        Unscalable = GetBool(d, "unscalable"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    private static MonsterTag BuildMonsterTagTag(Dictionary<string, object?> d) => new()
    {
        MonsterTagValue = GetString(d, "monsterTag"),
        MonsterTagList = GetStringList(d, "monsterTagList"),
        Neg = GetBool(d, "neg"),
        Actor = GetString(d, "actor"),
    };

    // --- Helper methods ---

    private static string ResolveToString(object? value)
    {
        var resolved = Resolve(value);
        return resolved switch
        {
            string s => s,
            DottedIdentifier di when string.IsNullOrEmpty(di.Namespace) => di.Name,
            DottedIdentifier di => $"{di.Namespace}.{di.Name}",
            _ => resolved?.ToString() ?? "",
        };
    }

    private static ModType ResolveToModType(object? value)
    {
        var s = ResolveToString(value).ToUpperInvariant();
        return s switch
        {
            "BASE" => ModType.Base,
            "INC" => ModType.Inc,
            "MORE" => ModType.More,
            "FLAG" => ModType.Flag,
            "OVERRIDE" => ModType.Override,
            "LIST" => ModType.List,
            "MAX" => ModType.Max,
            _ => ModType.Base, // Unknown types (e.g., "DUMMY") default to Base
        };
    }

    private static ModValue ResolveModValue(object? rawValue, ModType type)
    {
        var resolved = Resolve(rawValue);

        if (resolved == null)
            return 0; // nil → 0 (placeholder, filled at calc time)

        if (resolved is double d)
            return d;
        if (resolved is bool b)
            return b;
        if (type == ModType.Flag)
            return resolved is true;

        // Complex value (dict/list for LIST mods)
        return ModValue.FromComplex(resolved);
    }

    private static ModFlag ResolveToModFlag(object? value)
    {
        var resolved = Resolve(value);
        if (resolved == null) return ModFlag.None;
        if (resolved is double d) return (ModFlag)(uint)d;
        if (resolved is DottedIdentifier di && di.Namespace == "ModFlag")
            return Enum.Parse<ModFlag>(di.Name);
        return ModFlag.None;
    }

    private static KeywordFlag ResolveToKeywordFlag(object? value)
    {
        var resolved = Resolve(value);
        if (resolved == null) return KeywordFlag.None;
        if (resolved is double d) return (KeywordFlag)(uint)d;
        if (resolved is DottedIdentifier di && di.Namespace == "KeywordFlag")
            return Enum.Parse<KeywordFlag>(di.Name);
        return KeywordFlag.None;
    }

    private static long ResolveToLong(object? value)
    {
        if (value is double d) return (long)d;
        if (value is int i) return i;
        return 0;
    }

    private static ModTag? ResolveToTag(object? value)
    {
        var resolved = Resolve(value);
        if (resolved is Dictionary<string, object?> dict)
            return BuildTag(dict);
        return null;
    }

    // --- Dictionary accessor helpers ---

    private static string? GetString(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;
        if (v is string s) return s;
        if (v is DottedIdentifier di && string.IsNullOrEmpty(di.Namespace)) return di.Name;
        return v.ToString();
    }

    private static double GetDouble(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return 0;
        return ToDouble(v);
    }

    private static double? GetNullableDouble(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;
        return ToDouble(v);
    }

    private static bool GetBool(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return false;
        if (v is bool b) return b;
        if (v is double dv) return dv != 0;
        return false;
    }

    private static bool? GetNullableBool(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;
        if (v is bool b) return b;
        return null;
    }

    private static int? GetNullableInt(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;
        return (int)ToDouble(v);
    }

    private static SkillType? GetSkillType(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;
        if (v is double dv) return (SkillType)(int)dv;
        if (v is DottedIdentifier di && di.Namespace == "SkillType")
            return Enum.Parse<SkillType>(di.Name);
        return null;
    }

    private static ModFlag GetModFlag(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return ModFlag.None;
        if (v is double dv) return (ModFlag)(uint)dv;
        return ModFlag.None;
    }

    private static KeywordFlag GetKeywordFlag(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return KeywordFlag.None;
        if (v is double dv) return (KeywordFlag)(uint)dv;
        return KeywordFlag.None;
    }

    private static List<string>? GetStringList(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return null;
        if (v is not List<object?> list) return null;
        return list.Select(x => x?.ToString() ?? "").ToList();
    }

    private static List<SkillType>? GetSkillTypeList(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return null;
        if (v is not List<object?> list) return null;
        return list.Select(x => (SkillType)(int)ToDouble(x)).ToList();
    }

    private static List<int>? GetIntList(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return null;
        if (v is not List<object?> list) return null;
        return list.Select(x => (int)ToDouble(x)).ToList();
    }

    private static double ToDouble(object? v)
    {
        if (v is double d) return d;
        if (v is int i) return i;
        if (v is string s) return double.Parse(s, CultureInfo.InvariantCulture);
        return 0;
    }
}
