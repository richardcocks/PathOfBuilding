using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Calculation;

public class BuildPipelineTests
{
    private static BuildData MinimalBuild(string className = "Witch", string ascendancy = "None",
        int level = 1, string bandit = "None")
    {
        return new BuildData
        {
            Metadata = new BuildMetadata
            {
                ClassName = className,
                AscendClassName = ascendancy,
                Level = level,
                Bandit = bandit,
            },
        };
    }

    // ─── Actor creation ───

    [Fact]
    public void CreateActors_ReturnsPlayerAndEnemy()
    {
        var build = MinimalBuild();
        var (player, enemy) = BuildPipeline.CreateActors(build);

        Assert.NotNull(player);
        Assert.NotNull(enemy);
    }

    [Fact]
    public void CreateActors_CrossReferencesSet()
    {
        var build = MinimalBuild();
        var (player, enemy) = BuildPipeline.CreateActors(build);

        Assert.Same(enemy, player.Enemy);
        Assert.Same(player, enemy.Enemy);
    }

    [Fact]
    public void CreateActors_ModDBsInitialized()
    {
        var build = MinimalBuild();
        var (player, enemy) = BuildPipeline.CreateActors(build);

        // Player ModDB has resist caps from InitModDB
        Assert.Equal(75, player.ModDB.Sum(ModType.Base, null, "FireResistMax"));
        // Enemy ModDB has accuracy
        Assert.True(enemy.ModDB.Sum(ModType.Base, null, "Accuracy") > 0);
    }

    // ─── Class stats applied ───

    [Fact]
    public void CreateActors_Witch_HasCorrectStats()
    {
        var build = MinimalBuild("Witch");
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(14, player.ModDB.Sum(ModType.Base, null, "Str"));
        Assert.Equal(14, player.ModDB.Sum(ModType.Base, null, "Dex"));
        Assert.Equal(32, player.ModDB.Sum(ModType.Base, null, "Int"));
    }

    [Fact]
    public void CreateActors_AscendancyResolved_Occultist()
    {
        var build = MinimalBuild("Witch", "Occultist");
        var (player, _) = BuildPipeline.CreateActors(build);

        // Occultist → Witch base stats
        Assert.Equal(14, player.ModDB.Sum(ModType.Base, null, "Str"));
        Assert.Equal(32, player.ModDB.Sum(ModType.Base, null, "Int"));
    }

    [Fact]
    public void CreateActors_AscendancyResolved_Juggernaut()
    {
        var build = MinimalBuild("Marauder", "Juggernaut", level: 90);
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(32, player.ModDB.Sum(ModType.Base, null, "Str"));
        Assert.Equal(90, player.ModDB.Multipliers["Level"]);
    }

    // ─── Level multiplier ───

    [Fact]
    public void CreateActors_Level99_MultiplierSet()
    {
        var build = MinimalBuild(level: 99);
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(99, player.ModDB.Multipliers["Level"]);
    }

    [Fact]
    public void CreateActors_Level1_MultiplierSet()
    {
        var build = MinimalBuild(level: 1);
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(1, player.ModDB.Multipliers["Level"]);
    }

    // ─── Resistance penalty from config ───

    [Fact]
    public void CreateActors_DefaultPenalty_Minus60()
    {
        var build = MinimalBuild();
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(-60, player.ModDB.Sum(ModType.Base, null, "FireResist"));
    }

    [Fact]
    public void CreateActors_CustomPenalty_FromConfig()
    {
        var build = MinimalBuild();
        build.ConfigSets.Add(new ConfigSetData
        {
            Inputs = new List<ConfigInput>
            {
                new ConfigInput { Name = "resistancePenalty", Kind = ConfigInputKind.Number, NumberValue = -30 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(-30, player.ModDB.Sum(ModType.Base, null, "FireResist"));
    }

    // ─── Bandit from metadata ───

    [Fact]
    public void CreateActors_BanditAlira_ElementalResist()
    {
        var build = MinimalBuild(bandit: "Alira");
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(15, player.ModDB.Sum(ModType.Base, null, "ElementalResist"));
    }

    [Fact]
    public void CreateActors_BanditOak_Life()
    {
        var build = MinimalBuild(bandit: "Oak");
        var (player, _) = BuildPipeline.CreateActors(build);

        // Oak adds 40 base life; per-level adds (12 * 1 + 38) = 50; total = 90
        Assert.Equal(90, player.ModDB.Sum(ModType.Base, null, "Life"));
    }

    // ─── Enemy level ───

    [Fact]
    public void GetEnemyLevel_NoConfig_DefaultsByBoss()
    {
        var build = MinimalBuild(level: 90);
        int level = BuildPipeline.GetEnemyLevel(build, 90);

        // No boss config, defaults to min(83, playerLevel) = 83
        Assert.Equal(83, level);
    }

    [Fact]
    public void GetEnemyLevel_ExplicitLevel_Used()
    {
        var build = MinimalBuild();
        build.ConfigSets.Add(new ConfigSetData
        {
            Inputs = new List<ConfigInput>
            {
                new ConfigInput { Name = "enemyLevel", Kind = ConfigInputKind.Number, NumberValue = 84 },
            }
        });

        int level = BuildPipeline.GetEnemyLevel(build, 90);
        Assert.Equal(84, level);
    }

    [Fact]
    public void GetEnemyLevel_PinnacleBoss_84()
    {
        var build = MinimalBuild();
        build.ConfigSets.Add(new ConfigSetData
        {
            Inputs = new List<ConfigInput>
            {
                new ConfigInput { Name = "enemyIsBoss", Kind = ConfigInputKind.String, StringValue = "Pinnacle" },
            }
        });

        int level = BuildPipeline.GetEnemyLevel(build, 90);
        Assert.Equal(84, level);
    }

    // ─── Config propagated ───

    [Fact]
    public void CreateActors_ConfigPropagated()
    {
        var build = MinimalBuild();
        build.ConfigSets.Add(new ConfigSetData
        {
            Inputs = new List<ConfigInput>
            {
                new ConfigInput { Name = "usePowerCharges", Kind = ConfigInputKind.Boolean, BooleanValue = true },
            }
        });

        var (player, enemy) = BuildPipeline.CreateActors(build);

        Assert.True(player.Config.UsePowerCharges);
        Assert.Same(player.Config, enemy.Config);
    }

    [Fact]
    public void CreateActors_BuffOnslaught_SetsCondition()
    {
        var build = MinimalBuild();
        build.ConfigSets.Add(new ConfigSetData
        {
            Inputs = new List<ConfigInput>
            {
                new ConfigInput { Name = "buffOnslaught", Kind = ConfigInputKind.Boolean, BooleanValue = true },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.True(player.ModDB.Conditions.ContainsKey("Condition:Onslaught"));
        Assert.True(player.Config.BuffOnslaught);
    }

    [Fact]
    public void CreateActors_EnemyCondition_SetsOnEnemyDB()
    {
        var build = MinimalBuild();
        build.ConfigSets.Add(new ConfigSetData
        {
            Inputs = new List<ConfigInput>
            {
                new ConfigInput { Name = "conditionEnemyIgnited", Kind = ConfigInputKind.Boolean, BooleanValue = true },
            }
        });

        var (_, enemy) = BuildPipeline.CreateActors(build);

        Assert.True(enemy.ModDB.Conditions.ContainsKey("Condition:Ignited"));
    }

    // ─── CalcConfig populated ───

    [Fact]
    public void CreateActors_CalcConfig_EnemyBoss()
    {
        var build = MinimalBuild();
        build.ConfigSets.Add(new ConfigSetData
        {
            Inputs = new List<ConfigInput>
            {
                new ConfigInput { Name = "enemyIsBoss", Kind = ConfigInputKind.String, StringValue = "Boss" },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal("Boss", player.Config.EnemyIsBoss);
    }

    [Fact]
    public void CreateActors_CalcConfig_MultipleFlags()
    {
        var build = MinimalBuild();
        build.ConfigSets.Add(new ConfigSetData
        {
            Inputs = new List<ConfigInput>
            {
                new ConfigInput { Name = "usePowerCharges", Kind = ConfigInputKind.Boolean, BooleanValue = true },
                new ConfigInput { Name = "buffFortification", Kind = ConfigInputKind.Boolean, BooleanValue = true },
                new ConfigInput { Name = "multiplierRageStack", Kind = ConfigInputKind.Number, NumberValue = 20 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.True(player.Config.UsePowerCharges);
        Assert.True(player.Config.BuffFortification);
        Assert.Equal(20, player.Config.MultiplierRageStack);
    }

    // ─── Full pipeline smoke tests ───

    [Fact]
    public void Calculate_MinimalBuild_HasPositiveLife()
    {
        var build = MinimalBuild("Marauder", level: 90);
        var (player, _) = BuildPipeline.Calculate(build);

        Assert.True(player.Output.ContainsKey("Life"));
        Assert.True(player.Output["Life"] > 0, $"Life should be > 0, got {player.Output["Life"]}");
    }

    [Fact]
    public void Calculate_MinimalBuild_HasPositiveMana()
    {
        var build = MinimalBuild("Witch", level: 50);
        var (player, _) = BuildPipeline.Calculate(build);

        Assert.True(player.Output.ContainsKey("Mana"));
        Assert.True(player.Output["Mana"] > 0, $"Mana should be > 0, got {player.Output["Mana"]}");
    }

    [Fact]
    public void Calculate_MinimalBuild_HasAttributes()
    {
        var build = MinimalBuild("Witch", level: 1);
        var (player, _) = BuildPipeline.Calculate(build);

        Assert.True(player.Output.ContainsKey("Str"));
        Assert.Equal(14, player.Output["Str"]);
        Assert.Equal(32, player.Output["Int"]);
    }

    [Fact]
    public void Calculate_Level90Marauder_LifeScales()
    {
        var build1 = MinimalBuild("Marauder", level: 1);
        var (player1, _) = BuildPipeline.Calculate(build1);

        var build90 = MinimalBuild("Marauder", level: 90);
        var (player90, _) = BuildPipeline.Calculate(build90);

        Assert.True(player90.Output["Life"] > player1.Output["Life"],
            $"Level 90 life ({player90.Output["Life"]}) should be > level 1 life ({player1.Output["Life"]})");
    }

    // ─── Minimal BuildData works ───

    [Fact]
    public void CreateActors_EmptyConfigSets_Works()
    {
        var build = new BuildData();
        var (player, enemy) = BuildPipeline.CreateActors(build);

        Assert.NotNull(player);
        Assert.NotNull(enemy);
    }

    [Fact]
    public void Calculate_EmptyBuild_DoesNotThrow()
    {
        var build = new BuildData();
        var (player, _) = BuildPipeline.Calculate(build);

        // Empty class → 0 base stats, but life per level still gives some life
        Assert.True(player.Output.ContainsKey("Life"));
    }

    // ─── OccVortex integration ───

    [Fact]
    public void CreateActors_OccVortex_LoadsAndCreatesActors()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return; // Skip if test data not available

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, enemy) = BuildPipeline.CreateActors(build);

        // Occultist → Witch: base Int = 32, items add more
        Assert.True(player.ModDB.Sum(ModType.Base, null, "Int") >= 32,
            "Int should include at least Witch base 32");
        Assert.Equal(99, player.ModDB.Multipliers["Level"]);
    }

    [Fact]
    public void Calculate_OccVortex_HasPositiveOutputs()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return; // Skip if test data not available

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.Calculate(build);

        Assert.True(player.Output["Life"] > 0);
        Assert.True(player.Output["Str"] > 0);
        Assert.True(player.Output["Int"] > 0);
    }

    private static string TestDataPath(string filename) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", filename);
}
