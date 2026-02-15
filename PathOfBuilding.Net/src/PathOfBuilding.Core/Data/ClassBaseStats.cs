namespace PathOfBuilding.Core.Data;

/// <summary>
/// Static lookup for class base attributes.
/// Data sourced from spec.tree.characterData in the passive tree JSON.
/// </summary>
public static class ClassBaseStats
{
    private static readonly Dictionary<string, (int Str, int Dex, int Int)> BaseAttributes = new()
    {
        ["Marauder"] = (32, 14, 14),
        ["Ranger"] = (14, 32, 14),
        ["Witch"] = (14, 14, 32),
        ["Duelist"] = (23, 23, 14),
        ["Templar"] = (23, 14, 23),
        ["Shadow"] = (14, 23, 23),
        ["Scion"] = (20, 20, 20),
    };

    private static readonly Dictionary<string, string> AscendancyToBase = new(StringComparer.OrdinalIgnoreCase)
    {
        // Marauder
        ["Juggernaut"] = "Marauder",
        ["Berserker"] = "Marauder",
        ["Chieftain"] = "Marauder",
        // Ranger
        ["Raider"] = "Ranger",
        ["Deadeye"] = "Ranger",
        ["Pathfinder"] = "Ranger",
        // Witch
        ["Necromancer"] = "Witch",
        ["Elementalist"] = "Witch",
        ["Occultist"] = "Witch",
        // Duelist
        ["Slayer"] = "Duelist",
        ["Gladiator"] = "Duelist",
        ["Champion"] = "Duelist",
        // Templar
        ["Inquisitor"] = "Templar",
        ["Hierophant"] = "Templar",
        ["Guardian"] = "Templar",
        // Shadow
        ["Assassin"] = "Shadow",
        ["Trickster"] = "Shadow",
        ["Saboteur"] = "Shadow",
        // Scion
        ["Ascendant"] = "Scion",
    };

    /// <summary>
    /// Returns (baseStr, baseDex, baseInt) for a base class name.
    /// Returns (0,0,0) if the class name is unknown.
    /// </summary>
    public static (int Str, int Dex, int Int) GetBaseAttributes(string className)
    {
        if (string.IsNullOrEmpty(className))
            return (0, 0, 0);
        return BaseAttributes.TryGetValue(className, out var attrs) ? attrs : (0, 0, 0);
    }

    /// <summary>
    /// Resolves an ascendancy class name to its base class name.
    /// If the name is already a base class, returns it unchanged.
    /// Returns empty string if unknown.
    /// </summary>
    public static string GetBaseClassName(string ascendClassName)
    {
        if (string.IsNullOrEmpty(ascendClassName) || ascendClassName == "None")
            return "";

        // Check if it's already a base class
        if (BaseAttributes.ContainsKey(ascendClassName))
            return ascendClassName;

        return AscendancyToBase.TryGetValue(ascendClassName, out var baseName) ? baseName : "";
    }
}
