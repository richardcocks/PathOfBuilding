using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Static utility methods for creating, comparing, and formatting modifiers.
/// Ported from ModTools.lua (modLib).
/// </summary>
public static class ModHelper
{
    /// <summary>
    /// Creates a new modifier with the given parameters.
    /// </summary>
    public static Mod CreateMod(
        string name,
        ModType type,
        ModValue value,
        string source = "",
        ModFlag flags = ModFlag.None,
        KeywordFlag keywordFlags = KeywordFlag.None,
        params ModTag[] tags)
    {
        return new Mod
        {
            Name = name,
            Type = type,
            Value = value,
            Source = source,
            Flags = flags,
            KeywordFlags = keywordFlags,
            Tags = tags.Length > 0 ? tags : [],
        };
    }

    /// <summary>
    /// Compares two mods for parameter equality (name, type, flags, keywordFlags, tags).
    /// Does not compare value or source.
    /// Used by MergeMod to find identical mods for additive merging.
    /// </summary>
    public static bool CompareModParams(Mod a, Mod b)
    {
        if (a.Name != b.Name) return false;
        if (a.Type != b.Type) return false;
        if (a.Flags != b.Flags) return false;
        if (a.KeywordFlags != b.KeywordFlags) return false;
        if (a.Tags.Count != b.Tags.Count) return false;

        for (int i = 0; i < a.Tags.Count; i++)
        {
            if (!a.Tags[i].Equals(b.Tags[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether a modifier matches the given query parameters.
    /// This is the core filtering logic used by Sum/More/Flag/Override/List/Tabulate.
    /// </summary>
    public static bool MatchesMod(Mod mod, ModType? modType, ModFlag flags, KeywordFlag keywordFlags, string? source)
    {
        // Type filter
        if (modType.HasValue && mod.Type != modType.Value)
            return false;

        // Flag filter: all mod flags must be present in the query flags
        if (mod.Flags != ModFlag.None && (flags & mod.Flags) != mod.Flags)
            return false;

        // Keyword flag matching
        if (!KeywordFlagHelper.MatchKeywordFlags(keywordFlags, mod.KeywordFlags))
            return false;

        // Source filter (prefix match on colon-delimited source)
        if (source != null && !string.IsNullOrEmpty(mod.Source))
        {
            var modSourcePrefix = mod.Source;
            int colonIndex = modSourcePrefix.IndexOf(':');
            if (colonIndex >= 0)
                modSourcePrefix = modSourcePrefix[..colonIndex];

            if (modSourcePrefix != source)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Sets the source on a modifier, including any nested mod inside a complex value.
    /// </summary>
    public static Mod SetSource(Mod mod, string source)
    {
        mod.Source = source;
        return mod;
    }

    /// <summary>
    /// Returns a debug string representation of a modifier.
    /// </summary>
    public static string FormatMod(Mod mod)
    {
        return $"{mod.Value} = {mod.Name}|{mod.Type}|{mod.Flags}|{mod.KeywordFlags}|{FormatTags(mod.Tags)}";
    }

    /// <summary>
    /// Formats a list of tags as a comma-separated string.
    /// </summary>
    public static string FormatTags(IReadOnlyList<ModTag> tags)
    {
        if (tags.Count == 0) return "";
        return string.Join(",", tags.Select(t => t.ToString()));
    }
}
