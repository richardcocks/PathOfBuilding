using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// A single game modifier. The fundamental unit of the PoB calculation engine.
/// Ported from the Lua mod table: { name, type, value, flags, keywordFlags, source, [tags...] }.
/// </summary>
public sealed class Mod
{
    public required string Name { get; init; }
    public required ModType Type { get; init; }
    public ModValue Value { get; set; }
    public ModFlag Flags { get; init; }
    public KeywordFlag KeywordFlags { get; init; }
    public string Source { get; set; } = "";
    public IReadOnlyList<ModTag> Tags { get; init; } = [];

    /// <summary>
    /// Set to true when this mod has been replaced by ReplaceModInternal.
    /// Prevents double-replacement.
    /// </summary>
    internal bool Replaced { get; set; }

    /// <summary>Whether this mod has any conditional tags that require evaluation.</summary>
    public bool HasTags => Tags.Count > 0;
}
