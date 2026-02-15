using System.Collections;

namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Flat list modifier storage. Modifiers are stored in a simple list and
/// looked up via linear scan. Used for smaller collections (item mods, gem mods).
/// Ported from ModList.lua.
/// </summary>
public class ModList : ModStore, IEnumerable<Mod>
{
    private readonly List<Mod> _mods = new();

    public ModList(ModStore? parent = null) : base(parent) { }

    public int Count => _mods.Count;
    public Mod this[int index] => _mods[index];

    public override void AddMod(Mod mod) => _mods.Add(mod);

    public override bool ReplaceModInternal(Mod mod)
    {
        for (int i = 0; i < _mods.Count; i++)
        {
            var cur = _mods[i];
            if (cur.Name == mod.Name && cur.Type == mod.Type &&
                cur.Flags == mod.Flags && cur.KeywordFlags == mod.KeywordFlags &&
                cur.Source == mod.Source)
            {
                _mods[i] = mod;
                return true;
            }
        }

        return Parent?.ReplaceModInternal(mod) ?? false;
    }

    /// <summary>
    /// Smart merge: if a mod with identical parameters already exists (for BASE/INC/MORE types),
    /// add the values together instead of creating a duplicate.
    /// </summary>
    public void MergeMod(Mod mod, bool skipNonAdditive = false)
    {
        if (mod.Type is ModType.Base or ModType.Inc or ModType.More)
        {
            for (int i = 0; i < _mods.Count; i++)
            {
                if (ModHelper.CompareModParams(_mods[i], mod))
                {
                    // Clone and merge values
                    var existing = _mods[i];
                    _mods[i] = new Mod
                    {
                        Name = existing.Name,
                        Type = existing.Type,
                        Value = existing.Value.AsNumber() + mod.Value.AsNumber(),
                        Flags = existing.Flags,
                        KeywordFlags = existing.KeywordFlags,
                        Source = existing.Source,
                        Tags = existing.Tags,
                    };
                    return;
                }
            }
        }

        if (!skipNonAdditive)
            AddMod(mod);
    }

    public override void AddList(ModList modList)
    {
        _mods.AddRange(modList._mods);
    }

    public void MergeNewMod(string name, ModType type, ModValue value, string source = "",
        ModFlag flags = ModFlag.None, KeywordFlag keywordFlags = KeywordFlag.None,
        params Tags.ModTag[] tags)
    {
        MergeMod(ModHelper.CreateMod(name, type, value, source, flags, keywordFlags, tags));
    }

    // ─── Internal query implementations ───

    internal override double SumInternal(ModStore context, ModType modType, ModConfig? cfg,
        ModFlag flags, KeywordFlag keywordFlags, string? source, params string[] statNames)
    {
        double result = 0;

        foreach (var statName in statNames)
        {
            foreach (var mod in _mods)
            {
                if (mod.Name != statName) continue;
                if (!ModHelper.MatchesMod(mod, modType, flags, keywordFlags, source)) continue;

                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg);
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

        foreach (var statName in statNames)
        {
            double modResult = 1;
            foreach (var mod in _mods)
            {
                if (mod.Name != statName) continue;
                if (mod.Type != ModType.More) continue;
                if (!ModHelper.MatchesMod(mod, ModType.More, flags, keywordFlags, source)) continue;

                double value;
                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg);
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
            foreach (var mod in _mods)
            {
                if (mod.Name != statName) continue;
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
            foreach (var mod in _mods)
            {
                if (mod.Name != statName) continue;
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
            foreach (var mod in _mods)
            {
                if (mod.Name != statName) continue;
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
        foreach (var statName in statNames)
        {
            foreach (var mod in _mods)
            {
                if (mod.Name != statName) continue;
                if (!ModHelper.MatchesMod(mod, modType, flags, keywordFlags, source)) continue;

                ModValue value;
                if (mod.HasTags)
                {
                    var evalResult = context.EvalMod(mod, cfg);
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
            foreach (var mod in _mods)
            {
                if (mod.Name != statName) continue;
                if (ModHelper.MatchesMod(mod, modType, flags, keywordFlags, source))
                    return true;
            }
        }

        if (Parent != null)
            return Parent.HasModInternal(modType, flags, keywordFlags, source, statNames);

        return false;
    }

    // ─── IEnumerable ───

    public IEnumerator<Mod> GetEnumerator() => _mods.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
