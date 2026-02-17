using PathOfBuilding.Core.Tree;

namespace PathOfBuilding.Core.Tests.Tree;

public class TreeDataLoaderTests
{
    [Fact]
    public void LoadFromString_SingleNode()
    {
        var lua = """
            return {
                ["nodes"]= {
                    [7388]= {
                        ["skill"]= 7388,
                        ["name"]= "Intelligence",
                        ["stats"]= {
                            "+10 to Intelligence"
                        },
                        ["group"]= 272,
                        ["orbit"]= 2,
                        ["orbitIndex"]= 10,
                        ["out"]= {},
                        ["in"]= {}
                    }
                }
            }
        """;

        var tree = TreeDataLoader.LoadFromString(lua, "3_13");

        Assert.Equal("3_13", tree.Version);
        Assert.Single(tree.Nodes);
        Assert.True(tree.Nodes.ContainsKey(7388));

        var node = tree.Nodes[7388];
        Assert.Equal(7388, node.Id);
        Assert.Equal("Intelligence", node.Name);
        Assert.Single(node.Stats);
        Assert.Equal("+10 to Intelligence", node.Stats[0]);
        Assert.False(node.IsNotable);
        Assert.False(node.IsKeystone);
        Assert.False(node.IsMastery);
        Assert.Equal(272, node.Group);
    }

    [Fact]
    public void LoadFromString_NotableNode()
    {
        var lua = """
            return {
                ["nodes"]= {
                    [49254]= {
                        ["skill"]= 49254,
                        ["name"]= "Retribution",
                        ["isNotable"]= true,
                        ["stats"]= {
                            "14% increased Damage",
                            "Minions deal 10% increased Damage",
                            "5% increased Attack and Cast Speed",
                            "+10 to Strength and Intelligence"
                        },
                        ["group"]= 168,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    }
                }
            }
        """;

        var tree = TreeDataLoader.LoadFromString(lua);
        var node = tree.Nodes[49254];

        Assert.True(node.IsNotable);
        Assert.Equal("Retribution", node.Name);
        Assert.Equal(4, node.Stats.Count);
        Assert.Equal("14% increased Damage", node.Stats[0]);
        Assert.Equal("+10 to Strength and Intelligence", node.Stats[3]);
    }

    [Fact]
    public void LoadFromString_KeystoneNode()
    {
        var lua = """
            return {
                ["nodes"]= {
                    [10808]= {
                        ["skill"]= 10808,
                        ["name"]= "Iron Reflexes",
                        ["isKeystone"]= true,
                        ["stats"]= {
                            "Converts all Evasion Rating to Armour. Dexterity provides no bonus to Evasion Rating"
                        },
                        ["group"]= 500,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    }
                }
            }
        """;

        var tree = TreeDataLoader.LoadFromString(lua);
        var node = tree.Nodes[10808];

        Assert.True(node.IsKeystone);
    }

    [Fact]
    public void LoadFromString_JewelSocket()
    {
        var lua = """
            return {
                ["nodes"]= {
                    [26725]= {
                        ["skill"]= 26725,
                        ["name"]= "Jewel Socket",
                        ["isJewelSocket"]= true,
                        ["stats"]= {},
                        ["group"]= 300,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    }
                }
            }
        """;

        var tree = TreeDataLoader.LoadFromString(lua);
        var node = tree.Nodes[26725];

        Assert.True(node.IsJewelSocket);
        Assert.Empty(node.Stats);
    }

    [Fact]
    public void LoadFromString_MasteryNode_WithEffects()
    {
        var lua = """
            return {
                ["nodes"]= {
                    [63824]= {
                        ["skill"]= 63824,
                        ["name"]= "Elemental Damage Mastery",
                        ["isMastery"]= true,
                        ["stats"]= {},
                        ["masteryEffects"]= {
                            {
                                ["effect"]= 48385,
                                ["stats"]= {
                                    "Exposure you inflict applies at least -18% to the affected Resistance"
                                }
                            },
                            {
                                ["effect"]= 4119,
                                ["stats"]= {
                                    "60% reduced Reflected Elemental Damage taken"
                                }
                            }
                        },
                        ["group"]= 47,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    }
                }
            }
        """;

        var tree = TreeDataLoader.LoadFromString(lua);
        var node = tree.Nodes[63824];

        Assert.True(node.IsMastery);
        Assert.Empty(node.Stats);
        Assert.NotNull(node.MasteryEffects);
        Assert.Equal(2, node.MasteryEffects.Count);
        Assert.True(node.MasteryEffects.ContainsKey(48385));
        Assert.Single(node.MasteryEffects[48385]);
        Assert.Equal("Exposure you inflict applies at least -18% to the affected Resistance",
            node.MasteryEffects[48385][0]);
        Assert.True(node.MasteryEffects.ContainsKey(4119));
    }

    [Fact]
    public void LoadFromString_AscendancyNode()
    {
        var lua = """
            return {
                ["nodes"]= {
                    [5865]= {
                        ["skill"]= 5865,
                        ["name"]= "Physical Damage, Life Leeched per Second",
                        ["ascendancyName"]= "Berserker",
                        ["stats"]= {
                            "30% increased total Recovery per second from Life Leech",
                            "10% increased Physical Damage"
                        },
                        ["group"]= 1,
                        ["orbit"]= 2,
                        ["orbitIndex"]= 5,
                        ["out"]= {},
                        ["in"]= {}
                    }
                }
            }
        """;

        var tree = TreeDataLoader.LoadFromString(lua);
        var node = tree.Nodes[5865];

        Assert.Equal("Berserker", node.AscendancyName);
        Assert.Equal(2, node.Stats.Count);
    }

    [Fact]
    public void LoadFromString_MultipleNodes()
    {
        var lua = """
            return {
                ["nodes"]= {
                    [100]= {
                        ["skill"]= 100,
                        ["name"]= "Str",
                        ["stats"]= {"+10 to Strength"},
                        ["group"]= 1,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    },
                    [200]= {
                        ["skill"]= 200,
                        ["name"]= "Dex",
                        ["stats"]= {"+10 to Dexterity"},
                        ["group"]= 2,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    },
                    [300]= {
                        ["skill"]= 300,
                        ["name"]= "Int",
                        ["stats"]= {"+10 to Intelligence"},
                        ["group"]= 3,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    }
                }
            }
        """;

        var tree = TreeDataLoader.LoadFromString(lua);
        Assert.Equal(3, tree.Nodes.Count);
        Assert.True(tree.Nodes.ContainsKey(100));
        Assert.True(tree.Nodes.ContainsKey(200));
        Assert.True(tree.Nodes.ContainsKey(300));
    }

    [Fact]
    public void LoadFromString_EmptyNodes()
    {
        var lua = """return {["nodes"]= {}}""";
        var tree = TreeDataLoader.LoadFromString(lua);
        Assert.Empty(tree.Nodes);
    }

    [Fact]
    public void LoadFromString_NoNodesKey()
    {
        var lua = """return {["classes"]= {}}""";
        var tree = TreeDataLoader.LoadFromString(lua);
        Assert.Empty(tree.Nodes);
    }

    [Fact]
    public void LoadFromString_WithClassesAndGroups()
    {
        // Full tree-like structure with multiple top-level keys
        var lua = """
            return {
                ["classes"]= {
                    {
                        ["name"]= "Scion",
                        ["base_str"]= 20,
                        ["base_dex"]= 20,
                        ["base_int"]= 20
                    }
                },
                ["groups"]= {},
                ["nodes"]= {
                    [100]= {
                        ["skill"]= 100,
                        ["name"]= "Test",
                        ["stats"]= {"5% increased Life"},
                        ["group"]= 1,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    }
                }
            }
        """;

        var tree = TreeDataLoader.LoadFromString(lua);
        Assert.Single(tree.Nodes);
        Assert.Equal("Test", tree.Nodes[100].Name);
    }
}
