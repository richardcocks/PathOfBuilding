namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Helper for matching KeywordFlags between a query context and a modifier.
/// Ported from Global.lua MatchKeywordFlags function with two-level cache.
/// </summary>
public static class KeywordFlagHelper
{
    private static readonly Dictionary<ulong, bool> Cache = new();

    /// <summary>
    /// Determines whether a modifier's keyword flags are satisfied by the query's keyword flags.
    /// Default (no MatchAll): ANY of the mod's flags must be present, or the mod has no flags.
    /// With MatchAll: ALL of the mod's flags must be present.
    /// </summary>
    public static bool MatchKeywordFlags(KeywordFlag queryFlags, KeywordFlag modFlags)
    {
        // Pack both flags into a single ulong key for cache lookup
        ulong cacheKey = ((ulong)queryFlags << 32) | (ulong)modFlags;

        if (Cache.TryGetValue(cacheKey, out bool cached))
            return cached;

        bool result = ComputeMatch(queryFlags, modFlags);
        Cache[cacheKey] = result;
        return result;
    }

    private static bool ComputeMatch(KeywordFlag queryFlags, KeywordFlag modFlags)
    {
        const KeywordFlag matchAllMask = ~KeywordFlag.MatchAll;

        bool matchAll = (modFlags & KeywordFlag.MatchAll) != 0;
        var modMasked = modFlags & matchAllMask;
        var queryMasked = queryFlags & matchAllMask;

        if (matchAll)
        {
            // ALL mod flags must be present in query
            return (queryMasked & modMasked) == modMasked;
        }
        else
        {
            // No flags on mod = always matches; otherwise ANY flag must match
            return modMasked == 0 || (queryMasked & modMasked) != 0;
        }
    }

    /// <summary>Clears the match cache. Call when keyword flag context changes significantly.</summary>
    public static void ClearCache() => Cache.Clear();
}
