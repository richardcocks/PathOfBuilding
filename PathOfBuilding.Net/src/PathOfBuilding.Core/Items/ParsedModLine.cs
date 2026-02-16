using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Items;

public class ParsedModLine
{
    public string RawText { get; init; } = "";
    public ModLineCategory Category { get; init; }
    public List<Mod> Mods { get; init; } = new();
    public bool ParseFailed { get; init; }
}
