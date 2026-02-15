namespace PathOfBuilding.Core.Import.Sections;

public class SocketGroupData
{
    public bool Enabled { get; set; } = true;
    public string? Slot { get; set; }
    public string? Label { get; set; }
    public string? Source { get; set; }
    public int MainActiveSkill { get; set; } = 1;
    public int MainActiveSkillCalcs { get; set; } = 1;
    public bool IncludeInFullDPS { get; set; }
    public int? GroupCount { get; set; }
    public List<GemInstanceData> Gems { get; set; } = new();
}
