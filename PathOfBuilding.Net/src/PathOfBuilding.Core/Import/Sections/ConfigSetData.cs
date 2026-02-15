namespace PathOfBuilding.Core.Import.Sections;

public class ConfigSetData
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public List<ConfigInput> Inputs { get; set; } = new();
}
