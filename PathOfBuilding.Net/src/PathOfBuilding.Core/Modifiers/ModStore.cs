using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Abstract base class for modifier storage (ModDB and ModList).
/// Provides the public query API (Sum, More, Flag, Override, List, Tabulate, Max, Combine),
/// condition/multiplier evaluation, and the tag evaluation engine (EvalMod).
/// Ported from ModStore.lua.
/// </summary>
public abstract class ModStore
{
    public ModStore? Parent { get; set; }
    public Dictionary<string, bool> Conditions { get; } = new();
    public Dictionary<string, double> Multipliers { get; } = new();

    /// <summary>
    /// The actor this ModStore belongs to. Simplified stub for Phase 1;
    /// full actor system will be implemented in Phase 2.
    /// </summary>
    public Actor? Actor { get; set; }

    protected ModStore(ModStore? parent = null)
    {
        Parent = parent;
        Actor = parent?.Actor;
    }

    // ─── Abstract methods (implemented by ModDB and ModList) ───

    public abstract void AddMod(Mod mod);
    public abstract bool ReplaceModInternal(Mod mod);

    internal abstract double SumInternal(ModStore context, ModType modType, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames);

    internal abstract double MoreInternal(ModStore context, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames);

    internal abstract bool FlagInternal(ModStore context, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames);

    internal abstract ModValue? OverrideInternal(ModStore context, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames);

    internal abstract void ListInternal(ModStore context, List<ModValue> result, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames);

    internal abstract void TabulateInternal(ModStore context, List<TabulatedMod> result,
        ModType? modType, ModConfig? cfg, ModFlag flags, KeywordFlag keywordFlags,
        string? source, params string[] statNames);

    public abstract bool HasModInternal(ModType modType, ModFlag flags,
        KeywordFlag keywordFlags, string? source, params string[] statNames);

    // ─── Batch operations ───

    public abstract void AddList(ModList modList);

    // ─── Public API ───

    public double Sum(ModType modType, ModConfig? cfg, params string[] statNames)
    {
        var (flags, keywordFlags, source) = ExtractConfig(cfg);
        return SumInternal(this, modType, cfg, flags, keywordFlags, source, statNames);
    }

    public double More(ModConfig? cfg, params string[] statNames)
    {
        var (flags, keywordFlags, source) = ExtractConfig(cfg);
        return MoreInternal(this, cfg, flags, keywordFlags, source, statNames);
    }

    public bool Flag(ModConfig? cfg, params string[] statNames)
    {
        var (flags, keywordFlags, source) = ExtractConfig(cfg);
        return FlagInternal(this, cfg, flags, keywordFlags, source, statNames);
    }

    public ModValue? Override(ModConfig? cfg, params string[] statNames)
    {
        var (flags, keywordFlags, source) = ExtractConfig(cfg);
        return OverrideInternal(this, cfg, flags, keywordFlags, source, statNames);
    }

    public List<ModValue> List(ModConfig? cfg, params string[] statNames)
    {
        var (flags, keywordFlags, source) = ExtractConfig(cfg);
        var result = new List<ModValue>();
        ListInternal(this, result, cfg, flags, keywordFlags, source, statNames);
        return result;
    }

    public List<TabulatedMod> Tabulate(ModType? modType, ModConfig? cfg, params string[] statNames)
    {
        var (flags, keywordFlags, source) = ExtractConfig(cfg);
        var result = new List<TabulatedMod>();
        TabulateInternal(this, result, modType, cfg, flags, keywordFlags, source, statNames);
        return result;
    }

    public double? Max(ModConfig? cfg, params string[] statNames)
    {
        double? max = null;
        foreach (var entry in Tabulate(ModType.Max, cfg, statNames))
        {
            double val = EvalModNumber(entry.Mod, cfg);
            if (val > (max ?? 0))
                max = val;
        }
        return max;
    }

    public object? Combine(ModType modType, ModConfig? cfg, params string[] statNames)
    {
        return modType switch
        {
            ModType.More => More(cfg, statNames),
            ModType.Flag => Flag(cfg, statNames),
            ModType.Override => Override(cfg, statNames),
            ModType.List => List(cfg, statNames),
            ModType.Max => Max(cfg, statNames),
            _ => Sum(modType, cfg, statNames),
        };
    }

    public bool HasMod(ModType modType, ModConfig? cfg, params string[] statNames)
    {
        var (flags, keywordFlags, source) = ExtractConfig(cfg);
        return HasModInternal(modType, flags, keywordFlags, source, statNames);
    }

    // ─── Condition/Multiplier/Stat lookups ───

    public bool GetCondition(string var, ModConfig? cfg, bool noMod = false)
    {
        if (Conditions.TryGetValue(var, out bool value) && value)
            return true;

        if (Parent != null && Parent.GetCondition(var, cfg, noMod: true))
            return true;

        if (!noMod && Flag(cfg, $"Condition:{var}"))
            return true;

        return false;
    }

    public double GetMultiplier(string var, ModConfig? cfg, bool noMod = false)
    {
        if (!noMod)
        {
            var overrideVal = Override(cfg, $"Multiplier:{var}");
            if (overrideVal.HasValue)
                return overrideVal.Value.AsNumber();
        }

        double result = Multipliers.TryGetValue(var, out double local) ? local : 0;

        if (Parent != null)
            result += Parent.GetMultiplier(var, cfg, noMod: true);

        if (!noMod)
            result += Sum(ModType.Base, cfg, $"Multiplier:{var}");

        return result;
    }

    public double GetStat(string stat, ModConfig? cfg)
    {
        // Simplified stub — full implementation needs the actor/output system.
        // For now, check actor output, then skillStats, then return 0.
        if (Actor?.Output != null && Actor.Output.TryGetValue(stat, out double actorVal))
            return actorVal;

        if (cfg?.SkillStats != null && cfg.SkillStats.TryGetValue(stat, out double skillVal))
            return skillVal;

        return 0;
    }

    // ─── Mod addition helpers ───

    public void NewMod(string name, ModType type, ModValue value, string source = "",
        ModFlag flags = ModFlag.None, KeywordFlag keywordFlags = KeywordFlag.None,
        params ModTag[] tags)
    {
        AddMod(ModHelper.CreateMod(name, type, value, source, flags, keywordFlags, tags));
    }

    public void ReplaceMod(string name, ModType type, ModValue value, string source = "",
        ModFlag flags = ModFlag.None, KeywordFlag keywordFlags = KeywordFlag.None,
        params ModTag[] tags)
    {
        var mod = ModHelper.CreateMod(name, type, value, source, flags, keywordFlags, tags);
        if (!ReplaceModInternal(mod))
            AddMod(mod);
    }

    public void ScaleAddMod(Mod mod, double scale)
    {
        // Check for unscalable tag
        foreach (var tag in mod.Tags)
        {
            if (tag is MultiplierTag { GlobalLimitKey: "unscalable" })
            {
                AddMod(mod);
                return;
            }
        }

        if (scale == 1.0)
        {
            AddMod(mod);
            return;
        }

        // Clone and scale
        var scaledMod = CloneMod(mod);
        if (scaledMod.Value.Kind == ModValueKind.Number)
        {
            double scaled = scaledMod.Value.AsNumber() * scale;
            scaled = Data.PrecisionData.ApplyPrecision(scaled, scaledMod.Name, scaledMod.Type);
            scaledMod.Value = scaled;
        }
        AddMod(scaledMod);
    }

    public void ScaleAddList(ModList modList, double scale)
    {
        if (scale == 1.0)
        {
            AddList(modList);
            return;
        }

        foreach (var mod in modList)
        {
            ScaleAddMod(mod, scale);
        }
    }

    // ─── Tag Evaluation Engine ───

    /// <summary>
    /// Evaluates all tags on a modifier and returns the effective value,
    /// or null if any tag condition fails (gates the modifier).
    /// Ported from ModStore.lua:EvalMod.
    /// </summary>
    public ModValue? EvalMod(Mod mod, ModConfig? cfg, Dictionary<string, double>? globalLimits = null)
    {
        double value = mod.Value.Kind == ModValueKind.Number ? mod.Value.AsNumber() : 0;
        bool isBoolValue = mod.Value.Kind == ModValueKind.Boolean;

        foreach (var tag in mod.Tags)
        {
            switch (tag)
            {
                case MultiplierTag mt:
                {
                    ModStore target = ResolveActor(mt.Actor) ?? this;
                    double baseVal = 0;
                    if (mt.VarList != null)
                    {
                        foreach (var v in mt.VarList)
                            baseVal += target.GetMultiplier(v, cfg);
                    }
                    else if (mt.Var != null)
                    {
                        baseVal = target.GetMultiplier(mt.Var, cfg);
                    }

                    double div = mt.Div ?? 1;
                    double mult = mt.NoFloor ? baseVal / div : Math.Floor(baseVal / div + 0.0001);

                    if (mt.Limit.HasValue)
                    {
                        if (mt.LimitTotal)
                        {
                            value = Math.Min(value * mult + (mt.Base ?? 0), mt.Limit.Value);
                            continue;
                        }
                        else if (mt.LimitNegTotal)
                        {
                            value = Math.Max(value * mult + (mt.Base ?? 0), mt.Limit.Value);
                            continue;
                        }
                        else
                        {
                            mult = Math.Min(mult, mt.Limit.Value);
                        }
                    }

                    if (mt.Invert && mult != 0)
                        mult = 1.0 / mult;

                    value = value * mult + (mt.Base ?? 0);
                    break;
                }

                case MultiplierThresholdTag mtt:
                {
                    ModStore target = ResolveActor(mtt.Actor) ?? this;
                    double mult = target.GetMultiplier(mtt.Var ?? "", cfg);
                    double threshold = mtt.ThresholdVar != null
                        ? target.GetMultiplier(mtt.ThresholdVar, cfg)
                        : mtt.Threshold;

                    if (mtt.Upper && mult > threshold) return null;
                    if (mtt.Equals_ && mult != threshold) return null;
                    if (!mtt.Upper && !mtt.Equals_ && mult < threshold) return null;
                    break;
                }

                case PerStatTag pst:
                {
                    ModStore target = ResolveActor(pst.Actor) ?? this;
                    double baseVal = 0;
                    if (pst.StatList != null)
                    {
                        foreach (var stat in pst.StatList)
                            baseVal += target.GetStat(stat, cfg);
                    }
                    else if (pst.Stat != null)
                    {
                        baseVal = target.GetStat(pst.Stat, cfg);
                    }

                    double div = pst.Div ?? 1;
                    double mult = Math.Floor(baseVal / div + 0.0001);

                    if (pst.Limit.HasValue)
                    {
                        if (pst.LimitTotal)
                        {
                            value = Math.Min(value * mult + (pst.Base ?? 0), pst.Limit.Value);
                            continue;
                        }
                        mult = Math.Min(mult, pst.Limit.Value);
                    }

                    value = value * mult + (pst.Base ?? 0);
                    break;
                }

                case PercentStatTag pcst:
                {
                    ModStore target = ResolveActor(pcst.Actor) ?? this;
                    double baseVal = 0;
                    if (pcst.Stat != null)
                        baseVal = target.GetStat(pcst.Stat, cfg);

                    double percent = pcst.PercentVar != null
                        ? GetMultiplier(pcst.PercentVar, cfg)
                        : pcst.Percent;
                    double mult = baseVal * (percent / 100.0);

                    if (pcst.Floor)
                        mult = Math.Floor(mult);

                    if (pcst.Limit.HasValue)
                    {
                        if (pcst.LimitTotal)
                        {
                            value = Math.Min(Math.Ceiling(value * mult), pcst.Limit.Value);
                            continue;
                        }
                        mult = Math.Min(mult, pcst.Limit.Value);
                    }

                    value = Math.Ceiling(value * mult);
                    break;
                }

                case StatThresholdTag stt:
                {
                    double stat = stt.Stat != null ? GetStat(stt.Stat, cfg) : 0;
                    double threshold = stt.ThresholdStat != null
                        ? GetStat(stt.ThresholdStat, cfg)
                        : stt.Threshold;

                    if (stt.Upper && stat > threshold) return null;
                    if (!stt.Upper && stat < threshold) return null;
                    break;
                }

                case DistanceRampTag drt:
                {
                    if (cfg?.SkillDist == null) return null;
                    double dist = cfg.SkillDist.Value;
                    var ramp = drt.Ramp;

                    if (dist <= ramp[0].Distance)
                    {
                        value *= ramp[0].Multiplier;
                    }
                    else if (dist >= ramp[^1].Distance)
                    {
                        value *= ramp[^1].Multiplier;
                    }
                    else
                    {
                        for (int i = 0; i < ramp.Count - 1; i++)
                        {
                            var next = ramp[i + 1];
                            if (dist <= next.Distance)
                            {
                                var cur = ramp[i];
                                value *= cur.Multiplier + (next.Multiplier - cur.Multiplier)
                                    * (dist - cur.Distance) / (next.Distance - cur.Distance);
                                break;
                            }
                        }
                    }
                    break;
                }

                case MeleeProximityTag mpt:
                {
                    if (cfg?.SkillDist == null) return null;
                    double dist = cfg.SkillDist.Value;

                    if (dist <= 15)
                        value *= mpt.Near;
                    else if (dist <= 39)
                        value *= mpt.Near - (mpt.Near / 25.0) * (dist - 15);
                    else
                        value = 0;
                    break;
                }

                case LimitTag lt:
                {
                    double limit = lt.Limit ?? (lt.LimitVar != null ? GetMultiplier(lt.LimitVar, cfg) : double.MaxValue);
                    value = Math.Min(value, limit);
                    break;
                }

                case ConditionTag ct:
                {
                    bool match = false;
                    if (ct.VarList != null)
                    {
                        foreach (var v in ct.VarList)
                        {
                            if (GetCondition(v, cfg) || (cfg?.SkillCond != null && cfg.SkillCond.TryGetValue(v, out bool sc) && sc))
                            {
                                match = true;
                                break;
                            }
                        }
                    }
                    else if (ct.Var != null)
                    {
                        match = GetCondition(ct.Var, cfg) || (cfg?.SkillCond != null && cfg.SkillCond.TryGetValue(ct.Var, out bool sc) && sc);
                    }

                    if (ct.Neg) match = !match;
                    if (!match) return null;
                    break;
                }

                case ActorConditionTag act:
                {
                    bool match = false;
                    ModStore? target = ResolveActor(act.Actor) ?? this;

                    if (act.VarList != null)
                    {
                        foreach (var v in act.VarList)
                        {
                            if (target.GetCondition(v, cfg))
                            {
                                match = true;
                                break;
                            }
                        }
                    }
                    else if (act.Var != null)
                    {
                        match = target.GetCondition(act.Var, cfg);
                    }

                    if (act.Neg) match = !match;
                    if (!match) return null;
                    break;
                }

                case SkillTypeTag stt:
                {
                    bool match = false;
                    if (cfg?.SkillTypes != null)
                    {
                        if (stt.SkillTypeList != null)
                        {
                            foreach (var st in stt.SkillTypeList)
                            {
                                if (cfg.SkillTypes.Contains(st))
                                {
                                    match = true;
                                    break;
                                }
                            }
                        }
                        else if (stt.SkillTypeValue.HasValue)
                        {
                            match = cfg.SkillTypes.Contains(stt.SkillTypeValue.Value);
                        }
                    }

                    if (stt.Neg) match = !match;
                    if (!match) return null;
                    break;
                }

                case SkillNameTag snt:
                {
                    bool match = false;
                    string matchName = snt.SummonSkill
                        ? (cfg?.SummonSkillName ?? "")
                        : (cfg?.SkillName ?? "");
                    matchName = matchName.ToLowerInvariant();

                    if (snt.SkillNameList != null)
                    {
                        foreach (var name in snt.SkillNameList)
                        {
                            if (string.Equals(name, matchName, StringComparison.OrdinalIgnoreCase))
                            {
                                match = true;
                                break;
                            }
                        }
                    }
                    else if (snt.SkillName != null)
                    {
                        match = string.Equals(snt.SkillName, matchName, StringComparison.OrdinalIgnoreCase);
                    }

                    if (snt.Neg) match = !match;
                    if (!match) return null;
                    break;
                }

                case SkillIdTag sit:
                {
                    // Simplified: no skillGrantedEffect yet
                    return null;
                }

                case SkillPartTag spt:
                {
                    if (cfg == null) return null;
                    bool match = false;
                    if (spt.SkillPartList != null)
                    {
                        foreach (var part in spt.SkillPartList)
                        {
                            if (part == cfg.SkillPart)
                            {
                                match = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        match = spt.SkillPart == cfg.SkillPart;
                    }

                    if (spt.Neg) match = !match;
                    if (!match) return null;
                    break;
                }

                case SlotNameTag slnt:
                {
                    if (cfg == null) return null;
                    bool match = false;
                    if (slnt.SlotNameList != null)
                    {
                        foreach (var slot in slnt.SlotNameList)
                        {
                            if (slot == cfg.SlotName)
                            {
                                match = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        match = slnt.SlotName == cfg.SlotName;
                    }

                    if (slnt.Neg) match = !match;
                    if (!match) return null;
                    break;
                }

                case ModFlagOrTag mfot:
                {
                    if (cfg == null) return null;
                    if ((cfg.Flags & mfot.ModFlags) == 0) return null;
                    break;
                }

                case KeywordFlagAndTag kfat:
                {
                    if (cfg == null) return null;
                    if ((cfg.KeywordFlags & kfat.KeywordFlags) != kfat.KeywordFlags) return null;
                    break;
                }

                case SocketedInTag:
                case ItemConditionTag:
                case MonsterTag:
                {
                    // These require the full item/actor system — stub for Phase 1.
                    // Return null (tag fails) since we can't evaluate without item data.
                    return null;
                }
            }
        }

        // Apply global limits
        if (globalLimits != null)
        {
            foreach (var tag in mod.Tags)
            {
                if (tag is MultiplierTag { GlobalLimit: not null, GlobalLimitKey: not null } mt)
                {
                    globalLimits.TryGetValue(mt.GlobalLimitKey, out double current);
                    if (current + value > mt.GlobalLimit.Value)
                        value = mt.GlobalLimit.Value - current;
                    globalLimits[mt.GlobalLimitKey] = current + value;
                }
            }
        }

        if (isBoolValue)
            return mod.Value;

        return (ModValue)value;
    }

    // ─── Helpers ───

    private double EvalModNumber(Mod mod, ModConfig? cfg)
    {
        var result = EvalMod(mod, cfg);
        if (result.HasValue && result.Value.Kind == ModValueKind.Number)
            return result.Value.AsNumber();
        return 0;
    }

    private static (ModFlag Flags, KeywordFlag KeywordFlags, string? Source) ExtractConfig(ModConfig? cfg)
    {
        if (cfg == null)
            return (ModFlag.None, KeywordFlag.None, null);
        return (cfg.Flags, cfg.KeywordFlags, cfg.Source);
    }

    private ModStore? ResolveActor(string? actorName)
    {
        if (actorName == null || Actor == null)
            return null;

        return Actor.ResolveModDB(actorName);
    }

    private static Mod CloneMod(Mod mod)
    {
        return new Mod
        {
            Name = mod.Name,
            Type = mod.Type,
            Value = mod.Value,
            Flags = mod.Flags,
            KeywordFlags = mod.KeywordFlags,
            Source = mod.Source,
            Tags = mod.Tags,
        };
    }
}

/// <summary>
/// Result of Tabulate: a mod paired with its evaluated value.
/// </summary>
public record struct TabulatedMod(ModValue Value, Mod Mod);

