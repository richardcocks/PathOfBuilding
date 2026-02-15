using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcDamageTests
{
    // ─── CalcRadius ───

    [Fact]
    public void CalcRadius_Basic_ReturnsBaseRadius()
    {
        // areaMod = 1.0: floor(10 * floor(100 * sqrt(1)) / 100) = 10
        Assert.Equal(10, CalcDamage.CalcRadius(10, 1.0));
    }

    [Fact]
    public void CalcRadius_WithAreaMod_ScalesCorrectly()
    {
        // areaMod = 4.0: sqrt(4) = 2, floor(100 * 2) = 200, 10 * 200 / 100 = 20
        Assert.Equal(20, CalcDamage.CalcRadius(10, 4.0));
    }

    [Fact]
    public void CalcRadius_FractionalAreaMod_Floors()
    {
        // areaMod = 1.5: sqrt(1.5) ≈ 1.2247, floor(100 * 1.2247) = 122, 10 * 122 / 100 = 12.2 → floor = 12
        Assert.Equal(12, CalcDamage.CalcRadius(10, 1.5));
    }

    [Fact]
    public void CalcRadius_ZeroRadius_ReturnsZero()
    {
        Assert.Equal(0, CalcDamage.CalcRadius(0, 2.0));
    }

    // ─── ConversionTable.Build ───

    [Fact]
    public void ConversionTable_NoMods_AllMultsAreOne()
    {
        var modList = new ModList();
        var table = ConversionTable.Build(modList);

        Assert.Equal(1, table["Physical"].Mult);
        Assert.Equal(1, table["Lightning"].Mult);
        Assert.Equal(1, table["Cold"].Mult);
        Assert.Equal(1, table["Fire"].Mult);
        Assert.Equal(1, table["Chaos"].Mult);
    }

    [Fact]
    public void ConversionTable_FullConversion_MultIsZero()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageConvertToLightning", ModType.Base, 100, "Test"));
        var table = ConversionTable.Build(modList);

        Assert.Equal(0, table["Physical"].Mult);
        Assert.Equal(1, table["Physical"].Conversion["Lightning"]);
    }

    [Fact]
    public void ConversionTable_PartialConversion()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageConvertToFire", ModType.Base, 50, "Test"));
        var table = ConversionTable.Build(modList);

        Assert.Equal(0.5, table["Physical"].Mult);
        Assert.Equal(0.5, table["Physical"].Conversion["Fire"]);
    }

    [Fact]
    public void ConversionTable_MultiStep_ChainedConversion()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageConvertToLightning", ModType.Base, 50, "Test"));
        modList.AddMod(ModHelper.CreateMod("LightningDamageConvertToCold", ModType.Base, 50, "Test"));
        var table = ConversionTable.Build(modList);

        Assert.Equal(0.5, table["Physical"].Mult);
        Assert.Equal(0.5, table["Lightning"].Mult);
        Assert.Equal(0.5, table["Physical"].Conversion["Lightning"]);
        Assert.Equal(0.5, table["Lightning"].Conversion["Cold"]);
    }

    [Fact]
    public void ConversionTable_GainAs_DoesNotReduceMult()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageGainAsFire", ModType.Base, 30, "Test"));
        var table = ConversionTable.Build(modList);

        // Gain doesn't reduce the original type
        Assert.Equal(1, table["Physical"].Mult);
        Assert.Equal(0.3, table["Physical"].Gain["Fire"]);
        Assert.Equal(0.3, table["Physical"].GetCombined("Fire"));
    }

    [Fact]
    public void ConversionTable_Over100Percent_ScaledDown()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageConvertToLightning", ModType.Base, 60, "Test"));
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageConvertToFire", ModType.Base, 60, "Test"));
        var table = ConversionTable.Build(modList);

        // Total = 120%, scaled down: 60/120 * 100 = 50% each
        Assert.Equal(0, table["Physical"].Mult);
        Assert.Equal(0.5, table["Physical"].Conversion["Lightning"]);
        Assert.Equal(0.5, table["Physical"].Conversion["Fire"]);
    }

    // ─── CalcDamageMinMax ───

    [Fact]
    public void CalcDamageMinMax_NoConversion_JustBase()
    {
        var modList = new ModList();
        var convTable = ConversionTable.Build(modList);
        var baseDamage = new Dictionary<string, double>
        {
            ["PhysicalMinBase"] = 100,
            ["PhysicalMaxBase"] = 200,
        };

        var (min, max) = CalcDamage.CalcDamageMinMax(convTable, modList, null, baseDamage, "Physical", 0);

        Assert.Equal(100, min);
        Assert.Equal(200, max);
    }

    [Fact]
    public void CalcDamageMinMax_WithIncMore()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("Damage", ModType.Inc, 100, "Test")); // +100% inc
        modList.AddMod(ModHelper.CreateMod("Damage", ModType.More, 50, "Test")); // 50% more
        var convTable = ConversionTable.Build(modList);
        var baseDamage = new Dictionary<string, double>
        {
            ["PhysicalMinBase"] = 100,
            ["PhysicalMaxBase"] = 200,
        };

        var (min, max) = CalcDamage.CalcDamageMinMax(convTable, modList, null, baseDamage, "Physical", 0);

        // 100 * (1 + 100/100) * (1 + 50/100) = 100 * 2 * 1.5 = 300
        Assert.Equal(300, min);
        Assert.Equal(600, max);
    }

    [Fact]
    public void CalcDamageMinMax_SingleConversion()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageConvertToFire", ModType.Base, 50, "Test"));
        var convTable = ConversionTable.Build(modList);
        var baseDamage = new Dictionary<string, double>
        {
            ["PhysicalMinBase"] = 100,
            ["PhysicalMaxBase"] = 200,
        };

        // Fire damage includes 50% conversion from physical
        var (min, max) = CalcDamage.CalcDamageMinMax(convTable, modList, null, baseDamage, "Fire", 0);

        // Physical → Fire: 100 * 0.5 = 50, 200 * 0.5 = 100
        Assert.Equal(50, min);
        Assert.Equal(100, max);
    }

    [Fact]
    public void CalcDamageMinMax_ZeroBase_ReturnsZero()
    {
        var modList = new ModList();
        var convTable = ConversionTable.Build(modList);
        var baseDamage = new Dictionary<string, double>();

        var (min, max) = CalcDamage.CalcDamageMinMax(convTable, modList, null, baseDamage, "Physical", 0);

        Assert.Equal(0, min);
        Assert.Equal(0, max);
    }

    [Fact]
    public void CalcDamageMinMax_MultiStepChain()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageConvertToLightning", ModType.Base, 100, "Test"));
        modList.AddMod(ModHelper.CreateMod("LightningDamageConvertToFire", ModType.Base, 100, "Test"));
        var convTable = ConversionTable.Build(modList);
        var baseDamage = new Dictionary<string, double>
        {
            ["PhysicalMinBase"] = 100,
            ["PhysicalMaxBase"] = 100,
        };

        // Phys → Lightning → Fire: full chain
        var (min, max) = CalcDamage.CalcDamageMinMax(convTable, modList, null, baseDamage, "Fire", 0);
        Assert.True(min > 0);
        Assert.True(max > 0);
    }

    // ─── CalcAilmentSourceDamage ───

    [Fact]
    public void CalcAilmentSourceDamage_WithPartialConversion()
    {
        var modList = new ModList();
        modList.AddMod(ModHelper.CreateMod("PhysicalDamageConvertToFire", ModType.Base, 50, "Test"));
        var convTable = ConversionTable.Build(modList);
        var baseDamage = new Dictionary<string, double>
        {
            ["PhysicalMinBase"] = 100,
            ["PhysicalMaxBase"] = 200,
        };

        // Physical ailment source: total damage * remaining fraction
        var (min, max) = CalcDamage.CalcAilmentSourceDamage(convTable, modList, null, baseDamage, "Physical", 0);

        // Base: 100/200, mult = 0.5 (50% converted out)
        Assert.Equal(50, min);
        Assert.Equal(100, max);
    }
}
