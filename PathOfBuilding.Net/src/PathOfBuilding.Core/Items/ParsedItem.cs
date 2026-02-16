namespace PathOfBuilding.Core.Items;

public class ParsedItem
{
    public int Id { get; init; }
    public ItemRarity Rarity { get; init; }
    public string Name { get; init; } = "";
    public string BaseName { get; init; } = "";
    public int ItemLevel { get; init; }
    public int Quality { get; init; }
    public string Sockets { get; init; } = "";
    public int LevelReq { get; init; }
    public int ImplicitCount { get; init; }
    public bool Corrupted { get; init; }

    public List<ParsedModLine> ModLines { get; init; } = new();

    public IEnumerable<ParsedModLine> Implicits =>
        ModLines.Where(m => m.Category is ModLineCategory.Implicit or ModLineCategory.Enchant);

    public IEnumerable<ParsedModLine> Explicits =>
        ModLines.Where(m => m.Category is ModLineCategory.Explicit or ModLineCategory.Crafted or ModLineCategory.Fractured);

    public IEnumerable<ParsedModLine> AllModLines => ModLines;
}
