using System.Xml.Linq;
using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Tests.Import;

public class BuildXmlSaverTests
{
    private static string TestDataPath(string filename) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", filename);

    // ─── Round-trip helpers ───

    private static BuildData RoundTrip(BuildData original)
    {
        var xml = BuildXmlSaver.Save(original);
        return BuildXmlLoader.Load(xml);
    }

    // ─── Round-trip tests ───

    [Fact]
    public void RoundTrip_MinimalBuild_Matches()
    {
        var original = BuildXmlLoader.LoadFromFile(TestDataPath("MinimalBuild.xml"));
        var result = RoundTrip(original);
        Assert.Equal(original.Metadata.Level, result.Metadata.Level);
        Assert.Equal(original.Metadata.ClassName, result.Metadata.ClassName);
        Assert.Equal(original.Metadata.AscendClassName, result.Metadata.AscendClassName);
    }

    [Fact]
    public void RoundTrip_Metadata_Preserved()
    {
        var build = new BuildData();
        build.Metadata.Level = 95;
        build.Metadata.ClassName = "Shadow";
        build.Metadata.AscendClassName = "Assassin";
        build.Metadata.Bandit = "Alira";
        build.Metadata.PantheonMajorGod = "TheBrineKing";
        build.Metadata.PantheonMinorGod = "Gruthkul";
        build.Metadata.MainSocketGroup = 3;
        build.Metadata.ViewMode = "CALCS";
        build.Metadata.TargetVersion = "3_0";

        var result = RoundTrip(build);
        Assert.Equal(95, result.Metadata.Level);
        Assert.Equal("Shadow", result.Metadata.ClassName);
        Assert.Equal("Assassin", result.Metadata.AscendClassName);
        Assert.Equal("Alira", result.Metadata.Bandit);
        Assert.Equal("TheBrineKing", result.Metadata.PantheonMajorGod);
        Assert.Equal("Gruthkul", result.Metadata.PantheonMinorGod);
        Assert.Equal(3, result.Metadata.MainSocketGroup);
        Assert.Equal("CALCS", result.Metadata.ViewMode);
    }

    [Fact]
    public void RoundTrip_Config_Preserved()
    {
        var build = new BuildData();
        build.ConfigSets.Add(new ConfigSetData
        {
            Id = 1,
            Inputs = new List<ConfigInput>
            {
                new() { Name = "boolTest", Kind = ConfigInputKind.Boolean, BooleanValue = true },
                new() { Name = "numTest", Kind = ConfigInputKind.Number, NumberValue = 84.5 },
                new() { Name = "strTest", Kind = ConfigInputKind.String, StringValue = "Shaper" },
            }
        });

        var result = RoundTrip(build);
        var inputs = result.ConfigSets[0].Inputs;
        Assert.Equal(3, inputs.Count);
        Assert.True(inputs[0].BooleanValue);
        Assert.Equal(84.5, inputs[1].NumberValue);
        Assert.Equal("Shaper", inputs[2].StringValue);
    }

    [Fact]
    public void RoundTrip_Skills_Preserved()
    {
        var build = new BuildData();
        build.SkillSets.Add(new SkillSetData
        {
            Id = 1,
            SocketGroups = new List<SocketGroupData>
            {
                new()
                {
                    Enabled = true,
                    Slot = "Body Armour",
                    MainActiveSkill = 1,
                    Gems = new List<GemInstanceData>
                    {
                        new()
                        {
                            NameSpec = "Vortex",
                            GemId = "Metadata/Items/Gems/SkillGemFrostBoltNova",
                            SkillId = "FrostBoltNova",
                            Level = 21,
                            Quality = 0,
                        }
                    }
                }
            }
        });

        var result = RoundTrip(build);
        var group = result.SkillSets[0].SocketGroups[0];
        Assert.Equal("Body Armour", group.Slot);
        Assert.True(group.Enabled);
        Assert.Equal("Vortex", group.Gems[0].NameSpec);
        Assert.Equal(21, group.Gems[0].Level);
    }

    [Fact]
    public void RoundTrip_Tree_Preserved()
    {
        var build = new BuildData();
        build.ActiveSpec = 1;
        build.TreeSpecs.Add(new TreeSpecData
        {
            ClassId = 3,
            AscendClassId = 1,
            TreeVersion = "3_22",
            AllocatedNodes = new HashSet<int> { 100, 200, 300 },
            JewelSockets = new Dictionary<int, int> { { 500, 17 } },
        });

        var result = RoundTrip(build);
        Assert.Equal(1, result.ActiveSpec);
        var spec = result.TreeSpecs[0];
        Assert.Equal(3, spec.ClassId);
        Assert.Equal(1, spec.AscendClassId);
        Assert.Equal("3_22", spec.TreeVersion);
        Assert.Contains(100, spec.AllocatedNodes);
        Assert.Contains(200, spec.AllocatedNodes);
        Assert.Contains(300, spec.AllocatedNodes);
        Assert.Equal(17, spec.JewelSockets[500]);
    }

    [Fact]
    public void RoundTrip_Items_Preserved()
    {
        var build = new BuildData();
        build.Items.Add(new ItemData { Id = 1, RawText = "Rarity: RARE\nTest Sword\nCorsair Sword" });
        build.DefaultSlots.Add(new SlotAssignment { SlotName = "Weapon 1", ItemId = 1 });
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new() { SlotName = "Weapon 1", ItemId = 1 }
            }
        });

        var result = RoundTrip(build);
        Assert.Single(result.Items);
        Assert.Contains("Test Sword", result.Items[0].RawText);
        Assert.Equal("Weapon 1", result.DefaultSlots[0].SlotName);
        Assert.Equal(1, result.DefaultSlots[0].ItemId);
        Assert.Single(result.ItemSets);
        Assert.Equal("Weapon 1", result.ItemSets[0].Slots[0].SlotName);
    }

    [Fact]
    public void RoundTrip_Notes_Preserved()
    {
        var build = new BuildData();
        build.Notes = "This is a test note with special chars: <>&\"";

        var result = RoundTrip(build);
        Assert.Equal(build.Notes, result.Notes);
    }

    [Fact]
    public void RoundTrip_EntityEncoding()
    {
        var build = new BuildData();
        build.Items.Add(new ItemData { Id = 1, RawText = "Heretic's Veil" });

        var result = RoundTrip(build);
        Assert.Contains("Heretic's Veil", result.Items[0].RawText);
    }

    [Fact]
    public void RoundTrip_RealBuild_Stable()
    {
        var original = BuildXmlLoader.LoadFromFile(TestDataPath("OccVortex.xml"));
        var result = RoundTrip(original);

        Assert.Equal(original.Metadata.Level, result.Metadata.Level);
        Assert.Equal(original.Metadata.ClassName, result.Metadata.ClassName);
        Assert.Equal(original.Metadata.AscendClassName, result.Metadata.AscendClassName);
        Assert.Equal(original.Items.Count, result.Items.Count);

        var origGroups = original.SkillSets.SelectMany(s => s.SocketGroups).ToList();
        var resultGroups = result.SkillSets.SelectMany(s => s.SocketGroups).ToList();
        Assert.Equal(origGroups.Count, resultGroups.Count);

        Assert.Equal(
            original.TreeSpecs[0].AllocatedNodes.Count,
            result.TreeSpecs[0].AllocatedNodes.Count
        );
    }

    [Fact]
    public void Save_ProducesValidXml()
    {
        var build = new BuildData();
        build.Metadata.Level = 1;
        build.Metadata.ClassName = "Marauder";

        var xml = BuildXmlSaver.Save(build);
        var doc = XDocument.Parse(xml);
        Assert.NotNull(doc.Root);
        Assert.Equal("PathOfBuilding", doc.Root.Name.LocalName);
    }
}
