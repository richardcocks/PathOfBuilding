namespace PathOfBuilding.Core.Import.Sections;

public class GemInstanceData
{
    public string NameSpec { get; set; } = "";
    public string GemId { get; set; } = "";
    public string SkillId { get; set; } = "";
    public string QualityId { get; set; } = "Default";
    public int Level { get; set; } = 1;
    public int Quality { get; set; }
    public bool Enabled { get; set; } = true;
    public bool EnableGlobal1 { get; set; } = true;
    public bool EnableGlobal2 { get; set; } = true;
    public int? SkillPart { get; set; }
    public int? SkillPartCalcs { get; set; }
    public string? SkillMinion { get; set; }
}
