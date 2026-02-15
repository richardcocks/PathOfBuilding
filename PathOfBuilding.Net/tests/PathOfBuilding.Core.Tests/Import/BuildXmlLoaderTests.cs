using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Tests.Import;

public class BuildXmlLoaderTests
{
    private static string TestDataPath(string filename) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", filename);

    private static BuildData LoadMinimal() =>
        BuildXmlLoader.LoadFromFile(TestDataPath("MinimalBuild.xml"));

    private static BuildData LoadOccVortex() =>
        BuildXmlLoader.LoadFromFile(TestDataPath("OccVortex.xml"));

    // ─── Minimal Build ───

    [Fact]
    public void Load_MinimalBuild_Succeeds()
    {
        var build = LoadMinimal();
        Assert.NotNull(build);
        Assert.NotNull(build.Metadata);
    }

    // ─── Metadata ───

    [Fact]
    public void Load_Metadata_Level()
    {
        var build = LoadMinimal();
        Assert.Equal(1, build.Metadata.Level);
    }

    [Fact]
    public void Load_Metadata_Class()
    {
        var build = LoadMinimal();
        Assert.Equal("Marauder", build.Metadata.ClassName);
        Assert.Equal("None", build.Metadata.AscendClassName);
    }

    [Fact]
    public void Load_Metadata_Bandit()
    {
        var build = LoadMinimal();
        Assert.Equal("None", build.Metadata.Bandit);
    }

    [Fact]
    public void Load_Metadata_Pantheon()
    {
        var build = LoadMinimal();
        Assert.Equal("None", build.Metadata.PantheonMajorGod);
        Assert.Equal("None", build.Metadata.PantheonMinorGod);
    }

    [Fact]
    public void Load_Metadata_ViewMode()
    {
        var build = LoadMinimal();
        Assert.Equal("TREE", build.Metadata.ViewMode);
    }

    [Fact]
    public void Load_RealBuild_Level99()
    {
        var build = LoadOccVortex();
        Assert.Equal(99, build.Metadata.Level);
    }

    [Fact]
    public void Load_RealBuild_ClassAndAscendancy()
    {
        var build = LoadOccVortex();
        Assert.Equal("Witch", build.Metadata.ClassName);
        Assert.Equal("Occultist", build.Metadata.AscendClassName);
    }

    [Fact]
    public void Load_RealBuild_ViewMode()
    {
        var build = LoadOccVortex();
        Assert.Equal("CALCS", build.Metadata.ViewMode);
    }

    // ─── Player Stats ───

    [Fact]
    public void Load_PlayerStats_Parsed()
    {
        var build = LoadOccVortex();
        Assert.NotEmpty(build.PlayerStats);
    }

    [Fact]
    public void Load_PlayerStats_Values()
    {
        var build = LoadOccVortex();
        var life = build.PlayerStats.First(s => s.Name == "Life");
        Assert.Equal(6728, life.Value);
    }

    [Fact]
    public void Load_PlayerStats_DoubleValues()
    {
        var build = LoadOccVortex();
        var dot = build.PlayerStats.First(s => s.Name == "TotalDot");
        Assert.Equal(566925.51596343, dot.Value, 5);
    }

    // ─── Config ───

    [Fact]
    public void Load_Config_BooleanInput()
    {
        var build = LoadOccVortex();
        var inputs = build.ConfigSets.SelectMany(s => s.Inputs).ToList();
        var input = inputs.First(i => i.Name == "conditionEnemyChilled");
        Assert.Equal(ConfigInputKind.Boolean, input.Kind);
        Assert.True(input.BooleanValue);
    }

    [Fact]
    public void Load_Config_StringInput()
    {
        var build = LoadOccVortex();
        var inputs = build.ConfigSets.SelectMany(s => s.Inputs).ToList();
        var input = inputs.First(i => i.Name == "enemyIsBoss");
        Assert.Equal(ConfigInputKind.String, input.Kind);
        Assert.Equal("Shaper", input.StringValue);
    }

    [Fact]
    public void Load_Config_NumberInput()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""1"" className=""Marauder"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
  <Config>
    <Input name=""enemyLevel"" number=""84""/>
  </Config>
</PathOfBuilding>";
        var build = BuildXmlLoader.Load(xml);
        var inputs = build.ConfigSets.SelectMany(s => s.Inputs).ToList();
        var input = inputs.First(i => i.Name == "enemyLevel");
        Assert.Equal(ConfigInputKind.Number, input.Kind);
        Assert.Equal(84, input.NumberValue);
    }

    [Fact]
    public void Load_Config_MultipleInputs()
    {
        var build = LoadOccVortex();
        var inputs = build.ConfigSets.SelectMany(s => s.Inputs).ToList();
        Assert.True(inputs.Count >= 9);
    }

    // ─── Skills ───

    [Fact]
    public void Load_Skills_SocketGroup()
    {
        var build = LoadOccVortex();
        var groups = build.SkillSets.SelectMany(s => s.SocketGroups).ToList();
        Assert.NotEmpty(groups);
    }

    [Fact]
    public void Load_Skills_GemAttributes()
    {
        var build = LoadOccVortex();
        var groups = build.SkillSets.SelectMany(s => s.SocketGroups).ToList();
        var vortexGroup = groups.First(g => g.Slot == "Body Armour");
        var vortex = vortexGroup.Gems.First(g => g.NameSpec == "Vortex");
        Assert.Equal("Metadata/Items/Gems/SkillGemFrostBoltNova", vortex.GemId);
        Assert.Equal("FrostBoltNova", vortex.SkillId);
        Assert.Equal(21, vortex.Level);
        Assert.Equal(0, vortex.Quality);
        Assert.Equal("Default", vortex.QualityId);
        Assert.True(vortex.Enabled);
    }

    [Fact]
    public void Load_Skills_MultipleGroups()
    {
        var build = LoadOccVortex();
        var groups = build.SkillSets.SelectMany(s => s.SocketGroups).ToList();
        Assert.Equal(6, groups.Count);
    }

    [Fact]
    public void Load_Skills_DisabledGem()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""1"" className=""Marauder"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
  <Skills>
    <Skill enabled=""true"" mainActiveSkill=""1"">
      <Gem nameSpec=""Test"" gemId=""test"" skillId=""test"" qualityId=""Default""
           level=""1"" quality=""0"" enabled=""false"" enableGlobal1=""true"" enableGlobal2=""true""/>
    </Skill>
  </Skills>
</PathOfBuilding>";
        var build = BuildXmlLoader.Load(xml);
        var gem = build.SkillSets[0].SocketGroups[0].Gems[0];
        Assert.False(gem.Enabled);
    }

    [Fact]
    public void Load_Skills_OptionalAttributes()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""1"" className=""Marauder"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
  <Skills>
    <Skill enabled=""true"" mainActiveSkill=""1"">
      <Gem nameSpec=""Test"" gemId=""test"" skillId=""test"" qualityId=""Default""
           level=""1"" quality=""0"" enabled=""true"" enableGlobal1=""true"" enableGlobal2=""true""/>
    </Skill>
  </Skills>
</PathOfBuilding>";
        var build = BuildXmlLoader.Load(xml);
        var group = build.SkillSets[0].SocketGroups[0];
        Assert.Null(group.Slot);
        Assert.Null(group.Label);
        Assert.Null(group.GroupCount);
    }

    // ─── Tree ───

    [Fact]
    public void Load_Tree_AllocatedNodes()
    {
        var build = LoadOccVortex();
        var spec = build.TreeSpecs[0];
        Assert.Contains(7388, spec.AllocatedNodes);
        Assert.Contains(4184, spec.AllocatedNodes);
        Assert.True(spec.AllocatedNodes.Count > 100);
    }

    [Fact]
    public void Load_Tree_EmptyNodes()
    {
        var build = LoadMinimal();
        var spec = build.TreeSpecs[0];
        Assert.Empty(spec.AllocatedNodes);
    }

    [Fact]
    public void Load_Tree_ClassAndAscendancy()
    {
        var build = LoadOccVortex();
        var spec = build.TreeSpecs[0];
        Assert.Equal(3, spec.ClassId);
        Assert.Equal(1, spec.AscendClassId);
    }

    [Fact]
    public void Load_Tree_JewelSockets()
    {
        var build = LoadOccVortex();
        var spec = build.TreeSpecs[0];
        Assert.NotEmpty(spec.JewelSockets);
        // Socket 36634 has itemId=17
        Assert.Equal(17, spec.JewelSockets[36634]);
    }

    [Fact]
    public void Load_Tree_TreeVersion()
    {
        var build = LoadOccVortex();
        var spec = build.TreeSpecs[0];
        Assert.Equal("3_13", spec.TreeVersion);
    }

    [Fact]
    public void Load_Tree_Url()
    {
        var build = LoadOccVortex();
        var spec = build.TreeSpecs[0];
        Assert.NotNull(spec.Url);
        Assert.StartsWith("https://", spec.Url);
    }

    // ─── Items ───

    [Fact]
    public void Load_Items_RawText()
    {
        var build = LoadOccVortex();
        var item = build.Items.First(i => i.Id == 1);
        Assert.Contains("Loath Sanctuary", item.RawText);
        Assert.Contains("Vaal Spirit Shield", item.RawText);
    }

    [Fact]
    public void Load_Items_ModRanges()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""1"" className=""Marauder"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
  <Items activeItemSet=""1"">
    <Item id=""1"">
      Rarity: RARE
Test Item
<ModRange id=""3"" range=""0.5""/></Item>
  </Items>
</PathOfBuilding>";
        var build = BuildXmlLoader.Load(xml);
        var item = build.Items[0];
        Assert.Equal(0.5, item.ModRanges[3]);
    }

    [Fact]
    public void Load_Items_SlotAssignments()
    {
        var build = LoadOccVortex();
        Assert.NotEmpty(build.DefaultSlots);
        var weapon1 = build.DefaultSlots.First(s => s.SlotName == "Weapon 1");
        Assert.Equal(3, weapon1.ItemId);
    }

    [Fact]
    public void Load_Items_ItemSet()
    {
        var build = LoadOccVortex();
        Assert.NotEmpty(build.ItemSets);
        var set = build.ItemSets[0];
        Assert.Equal(1, set.Id);
        Assert.NotEmpty(set.Slots);
    }

    [Fact]
    public void Load_Items_ActiveFlask()
    {
        var build = LoadOccVortex();
        var flask3 = build.DefaultSlots.First(s => s.SlotName == "Flask 3");
        Assert.True(flask3.Active);
        Assert.Equal(14, flask3.ItemId);
    }

    [Fact]
    public void Load_Items_Count()
    {
        var build = LoadOccVortex();
        Assert.Equal(18, build.Items.Count);
    }

    // ─── Notes ───

    [Fact]
    public void Load_Notes_Text()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""1"" className=""Marauder"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
  <Notes>This is a test build note.</Notes>
</PathOfBuilding>";
        var build = BuildXmlLoader.Load(xml);
        Assert.Equal("This is a test build note.", build.Notes);
    }

    // ─── Missing Sections ───

    [Fact]
    public void Load_MissingSections_Defaults()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""50"" className=""Ranger"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
</PathOfBuilding>";
        var build = BuildXmlLoader.Load(xml);
        Assert.Empty(build.ConfigSets);
        Assert.Empty(build.SkillSets);
        Assert.Empty(build.TreeSpecs);
        Assert.Empty(build.Items);
        Assert.Empty(build.ItemSets);
        Assert.Equal("", build.Notes);
        Assert.Equal(50, build.Metadata.Level);
    }

    // ─── Real Build Full Validation ───

    [Fact]
    public void Load_RealBuild_AllSectionsPopulated()
    {
        var build = LoadOccVortex();
        Assert.Equal(99, build.Metadata.Level);
        Assert.Equal("Witch", build.Metadata.ClassName);
        Assert.Equal("Occultist", build.Metadata.AscendClassName);
        Assert.NotEmpty(build.PlayerStats);
        Assert.NotEmpty(build.ConfigSets);
        Assert.NotEmpty(build.SkillSets);
        Assert.NotEmpty(build.TreeSpecs);
        Assert.NotEmpty(build.Items);
        Assert.NotEmpty(build.ItemSets);
        Assert.NotEmpty(build.DefaultSlots);

        // Verify counts
        var inputs = build.ConfigSets.SelectMany(s => s.Inputs).ToList();
        Assert.True(inputs.Count >= 9);
        var groups = build.SkillSets.SelectMany(s => s.SocketGroups).ToList();
        Assert.Equal(6, groups.Count);
        Assert.True(build.TreeSpecs[0].AllocatedNodes.Count > 100);
        Assert.Equal(18, build.Items.Count);
    }

    // ─── ConfigSet wrapper format ───

    [Fact]
    public void Load_ConfigSet_NewFormat()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""1"" className=""Marauder"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
  <Config activeConfigSet=""1"">
    <ConfigSet id=""1"" title=""Default"">
      <Input name=""test"" boolean=""true""/>
    </ConfigSet>
    <ConfigSet id=""2"" title=""Mapping"">
      <Input name=""test2"" string=""hello""/>
    </ConfigSet>
  </Config>
</PathOfBuilding>";
        var build = BuildXmlLoader.Load(xml);
        Assert.Equal(2, build.ConfigSets.Count);
        Assert.Equal("Default", build.ConfigSets[0].Title);
        Assert.Equal("Mapping", build.ConfigSets[1].Title);
        Assert.Single(build.ConfigSets[0].Inputs);
        Assert.Single(build.ConfigSets[1].Inputs);
    }

    // ─── SkillSet wrapper format ───

    [Fact]
    public void Load_SkillSet_NewFormat()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""1"" className=""Marauder"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
  <Skills>
    <SkillSet id=""1"" title=""Main"">
      <Skill enabled=""true"" mainActiveSkill=""1"">
        <Gem nameSpec=""Test"" gemId=""t"" skillId=""t"" qualityId=""Default""
             level=""1"" quality=""0"" enabled=""true"" enableGlobal1=""true"" enableGlobal2=""true""/>
      </Skill>
    </SkillSet>
  </Skills>
</PathOfBuilding>";
        var build = BuildXmlLoader.Load(xml);
        Assert.Single(build.SkillSets);
        Assert.Equal("Main", build.SkillSets[0].Title);
        Assert.Single(build.SkillSets[0].SocketGroups);
    }
}
