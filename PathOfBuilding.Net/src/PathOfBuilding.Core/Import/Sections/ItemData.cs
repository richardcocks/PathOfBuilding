namespace PathOfBuilding.Core.Import.Sections;

public class ItemData
{
    public int Id { get; set; }
    public string RawText { get; set; } = "";
    public int? Variant { get; set; }
    public int? VariantAlt { get; set; }
    public int? VariantAlt2 { get; set; }
    public int? VariantAlt3 { get; set; }
    public Dictionary<int, double> ModRanges { get; set; } = new();
}
