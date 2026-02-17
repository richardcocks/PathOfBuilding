using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Tests.Skills;

public class GlobalEffectTagTests
{
    [Fact]
    public void Construction_AllProperties()
    {
        var tag = new GlobalEffectTag
        {
            EffectType = "Buff",
            EffectName = "Onslaught",
            EffectCond = "OnsightActive",
            Unscalable = true,
        };

        Assert.Equal("Buff", tag.EffectType);
        Assert.Equal("Onslaught", tag.EffectName);
        Assert.Equal("OnsightActive", tag.EffectCond);
        Assert.True(tag.Unscalable);
    }

    [Fact]
    public void Construction_MinimalProperties()
    {
        var tag = new GlobalEffectTag { EffectType = "Debuff" };

        Assert.Equal("Debuff", tag.EffectType);
        Assert.Null(tag.EffectName);
        Assert.Null(tag.EffectCond);
        Assert.False(tag.Unscalable);
    }

    [Fact]
    public void Equality_SameValues()
    {
        var a = new GlobalEffectTag { EffectType = "Buff", EffectName = "Onslaught" };
        var b = new GlobalEffectTag { EffectType = "Buff", EffectName = "Onslaught" };
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equality_DifferentValues()
    {
        var a = new GlobalEffectTag { EffectType = "Buff" };
        var b = new GlobalEffectTag { EffectType = "Debuff" };
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void ToString_Format()
    {
        var tag = new GlobalEffectTag { EffectType = "Buff", EffectName = "Onslaught" };
        Assert.Equal("GlobalEffect(Buff,Onslaught)", tag.ToString());
    }
}
