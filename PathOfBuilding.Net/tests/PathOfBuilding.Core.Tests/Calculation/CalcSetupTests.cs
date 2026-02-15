using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcSetupTests
{
    private static ModDB CreateInitializedModDB(bool isEnemy = false)
    {
        var db = new ModDB();
        CalcSetup.InitModDB(db, isEnemy);
        return db;
    }

    [Fact]
    public void InitModDB_SetsFireResistMax_To75()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(75, db.Sum(ModType.Base, null, "FireResistMax"));
    }

    [Fact]
    public void InitModDB_SetsColdResistMax_To75()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(75, db.Sum(ModType.Base, null, "ColdResistMax"));
    }

    [Fact]
    public void InitModDB_SetsLightningResistMax_To75()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(75, db.Sum(ModType.Base, null, "LightningResistMax"));
    }

    [Fact]
    public void InitModDB_SetsChaosResistMax_To75()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(75, db.Sum(ModType.Base, null, "ChaosResistMax"));
    }

    [Fact]
    public void InitModDB_SetsPowerChargesMax_To3()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(3, db.Sum(ModType.Base, null, "PowerChargesMax"));
    }

    [Fact]
    public void InitModDB_SetsFrenzyChargesMax_To3()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(3, db.Sum(ModType.Base, null, "FrenzyChargesMax"));
    }

    [Fact]
    public void InitModDB_SetsEnduranceChargesMax_To3()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(3, db.Sum(ModType.Base, null, "EnduranceChargesMax"));
    }

    [Fact]
    public void InitModDB_SetsMaxLifeLeechRate()
    {
        var db = CreateInitializedModDB();
        // 1200 / 60 = 20
        Assert.Equal(20, db.Sum(ModType.Base, null, "MaxLifeLeechRate"));
    }

    [Fact]
    public void InitModDB_SetsBlockChanceMax_To75()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(75, db.Sum(ModType.Base, null, "BlockChanceMax"));
    }

    [Fact]
    public void InitModDB_SetsChargeDuration_To10()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(10, db.Sum(ModType.Base, null, "ChargeDuration"));
    }

    [Fact]
    public void InitModDB_SetsCritChanceCap_To100()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(100, db.Sum(ModType.Base, null, "CritChanceCap"));
    }

    [Fact]
    public void InitModDB_ConditionalMod_Maimed_MovementSpeed()
    {
        var db = CreateInitializedModDB();
        // Without Maimed condition, the conditional mod should not apply
        Assert.Equal(0, db.Sum(ModType.Inc, null, "MovementSpeed"));

        // With Maimed condition, it should apply -30
        db.Conditions["Maimed"] = true;
        Assert.Equal(-30, db.Sum(ModType.Inc, null, "MovementSpeed"));
    }

    [Fact]
    public void InitModDB_BuffFlag_Onslaught_GatedOnCondition()
    {
        var db = CreateInitializedModDB();
        // Without condition, flag should not be set
        Assert.False(db.Flag(null, "Onslaught"));

        // With condition, flag should be set
        db.Conditions["Onslaught"] = true;
        Assert.True(db.Flag(null, "Onslaught"));
    }

    [Fact]
    public void InitModDB_BuffFlag_Fortify_GatedOnCondition()
    {
        var db = CreateInitializedModDB();
        Assert.False(db.Flag(null, "Fortify"));

        db.Conditions["Fortify"] = true;
        Assert.True(db.Flag(null, "Fortify"));
    }

    [Fact]
    public void InitModDB_Enemy_UsesMonsterConstants()
    {
        var db = CreateInitializedModDB(isEnemy: true);
        // Monster max phys damage reduction is 75, not 90
        Assert.Equal(75, db.Sum(ModType.Base, null, "BlockChanceMax"));
    }

    [Fact]
    public void InitModDB_SetsInspirationChargesMax()
    {
        var db = CreateInitializedModDB();
        Assert.Equal(5, db.Sum(ModType.Base, null, "InspirationChargesMax"));
    }
}
