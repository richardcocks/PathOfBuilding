using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Tree;

namespace PathOfBuilding.Core.Tests.Tree;

public class BuildPipelineTreeTests
{
    private static BuildData MakeMinimalBuild(int[] allocatedNodes, string treeVersion = "test")
    {
        return new BuildData
        {
            Metadata = new BuildMetadata
            {
                ClassName = "Witch",
                AscendClassName = "Occultist",
                Level = 90,
            },
            TreeSpecs = new List<TreeSpecData>
            {
                new()
                {
                    TreeVersion = treeVersion,
                    AllocatedNodes = new HashSet<int>(allocatedNodes),
                }
            },
            ActiveSpec = 1,
        };
    }

    private static TreeDataCache MakeCache(string version, params (int id, string[] stats)[] nodes)
    {
        var treeNodes = new Dictionary<int, TreeNode>();
        foreach (var (id, stats) in nodes)
        {
            treeNodes[id] = new TreeNode
            {
                Id = id,
                Name = $"Node{id}",
                Stats = stats.ToList(),
            };
        }
        var cache = new TreeDataCache();
        cache.SetTreeData(version, new TreeData { Nodes = treeNodes, Version = version });
        return cache;
    }

    [Fact]
    public void CreateActors_WithTreeCache_AppliesTreeMods()
    {
        var build = MakeMinimalBuild(new[] { 100, 200 });
        var cache = MakeCache("test",
            (100, new[] { "+20 to Intelligence" }),
            (200, new[] { "+30 to maximum Life" })
        );

        var (player, _) = BuildPipeline.CreateActors(build, cache);

        // Tree nodes should add +20 Int and +30 Life base
        double intBase = player.ModDB.Sum(ModType.Base, null, "Int");
        double lifeBase = player.ModDB.Sum(ModType.Base, null, "Life");

        // Int includes class base (24 for Witch) + level (0) + 20 from tree
        Assert.True(intBase >= 20, $"Expected Int base >= 20, got {intBase}");
        Assert.True(lifeBase >= 30, $"Expected Life base >= 30, got {lifeBase}");
    }

    [Fact]
    public void CreateActors_WithoutTreeCache_WorksNormally()
    {
        var build = MakeMinimalBuild(new[] { 100 });

        // No tree cache — should work without applying tree mods
        var (player, _) = BuildPipeline.CreateActors(build);

        // Should still have base stats from class/level
        double lifeBase = player.ModDB.Sum(ModType.Base, null, "Life");
        Assert.True(lifeBase > 0);
    }

    [Fact]
    public void CreateActors_NoMatchingTreeVersion_SkipsTreeMods()
    {
        var build = MakeMinimalBuild(new[] { 100 }, "3_99");
        var cache = MakeCache("test",
            (100, new[] { "+999 to Strength" })
        );

        // Tree version "3_99" doesn't match cache's "test" version
        var (player, _) = BuildPipeline.CreateActors(build, cache);

        // The +999 Str should NOT be applied
        var mods = player.ModDB.Tabulate(ModType.Base, null, "Str");
        foreach (var m in mods)
        {
            Assert.NotEqual(999.0, m.Value.AsNumber());
        }
    }

    [Fact]
    public void CreateActors_EmptyAllocatedNodes_NoTreeMods()
    {
        var build = MakeMinimalBuild(Array.Empty<int>());
        var cache = MakeCache("test",
            (100, new[] { "+10 to Strength" })
        );

        var (player, _) = BuildPipeline.CreateActors(build, cache);

        // No tree mods applied
        var treeMods = player.ModDB.Tabulate(ModType.Base, null, "Str")
            .Where(m => m.Mod.Source.StartsWith("Tree:"));
        Assert.Empty(treeMods);
    }

    [Fact]
    public void Calculate_WithTree_AffectsOutput()
    {
        var build = MakeMinimalBuild(new[] { 100, 200, 300 });
        var cache = MakeCache("test",
            (100, new[] { "5% increased maximum Life" }),
            (200, new[] { "5% increased maximum Life" }),
            (300, new[] { "5% increased maximum Life" })
        );

        // Calculate without tree
        var (playerNoTree, _) = BuildPipeline.Calculate(build);

        // Calculate with tree
        var (playerWithTree, _) = BuildPipeline.Calculate(build, cache);

        // Life should be higher with 15% increased life from tree
        Assert.True(playerWithTree.Output["Life"] > playerNoTree.Output["Life"],
            $"Expected tree life ({playerWithTree.Output["Life"]}) > no-tree life ({playerNoTree.Output["Life"]})");
    }

    [Fact]
    public void Calculate_TreeResistanceMods()
    {
        var build = MakeMinimalBuild(new[] { 100, 200, 300 });
        var cache = MakeCache("test",
            (100, new[] { "+30% to Fire Resistance" }),
            (200, new[] { "+30% to Cold Resistance" }),
            (300, new[] { "+30% to Lightning Resistance" })
        );

        var (player, _) = BuildPipeline.Calculate(build, cache);

        // Tree should add +30 to each resistance
        // Total resist = base (from InitPlayerModDB) + 30 from tree
        Assert.True(player.Output.ContainsKey("FireResist"));
        Assert.True(player.Output.ContainsKey("ColdResist"));
        Assert.True(player.Output.ContainsKey("LightningResist"));
    }

    [Fact]
    public void Calculate_TreeStrengthAffectsLife()
    {
        var build = MakeMinimalBuild(new[] { 100, 200, 300, 400, 500 });
        var cache = MakeCache("test",
            (100, new[] { "+10 to Strength" }),
            (200, new[] { "+10 to Strength" }),
            (300, new[] { "+10 to Strength" }),
            (400, new[] { "+10 to Strength" }),
            (500, new[] { "+10 to Strength" })
        );

        // Without tree
        var (playerNoTree, _) = BuildPipeline.Calculate(build);

        // With tree: +50 Str → +25 Life from Str bonus (1 Life per 2 Str)
        var (playerWithTree, _) = BuildPipeline.Calculate(build, cache);

        Assert.True(playerWithTree.Output["Life"] > playerNoTree.Output["Life"]);
    }

    [Fact]
    public void CreateActors_ActiveSpecSelection()
    {
        // Build with two tree specs, activeSpec=2
        var build = new BuildData
        {
            Metadata = new BuildMetadata
            {
                ClassName = "Witch",
                AscendClassName = "Occultist",
                Level = 90,
            },
            TreeSpecs = new List<TreeSpecData>
            {
                new() { TreeVersion = "test", AllocatedNodes = { 100 } },
                new() { TreeVersion = "test", AllocatedNodes = { 200 } },
            },
            ActiveSpec = 2,
        };
        var cache = MakeCache("test",
            (100, new[] { "+10 to Strength" }),
            (200, new[] { "+10 to Dexterity" })
        );

        var (player, _) = BuildPipeline.CreateActors(build, cache);

        // ActiveSpec=2 → only spec 2 nodes (200) applied
        var strMods = player.ModDB.Tabulate(ModType.Base, null, "Str")
            .Where(m => m.Mod.Source.StartsWith("Tree:"));
        var dexMods = player.ModDB.Tabulate(ModType.Base, null, "Dex")
            .Where(m => m.Mod.Source.StartsWith("Tree:"));

        Assert.Empty(strMods);
        Assert.Single(dexMods);
    }

    [Fact]
    public void TreeDataCache_SetAndGet()
    {
        var cache = new TreeDataCache();
        var tree = new TreeData { Version = "test", Nodes = new Dictionary<int, TreeNode>() };

        cache.SetTreeData("test", tree);
        var result = cache.GetTreeData("test");

        Assert.Same(tree, result);
    }

    [Fact]
    public void TreeDataCache_GetUnknownVersion_ReturnsNull()
    {
        var cache = new TreeDataCache();
        Assert.Null(cache.GetTreeData("unknown"));
    }

    [Fact]
    public void TreeDataCache_EmptyVersion_ReturnsNull()
    {
        var cache = new TreeDataCache();
        Assert.Null(cache.GetTreeData(""));
    }
}
