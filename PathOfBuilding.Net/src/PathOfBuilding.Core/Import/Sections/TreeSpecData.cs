namespace PathOfBuilding.Core.Import.Sections;

public class TreeSpecData
{
    public int ClassId { get; set; }
    public int AscendClassId { get; set; }
    public string TreeVersion { get; set; } = "";
    public HashSet<int> AllocatedNodes { get; set; } = new();
    public Dictionary<int, int> MasterySelections { get; set; } = new();
    public Dictionary<int, int> JewelSockets { get; set; } = new();
    public string? Url { get; set; }
}
