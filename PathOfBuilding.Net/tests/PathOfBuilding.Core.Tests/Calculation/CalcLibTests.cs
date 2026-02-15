using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcLibTests
{
    [Fact]
    public void Mod_OnlyBase_Returns1()
    {
        var db = new ModDB();
        db.NewMod("Life", ModType.Base, 100, "Test");

        Assert.Equal(1.0, CalcLib.Mod(db, null, "Life"));
    }

    [Fact]
    public void Mod_WithInc50_Returns1_5()
    {
        var db = new ModDB();
        db.NewMod("Life", ModType.Inc, 50, "Test");

        Assert.Equal(1.5, CalcLib.Mod(db, null, "Life"));
    }

    [Fact]
    public void Mod_WithMore30_Returns1_3()
    {
        var db = new ModDB();
        db.NewMod("Life", ModType.More, 30, "Test");

        Assert.Equal(1.3, CalcLib.Mod(db, null, "Life"), 6);
    }

    [Fact]
    public void Mod_IncAndMore_Returns1_95()
    {
        var db = new ModDB();
        db.NewMod("Life", ModType.Inc, 50, "Test");
        db.NewMod("Life", ModType.More, 30, "Test");

        Assert.Equal(1.95, CalcLib.Mod(db, null, "Life"), 6);
    }

    [Fact]
    public void Val_Base100_Inc50_Returns150()
    {
        var db = new ModDB();
        db.NewMod("Life", ModType.Base, 100, "Test");
        db.NewMod("Life", ModType.Inc, 50, "Test");

        Assert.Equal(150, CalcLib.Val(db, "Life"));
    }

    [Fact]
    public void Val_BaseZero_ReturnsZero()
    {
        var db = new ModDB();
        db.NewMod("Life", ModType.Inc, 50, "Test");

        Assert.Equal(0, CalcLib.Val(db, "Life"));
    }

    [Fact]
    public void Round_HalfUp_0_5_Returns1()
    {
        Assert.Equal(1, CalcLib.Round(0.5));
    }

    [Fact]
    public void Round_HalfUp_1_5_Returns2()
    {
        Assert.Equal(2, CalcLib.Round(1.5));
    }

    [Fact]
    public void Round_Below_Half_RoundsDown()
    {
        Assert.Equal(1, CalcLib.Round(1.4));
    }

    [Fact]
    public void Round_WithDecimals()
    {
        Assert.Equal(1.5, CalcLib.Round(1.45, 1));
        Assert.Equal(1.4, CalcLib.Round(1.44, 1));
    }
}
