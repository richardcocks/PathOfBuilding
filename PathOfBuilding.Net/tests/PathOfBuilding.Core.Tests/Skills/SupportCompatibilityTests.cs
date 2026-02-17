using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class SupportCompatibilityTests
{
    // Helper to make a minimal SkillDefinition
    private static SkillDefinition MakeSkill(
        string id, bool support = false,
        List<SkillType>? requireTypes = null,
        List<SkillType>? excludeTypes = null,
        List<SkillType>? addTypes = null,
        Dictionary<SkillType, bool>? skillTypes = null)
    {
        return new SkillDefinition
        {
            Id = id,
            Name = id,
            Support = support,
            RequireSkillTypes = requireTypes ?? new(),
            ExcludeSkillTypes = excludeTypes ?? new(),
            AddSkillTypes = addTypes ?? new(),
            SkillTypes = skillTypes ?? new(),
        };
    }

    private static ActiveSkill MakeActiveSkill(Dictionary<SkillType, bool> types)
    {
        var gem = new PathOfBuilding.Core.Import.Sections.GemInstanceData();
        var skill = MakeSkill("Active", skillTypes: types);
        return new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = types,
        };
    }

    // ─── DoesTypeExpressionMatch ───

    [Fact]
    public void SingleType_Match()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Attack] = true };
        var check = new List<SkillType> { SkillType.Attack };
        Assert.True(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void SingleType_NoMatch()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Spell] = true };
        var check = new List<SkillType> { SkillType.Attack };
        Assert.False(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void OR_Expression_BothPresent()
    {
        var types = new Dictionary<SkillType, bool>
        {
            [SkillType.Attack] = true,
            [SkillType.Spell] = true,
        };
        var check = new List<SkillType> { SkillType.Attack, SkillType.Spell, SkillType.OR };
        Assert.True(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void OR_Expression_OnePresent()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Spell] = true };
        var check = new List<SkillType> { SkillType.Attack, SkillType.Spell, SkillType.OR };
        Assert.True(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void OR_Expression_NonePresent()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Melee] = true };
        var check = new List<SkillType> { SkillType.Attack, SkillType.Spell, SkillType.OR };
        Assert.False(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void AND_Expression_BothPresent()
    {
        var types = new Dictionary<SkillType, bool>
        {
            [SkillType.Attack] = true,
            [SkillType.Melee] = true,
        };
        var check = new List<SkillType> { SkillType.Attack, SkillType.Melee, SkillType.AND };
        Assert.True(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void AND_Expression_OnePresent()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Attack] = true };
        var check = new List<SkillType> { SkillType.Attack, SkillType.Melee, SkillType.AND };
        Assert.False(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void NOT_Expression_Negates()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Spell] = true };
        var check = new List<SkillType> { SkillType.Attack, SkillType.NOT };
        // Attack is NOT in types, so NOT makes it true
        Assert.True(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void NOT_Expression_NegatesPresent()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Attack] = true };
        var check = new List<SkillType> { SkillType.Attack, SkillType.NOT };
        // Attack IS in types, so NOT makes it false
        Assert.False(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void Complex_Postfix_Expression()
    {
        // "Damage AND (Attack OR DegenOnlySpellDamage)" in postfix:
        // Damage, Attack, DegenOnlySpellDamage, OR, AND
        var types = new Dictionary<SkillType, bool>
        {
            [SkillType.Damage] = true,
            [SkillType.Attack] = true,
        };
        var check = new List<SkillType>
        {
            SkillType.Damage,
            SkillType.Attack,
            SkillType.DegenOnlySpellDamage,
            SkillType.OR,
            SkillType.AND,
        };
        Assert.True(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void Complex_Postfix_Expression_NoMatch()
    {
        var types = new Dictionary<SkillType, bool>
        {
            [SkillType.Spell] = true, // Has Spell, not Damage → AND fails
            [SkillType.Attack] = true,
        };
        var check = new List<SkillType>
        {
            SkillType.Damage,
            SkillType.Attack,
            SkillType.DegenOnlySpellDamage,
            SkillType.OR,
            SkillType.AND,
        };
        Assert.False(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void EmptyTypes_ReturnsFalse()
    {
        var types = new Dictionary<SkillType, bool>();
        var check = new List<SkillType> { SkillType.Attack };
        Assert.False(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void EmptyCheckTypes_ReturnsFalse()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Attack] = true };
        var check = new List<SkillType>();
        Assert.False(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void MultipleUnconsumedStackValues_TrueIfAnyTrue()
    {
        // Two separate type checks left on stack (implicit OR at stack level)
        var types = new Dictionary<SkillType, bool> { [SkillType.Spell] = true };
        var check = new List<SkillType> { SkillType.Attack, SkillType.Spell };
        // Stack: [false, true] → returns true because any is truthy
        Assert.True(SupportCompatibility.DoesTypeExpressionMatch(check, types));
    }

    [Fact]
    public void MinionTypes_Considered()
    {
        var types = new Dictionary<SkillType, bool> { [SkillType.Spell] = true };
        var minionTypes = new Dictionary<SkillType, bool> { [SkillType.Attack] = true };
        var check = new List<SkillType> { SkillType.Attack };
        Assert.True(SupportCompatibility.DoesTypeExpressionMatch(check, types, minionTypes));
    }

    // ─── CanSupport ───

    [Fact]
    public void CanSupport_NonSupport_ReturnsFalse()
    {
        var support = MakeSkill("NotSupport", support: false);
        var active = MakeActiveSkill(new Dictionary<SkillType, bool> { [SkillType.Attack] = true });
        Assert.False(SupportCompatibility.CanSupport(support, active));
    }

    [Fact]
    public void CanSupport_NoRequirements_ReturnsTrue()
    {
        var support = MakeSkill("Support", support: true);
        var active = MakeActiveSkill(new Dictionary<SkillType, bool> { [SkillType.Attack] = true });
        Assert.True(SupportCompatibility.CanSupport(support, active));
    }

    [Fact]
    public void CanSupport_RequireMatches()
    {
        var support = MakeSkill("Support", support: true,
            requireTypes: new List<SkillType> { SkillType.Attack });
        var active = MakeActiveSkill(new Dictionary<SkillType, bool> { [SkillType.Attack] = true });
        Assert.True(SupportCompatibility.CanSupport(support, active));
    }

    [Fact]
    public void CanSupport_RequireNoMatch()
    {
        var support = MakeSkill("Support", support: true,
            requireTypes: new List<SkillType> { SkillType.Attack });
        var active = MakeActiveSkill(new Dictionary<SkillType, bool> { [SkillType.Spell] = true });
        Assert.False(SupportCompatibility.CanSupport(support, active));
    }

    [Fact]
    public void CanSupport_ExcludeMatches_ReturnsFalse()
    {
        var support = MakeSkill("Support", support: true,
            excludeTypes: new List<SkillType> { SkillType.Triggered });
        var active = MakeActiveSkill(new Dictionary<SkillType, bool>
        {
            [SkillType.Attack] = true,
            [SkillType.Triggered] = true,
        });
        Assert.False(SupportCompatibility.CanSupport(support, active));
    }

    [Fact]
    public void CanSupport_ExcludeNoMatch_ReturnsTrue()
    {
        var support = MakeSkill("Support", support: true,
            excludeTypes: new List<SkillType> { SkillType.Triggered });
        var active = MakeActiveSkill(new Dictionary<SkillType, bool> { [SkillType.Attack] = true });
        Assert.True(SupportCompatibility.CanSupport(support, active));
    }
}
