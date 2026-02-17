using PathOfBuilding.Core.Tree;

namespace PathOfBuilding.Core.Tests.Tree;

public class LuaTableParserTests
{
    [Fact]
    public void Parse_EmptyTable_ReturnsEmptyList()
    {
        var result = LuaTableParser.Parse("return {}");
        Assert.IsType<List<object?>>(result);
        Assert.Empty((List<object?>)result!);
    }

    [Fact]
    public void Parse_StringValue()
    {
        var result = LuaTableParser.Parse("return {\"hello\"}");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Single(list);
        Assert.Equal("hello", list[0]);
    }

    [Fact]
    public void Parse_NumberValue()
    {
        var result = LuaTableParser.Parse("return {42}");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Single(list);
        Assert.Equal(42.0, list[0]);
    }

    [Fact]
    public void Parse_NegativeNumber()
    {
        var result = LuaTableParser.Parse("return {-5.5}");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(-5.5, list[0]);
    }

    [Fact]
    public void Parse_BooleanValues()
    {
        var result = LuaTableParser.Parse("return {true, false}");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal(true, list[0]);
        Assert.Equal(false, list[1]);
    }

    [Fact]
    public void Parse_NilValue()
    {
        var result = LuaTableParser.Parse("return nil");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_StringKeyDict()
    {
        var result = LuaTableParser.Parse("""return {["name"]= "test", ["value"]= 42}""");
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("test", dict["name"]);
        Assert.Equal(42.0, dict["value"]);
    }

    [Fact]
    public void Parse_NumericKeyDict()
    {
        var result = LuaTableParser.Parse("return {[1]= \"a\", [2]= \"b\"}");
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("a", dict["1"]);
        Assert.Equal("b", dict["2"]);
    }

    [Fact]
    public void Parse_NestedTable()
    {
        var result = LuaTableParser.Parse("""
            return {
                ["inner"]= {
                    ["x"]= 10,
                    ["y"]= 20
                }
            }
        """);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        var inner = Assert.IsType<Dictionary<string, object?>>(dict["inner"]);
        Assert.Equal(10.0, inner["x"]);
        Assert.Equal(20.0, inner["y"]);
    }

    [Fact]
    public void Parse_ArrayOfStrings()
    {
        var result = LuaTableParser.Parse("""
            return {"one", "two", "three"}
        """);
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal("one", list[0]);
        Assert.Equal("two", list[1]);
        Assert.Equal("three", list[2]);
    }

    [Fact]
    public void Parse_StringEscapes()
    {
        var result = LuaTableParser.Parse("""return {"hello\nworld", "tab\there"}""");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal("hello\nworld", list[0]);
        Assert.Equal("tab\there", list[1]);
    }

    [Fact]
    public void Parse_LineComment()
    {
        var result = LuaTableParser.Parse("""
            return {
                -- this is a comment
                ["x"]= 5
            }
        """);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(5.0, dict["x"]);
    }

    [Fact]
    public void Parse_BlockComment()
    {
        var result = LuaTableParser.Parse("""
            return {
                --[[ block comment ]]
                ["x"]= 5
            }
        """);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(5.0, dict["x"]);
    }

    [Fact]
    public void Parse_TrailingComma()
    {
        var result = LuaTableParser.Parse("return {1, 2, 3,}");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Parse_MixedTable()
    {
        // Mixed tables: array entries get numeric string keys merged into dict
        var result = LuaTableParser.Parse("""return {[5]= "five", "array_entry"}""");
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("five", dict["5"]);
        Assert.Equal("array_entry", dict["1"]);
    }

    [Fact]
    public void Parse_TreeNodeLike()
    {
        var lua = """
            return {
                [7388]= {
                    ["skill"]= 7388,
                    ["name"]= "Intelligence",
                    ["stats"]= {
                        "+10 to Intelligence"
                    },
                    ["group"]= 272,
                    ["orbit"]= 2,
                    ["isNotable"]= false,
                    ["out"]= {
                        "12345"
                    },
                    ["in"]= {}
                }
            }
        """;
        var root = Assert.IsType<Dictionary<string, object?>>(LuaTableParser.Parse(lua));
        var node = Assert.IsType<Dictionary<string, object?>>(root["7388"]);
        Assert.Equal(7388.0, node["skill"]);
        Assert.Equal("Intelligence", node["name"]);
        var stats = Assert.IsType<List<object?>>(node["stats"]);
        Assert.Single(stats);
        Assert.Equal("+10 to Intelligence", stats[0]);
    }

    [Fact]
    public void Parse_BareReturn()
    {
        var result = LuaTableParser.Parse("return 42");
        Assert.Equal(42.0, result);
    }

    [Fact]
    public void Parse_FloatNumber()
    {
        var result = LuaTableParser.Parse("return {3.14}");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(3.14, list[0]);
    }

    [Fact]
    public void Parse_ScientificNotation()
    {
        var result = LuaTableParser.Parse("return {1.5e10}");
        var list = Assert.IsType<List<object?>>(result);
        Assert.Equal(1.5e10, list[0]);
    }

    [Fact]
    public void Parse_TreeNodesDict()
    {
        var lua = """
            return {
                ["nodes"]= {
                    [100]= {
                        ["skill"]= 100,
                        ["name"]= "Strength",
                        ["stats"]= {"+10 to Strength"},
                        ["group"]= 1,
                        ["orbit"]= 0,
                        ["orbitIndex"]= 0,
                        ["out"]= {},
                        ["in"]= {}
                    },
                    [200]= {
                        ["skill"]= 200,
                        ["name"]= "Life Node",
                        ["stats"]= {"5% increased maximum Life"},
                        ["isNotable"]= true,
                        ["group"]= 2,
                        ["orbit"]= 1,
                        ["orbitIndex"]= 3,
                        ["out"]= {"100"},
                        ["in"]= {"300"}
                    }
                }
            }
        """;
        var root = Assert.IsType<Dictionary<string, object?>>(LuaTableParser.Parse(lua));
        var nodes = Assert.IsType<Dictionary<string, object?>>(root["nodes"]);
        Assert.Equal(2, nodes.Count);
        Assert.True(nodes.ContainsKey("100"));
        Assert.True(nodes.ContainsKey("200"));

        var notable = Assert.IsType<Dictionary<string, object?>>(nodes["200"]);
        Assert.Equal(true, notable["isNotable"]);
    }
}
