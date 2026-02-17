using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Tree;

namespace PathOfBuilding.Core.Tests.Tree;

public class TreeModApplicatorTests
{
    private static TreeData MakeTree(params (int id, string name, string[] stats, bool isMastery, Dictionary<int, List<string>>? masteryEffects)[] nodes)
    {
        var dict = new Dictionary<int, TreeNode>();
        foreach (var (id, name, stats, isMastery, masteryEffects) in nodes)
        {
            dict[id] = new TreeNode
            {
                Id = id,
                Name = name,
                Stats = stats.ToList(),
                IsMastery = isMastery,
                MasteryEffects = masteryEffects,
            };
        }
        return new TreeData { Nodes = dict, Version = "test" };
    }

    [Fact]
    public void Apply_SingleNode_AddsMods()
    {
        var tree = MakeTree(
            (100, "Strength", new[] { "+10 to Strength" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        double str = modDB.Sum(ModType.Base, null, "Str");
        Assert.Equal(10, str);
    }

    [Fact]
    public void Apply_MultipleNodes_AllModsApplied()
    {
        var tree = MakeTree(
            (100, "Str", new[] { "+10 to Strength" }, false, null),
            (200, "Dex", new[] { "+10 to Dexterity" }, false, null),
            (300, "Int", new[] { "+10 to Intelligence" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100, 200, 300 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        Assert.Equal(10, modDB.Sum(ModType.Base, null, "Str"));
        Assert.Equal(10, modDB.Sum(ModType.Base, null, "Dex"));
        Assert.Equal(10, modDB.Sum(ModType.Base, null, "Int"));
    }

    [Fact]
    public void Apply_NodeWithMultipleStats()
    {
        var tree = MakeTree(
            (49254, "Retribution", new[]
            {
                "14% increased Damage",
                "5% increased Attack and Cast Speed",
                "+10 to Strength and Intelligence"
            }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 49254 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        // Check that multiple stats from a single node are all applied
        Assert.True(modDB.Sum(ModType.Inc, null, "Damage") > 0);
        Assert.Equal(10, modDB.Sum(ModType.Base, null, "Str"));
        Assert.Equal(10, modDB.Sum(ModType.Base, null, "Int"));
    }

    [Fact]
    public void Apply_IncMod_CorrectType()
    {
        var tree = MakeTree(
            (100, "Life", new[] { "5% increased maximum Life" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        Assert.Equal(5, modDB.Sum(ModType.Inc, null, "Life"));
        Assert.Equal(0, modDB.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_MultipleSameStatNodes_Sum()
    {
        var tree = MakeTree(
            (100, "Int1", new[] { "+10 to Intelligence" }, false, null),
            (200, "Int2", new[] { "+10 to Intelligence" }, false, null),
            (300, "Int3", new[] { "+10 to Intelligence" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100, 200, 300 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        Assert.Equal(30, modDB.Sum(ModType.Base, null, "Int"));
    }

    [Fact]
    public void Apply_NodeNotInTree_Skipped()
    {
        var tree = MakeTree(
            (100, "Str", new[] { "+10 to Strength" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100, 999 } };
        var modDB = new ModDB();

        // Node 999 doesn't exist in tree — should be silently skipped
        TreeModApplicator.Apply(tree, spec, modDB);

        Assert.Equal(10, modDB.Sum(ModType.Base, null, "Str"));
    }

    [Fact]
    public void Apply_MasteryNode_SkippedWithoutSelection()
    {
        var effects = new Dictionary<int, List<string>>
        {
            [48385] = new() { "Exposure you inflict applies at least -18% to the affected Resistance" }
        };
        var tree = MakeTree(
            (63824, "Elemental Mastery", Array.Empty<string>(), true, effects)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 63824 } };
        var modDB = new ModDB();

        // Mastery node without selection → no mods applied
        TreeModApplicator.Apply(tree, spec, modDB);

        var allMods = modDB.Tabulate(null, null);
        Assert.Empty(allMods);
    }

    [Fact]
    public void Apply_MasterySelection_AppliesEffectStats()
    {
        var effects = new Dictionary<int, List<string>>
        {
            [48385] = new() { "+5% to maximum Fire Resistance" },
            [4119] = new() { "60% reduced Reflected Elemental Damage taken" }
        };
        var tree = MakeTree(
            (63824, "Elemental Mastery", Array.Empty<string>(), true, effects)
        );
        var spec = new TreeSpecData
        {
            AllocatedNodes = { 63824 },
            MasterySelections = { [63824] = 48385 }
        };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        Assert.Equal(5, modDB.Sum(ModType.Base, null, "FireResistMax"));
    }

    [Fact]
    public void Apply_SourceTracking()
    {
        var tree = MakeTree(
            (100, "Str", new[] { "+10 to Strength" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        var mods = modDB.Tabulate(ModType.Base, null, "Str");
        Assert.Single(mods);
        Assert.Equal("Tree:100", mods[0].Mod.Source);
    }

    [Fact]
    public void Apply_MasterySourceTracking()
    {
        var effects = new Dictionary<int, List<string>>
        {
            [48385] = new() { "+5% to maximum Fire Resistance" }
        };
        var tree = MakeTree(
            (63824, "Mastery", Array.Empty<string>(), true, effects)
        );
        var spec = new TreeSpecData
        {
            AllocatedNodes = { 63824 },
            MasterySelections = { [63824] = 48385 }
        };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        var mods = modDB.Tabulate(ModType.Base, null, "FireResistMax");
        Assert.Single(mods);
        Assert.Equal("Tree:63824:Mastery:48385", mods[0].Mod.Source);
    }

    [Fact]
    public void Apply_EmptySpec_NoMods()
    {
        var tree = MakeTree(
            (100, "Str", new[] { "+10 to Strength" }, false, null)
        );
        var spec = new TreeSpecData();
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        var allMods = modDB.Tabulate(null, null);
        Assert.Empty(allMods);
    }

    [Fact]
    public void Apply_UnparseableStat_Skipped()
    {
        var tree = MakeTree(
            (100, "Weird", new[] { "This is not a real mod text xyz123" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        var allMods = modDB.Tabulate(null, null);
        Assert.Empty(allMods);
    }

    [Fact]
    public void Apply_ResistanceMod()
    {
        var tree = MakeTree(
            (100, "Resist", new[] { "+10% to Fire Resistance" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        Assert.Equal(10, modDB.Sum(ModType.Base, null, "FireResist"));
    }

    [Fact]
    public void Apply_PercentMod()
    {
        var tree = MakeTree(
            (100, "ElemDmg", new[] { "10% increased Elemental Damage" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        Assert.Equal(10, modDB.Sum(ModType.Inc, null, "ElementalDamage"));
    }

    [Fact]
    public void Apply_MoreMod()
    {
        var tree = MakeTree(
            (100, "MoreDmg", new[] { "40% more Damage" }, false, null)
        );
        var spec = new TreeSpecData { AllocatedNodes = { 100 } };
        var modDB = new ModDB();

        TreeModApplicator.Apply(tree, spec, modDB);

        // More multiplier: (1 + 40/100) = 1.4
        Assert.Equal(1.4, modDB.More(null, "Damage"), 4);
    }
}
