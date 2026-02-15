namespace PathOfBuilding.Core.Import.Sections;

public class ItemSetData
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public bool UseSecondWeaponSet { get; set; }
    public List<SlotAssignment> Slots { get; set; } = new();
}
