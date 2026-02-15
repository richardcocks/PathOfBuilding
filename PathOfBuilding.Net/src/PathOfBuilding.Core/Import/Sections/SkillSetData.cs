namespace PathOfBuilding.Core.Import.Sections;

public class SkillSetData
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public List<SocketGroupData> SocketGroups { get; set; } = new();
}
