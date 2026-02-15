using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Modifiers.Parsing;
using Xunit;

namespace PathOfBuilding.Core.Tests.Modifiers.Parsing;

public class ModNameTests
{
    [Theory]
    [InlineData("strength", "Str")]
    [InlineData("dexterity", "Dex")]
    [InlineData("intelligence", "Int")]
    [InlineData("life", "Life")]
    [InlineData("maximum life", "Life")]
    [InlineData("mana", "Mana")]
    [InlineData("energy shield", "EnergyShield")]
    [InlineData("armour", "Armour")]
    [InlineData("evasion", "Evasion")]
    [InlineData("evasion rating", "Evasion")]
    [InlineData("fire resistance", "FireResist")]
    [InlineData("cold resistance", "ColdResist")]
    [InlineData("lightning resistance", "LightningResist")]
    [InlineData("chaos resistance", "ChaosResist")]
    [InlineData("damage", "Damage")]
    [InlineData("physical damage", "PhysicalDamage")]
    [InlineData("fire damage", "FireDamage")]
    [InlineData("cold damage", "ColdDamage")]
    [InlineData("lightning damage", "LightningDamage")]
    [InlineData("chaos damage", "ChaosDamage")]
    [InlineData("elemental damage", "ElementalDamage")]
    [InlineData("critical strike chance", "CritChance")]
    [InlineData("critical strike multiplier", "CritMultiplier")]
    [InlineData("accuracy rating", "Accuracy")]
    [InlineData("movement speed", "MovementSpeed")]
    [InlineData("projectile speed", "ProjectileSpeed")]
    public void ModNameList_ContainsSingleStat(string key, string expectedName)
    {
        Assert.True(ModNameData.ModNameList.ContainsKey(key), $"ModNameList should contain '{key}'");
        var entry = ModNameData.ModNameList[key];
        Assert.Single(entry.Names);
        Assert.Equal(expectedName, entry.Names[0]);
    }

    [Theory]
    [InlineData("attributes", new[] { "Str", "Dex", "Int", "All" })]
    [InlineData("all attributes", new[] { "Str", "Dex", "Int", "All" })]
    [InlineData("fire and cold resistances", new[] { "FireResist", "ColdResist" })]
    [InlineData("all resistances", new[] { "ElementalResist", "ChaosResist" })]
    public void ModNameList_ContainsMultiStat(string key, string[] expectedNames)
    {
        Assert.True(ModNameData.ModNameList.ContainsKey(key));
        var entry = ModNameData.ModNameList[key];
        Assert.Equal(expectedNames.Length, entry.Names.Length);
        for (int i = 0; i < expectedNames.Length; i++)
            Assert.Equal(expectedNames[i], entry.Names[i]);
    }

    [Fact]
    public void ModNameList_AttackSpeed_HasAttackFlag()
    {
        var entry = ModNameData.ModNameList["attack speed"];
        Assert.Equal(ModFlag.Attack, entry.Flags);
    }

    [Fact]
    public void ModNameList_CastSpeed_HasCastFlag()
    {
        var entry = ModNameData.ModNameList["cast speed"];
        Assert.Equal(ModFlag.Cast, entry.Flags);
    }

    [Fact]
    public void ModNameList_CaseInsensitive()
    {
        Assert.True(ModNameData.ModNameList.ContainsKey("STRENGTH"));
        Assert.True(ModNameData.ModNameList.ContainsKey("Strength"));
        Assert.True(ModNameData.ModNameList.ContainsKey("strength"));
    }

    [Fact]
    public void DamageTypes_ContainsAllTypes()
    {
        Assert.Equal("Physical", DamageTypes.DmgTypes["physical"]);
        Assert.Equal("Lightning", DamageTypes.DmgTypes["lightning"]);
        Assert.Equal("Cold", DamageTypes.DmgTypes["cold"]);
        Assert.Equal("Fire", DamageTypes.DmgTypes["fire"]);
        Assert.Equal("Chaos", DamageTypes.DmgTypes["chaos"]);
    }

    [Fact]
    public void PenTypes_ContainsAllTypes()
    {
        Assert.Equal("FirePenetration", DamageTypes.PenTypes["fire resistance"]);
        Assert.Equal("ColdPenetration", DamageTypes.PenTypes["cold resistance"]);
        Assert.Equal("LightningPenetration", DamageTypes.PenTypes["lightning resistance"]);
        Assert.Equal("ElementalPenetration", DamageTypes.PenTypes["elemental resistance"]);
        Assert.Equal("ChaosPenetration", DamageTypes.PenTypes["chaos resistance"]);
    }

    [Fact]
    public void RegenTypes_ContainsLifeRegen()
    {
        Assert.True(DamageTypes.RegenTypes.ContainsKey("life"));
        Assert.Equal("LifeRegen", DamageTypes.RegenTypes["life"][0]);
    }

    [Fact]
    public void DegenTypes_ContainsLifeDegen()
    {
        Assert.True(DamageTypes.DegenTypes.ContainsKey("life"));
        Assert.Equal("LifeDegen", DamageTypes.DegenTypes["life"][0]);
    }

    [Fact]
    public void FlagTypes_ContainsPhasing()
    {
        Assert.True(DamageTypes.FlagTypes.ContainsKey("phasing"));
        Assert.Equal("Condition:Phasing", DamageTypes.FlagTypes["phasing"].Name);
        Assert.True(DamageTypes.FlagTypes["phasing"].IsSimple);
    }

    [Fact]
    public void FlagTypes_Hexproof_IsComplex()
    {
        Assert.True(DamageTypes.FlagTypes.ContainsKey("hexproof"));
        var entry = DamageTypes.FlagTypes["hexproof"];
        Assert.False(entry.IsSimple);
        Assert.Equal("CurseEffectOnSelf", entry.Name);
        Assert.Equal(-100.0, entry.Value);
        Assert.Equal("MORE", entry.Type);
    }
}
