using PathOfBuilding.Core.Data;

namespace PathOfBuilding.Core.Tests.Data;

public class ClassBaseStatsTests
{
    // ─── Base class attributes ───

    [Fact]
    public void Marauder_Returns_32_14_14()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("Marauder");
        Assert.Equal(32, str);
        Assert.Equal(14, dex);
        Assert.Equal(14, int_);
    }

    [Fact]
    public void Ranger_Returns_14_32_14()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("Ranger");
        Assert.Equal(14, str);
        Assert.Equal(32, dex);
        Assert.Equal(14, int_);
    }

    [Fact]
    public void Witch_Returns_14_14_32()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("Witch");
        Assert.Equal(14, str);
        Assert.Equal(14, dex);
        Assert.Equal(32, int_);
    }

    [Fact]
    public void Duelist_Returns_23_23_14()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("Duelist");
        Assert.Equal(23, str);
        Assert.Equal(23, dex);
        Assert.Equal(14, int_);
    }

    [Fact]
    public void Templar_Returns_23_14_23()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("Templar");
        Assert.Equal(23, str);
        Assert.Equal(14, dex);
        Assert.Equal(23, int_);
    }

    [Fact]
    public void Shadow_Returns_14_23_23()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("Shadow");
        Assert.Equal(14, str);
        Assert.Equal(23, dex);
        Assert.Equal(23, int_);
    }

    [Fact]
    public void Scion_Returns_20_20_20()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("Scion");
        Assert.Equal(20, str);
        Assert.Equal(20, dex);
        Assert.Equal(20, int_);
    }

    [Fact]
    public void Unknown_Returns_0_0_0()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("Unknown");
        Assert.Equal(0, str);
        Assert.Equal(0, dex);
        Assert.Equal(0, int_);
    }

    [Fact]
    public void Empty_Returns_0_0_0()
    {
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes("");
        Assert.Equal(0, str);
        Assert.Equal(0, dex);
        Assert.Equal(0, int_);
    }

    // ─── Ascendancy resolution ───

    [Theory]
    [InlineData("Juggernaut", "Marauder")]
    [InlineData("Berserker", "Marauder")]
    [InlineData("Chieftain", "Marauder")]
    [InlineData("Deadeye", "Ranger")]
    [InlineData("Raider", "Ranger")]
    [InlineData("Pathfinder", "Ranger")]
    [InlineData("Occultist", "Witch")]
    [InlineData("Necromancer", "Witch")]
    [InlineData("Elementalist", "Witch")]
    [InlineData("Slayer", "Duelist")]
    [InlineData("Gladiator", "Duelist")]
    [InlineData("Champion", "Duelist")]
    [InlineData("Inquisitor", "Templar")]
    [InlineData("Hierophant", "Templar")]
    [InlineData("Guardian", "Templar")]
    [InlineData("Assassin", "Shadow")]
    [InlineData("Trickster", "Shadow")]
    [InlineData("Saboteur", "Shadow")]
    [InlineData("Ascendant", "Scion")]
    public void Ascendancy_ResolvesToBaseClass(string ascendancy, string expected)
    {
        Assert.Equal(expected, ClassBaseStats.GetBaseClassName(ascendancy));
    }

    [Fact]
    public void BaseClass_ResolvesToSelf()
    {
        Assert.Equal("Witch", ClassBaseStats.GetBaseClassName("Witch"));
        Assert.Equal("Marauder", ClassBaseStats.GetBaseClassName("Marauder"));
    }

    [Fact]
    public void None_ReturnsEmpty()
    {
        Assert.Equal("", ClassBaseStats.GetBaseClassName("None"));
    }

    [Fact]
    public void NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal("", ClassBaseStats.GetBaseClassName(""));
        Assert.Equal("", ClassBaseStats.GetBaseClassName(null!));
    }
}
