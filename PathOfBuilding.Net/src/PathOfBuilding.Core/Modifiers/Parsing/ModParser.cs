using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using PathOfBuilding.Core.Skills;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Modifiers.Parsing;

/// <summary>
/// The core modifier parsing engine. Converts human-readable modifier text
/// (e.g., "+50 to maximum Life", "10% increased Attack Speed") into structured Mod objects.
/// Ported from ModParser.lua scan() + parseMod() functions.
/// </summary>
public static class ModParser
{
    private static readonly ConcurrentDictionary<string, (List<Mod>?, string?)> Cache = new();

    /// <summary>
    /// Parses a modifier line into a list of Mod objects.
    /// Returns null if the line cannot be parsed.
    /// Results are cached for performance.
    /// </summary>
    public static List<Mod>? ParseMod(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        if (Cache.TryGetValue(line, out var cached))
            return cached.Item1;

        // Two-pass parsing: order=1 first, if incomplete (has extra text) try order=2
        var (modList, extra) = ParseModInternal(line, 1);
        if (modList != null && extra != null)
        {
            (modList, extra) = ParseModInternal(line, 2);
        }

        // Normalize empty list to null (no mods parsed = failure)
        if (modList != null && modList.Count == 0)
            modList = null;

        Cache[line] = (modList, extra);
        return modList;
    }

    /// <summary>
    /// Clears the parse cache. Useful for testing.
    /// </summary>
    public static void ClearCache() => Cache.Clear();

    // ─── Scan functions ───

    /// <summary>
    /// Scans a line for the earliest and longest match from a regex pattern list.
    /// Returns the matched value, remainder of line with match removed, and captures.
    /// Ported from ModParser.lua scan() function.
    /// </summary>
    internal static ScanResult<T> ScanRegex<T>(string line, (Regex Pattern, T Value)[] patternList)
    {
        string lineLower = line.ToLowerInvariant();

        int bestIndex = int.MaxValue;
        int bestEndIndex = -1;
        int bestPatternLen = -1;
        T? bestVal = default;
        int bestStart = 0;
        int bestEnd = 0;
        string[]? bestCaps = null;

        for (int p = 0; p < patternList.Length; p++)
        {
            var (pattern, value) = patternList[p];
            var match = pattern.Match(lineLower);
            if (match.Success)
            {
                int index = match.Index;
                int endIndex = match.Index + match.Length - 1;
                int patternLen = pattern.ToString().Length;

                if (index < bestIndex
                    || (index == bestIndex && endIndex > bestEndIndex)
                    || (index == bestIndex && endIndex == bestEndIndex && patternLen > bestPatternLen))
                {
                    bestIndex = index;
                    bestEndIndex = endIndex;
                    bestPatternLen = patternLen;
                    bestVal = value;
                    bestStart = match.Index;
                    bestEnd = match.Index + match.Length;

                    // Extract captures
                    if (match.Groups.Count > 1)
                    {
                        bestCaps = new string[match.Groups.Count - 1];
                        for (int i = 1; i < match.Groups.Count; i++)
                            bestCaps[i - 1] = match.Groups[i].Value;
                    }
                    else
                    {
                        bestCaps = null;
                    }
                }
            }
        }

        if (bestVal is not null)
        {
            // Remove the matched portion from the line (preserving original case)
            string remainder = line[..bestStart] + line[bestEnd..];
            return new ScanResult<T>(bestVal, remainder, bestCaps);
        }

        return new ScanResult<T>(default, line, null);
    }

    /// <summary>
    /// Scans a line for the earliest and longest match from a plain text lookup dictionary.
    /// Uses case-insensitive string matching (no regex).
    /// </summary>
    internal static ScanResult<T> ScanPlain<T>(string line, Dictionary<string, T> patternDict) where T : class
    {
        string lineLower = line.ToLowerInvariant();

        int bestIndex = int.MaxValue;
        int bestEndIndex = -1;
        int bestPatternLen = -1;
        T? bestVal = default;
        int bestStart = 0;
        int bestEnd = 0;

        foreach (var (pattern, value) in patternDict)
        {
            int index = lineLower.IndexOf(pattern, StringComparison.Ordinal);
            if (index >= 0)
            {
                int endIndex = index + pattern.Length - 1;
                int patternLen = pattern.Length;

                if (index < bestIndex
                    || (index == bestIndex && endIndex > bestEndIndex)
                    || (index == bestIndex && endIndex == bestEndIndex && patternLen > bestPatternLen))
                {
                    bestIndex = index;
                    bestEndIndex = endIndex;
                    bestPatternLen = patternLen;
                    bestVal = value;
                    bestStart = index;
                    bestEnd = index + pattern.Length;
                }
            }
        }

        if (bestVal is not null)
        {
            string remainder = line[..bestStart] + line[bestEnd..];
            return new ScanResult<T>(bestVal, remainder, null);
        }

        return new ScanResult<T>(default, line, null);
    }

    /// <summary>
    /// ScanPlain variant for string value dictionaries.
    /// </summary>
    internal static (string? Value, string Remainder) ScanPlainString(string line, Dictionary<string, string> patternDict)
    {
        string lineLower = line.ToLowerInvariant();

        int bestIndex = int.MaxValue;
        int bestEndIndex = -1;
        int bestPatternLen = -1;
        string? bestVal = null;
        int bestStart = 0;
        int bestEnd = 0;

        foreach (var (pattern, value) in patternDict)
        {
            int index = lineLower.IndexOf(pattern, StringComparison.Ordinal);
            if (index >= 0)
            {
                int endIndex = index + pattern.Length - 1;
                int patternLen = pattern.Length;

                if (index < bestIndex
                    || (index == bestIndex && endIndex > bestEndIndex)
                    || (index == bestIndex && endIndex == bestEndIndex && patternLen > bestPatternLen))
                {
                    bestIndex = index;
                    bestEndIndex = endIndex;
                    bestPatternLen = patternLen;
                    bestVal = value;
                    bestStart = index;
                    bestEnd = index + pattern.Length;
                }
            }
        }

        if (bestVal is not null)
        {
            string remainder = line[..bestStart] + line[bestEnd..];
            return (bestVal, remainder);
        }

        return (null, line);
    }

    /// <summary>
    /// ScanPlain for string[] value dictionaries (regen/degen types).
    /// </summary>
    internal static (string[]? Value, string Remainder) ScanPlainStringArray(string line, Dictionary<string, string[]> patternDict)
    {
        string lineLower = line.ToLowerInvariant();

        int bestIndex = int.MaxValue;
        int bestEndIndex = -1;
        int bestPatternLen = -1;
        string[]? bestVal = null;
        int bestStart = 0;
        int bestEnd = 0;

        foreach (var (pattern, value) in patternDict)
        {
            int index = lineLower.IndexOf(pattern, StringComparison.Ordinal);
            if (index >= 0)
            {
                int endIndex = index + pattern.Length - 1;
                int patternLen = pattern.Length;

                if (index < bestIndex
                    || (index == bestIndex && endIndex > bestEndIndex)
                    || (index == bestIndex && endIndex == bestEndIndex && patternLen > bestPatternLen))
                {
                    bestIndex = index;
                    bestEndIndex = endIndex;
                    bestPatternLen = patternLen;
                    bestVal = value;
                    bestStart = index;
                    bestEnd = index + pattern.Length;
                }
            }
        }

        if (bestVal is not null)
        {
            string remainder = line[..bestStart] + line[bestEnd..];
            return (bestVal, remainder);
        }

        return (null, line);
    }

    // ─── Core parsing logic ───

    private static (List<Mod>?, string?) ParseModInternal(string line, int order)
    {
        // Check specialModList first
        var specialResult = ScanRegex(line, ModSpecialData.SpecialModList);
        if (specialResult.HasMatch && specialResult.Remainder.Trim().Length == 0)
        {
            if (specialResult.Value is ModSpecialData.SpecialModValue smv)
            {
                if (smv.IsFunction)
                {
                    var caps = specialResult.Captures ?? [];
                    return (smv.Function!(caps), null);
                }
                else
                {
                    // Deep copy the static mod list
                    return (CloneModList(smv.Mods!), null);
                }
            }
        }

        // Add space to line for consistent scanning (matches Lua behavior)
        line = line + " ";

        // Check for pre-flag at start of line
        ModFlagEntry? preFlag = null;
        var preFlagResult = ScanPreFlag(line);
        if (preFlagResult.HasMatch)
        {
            preFlag = preFlagResult.Value;
            line = preFlagResult.Remainder;
        }

        // Scan for modifier form
        var formResult = ScanRegex(line, FormPatterns.List);
        if (!formResult.HasMatch)
            return (null, line);

        ModForm modForm = formResult.Value;
        line = formResult.Remainder;
        string[] formCap = formResult.Captures ?? [""];

        // Check for tags (per-charge, conditionals) - scan up to 2 tags
        ModTagEntry? modTag = null;
        ModTagEntry? modTag2 = null;

        var tagResult = ScanModTag(line);
        if (tagResult.Value is not null)
        {
            modTag = tagResult.Value;
            line = tagResult.Remainder;

            var tag2Result = ScanModTag(line);
            if (tag2Result.Value is not null)
            {
                modTag2 = tag2Result.Value;
                line = tag2Result.Remainder;
            }
        }

        // Scan for modifier name
        string[]? modNames = null;
        ModNameEntry? modNameEntry = null;

        if (modForm == ModForm.PEN)
        {
            var (penName, penRemainder) = ScanPlainString(line, DamageTypes.PenTypes);
            if (penName == null)
                return ([], line);
            modNames = [penName];
            line = penRemainder;
            // Also scan and discard any modName match
            var discardResult = ScanPlain(line, ModNameData.ModNameList);
            if (discardResult.HasMatch) line = discardResult.Remainder;
        }
        else if (modForm == ModForm.BASECOST)
        {
            var (costNames, costRemainder) = ScanPlainStringArray(line, DamageTypes.BaseCostTypes);
            if (costNames == null)
                return ([], line);
            modNames = costNames;
            line = costRemainder;
            var discardResult = ScanPlain(line, ModNameData.ModNameList);
            if (discardResult.HasMatch) line = discardResult.Remainder;
        }
        else if (modForm == ModForm.TOTALCOST)
        {
            var (costNames, costRemainder) = ScanPlainStringArray(line, DamageTypes.CostTypes);
            if (costNames == null)
                return ([], line);
            modNames = costNames;
            line = costRemainder;
            var discardResult = ScanPlain(line, ModNameData.ModNameList);
            if (discardResult.HasMatch) line = discardResult.Remainder;
        }
        else if (modForm == ModForm.FLAG)
        {
            // For FLAG form, scan flagTypes to get the flag name
            var (flagName, flagRemainder) = ScanFlagTypes(line);
            if (flagName == null)
                return (null, line);
            formCap = [flagName];
            line = flagRemainder;

            // Then scan for modName
            var nameResult = ScanPlain(line, ModNameData.ModNameList);
            if (nameResult.HasMatch)
            {
                modNameEntry = nameResult.Value!;
                modNames = modNameEntry.Names;
                line = nameResult.Remainder;
            }
        }
        else
        {
            // Normal: scan modNameList
            var nameResult = ScanPlain(line, ModNameData.ModNameList);
            if (nameResult.HasMatch)
            {
                modNameEntry = nameResult.Value!;
                modNames = modNameEntry.Names;
                line = nameResult.Remainder;
            }
        }

        // Scan for flags
        ModFlagEntry? modFlag = null;
        var flagResult = ScanPlain(line, ModFlagData.ModFlagList);
        if (flagResult.HasMatch)
        {
            modFlag = flagResult.Value;
            line = flagResult.Remainder;
        }

        // ─── Determine modifier value and type ───
        double modValue;
        string modType;
        string? modSuffix = null;
        bool isDmgRange = false;
        double dmgMin = 0, dmgMax = 0;
        string? dmgTypeName = null;
        KeywordFlag dmgKeywordFlags = KeywordFlag.None;

        if (!double.TryParse(formCap.Length > 0 ? formCap[0] : "", out modValue))
            modValue = 0;

        modType = "BASE";

        switch (modForm)
        {
            case ModForm.INC:
                modType = "INC";
                break;
            case ModForm.RED:
                modValue = -modValue;
                modType = "INC";
                break;
            case ModForm.MORE:
                modType = "MORE";
                break;
            case ModForm.LESS:
                modValue = -modValue;
                modType = "MORE";
                break;
            case ModForm.BASE:
            case ModForm.CHANCE:
            case ModForm.PEN:
            {
                var (suffix, suffRemainder) = ScanPlainString(line, DamageTypes.SuffixTypes);
                if (suffix != null)
                {
                    modSuffix = suffix;
                    line = suffRemainder;
                }
                break;
            }
            case ModForm.GAIN:
            {
                modType = "BASE";
                var (suffix, suffRemainder) = ScanPlainString(line, DamageTypes.SuffixTypes);
                if (suffix != null)
                {
                    modSuffix = suffix;
                    line = suffRemainder;
                }
                break;
            }
            case ModForm.LOSE:
            {
                modValue = -modValue;
                modType = "BASE";
                var (suffix, suffRemainder) = ScanPlainString(line, DamageTypes.SuffixTypes);
                if (suffix != null)
                {
                    modSuffix = suffix;
                    line = suffRemainder;
                }
                break;
            }
            case ModForm.GRANTS:
            {
                modType = "BASE";
                var (suffix, suffRemainder) = ScanPlainString(line, DamageTypes.SuffixTypes);
                if (suffix != null) { modSuffix = suffix; line = suffRemainder; }
                break;
            }
            case ModForm.REMOVES:
            {
                modValue = -modValue;
                modType = "BASE";
                var (suffix, suffRemainder) = ScanPlainString(line, DamageTypes.SuffixTypes);
                if (suffix != null) { modSuffix = suffix; line = suffRemainder; }
                break;
            }
            case ModForm.TOTALCOST:
            case ModForm.BASECOST:
                modType = "BASE";
                break;
            case ModForm.REGENPERCENT:
            {
                if (formCap.Length >= 2)
                {
                    var (regenNames, _) = ScanPlainStringArray(formCap[1] + " ", DamageTypes.RegenTypes);
                    if (regenNames != null) modNames = regenNames;
                }
                modSuffix = "Percent";
                break;
            }
            case ModForm.REGENFLAT:
            {
                if (formCap.Length >= 2)
                {
                    var (regenNames, _) = ScanPlainStringArray(formCap[1] + " ", DamageTypes.RegenTypes);
                    if (regenNames != null) modNames = regenNames;
                }
                break;
            }
            case ModForm.DEGENPERCENT:
            {
                if (formCap.Length >= 2)
                {
                    var (degenNames, _) = ScanPlainStringArray(formCap[1] + " ", DamageTypes.DegenTypes);
                    if (degenNames != null) modNames = degenNames;
                }
                modSuffix = "Percent";
                break;
            }
            case ModForm.DEGENFLAT:
            {
                if (formCap.Length >= 2)
                {
                    var (degenNames, _) = ScanPlainStringArray(formCap[1] + " ", DamageTypes.DegenTypes);
                    if (degenNames != null) modNames = degenNames;
                }
                break;
            }
            case ModForm.DEGEN:
            {
                if (formCap.Length >= 2 && DamageTypes.DmgTypes.TryGetValue(formCap[1], out string? dt))
                {
                    modNames = [dt + "Degen"];
                    modSuffix = "";
                }
                else
                {
                    return ([], line);
                }
                break;
            }
            case ModForm.DMG:
            case ModForm.DMGATTACKS:
            case ModForm.DMGSPELLS:
            case ModForm.DMGBOTH:
            {
                if (formCap.Length >= 3 && DamageTypes.DmgTypes.TryGetValue(formCap[2], out string? dmgType))
                {
                    isDmgRange = true;
                    dmgMin = double.Parse(formCap[0]);
                    dmgMax = double.Parse(formCap[1]);
                    dmgTypeName = dmgType;
                    modNames = [dmgType + "Min", dmgType + "Max"];
                    modType = "BASE";

                    if (modForm == ModForm.DMGATTACKS)
                        dmgKeywordFlags = KeywordFlag.Attack;
                    else if (modForm == ModForm.DMGSPELLS)
                        dmgKeywordFlags = KeywordFlag.Spell;
                    else if (modForm == ModForm.DMGBOTH)
                        dmgKeywordFlags = KeywordFlag.Attack | KeywordFlag.Spell;
                }
                else
                {
                    return ([], line);
                }
                break;
            }
            case ModForm.FLAG:
            {
                string flagCap = formCap.Length > 0 ? formCap[0] : "";

                // Check if this is a complex flag type (like hexproof)
                if (DamageTypes.FlagTypes.TryGetValue(flagCap, out var flagEntry))
                {
                    if (!flagEntry.IsSimple)
                    {
                        // Complex: has custom name/value/type
                        modNames = [flagEntry.Name];
                        modValue = flagEntry.Value ?? 0;
                        modType = flagEntry.Type ?? "FLAG";
                    }
                    else
                    {
                        modNames = [flagEntry.Name];
                        modType = "FLAG";
                        modValue = 1; // bool true
                    }
                }
                else
                {
                    return (null, line);
                }
                break;
            }
            case ModForm.OVERRIDE:
                modType = "OVERRIDE";
                break;
            case ModForm.DOUBLED:
            {
                if (modNames != null && modNames.Length > 0)
                {
                    string modNameStr = modNames[0];
                    modNames = [modNameStr, "Multiplier:" + modNameStr + "Doubled"];
                    modType = "MORE"; // first mod is MORE 100, second is OVERRIDE 1
                }
                break;
            }
        }

        if (modNames == null || modNames.Length == 0)
            return ([], line);

        // ─── Combine flags and tags ───
        ModFlag combinedFlags = ModFlag.None;
        KeywordFlag combinedKeywordFlags = KeywordFlag.None;
        var tagList = new List<ModTag>();
        string? combinedModSuffix = modSuffix;
        bool addToAura = false;
        bool newAura = false;
        bool newAuraOnlyAllies = false;
        bool addToMinion = false;
        bool applyToEnemy = false;
        bool onlyAddToBanners = false;

        // Collect from modNameEntry
        if (modNameEntry != null)
        {
            combinedFlags |= modNameEntry.Flags;
            combinedKeywordFlags |= modNameEntry.KeywordFlags;
            if (modNameEntry.AddToMinion) addToMinion = true;
            if (modNameEntry.Tags != null)
            {
                foreach (var tag in modNameEntry.Tags)
                    tagList.Add(BuildTag(tag));
            }
        }

        // Collect from preFlag
        if (preFlag != null)
        {
            combinedFlags |= preFlag.Flags;
            combinedKeywordFlags |= preFlag.KeywordFlags;
            if (preFlag.AddToMinion) addToMinion = true;
            if (preFlag.AddToAura) addToAura = true;
            if (preFlag.NewAura) newAura = true;
            if (preFlag.NewAuraOnlyAllies) newAuraOnlyAllies = true;
            if (preFlag.ApplyToEnemy) applyToEnemy = true;
            if (preFlag.OnlyAddToBanners) onlyAddToBanners = true;
            if (preFlag.ModSuffix != null) combinedModSuffix ??= preFlag.ModSuffix;
            if (preFlag.Tags != null)
            {
                foreach (var tag in preFlag.Tags)
                    tagList.Add(BuildTag(tag));
            }
        }

        // Collect from modFlag
        if (modFlag != null)
        {
            combinedFlags |= modFlag.Flags;
            combinedKeywordFlags |= modFlag.KeywordFlags;
            if (modFlag.AddToMinion) addToMinion = true;
            if (modFlag.ApplyToEnemy) applyToEnemy = true;
            if (modFlag.ModSuffix != null) combinedModSuffix ??= modFlag.ModSuffix;
            if (modFlag.Tags != null)
            {
                foreach (var tag in modFlag.Tags)
                    tagList.Add(BuildTag(tag));
            }
        }

        // Collect from modTag
        if (modTag != null)
        {
            if (modTag.NewAura) newAura = true;
            if (modTag.NewAuraOnlyAllies) newAuraOnlyAllies = true;
            if (modTag.Tags != null)
            {
                foreach (var tag in modTag.Tags)
                    tagList.Add(BuildTag(tag));
            }
        }

        // Collect from modTag2
        if (modTag2 != null)
        {
            if (modTag2.NewAura) newAura = true;
            if (modTag2.Tags != null)
            {
                foreach (var tag in modTag2.Tags)
                    tagList.Add(BuildTag(tag));
            }
        }

        // Add DMG keyword flags
        combinedKeywordFlags |= dmgKeywordFlags;

        // ─── Generate modifier list ───
        var modList = new List<Mod>();
        string finalSuffix = combinedModSuffix ?? "";

        if (isDmgRange && modNames.Length >= 2)
        {
            // Damage range: create two mods (min and max)
            var tags = tagList.Count > 0 ? tagList.ToArray() : Array.Empty<ModTag>();
            modList.Add(ModHelper.CreateMod(
                modNames[0] + finalSuffix, ModType.Base, dmgMin, "",
                combinedFlags, combinedKeywordFlags, tags));
            modList.Add(ModHelper.CreateMod(
                modNames[1] + finalSuffix, ModType.Base, dmgMax, "",
                combinedFlags, combinedKeywordFlags, tags));
        }
        else if (modForm == ModForm.DOUBLED && modNames.Length >= 2)
        {
            // DOUBLED: first mod is MORE 100, second is OVERRIDE 1
            var tags1 = new List<ModTag>(tagList);
            tags1.Add(new MultiplierTag { Var = modNames[0].Replace("Multiplier:", "") + "Doubled", GlobalLimit = 100, GlobalLimitKey = modNames[0].Replace("Multiplier:", "") + "DoubledLimit" });
            modList.Add(ModHelper.CreateMod(
                modNames[0] + finalSuffix, ModType.More, 100, "",
                combinedFlags, combinedKeywordFlags, tags1.ToArray()));
            modList.Add(ModHelper.CreateMod(
                modNames[1] + finalSuffix, ModType.Override, 1, "",
                combinedFlags, combinedKeywordFlags, tagList.ToArray()));
        }
        else if (modForm == ModForm.FLAG && modType == "FLAG")
        {
            // FLAG form: create boolean flag mods
            var tags = tagList.Count > 0 ? tagList.ToArray() : Array.Empty<ModTag>();
            foreach (string name in modNames)
            {
                modList.Add(ModHelper.CreateMod(
                    name + finalSuffix, ModType.Flag, true, "",
                    combinedFlags, combinedKeywordFlags, tags));
            }
        }
        else
        {
            // Normal mods
            ModType mt = modType switch
            {
                "INC" => ModType.Inc,
                "MORE" => ModType.More,
                "FLAG" => ModType.Flag,
                "OVERRIDE" => ModType.Override,
                _ => ModType.Base,
            };

            var tags = tagList.Count > 0 ? tagList.ToArray() : Array.Empty<ModTag>();
            foreach (string name in modNames)
            {
                ModValue val = (mt == ModType.Flag) ? (ModValue)true : (ModValue)modValue;
                modList.Add(ModHelper.CreateMod(
                    name + finalSuffix, mt, val, "",
                    combinedFlags, combinedKeywordFlags, tags));
            }
        }

        if (modList.Count == 0)
            return ([], line);

        // ─── Special handling: addToAura, newAura, addToMinion, applyToEnemy ───
        if (addToAura)
        {
            var wrappedList = new List<Mod>();
            foreach (var effectMod in modList)
            {
                var auraTags = new List<ModTag>();
                if (onlyAddToBanners)
                    auraTags.Add(new SkillTypeTag { SkillTypeValue = SkillType.Banner });
                wrappedList.Add(ModHelper.CreateMod(
                    "ExtraAuraEffect", ModType.List, ModValue.FromComplex(effectMod), "",
                    ModFlag.None, KeywordFlag.None, auraTags.ToArray()));
            }
            modList = wrappedList;
        }
        else if (newAura)
        {
            var wrappedList = new List<Mod>();
            foreach (var effectMod in modList)
            {
                wrappedList.Add(ModHelper.CreateMod(
                    "ExtraAura", ModType.List,
                    ModValue.FromComplex(new Dictionary<string, object?> { ["mod"] = effectMod, ["onlyAllies"] = newAuraOnlyAllies }),
                    "", ModFlag.None, KeywordFlag.None));
            }
            modList = wrappedList;
        }
        else if (addToMinion)
        {
            var wrappedList = new List<Mod>();
            foreach (var effectMod in modList)
            {
                wrappedList.Add(ModHelper.CreateMod(
                    "MinionModifier", ModType.List, ModValue.FromComplex(effectMod), "",
                    ModFlag.None, KeywordFlag.None));
            }
            modList = wrappedList;
        }
        else if (applyToEnemy)
        {
            var wrappedList = new List<Mod>();
            foreach (var effectMod in modList)
            {
                wrappedList.Add(ModHelper.CreateMod(
                    "EnemyModifier", ModType.List, ModValue.FromComplex(effectMod), "",
                    ModFlag.None, KeywordFlag.None));
            }
            modList = wrappedList;
        }

        // Check if there's leftover text (indicates incomplete parse)
        string remaining = line.Trim();
        string? extra = remaining.Length > 0 ? remaining : null;

        return (modList, extra);
    }

    // ─── Helper: scan preFlagList ───

    private static ScanResult<ModFlagEntry> ScanPreFlag(string line)
    {
        string lineLower = line.ToLowerInvariant();

        int bestIndex = int.MaxValue;
        int bestEndIndex = -1;
        int bestPatternLen = -1;
        ModFlagEntry? bestVal = null;
        int bestStart = 0;
        int bestEnd = 0;

        foreach (var (pattern, entry) in ModFlagData.PreFlagList)
        {
            var match = Regex.Match(lineLower, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int index = match.Index;
                int endIndex = match.Index + match.Length - 1;
                int patternLen = pattern.Length;

                if (index < bestIndex
                    || (index == bestIndex && endIndex > bestEndIndex)
                    || (index == bestIndex && endIndex == bestEndIndex && patternLen > bestPatternLen))
                {
                    bestIndex = index;
                    bestEndIndex = endIndex;
                    bestPatternLen = patternLen;
                    bestVal = entry;
                    bestStart = match.Index;
                    bestEnd = match.Index + match.Length;
                }
            }
        }

        if (bestVal is not null)
        {
            string remainder = line[..bestStart] + line[bestEnd..];
            return new ScanResult<ModFlagEntry>(bestVal, remainder, null);
        }

        return new ScanResult<ModFlagEntry>(null, line, null);
    }

    // ─── Helper: scan modTagList ───

    private static ScanResult<ModTagEntry> ScanModTag(string line)
    {
        string lineLower = line.ToLowerInvariant();

        // First try plain entries
        int bestIndex = int.MaxValue;
        int bestEndIndex = -1;
        int bestPatternLen = -1;
        ModTagEntry? bestVal = null;
        int bestStart = 0;
        int bestEnd = 0;

        foreach (var (pattern, entry) in ModTagData.PlainEntries)
        {
            int index = lineLower.IndexOf(pattern, StringComparison.Ordinal);
            if (index >= 0)
            {
                int endIndex = index + pattern.Length - 1;
                int patternLen = pattern.Length;

                if (index < bestIndex
                    || (index == bestIndex && endIndex > bestEndIndex)
                    || (index == bestIndex && endIndex == bestEndIndex && patternLen > bestPatternLen))
                {
                    bestIndex = index;
                    bestEndIndex = endIndex;
                    bestPatternLen = patternLen;
                    bestVal = entry;
                    bestStart = index;
                    bestEnd = index + pattern.Length;
                }
            }
        }

        // Then try regex entries
        foreach (var (pattern, factory) in ModTagData.RegexEntries)
        {
            var match = Regex.Match(lineLower, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int index = match.Index;
                int endIndex = match.Index + match.Length - 1;
                int patternLen = pattern.Length;

                if (index < bestIndex
                    || (index == bestIndex && endIndex > bestEndIndex)
                    || (index == bestIndex && endIndex == bestEndIndex && patternLen > bestPatternLen))
                {
                    // Extract captures
                    var caps = new string[match.Groups.Count - 1];
                    for (int i = 1; i < match.Groups.Count; i++)
                        caps[i - 1] = match.Groups[i].Value;

                    bestIndex = index;
                    bestEndIndex = endIndex;
                    bestPatternLen = patternLen;
                    bestVal = factory(caps);
                    bestStart = match.Index;
                    bestEnd = match.Index + match.Length;
                }
            }
        }

        if (bestVal is not null)
        {
            string remainder = line[..bestStart] + line[bestEnd..];
            return new ScanResult<ModTagEntry>(bestVal, remainder, null);
        }

        return new ScanResult<ModTagEntry>(null, line, null);
    }

    // ─── Helper: scan flagTypes ───

    private static (string? Name, string Remainder) ScanFlagTypes(string line)
    {
        string lineLower = line.ToLowerInvariant();

        int bestIndex = int.MaxValue;
        int bestEndIndex = -1;
        int bestPatternLen = -1;
        string? bestName = null;
        int bestStart = 0;
        int bestEnd = 0;

        foreach (var (pattern, entry) in DamageTypes.FlagTypes)
        {
            // Some flagTypes have regex patterns (like "hindered,? with (%d+)%% reduced movement speed")
            // For now, do plain text matching
            int index = lineLower.IndexOf(pattern, StringComparison.Ordinal);
            if (index >= 0)
            {
                int endIndex = index + pattern.Length - 1;
                int patternLen = pattern.Length;

                if (index < bestIndex
                    || (index == bestIndex && endIndex > bestEndIndex)
                    || (index == bestIndex && endIndex == bestEndIndex && patternLen > bestPatternLen))
                {
                    bestIndex = index;
                    bestEndIndex = endIndex;
                    bestPatternLen = patternLen;
                    bestName = pattern; // Return the key, not the entry name
                    bestStart = index;
                    bestEnd = index + pattern.Length;
                }
            }
        }

        if (bestName is not null)
        {
            string remainder = line[..bestStart] + line[bestEnd..];
            return (bestName, remainder);
        }

        return (null, line);
    }

    // ─── Tag building ───

    internal static ModTag BuildTag(TagSpec spec)
    {
        return spec.Type switch
        {
            "Condition" => spec.VarList != null
                ? new ConditionTag { VarList = spec.VarList, Neg = spec.Neg }
                : new ConditionTag { Var = spec.Var, Neg = spec.Neg },

            "ActorCondition" => spec.VarList != null
                ? new ActorConditionTag { Actor = spec.Actor, VarList = spec.VarList, Neg = spec.Neg }
                : new ActorConditionTag { Actor = spec.Actor, Var = spec.Var, Neg = spec.Neg },

            "Multiplier" => new MultiplierTag
            {
                Var = spec.Var,
                VarList = spec.VarList,
                Div = spec.Div,
                Limit = spec.Limit,
                LimitTotal = spec.LimitTotal,
                LimitNegTotal = spec.LimitNegTotal,
                GlobalLimit = spec.GlobalLimit,
                GlobalLimitKey = spec.GlobalLimitKey,
                Base = spec.Base,
                NoFloor = spec.NoFloor,
                Invert = spec.Invert,
                Actor = spec.Actor,
            },

            "MultiplierThreshold" => new MultiplierThresholdTag
            {
                Var = spec.Var,
                Threshold = spec.Threshold ?? 0,
                ThresholdVar = spec.ThresholdVar,
                Upper = spec.Upper,
                Equals_ = spec.Equals_,
                Actor = spec.Actor,
            },

            "PerStat" => new PerStatTag
            {
                Stat = spec.Stat,
                StatList = spec.StatList,
                Div = spec.Div,
                Limit = spec.Limit,
                LimitTotal = spec.LimitTotal,
                Base = spec.Base,
                Actor = spec.Actor,
            },

            "StatThreshold" => new StatThresholdTag
            {
                Stat = spec.Stat,
                Threshold = spec.Threshold ?? 0,
                ThresholdStat = spec.ThresholdStat,
                Upper = spec.Upper,
            },

            "SkillType" => spec.SkillType.HasValue
                ? new SkillTypeTag { SkillTypeValue = (SkillType)spec.SkillType.Value, Neg = spec.Neg }
                : new SkillTypeTag { Neg = spec.Neg },

            "SkillName" => new SkillNameTag
            {
                SkillName = spec.SkillName,
                SkillNameList = spec.SkillNameList,
                IncludeTransfigured = spec.IncludeTransfigured,
                Neg = spec.Neg,
            },

            "SkillId" => new SkillIdTag { SkillId = spec.SkillId ?? "" },

            "SlotName" => new SlotNameTag
            {
                SlotName = spec.SlotName,
                SlotNameList = spec.SlotNameList,
                Neg = spec.Neg,
            },

            "ModFlagOr" => new ModFlagOrTag { ModFlags = (ModFlag)(spec.ModFlags ?? 0) },

            "Global" => new ConditionTag { Var = "Global" },

            _ => new ConditionTag { Var = spec.Type }, // fallback
        };
    }

    private static List<Mod> CloneModList(List<Mod> mods)
    {
        var result = new List<Mod>(mods.Count);
        foreach (var mod in mods)
        {
            result.Add(ModHelper.CreateMod(
                mod.Name, mod.Type, mod.Value, mod.Source,
                mod.Flags, mod.KeywordFlags,
                mod.Tags.Count > 0 ? mod.Tags.ToArray() : Array.Empty<ModTag>()));
        }
        return result;
    }
}
