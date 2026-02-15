namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Dictionary-indexed modifier database. Modifiers are stored in lists keyed by name
/// for O(1) lookup. This is the primary storage used by the calculation engine.
/// Ported from ModDB.lua.
/// </summary>
public class ModDB : ModStore
{
    private readonly Dictionary<string, List<Mod>> _mods = new();

    public ModDB(ModStore? parent = null) : base(parent) { }

    public override void AddMod(Mod mod)
    {
        if (!_mods.TryGetValue(mod.Name, out var list))
        {
            list = new List<Mod>();
            _mods[mod.Name] = list;
        }
        list.Add(mod);
    }

    public override bool ReplaceModInternal(Mod mod)
    {
        if (!_mods.TryGetValue(mod.Name, out var list))
        {
            _mods[mod.Name] = new List<Mod>();
            return Parent?.ReplaceModInternal(mod) ?? false;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var cur = list[i];
            if (cur.Name == mod.Name && cur.Type == mod.Type &&
                cur.Flags == mod.Flags && cur.KeywordFlags == mod.KeywordFlags &&
                cur.Source == mod.Source && !cur.Replaced)
            {
                mod.Replaced = true;
                list[i] = mod;
                return true;
            }
        }

        return Parent?.ReplaceModInternal(mod) ?? false;
    }

    public override void AddList(ModList modList)
    {
        foreach (var mod in modList)
        {
            AddMod(mod);
        }
    }

    public void AddDB(ModDB other)
    {
        foreach (var (name, otherList) in other._mods)
        {
            if (!_mods.TryGetValue(name, out var list))
            {
                list = new List<Mod>();
                _mods[name] = list;
            }
            list.AddRange(otherList);
        }
    }

    // ─── Internal query implementations ───

    internal override double SumInternal(ModStore context, ModType modType, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames)
    {
        double result = 0;
        var globalLimits = new Dictionary<string, double>();

        foreach (var statName in statNames)
        {
            if (!_mods.TryGetValue(statName, out var list))
                continue;

            foreach (var mod in list)
            {
                if (mod.Type != modType) continue;
                if (!ModHelper.MatchesMod(mod, modType, flags, keywordFlags, source)) continue;

                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg, globalLimits);
                    result += evalResult?.Kind == ModValueKind.Number ? evalResult.Value.AsNumber() : 0;
                }
                else
                {
                    result += mod.Value.AsNumber();
                }
            }
        }

        if (Parent != null)
            result += Parent.SumInternal(context, modType, cfg, flags, keywordFlags, source, statNames);

        return result;
    }

    internal override double MoreInternal(ModStore context, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames)
    {
        double result = 1;
        var globalLimits = new Dictionary<string, double>();

        foreach (var statName in statNames)
        {
            if (!_mods.TryGetValue(statName, out var list))
                continue;

            double modResult = 1;
            foreach (var mod in list)
            {
                if (mod.Type != ModType.More) continue;
                if (!ModHelper.MatchesMod(mod, ModType.More, flags, keywordFlags, source)) continue;

                double value;
                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg, globalLimits);
                    value = evalResult?.Kind == ModValueKind.Number ? evalResult.Value.AsNumber() : 0;
                }
                else
                {
                    value = mod.Value.AsNumber();
                }

                modResult *= (1 + value / 100.0);
            }

            result *= Math.Round(modResult, 2);
        }

        if (Parent != null)
            result *= Parent.MoreInternal(context, cfg, flags, keywordFlags, source, statNames);

        return result;
    }

    internal override bool FlagInternal(ModStore context, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames)
    {
        foreach (var statName in statNames)
        {
            if (!_mods.TryGetValue(statName, out var list))
                continue;

            foreach (var mod in list)
            {
                if (mod.Type != ModType.Flag) continue;
                if (!ModHelper.MatchesMod(mod, ModType.Flag, flags, keywordFlags, source)) continue;

                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg);
                    if (evalResult.HasValue) return true;
                }
                else if (mod.Value.Kind == ModValueKind.Boolean && mod.Value.AsBoolean())
                {
                    return true;
                }
            }
        }

        if (Parent != null)
            return Parent.FlagInternal(context, cfg, flags, keywordFlags, source, statNames);

        return false;
    }

    internal override ModValue? OverrideInternal(ModStore context, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames)
    {
        foreach (var statName in statNames)
        {
            if (!_mods.TryGetValue(statName, out var list))
                continue;

            foreach (var mod in list)
            {
                if (mod.Type != ModType.Override) continue;
                if (!ModHelper.MatchesMod(mod, ModType.Override, flags, keywordFlags, source)) continue;

                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg);
                    if (evalResult.HasValue) return evalResult;
                }
                else
                {
                    return mod.Value;
                }
            }
        }

        if (Parent != null)
            return Parent.OverrideInternal(context, cfg, flags, keywordFlags, source, statNames);

        return null;
    }

    internal override void ListInternal(ModStore context, List<ModValue> result, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames)
    {
        foreach (var statName in statNames)
        {
            if (!_mods.TryGetValue(statName, out var list))
                continue;

            foreach (var mod in list)
            {
                if (mod.Type != ModType.List) continue;
                if (!ModHelper.MatchesMod(mod, ModType.List, flags, keywordFlags, source)) continue;

                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg);
                    if (evalResult.HasValue) result.Add(evalResult.Value);
                }
                else
                {
                    result.Add(mod.Value);
                }
            }
        }

        if (Parent != null)
            Parent.ListInternal(context, result, cfg, flags, keywordFlags, source, statNames);
    }

    internal override void TabulateInternal(ModStore context, List<TabulatedMod> result,
        ModType? modType, ModConfig? cfg, ModFlag flags, KeywordFlag keywordFlags,
        string? source, params string[] statNames)
    {
        var globalLimits = new Dictionary<string, double>();

        foreach (var statName in statNames)
        {
            if (!_mods.TryGetValue(statName, out var list))
                continue;

            foreach (var mod in list)
            {
                if (!ModHelper.MatchesMod(mod, modType, flags, keywordFlags, source)) continue;

                ModValue value;
                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg, globalLimits);
                    if (!evalResult.HasValue) continue;
                    value = evalResult.Value;
                }
                else
                {
                    value = mod.Value;
                }

                bool isZero = value.Kind == ModValueKind.Number && value.AsNumber() == 0;
                if (!isZero || mod.Type == ModType.Override)
                    result.Add(new TabulatedMod(value, mod));
            }
        }

        if (Parent != null)
            Parent.TabulateInternal(context, result, modType, cfg, flags, keywordFlags, source, statNames);
    }

    public override bool HasModInternal(ModType modType, ModFlag flags,
        KeywordFlag keywordFlags, string? source, params string[] statNames)
    {
        foreach (var statName in statNames)
        {
            if (!_mods.TryGetValue(statName, out var list))
                continue;

            foreach (var mod in list)
            {
                if (ModHelper.MatchesMod(mod, modType, flags, keywordFlags, source))
                    return true;
            }
        }

        if (Parent != null)
            return Parent.HasModInternal(modType, flags, keywordFlags, source, statNames);

        return false;
    }
}
