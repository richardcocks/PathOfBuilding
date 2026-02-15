using System.Text.RegularExpressions;
using PathOfBuilding.Core.Modifiers.Parsing;
using Xunit;

namespace PathOfBuilding.Core.Tests.Modifiers.Parsing;

public class FormDetectionTests
{
    [Theory]
    [InlineData("10% increased maximum Life", ModForm.INC)]
    [InlineData("10% faster", ModForm.INC)]
    [InlineData("10% reduced maximum Life", ModForm.RED)]
    [InlineData("10% slower", ModForm.RED)]
    [InlineData("20% more Damage", ModForm.MORE)]
    [InlineData("15% less Attack Speed", ModForm.LESS)]
    public void ScanForm_PercentageForms(string line, ModForm expected)
    {
        var result = ModParser.ScanRegex(line.ToLowerInvariant(), FormPatterns.List);
        Assert.True(result.HasMatch);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("+50 to maximum Life", ModForm.BASE)]
    [InlineData("-20 to maximum Life", ModForm.BASE)]
    [InlineData("+5.5% to Fire Resistance", ModForm.BASE)]
    public void ScanForm_BaseForms(string line, ModForm expected)
    {
        var result = ModParser.ScanRegex(line.ToLowerInvariant(), FormPatterns.List);
        Assert.True(result.HasMatch);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("adds 10 to 20 physical damage", ModForm.DMG)]
    [InlineData("adds 5 to 10 fire damage to attacks", ModForm.DMGATTACKS)]
    [InlineData("adds 3 to 7 cold damage to spells", ModForm.DMGSPELLS)]
    [InlineData("adds 1 to 2 lightning damage to attacks and spells", ModForm.DMGBOTH)]
    public void ScanForm_DamageForms(string line, ModForm expected)
    {
        var result = ModParser.ScanRegex(line, FormPatterns.List);
        Assert.True(result.HasMatch);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("you have Iron Reflexes", ModForm.FLAG)]
    [InlineData("have Onslaught", ModForm.FLAG)]
    [InlineData("you are Elusive", ModForm.FLAG)]
    [InlineData("gain Phasing", ModForm.FLAG)]
    public void ScanForm_FlagForms(string line, ModForm expected)
    {
        var result = ModParser.ScanRegex(line.ToLowerInvariant(), FormPatterns.List);
        Assert.True(result.HasMatch);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("penetrates 10% fire resistance", ModForm.PEN)]
    [InlineData("penetrates 5% of enemy lightning resistance", ModForm.PEN)]
    public void ScanForm_PenForms(string line, ModForm expected)
    {
        var result = ModParser.ScanRegex(line, FormPatterns.List);
        Assert.True(result.HasMatch);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("regenerate 1.5% of life per second", ModForm.REGENPERCENT)]
    [InlineData("regenerate 10 life per second", ModForm.REGENFLAT)]
    public void ScanForm_RegenForms(string line, ModForm expected)
    {
        var result = ModParser.ScanRegex(line, FormPatterns.List);
        Assert.True(result.HasMatch);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void ScanForm_CaptureGroups_INC()
    {
        var result = ModParser.ScanRegex("25% increased fire damage", FormPatterns.List);
        Assert.True(result.HasMatch);
        Assert.Equal(ModForm.INC, result.Value);
        Assert.NotNull(result.Captures);
        Assert.Equal("25", result.Captures[0]);
    }

    [Fact]
    public void ScanForm_CaptureGroups_DmgRange()
    {
        var result = ModParser.ScanRegex("adds 5 to 10 fire damage", FormPatterns.List);
        Assert.True(result.HasMatch);
        Assert.Equal(ModForm.DMG, result.Value);
        Assert.NotNull(result.Captures);
        Assert.Equal("5", result.Captures[0]);
        Assert.Equal("10", result.Captures[1]);
        Assert.Equal("fire", result.Captures[2]);
    }
}
