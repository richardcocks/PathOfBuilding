using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Modifiers.Parsing;
using Xunit;

namespace PathOfBuilding.Core.Tests.Modifiers.Parsing;

public class ModParserTests : IDisposable
{
    public ModParserTests()
    {
        ModParser.ClearCache();
    }

    public void Dispose()
    {
        ModParser.ClearCache();
    }

    // ─── Basic INC/RED forms ───

    [Fact]
    public void ParseMod_PercentIncreased_Life()
    {
        var mods = ModParser.ParseMod("10% increased maximum Life");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Life", mods[0].Name);
        Assert.Equal(ModType.Inc, mods[0].Type);
        Assert.Equal(10.0, mods[0].Value.AsNumber());
    }

    [Fact]
    public void ParseMod_PercentReduced_MovementSpeed()
    {
        var mods = ModParser.ParseMod("10% reduced Movement Speed");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("MovementSpeed", mods[0].Name);
        Assert.Equal(ModType.Inc, mods[0].Type);
        Assert.Equal(-10.0, mods[0].Value.AsNumber());
    }

    // ─── MORE/LESS forms ───

    [Fact]
    public void ParseMod_PercentMore_Damage()
    {
        var mods = ModParser.ParseMod("20% more Damage");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Damage", mods[0].Name);
        Assert.Equal(ModType.More, mods[0].Type);
        Assert.Equal(20.0, mods[0].Value.AsNumber());
    }

    [Fact]
    public void ParseMod_PercentLess_AttackSpeed()
    {
        var mods = ModParser.ParseMod("15% less Attack Speed");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Speed", mods[0].Name);
        Assert.Equal(ModType.More, mods[0].Type);
        Assert.Equal(-15.0, mods[0].Value.AsNumber());
        Assert.Equal(ModFlag.Attack, mods[0].Flags);
    }

    // ─── BASE forms ───

    [Fact]
    public void ParseMod_FlatBase_Life()
    {
        var mods = ModParser.ParseMod("+50 to maximum Life");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Life", mods[0].Name);
        Assert.Equal(ModType.Base, mods[0].Type);
        Assert.Equal(50.0, mods[0].Value.AsNumber());
    }

    [Fact]
    public void ParseMod_NegativeBase_Life()
    {
        var mods = ModParser.ParseMod("-20 to maximum Life");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Life", mods[0].Name);
        Assert.Equal(ModType.Base, mods[0].Type);
        Assert.Equal(-20.0, mods[0].Value.AsNumber());
    }

    // ─── Resistance forms ───

    [Fact]
    public void ParseMod_FireResistance()
    {
        var mods = ModParser.ParseMod("+30% to Fire Resistance");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("FireResist", mods[0].Name);
        Assert.Equal(ModType.Base, mods[0].Type);
        Assert.Equal(30.0, mods[0].Value.AsNumber());
    }

    [Fact]
    public void ParseMod_AllElementalResistances()
    {
        var mods = ModParser.ParseMod("+15% to all Elemental Resistances");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("ElementalResist", mods[0].Name);
        Assert.Equal(ModType.Base, mods[0].Type);
        Assert.Equal(15.0, mods[0].Value.AsNumber());
    }

    // ─── Attribute forms ───

    [Fact]
    public void ParseMod_Attributes_MultiStat()
    {
        var mods = ModParser.ParseMod("+10 to all Attributes");
        Assert.NotNull(mods);
        Assert.Equal(4, mods.Count);
        Assert.Contains(mods, m => m.Name == "Str" && m.Value.AsNumber() == 10);
        Assert.Contains(mods, m => m.Name == "Dex" && m.Value.AsNumber() == 10);
        Assert.Contains(mods, m => m.Name == "Int" && m.Value.AsNumber() == 10);
        Assert.Contains(mods, m => m.Name == "All" && m.Value.AsNumber() == 10);
    }

    [Fact]
    public void ParseMod_Strength()
    {
        var mods = ModParser.ParseMod("+20 to Strength");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Str", mods[0].Name);
        Assert.Equal(ModType.Base, mods[0].Type);
        Assert.Equal(20.0, mods[0].Value.AsNumber());
    }

    // ─── Attack speed with flags ───

    [Fact]
    public void ParseMod_IncreasedAttackSpeed()
    {
        var mods = ModParser.ParseMod("10% increased Attack Speed");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Speed", mods[0].Name);
        Assert.Equal(ModType.Inc, mods[0].Type);
        Assert.Equal(10.0, mods[0].Value.AsNumber());
        Assert.Equal(ModFlag.Attack, mods[0].Flags);
    }

    [Fact]
    public void ParseMod_IncreasedCastSpeed()
    {
        var mods = ModParser.ParseMod("10% increased Cast Speed");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Speed", mods[0].Name);
        Assert.Equal(ModType.Inc, mods[0].Type);
        Assert.Equal(10.0, mods[0].Value.AsNumber());
        Assert.Equal(ModFlag.Cast, mods[0].Flags);
    }

    // ─── Damage ranges ───

    [Fact]
    public void ParseMod_AddedPhysicalDamage()
    {
        var mods = ModParser.ParseMod("Adds 10 to 20 Physical Damage");
        Assert.NotNull(mods);
        Assert.Equal(2, mods.Count);
        Assert.Equal("PhysicalMin", mods[0].Name);
        Assert.Equal(10.0, mods[0].Value.AsNumber());
        Assert.Equal("PhysicalMax", mods[1].Name);
        Assert.Equal(20.0, mods[1].Value.AsNumber());
    }

    [Fact]
    public void ParseMod_AddedFireDamageToAttacks()
    {
        var mods = ModParser.ParseMod("Adds 5 to 10 Fire Damage to Attacks");
        Assert.NotNull(mods);
        Assert.Equal(2, mods.Count);
        Assert.Equal("FireMin", mods[0].Name);
        Assert.Equal(5.0, mods[0].Value.AsNumber());
        Assert.Equal("FireMax", mods[1].Name);
        Assert.Equal(10.0, mods[1].Value.AsNumber());
        Assert.Equal(KeywordFlag.Attack, mods[0].KeywordFlags);
    }

    [Fact]
    public void ParseMod_AddedColdDamageToSpells()
    {
        var mods = ModParser.ParseMod("Adds 3 to 7 Cold Damage to Spells");
        Assert.NotNull(mods);
        Assert.Equal(2, mods.Count);
        Assert.Equal("ColdMin", mods[0].Name);
        Assert.Equal("ColdMax", mods[1].Name);
        Assert.Equal(KeywordFlag.Spell, mods[0].KeywordFlags);
    }

    // ─── Penetration ───

    [Fact]
    public void ParseMod_PenetratesFireResistance()
    {
        var mods = ModParser.ParseMod("Penetrates 10% Fire Resistance");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("FirePenetration", mods[0].Name);
        Assert.Equal(ModType.Base, mods[0].Type);
        Assert.Equal(10.0, mods[0].Value.AsNumber());
    }

    // ─── Special mods: keystones ───

    [Fact]
    public void ParseMod_Keystone_IronReflexes()
    {
        var mods = ModParser.ParseMod("Iron Reflexes");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Keystone:IronReflexes", mods[0].Name);
        Assert.Equal(ModType.Flag, mods[0].Type);
        Assert.True(mods[0].Value.AsBoolean());
    }

    [Fact]
    public void ParseMod_Keystone_ResoluteTechnique()
    {
        var mods = ModParser.ParseMod("Resolute Technique");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Keystone:ResoluteTechnique", mods[0].Name);
    }

    // ─── Special mods: damage conversion ───

    [Fact]
    public void ParseMod_PhysicalToFireConversion()
    {
        var mods = ModParser.ParseMod("50% of Physical Damage converted to Fire Damage");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("PhysicalDamageConvertToFire", mods[0].Name);
        Assert.Equal(ModType.Base, mods[0].Type);
        Assert.Equal(50.0, mods[0].Value.AsNumber());
    }

    // ─── Special mods: penetration ───

    [Fact]
    public void ParseMod_DamagePenetratesFireResist()
    {
        var mods = ModParser.ParseMod("Damage Penetrates 10% Fire Resistance");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("FirePenetration", mods[0].Name);
        Assert.Equal(10.0, mods[0].Value.AsNumber());
    }

    // ─── Special mods: immunity ───

    [Fact]
    public void ParseMod_ImmuneToFreeze()
    {
        var mods = ModParser.ParseMod("Immune to Freeze");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("AvoidFreeze", mods[0].Name);
        Assert.Equal(100.0, mods[0].Value.AsNumber());
    }

    // ─── Unparseable ───

    [Fact]
    public void ParseMod_GarbageText_ReturnsNull()
    {
        var mods = ModParser.ParseMod("this is completely random text that makes no sense");
        Assert.Null(mods);
    }

    // ─── Case insensitivity ───

    [Fact]
    public void ParseMod_CaseInsensitive()
    {
        var mods1 = ModParser.ParseMod("+50 to maximum Life");
        var mods2 = ModParser.ParseMod("+50 TO MAXIMUM LIFE");

        Assert.NotNull(mods1);
        Assert.NotNull(mods2);
        Assert.Equal(mods1[0].Name, mods2[0].Name);
        Assert.Equal(mods1[0].Value, mods2[0].Value);
    }

    // ─── Cache ───

    [Fact]
    public void ParseMod_CacheProducesSameResults()
    {
        var mods1 = ModParser.ParseMod("+50 to maximum Life");
        var mods2 = ModParser.ParseMod("+50 to maximum Life");

        Assert.Same(mods1, mods2);
    }

    // ─── Regen forms ───

    [Fact]
    public void ParseMod_RegenPercent()
    {
        var mods = ModParser.ParseMod("Regenerate 1.5% of Life per second");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("LifeRegenPercent", mods[0].Name);
        Assert.Equal(ModType.Base, mods[0].Type);
        Assert.Equal(1.5, mods[0].Value.AsNumber());
    }

    // ─── Multi-stat mods ───

    [Fact]
    public void ParseMod_FireAndColdResistances()
    {
        var mods = ModParser.ParseMod("+20% to Fire and Cold Resistances");
        Assert.NotNull(mods);
        Assert.Equal(2, mods.Count);
        Assert.Contains(mods, m => m.Name == "FireResist");
        Assert.Contains(mods, m => m.Name == "ColdResist");
    }

    // ─── Increased damage types ───

    [Fact]
    public void ParseMod_IncreasedPhysicalDamage()
    {
        var mods = ModParser.ParseMod("40% increased Physical Damage");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("PhysicalDamage", mods[0].Name);
        Assert.Equal(ModType.Inc, mods[0].Type);
        Assert.Equal(40.0, mods[0].Value.AsNumber());
    }

    [Fact]
    public void ParseMod_IncreasedElementalDamage()
    {
        var mods = ModParser.ParseMod("25% increased Elemental Damage");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("ElementalDamage", mods[0].Name);
        Assert.Equal(ModType.Inc, mods[0].Type);
    }

    // ─── Leech special mods ───

    [Fact]
    public void ParseMod_PhysicalAttackDamageLeechedAsLife()
    {
        var mods = ModParser.ParseMod("2% of Physical Attack Damage Leeched as Life");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("PhysicalDamageLifeLeech", mods[0].Name);
        Assert.Equal(2.0, mods[0].Value.AsNumber());
        Assert.Equal(ModFlag.Attack, mods[0].Flags);
    }

    // ─── Critical strike ───

    [Fact]
    public void ParseMod_IncreasedCritChance()
    {
        var mods = ModParser.ParseMod("30% increased Critical Strike Chance");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("CritChance", mods[0].Name);
        Assert.Equal(ModType.Inc, mods[0].Type);
        Assert.Equal(30.0, mods[0].Value.AsNumber());
    }

    // ─── Energy shield ───

    [Fact]
    public void ParseMod_FlatEnergyShield()
    {
        var mods = ModParser.ParseMod("+100 to maximum Energy Shield");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("EnergyShield", mods[0].Name);
        Assert.Equal(100.0, mods[0].Value.AsNumber());
    }

    // ─── Accuracy ───

    [Fact]
    public void ParseMod_FlatAccuracy()
    {
        var mods = ModParser.ParseMod("+200 to Accuracy Rating");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Accuracy", mods[0].Name);
        Assert.Equal(200.0, mods[0].Value.AsNumber());
    }

    // ─── Evasion ───

    [Fact]
    public void ParseMod_IncreasedEvasion()
    {
        var mods = ModParser.ParseMod("50% increased Evasion Rating");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Evasion", mods[0].Name);
        Assert.Equal(ModType.Inc, mods[0].Type);
    }

    // ─── Charges ───

    [Fact]
    public void ParseMod_MaxPowerCharges()
    {
        var mods = ModParser.ParseMod("+1 to Maximum Power Charges");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("PowerChargesMax", mods[0].Name);
        Assert.Equal(1.0, mods[0].Value.AsNumber());
    }

    // ─── Projectiles ───

    [Fact]
    public void ParseMod_AdditionalProjectile()
    {
        var mods = ModParser.ParseMod("1 additional Projectile");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("ProjectileCount", mods[0].Name);
        Assert.Equal(1.0, mods[0].Value.AsNumber());
    }

    // ─── Mana ───

    [Fact]
    public void ParseMod_FlatMana()
    {
        var mods = ModParser.ParseMod("+50 to maximum Mana");
        Assert.NotNull(mods);
        Assert.Single(mods);
        Assert.Equal("Mana", mods[0].Name);
        Assert.Equal(50.0, mods[0].Value.AsNumber());
    }
}
