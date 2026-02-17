using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class ActiveSkillTests
{
    [Fact]
    public void Construction_SetsRequiredProperties()
    {
        var skill = new SkillDefinition { Id = "Test", Name = "Test Skill" };
        var gem = new GemInstanceData { SkillId = "Test", Level = 20, Quality = 20 };

        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
        };

        Assert.Equal("Test Skill", activeSkill.GrantedEffect.Name);
        Assert.Equal(20, activeSkill.GemInstance.Level);
        Assert.Equal(1, activeSkill.SkillPart);
        Assert.False(activeSkill.Disabled);
        Assert.Null(activeSkill.DisableReason);
        Assert.Empty(activeSkill.SkillData);
        Assert.Empty(activeSkill.EffectList);
        Assert.NotNull(activeSkill.SkillModList);
    }

    [Fact]
    public void SkillTypes_Mutable()
    {
        var skill = new SkillDefinition { Id = "Test", Name = "Test" };
        var gem = new GemInstanceData();

        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
        };

        activeSkill.SkillTypes[SkillType.Attack] = true;
        activeSkill.SkillTypes[SkillType.Melee] = true;

        Assert.Equal(2, activeSkill.SkillTypes.Count);
    }

    [Fact]
    public void SkillFlags_Mutable()
    {
        var skill = new SkillDefinition { Id = "Test", Name = "Test" };
        var gem = new GemInstanceData();

        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
        };

        activeSkill.SkillFlags["hit"] = true;
        activeSkill.SkillFlags["attack"] = true;

        Assert.True(activeSkill.SkillFlags["hit"]);
        Assert.True(activeSkill.SkillFlags["attack"]);
    }

    [Fact]
    public void EffectList_CanAddEffects()
    {
        var skill = new SkillDefinition { Id = "Test", Name = "Test" };
        var gem = new GemInstanceData();

        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
        };

        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        Assert.Single(activeSkill.EffectList);
    }

    [Fact]
    public void ActiveSkillEffect_SetsProperties()
    {
        var skill = new SkillDefinition { Id = "Support", Name = "Support Gem" };
        var gem = new GemInstanceData { Level = 15, Quality = 10 };
        var gemDef = new GemDefinition
        {
            Id = "G1", Name = "Gem", GameId = "G1", VariantId = "V1", GrantedEffectId = "Support",
        };

        var effect = new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
            GemData = gemDef,
        };

        Assert.Equal("Support Gem", effect.GrantedEffect.Name);
        Assert.Equal(15, effect.GemInstance.Level);
        Assert.NotNull(effect.GemData);
    }
}
