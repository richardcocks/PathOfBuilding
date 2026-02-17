using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class LuaSkillParserTests
{
    // --- ParseReturnTable: bare identifier keys ---

    [Fact]
    public void ParseReturnTable_BareKeys()
    {
        var lua = """
            return {
                name = "Fireball",
                color = 3,
                support = false,
            }
        """;

        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Fireball", dict["name"]);
        Assert.Equal(3.0, dict["color"]);
        Assert.Equal(false, dict["support"]);
    }

    [Fact]
    public void ParseReturnTable_BooleanValues()
    {
        var lua = "return { active = true, passive = false }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(true, dict["active"]);
        Assert.Equal(false, dict["passive"]);
    }

    [Fact]
    public void ParseReturnTable_NilValue()
    {
        var lua = "return { x = nil }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Null(dict["x"]);
    }

    // --- Single-quoted strings ---

    [Fact]
    public void ParseReturnTable_SingleQuotedString()
    {
        var lua = "return { name = 'Arc' }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Arc", dict["name"]);
    }

    [Fact]
    public void ParseReturnTable_SingleQuotedWithEscape()
    {
        var lua = @"return { name = 'it\'s ok' }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("it's ok", dict["name"]);
    }

    // --- Dotted identifier values ---

    [Fact]
    public void ParseReturnTable_DottedIdentifier()
    {
        var lua = "return { skillType = SkillType.Spell }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        var ident = Assert.IsType<DottedIdentifier>(dict["skillType"]);
        Assert.Equal("SkillType", ident.Namespace);
        Assert.Equal("Spell", ident.Name);
    }

    [Fact]
    public void ParseReturnTable_IdentifierAsTableKey()
    {
        var lua = "return { [SkillType.Spell] = true }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        // The key is the string repr of the DottedIdentifier
        Assert.True(dict.Count > 0);
    }

    // --- Function calls ---

    [Fact]
    public void ParseReturnTable_FunctionCall_Mod()
    {
        var lua = """
            return {
                mod("Damage", "MORE", 20),
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var list = Assert.IsType<List<object?>>(result);
        var fc = Assert.IsType<FunctionCall>(list[0]);
        Assert.Equal("mod", fc.Name);
        Assert.Equal(3, fc.Args.Count);
        Assert.Equal("Damage", fc.Args[0]);
        Assert.Equal("MORE", fc.Args[1]);
        Assert.Equal(20.0, fc.Args[2]);
    }

    [Fact]
    public void ParseReturnTable_FunctionCall_Skill()
    {
        var lua = """
            return {
                skill("ColdMin", nil),
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var list = Assert.IsType<List<object?>>(result);
        var fc = Assert.IsType<FunctionCall>(list[0]);
        Assert.Equal("skill", fc.Name);
        Assert.Equal(2, fc.Args.Count);
        Assert.Equal("ColdMin", fc.Args[0]);
        Assert.Null(fc.Args[1]);
    }

    [Fact]
    public void ParseReturnTable_FunctionCall_Flag()
    {
        var lua = "return { flag(\"dotIsCorruptingBlood\") }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var list = Assert.IsType<List<object?>>(result);
        var fc = Assert.IsType<FunctionCall>(list[0]);
        Assert.Equal("flag", fc.Name);
        Assert.Single(fc.Args);
    }

    [Fact]
    public void ParseReturnTable_FunctionCall_BitBor()
    {
        var lua = "return { bit.bor(KeywordFlag.Hit, KeywordFlag.Ailment) }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var list = Assert.IsType<List<object?>>(result);
        var fc = Assert.IsType<FunctionCall>(list[0]);
        Assert.Equal("bit.bor", fc.Name);
        Assert.Equal(2, fc.Args.Count);
    }

    [Fact]
    public void ParseReturnTable_FunctionCall_WithTagTable()
    {
        var lua = """
            return {
                mod("Damage", "MORE", nil, 0, 0, { type = "Condition", var = "LowLife" }),
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var list = Assert.IsType<List<object?>>(result);
        var fc = Assert.IsType<FunctionCall>(list[0]);
        Assert.Equal(6, fc.Args.Count);
        var tag = Assert.IsType<Dictionary<string, object?>>(fc.Args[5]);
        Assert.Equal("Condition", tag["type"]);
        Assert.Equal("LowLife", tag["var"]);
    }

    [Fact]
    public void ParseReturnTable_NestedFunctionCall()
    {
        var lua = """
            return {
                mod("MinionModifier", "LIST", { mod = mod("Damage", "MORE", nil) }),
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var list = Assert.IsType<List<object?>>(result);
        var fc = Assert.IsType<FunctionCall>(list[0]);
        Assert.Equal("mod", fc.Name);

        // Third arg is a table with a nested mod() call
        var innerTable = Assert.IsType<Dictionary<string, object?>>(fc.Args[2]);
        var innerMod = Assert.IsType<FunctionCall>(innerTable["mod"]);
        Assert.Equal("mod", innerMod.Name);
    }

    // --- Function literal skipping ---

    [Fact]
    public void ParseReturnTable_FunctionLiteralSkipped()
    {
        var lua = """
            return {
                name = "Test",
                initialFunc = function(activeSkill, output)
                    local x = 1
                    if x > 0 then
                        x = 2
                    end
                end,
                color = 3,
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Test", dict["name"]);
        Assert.Null(dict["initialFunc"]); // function literal → null
        Assert.Equal(3.0, dict["color"]);
    }

    [Fact]
    public void ParseReturnTable_NestedFunctionLiterals()
    {
        var lua = """
            return {
                fn = function()
                    for i = 1, 10 do
                        while true do
                            break
                        end
                    end
                end,
                value = 42,
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Null(dict["fn"]);
        Assert.Equal(42.0, dict["value"]);
    }

    // --- Assignment parsing ---

    [Fact]
    public void ParseSkillFile_SimpleAssignment()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["TestSkill"] = {
                name = "Test",
                color = 3,
            }
        """;
        var result = LuaSkillParser.ParseSkillFile(lua);
        Assert.Single(result);
        Assert.True(result.ContainsKey("TestSkill"));

        var dict = Assert.IsType<Dictionary<string, object?>>(result["TestSkill"]);
        Assert.Equal("Test", dict["name"]);
    }

    [Fact]
    public void ParseSkillFile_MultipleAssignments()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["Skill1"] = {
                name = "First",
            }
            skills["Skill2"] = {
                name = "Second",
            }
        """;
        var result = LuaSkillParser.ParseSkillFile(lua);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("Skill1"));
        Assert.True(result.ContainsKey("Skill2"));
    }

    // --- Mixed tables ---

    [Fact]
    public void ParseReturnTable_MixedTable()
    {
        var lua = "return { 0.8, 1.2, critChance = 6, levelRequirement = 70 }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(0.8, dict["1"]);
        Assert.Equal(1.2, dict["2"]);
        Assert.Equal(6.0, dict["critChance"]);
        Assert.Equal(70.0, dict["levelRequirement"]);
    }

    [Fact]
    public void ParseReturnTable_NestedTable()
    {
        var lua = """
            return {
                cost = { Mana = 8 },
                statInterpolation = { 3, 3, 1 },
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);

        var cost = Assert.IsType<Dictionary<string, object?>>(dict["cost"]);
        Assert.Equal(8.0, cost["Mana"]);

        var si = Assert.IsType<List<object?>>(dict["statInterpolation"]);
        Assert.Equal(3, si.Count);
    }

    // --- Numeric bracket keys ---

    [Fact]
    public void ParseReturnTable_NumericBracketKeys()
    {
        var lua = "return { [1] = { 25, levelRequirement = 18 } }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.True(dict.ContainsKey("1"));
    }

    // --- Comments ---

    [Fact]
    public void ParseReturnTable_WithComments()
    {
        var lua = """
            -- This is a comment
            return {
                -- inline comment
                name = "Test", -- trailing comment
                value = 42,
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Test", dict["name"]);
        Assert.Equal(42.0, dict["value"]);
    }

    [Fact]
    public void ParseReturnTable_BlockComment()
    {
        var lua = """
            --[[ block comment ]]
            return { name = "Test" }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Test", dict["name"]);
    }

    // --- Preamble handling ---

    [Fact]
    public void ParseReturnTable_WithPreamble()
    {
        var lua = """
            local mod, flag, skill = ...
            return { name = "Test" }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("Test", dict["name"]);
    }

    [Fact]
    public void ParseSkillFile_SkipsPreamble()
    {
        var lua = """
            -- comment
            local skills, mod, flag, skill = ...
            skills["X"] = { name = "Test" }
        """;
        var result = LuaSkillParser.ParseSkillFile(lua);
        Assert.Single(result);
    }

    // --- Empty table ---

    [Fact]
    public void ParseReturnTable_EmptyTable()
    {
        var lua = "return { }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var list = Assert.IsType<List<object?>>(result);
        Assert.Empty(list);
    }

    // --- Scientific notation ---

    [Fact]
    public void ParseReturnTable_ScientificNotation()
    {
        var lua = "return { value = 1.5e10 }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(1.5e10, dict["value"]);
    }

    // --- Negative number ---

    [Fact]
    public void ParseReturnTable_NegativeNumber()
    {
        var lua = "return { value = -25 }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(-25.0, dict["value"]);
    }

    // --- Real file header patterns ---

    [Fact]
    public void ParseSkillFile_RealFileHeader_ActInt()
    {
        var lua = """
            -- This file is automatically generated, do not edit!
            -- Path of Building
            --
            -- Active Intelligence skill gems
            -- Skill data (c) Grinding Gear Games
            --
            local skills, mod, flag, skill = ...

            skills["TestArc"] = {
                name = "Arc",
                color = 3,
                baseEffectiveness = 1.584900021553,
            }
        """;
        var result = LuaSkillParser.ParseSkillFile(lua);
        Assert.Single(result);
        var dict = Assert.IsType<Dictionary<string, object?>>(result["TestArc"]);
        Assert.Equal("Arc", dict["name"]);
    }

    [Fact]
    public void ParseReturnTable_RealFileHeader_SkillStatMap()
    {
        var lua = """
            -- Path of Building
            --
            -- Stat to internal modifier mapping table for skills
            -- Stat data (c) Grinding Gear Games
            --
            local mod, flag, skill = ...
            return {
                ["test_stat"] = {
                    skill("duration", nil),
                    div = 1000,
                },
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.True(dict.ContainsKey("test_stat"));
    }

    // --- Skill types dict with identifier keys ---

    [Fact]
    public void ParseReturnTable_SkillTypeDictKeys()
    {
        var lua = """
            return {
                skillTypes = { [SkillType.Spell] = true, [SkillType.Damage] = true },
            }
        """;
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        var skillTypes = Assert.IsType<Dictionary<string, object?>>(dict["skillTypes"]);
        Assert.Equal(2, skillTypes.Count);
    }

    // --- String escape sequences ---

    [Fact]
    public void ParseReturnTable_StringEscapes()
    {
        var lua = "return { text = \"line1\\nline2\" }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("line1\nline2", dict["text"]);
    }

    // --- ModFlag direct usage ---

    [Fact]
    public void ParseReturnTable_ModFlagIdentifier()
    {
        var lua = "return { mod(\"Damage\", \"MORE\", nil, ModFlag.Spell) }";
        var result = LuaSkillParser.ParseReturnTable(lua);
        var list = Assert.IsType<List<object?>>(result);
        var fc = Assert.IsType<FunctionCall>(list[0]);
        Assert.Equal(4, fc.Args.Count);
        var flag = Assert.IsType<DottedIdentifier>(fc.Args[3]);
        Assert.Equal("ModFlag", flag.Namespace);
        Assert.Equal("Spell", flag.Name);
    }
}
