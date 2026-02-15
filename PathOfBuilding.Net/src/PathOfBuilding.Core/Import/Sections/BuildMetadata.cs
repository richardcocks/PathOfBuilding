namespace PathOfBuilding.Core.Import.Sections;

public class BuildMetadata
{
    public int Level { get; set; } = 1;
    public string TargetVersion { get; set; } = "3_0";
    public string ClassName { get; set; } = "";
    public string AscendClassName { get; set; } = "None";
    public string Bandit { get; set; } = "None";
    public string PantheonMajorGod { get; set; } = "None";
    public string PantheonMinorGod { get; set; } = "None";
    public int MainSocketGroup { get; set; } = 1;
    public string ViewMode { get; set; } = "TREE";
    public bool CharacterLevelAutoMode { get; set; }
}
