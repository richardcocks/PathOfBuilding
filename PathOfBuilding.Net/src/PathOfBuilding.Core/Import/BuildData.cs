using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Import;

public class BuildData
{
    public BuildMetadata Metadata { get; set; } = new();
    public List<PlayerStatData> PlayerStats { get; set; } = new();
    public List<ConfigSetData> ConfigSets { get; set; } = new();
    public List<SkillSetData> SkillSets { get; set; } = new();
    public List<TreeSpecData> TreeSpecs { get; set; } = new();
    public int ActiveSpec { get; set; } = 1;
    public List<ItemData> Items { get; set; } = new();
    public List<ItemSetData> ItemSets { get; set; } = new();
    public int ActiveItemSet { get; set; } = 1;
    public List<SlotAssignment> DefaultSlots { get; set; } = new();
    public bool UseSecondWeaponSet { get; set; }
    public string Notes { get; set; } = "";
}
