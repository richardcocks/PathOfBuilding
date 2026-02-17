using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcOffenceTests
{
    // ─── Helpers ───

    private static ActiveSkill MakeSpellSkill(
        double castTime = 1.0,
        double critChance = 5,
        Dictionary<string, double>? extraSkillData = null,
        Dictionary<string, bool>? extraFlags = null,
        Dictionary<SkillType, bool>? extraTypes = null)
    {
        var skill = new ActiveSkill
        {
            GrantedEffect = new SkillDefinition
            {
                Id = "TestSpell",
                Name = "TestSpell",
                CastTime = castTime,
            },
            GemInstance = new GemInstanceData { SkillId = "TestSpell", Level = 1, Quality = 0 },
        };

        skill.SkillFlags["hit"] = true;
        skill.SkillFlags["spell"] = true;
        skill.SkillFlags["selfCast"] = true;

        skill.SkillTypes[SkillType.Spell] = true;

        if (extraFlags != null)
            foreach (var kv in extraFlags)
                skill.SkillFlags[kv.Key] = kv.Value;

        if (extraTypes != null)
            foreach (var kv in extraTypes)
                skill.SkillTypes[kv.Key] = kv.Value;

        skill.SkillCfg = new ModConfig
        {
            Flags = ModFlag.Spell | ModFlag.Cast | ModFlag.Hit,
            KeywordFlags = KeywordFlag.Spell | KeywordFlag.Hit,
            SkillName = "TestSpell",
            SkillTypes = new HashSet<SkillType>(skill.SkillTypes.Keys),
            SkillCond = new Dictionary<string, bool>(),
        };

        if (critChance > 0)
            skill.SkillData["CritChance"] = critChance;

        if (extraSkillData != null)
            foreach (var kv in extraSkillData)
                skill.SkillData[kv.Key] = kv.Value;

        return skill;
    }

    private static (Actor player, Actor enemy) MakeActors(ActiveSkill skill,
        Action<ModDB>? configurePlayerMods = null,
        Action<ModDB>? configureEnemyMods = null)
    {
        var player = new Actor();
        var enemy = new Actor();
        player.Enemy = enemy;
        enemy.Enemy = player;

        // Base outputs needed by offence
        player.Output["ActionSpeedMod"] = 1.0;
        player.Output["Life"] = 1000;
        player.Output["EnergyShield"] = 0;
        player.Output["Mana"] = 500;

        // Leech caps (from CalcDefence)
        player.Output["MaxLifeLeechInstance"] = 100;
        player.Output["MaxLifeLeechRate"] = 200;
        player.Output["MaxEnergyShieldLeechInstance"] = 50;
        player.Output["MaxEnergyShieldLeechRate"] = 100;
        player.Output["MaxManaLeechInstance"] = 50;
        player.Output["MaxManaLeechRate"] = 100;

        // Base crit multiplier (150% = 50% extra)
        player.ModDB.NewMod("CritMultiplier", ModType.Base, 50, "Base");
        // Base crit cap
        player.ModDB.NewMod("CritChanceCap", ModType.Base, 100, "Base");

        configurePlayerMods?.Invoke(player.ModDB);
        configureEnemyMods?.Invoke(enemy.ModDB);

        // Set up skill mod list with player ModDB as parent
        skill.SkillModList = new ModList(player.ModDB);
        skill.Actor = player;

        player.ActiveSkillList.Add(skill);
        player.MainSkill = skill;

        return (player, enemy);
    }

    // ─── Disabled skill ───

    [Fact]
    public void Offence_DisabledSkill_CombinedDPSIsZero()
    {
        var skill = MakeSpellSkill(extraFlags: new() { ["disable"] = true });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["CombinedDPS"]);
    }

    // ─── Attack skill early return ───

    [Fact]
    public void Offence_AttackSkill_ReturnZeroDPS()
    {
        var skill = MakeSpellSkill(extraFlags: new() { ["attack"] = true });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["CombinedDPS"]);
    }

    // ═══════════════════════════════════════════
    // Cast Speed Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void CastSpeed_BaseTime1s_Speed1()
    {
        var skill = MakeSpellSkill(castTime: 1.0);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(1.0, player.Output["Speed"], 2);
        Assert.Equal(1.0, player.Output["Time"], 2);
    }

    [Fact]
    public void CastSpeed_BaseTimeHalf_Speed2()
    {
        var skill = MakeSpellSkill(castTime: 0.5);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(2.0, player.Output["Speed"], 2);
        Assert.Equal(0.5, player.Output["Time"], 2);
    }

    [Fact]
    public void CastSpeed_WithIncreasedSpeed()
    {
        var skill = MakeSpellSkill(castTime: 1.0);
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("Speed", ModType.Inc, 100, "Test", ModFlag.Cast | ModFlag.Spell);
        });

        CalcOffence.Offence(player, skill);

        // 1 / (1.0 / round(2.0, 2)) = 2.0
        Assert.Equal(2.0, player.Output["Speed"], 2);
    }

    [Fact]
    public void CastSpeed_WithMoreSpeed()
    {
        var skill = MakeSpellSkill(castTime: 1.0);
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("Speed", ModType.More, 50, "Test", ModFlag.Cast | ModFlag.Spell);
        });

        CalcOffence.Offence(player, skill);

        // More(50) = 1.5, speed = 1 / (1.0 / round(1.5, 2)) = 1.5
        Assert.Equal(1.5, player.Output["Speed"], 2);
    }

    [Fact]
    public void CastSpeed_WithActionSpeedMod()
    {
        var skill = MakeSpellSkill(castTime: 1.0);
        var (player, _) = MakeActors(skill);
        player.Output["ActionSpeedMod"] = 1.2;

        CalcOffence.Offence(player, skill);

        // selfCast: speed * actionSpeedMod = 1.0 * 1.2 = 1.2
        Assert.Equal(1.2, player.Output["Speed"], 2);
    }

    [Fact]
    public void CastSpeed_ZeroCastTime_ZeroSpeed()
    {
        var skill = MakeSpellSkill(castTime: 0);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["Speed"]);
        Assert.Equal(0, player.Output["Time"]);
    }

    [Fact]
    public void CastSpeed_CastTimeOverride()
    {
        var skill = MakeSpellSkill(castTime: 1.0,
            extraSkillData: new() { ["castTimeOverride"] = 0.25 });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // 1 / (0.25 / 1.0) = 4.0
        Assert.Equal(4.0, player.Output["Speed"], 2);
    }

    [Fact]
    public void CastSpeed_ServerTickRateCap()
    {
        // Very fast cast speed should be capped at ServerTickRate
        var skill = MakeSpellSkill(castTime: 0.01);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.True(player.Output["Speed"] <= MiscConstants.ServerTickRate + 0.01);
    }

    [Fact]
    public void CastSpeed_WithCooldown()
    {
        var skill = MakeSpellSkill(castTime: 0.5,
            extraSkillData: new() { ["cooldown"] = 2.0 });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // Cooldown caps speed at 1/cd = 0.5 casts/sec, speed without cd = 2.0
        // But cooldown is rounded to server ticks
        Assert.True(player.Output["Speed"] <= 2.0);
    }

    [Fact]
    public void CastSpeed_ChannelSkill_NoTickCap()
    {
        var skill = MakeSpellSkill(castTime: 0.01,
            extraTypes: new() { [SkillType.Channel] = true });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // Channel skills are not capped by ServerTickRate
        Assert.True(player.Output["Speed"] > MiscConstants.ServerTickRate);
    }

    [Fact]
    public void CastSpeed_TimeOverride()
    {
        var skill = MakeSpellSkill(castTime: 1.0,
            extraSkillData: new() { ["timeOverride"] = 0.2 });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0.2, player.Output["Time"], 3);
        Assert.Equal(5.0, player.Output["Speed"], 2);
    }

    [Fact]
    public void CastSpeed_FixedCastTime()
    {
        var skill = MakeSpellSkill(castTime: 0.75,
            extraSkillData: new() { ["fixedCastTime"] = 1 });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0.75, player.Output["Time"], 3);
        Assert.Equal(1.0 / 0.75, player.Output["Speed"], 2);
    }

    [Fact]
    public void CastSpeed_TriggerTime()
    {
        var skill = MakeSpellSkill(castTime: 1.0,
            extraSkillData: new() { ["triggerTime"] = 0.5, ["triggered"] = 1 });
        skill.SkillFlags.Remove("selfCast");
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0.5, player.Output["Time"], 3);
        Assert.Equal(2.0, player.Output["Speed"], 2);
    }

    [Fact]
    public void CastSpeed_TriggerRate()
    {
        var skill = MakeSpellSkill(castTime: 1.0,
            extraSkillData: new() { ["triggerRate"] = 3.0, ["triggered"] = 1 });
        skill.SkillFlags.Remove("selfCast");
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(3.0, player.Output["Speed"], 2);
    }

    // ═══════════════════════════════════════════
    // Crit Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Crit_BaseCritOnly()
    {
        var skill = MakeSpellSkill(critChance: 5);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(5, player.Output["CritChance"], 2);
    }

    [Fact]
    public void Crit_WithIncreasedCrit()
    {
        var skill = MakeSpellSkill(critChance: 5);
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("CritChance", ModType.Inc, 100, "Test");
        });

        CalcOffence.Offence(player, skill);

        // 5 * (1 + 100/100) = 10
        Assert.Equal(10, player.Output["CritChance"], 2);
    }

    [Fact]
    public void Crit_CappedAt100()
    {
        var skill = MakeSpellSkill(critChance: 50);
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("CritChance", ModType.Inc, 10000, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(100, player.Output["CritChance"], 2);
    }

    [Fact]
    public void Crit_FloorAtZero()
    {
        var skill = MakeSpellSkill(critChance: 5);
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("CritChance", ModType.Inc, -200, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["CritChance"], 2);
    }

    [Fact]
    public void Crit_Override100()
    {
        var skill = MakeSpellSkill(critChance: 5);
        var (player, _) = MakeActors(skill);
        skill.SkillModList.AddMod(new Mod
        {
            Name = "CritChance", Type = ModType.Override, Value = 100, Source = "Test"
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(100, player.Output["CritChance"]);
    }

    [Fact]
    public void Crit_Multiplier_Base150()
    {
        var skill = MakeSpellSkill(critChance: 100);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // Base 50% extra => 1 + 0.5 = 1.5
        Assert.Equal(1.5, player.Output["CritMultiplier"], 2);
    }

    [Fact]
    public void Crit_Multiplier_WithAdditional()
    {
        var skill = MakeSpellSkill(critChance: 100);
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("CritMultiplier", ModType.Base, 50, "Test");
        });

        CalcOffence.Offence(player, skill);

        // (50 + 50)/100 = 1.0 extra => 1 + 1.0 = 2.0
        Assert.Equal(2.0, player.Output["CritMultiplier"], 2);
    }

    [Fact]
    public void Crit_NoCritMultiplier_Flag()
    {
        var skill = MakeSpellSkill(critChance: 100);
        var (player, _) = MakeActors(skill);
        skill.SkillModList.AddMod(new Mod
        {
            Name = "NoCritMultiplier", Type = ModType.Flag, Value = true, Source = "Test"
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(1, player.Output["CritMultiplier"]);
    }

    [Fact]
    public void Crit_Effect_NoCrit_IsOne()
    {
        var skill = MakeSpellSkill(critChance: 0);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(1, player.Output["CritEffect"], 4);
    }

    [Fact]
    public void Crit_Effect_WithCrit()
    {
        var skill = MakeSpellSkill(critChance: 50);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // CritEffect = (1 - 0.5) + 0.5 * 1.5 = 0.5 + 0.75 = 1.25
        Assert.Equal(1.25, player.Output["CritEffect"], 4);
    }

    [Fact]
    public void Crit_Every3rdUseCrit()
    {
        var skill = MakeSpellSkill(critChance: 0);
        var (player, _) = MakeActors(skill);
        skill.SkillModList.AddMod(new Mod
        {
            Name = "Every3UseCrit", Type = ModType.Flag, Value = true, Source = "Test"
        });

        CalcOffence.Offence(player, skill);

        // (2 * 0 + 100) / 3 = 33.33
        Assert.Equal(100.0 / 3, player.Output["CritChance"], 1);
    }

    [Fact]
    public void Crit_WithMoreCrit()
    {
        var skill = MakeSpellSkill(critChance: 10);
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("CritChance", ModType.More, 50, "Test");
        });

        CalcOffence.Offence(player, skill);

        // 10 * (1+0/100) * More(50)=1.5 = 15
        Assert.Equal(15, player.Output["CritChance"], 2);
    }

    [Fact]
    public void Crit_LuckyCrit()
    {
        var skill = MakeSpellSkill(critChance: 50);
        var (player, _) = MakeActors(skill);
        skill.SkillModList.AddMod(new Mod
        {
            Name = "CritChanceLucky", Type = ModType.Flag, Value = true, Source = "Test"
        });

        CalcOffence.Offence(player, skill);

        // Lucky: 1 - (1 - 0.5)^2 = 1 - 0.25 = 0.75 = 75%
        Assert.Equal(75, player.Output["CritChance"], 1);
    }

    // ═══════════════════════════════════════════
    // Base Damage Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void BaseDamage_ColdDamageFromSkillData()
    {
        var skill = MakeSpellSkill(extraSkillData: new()
        {
            ["ColdMin"] = 50,
            ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(50, player.Output["ColdMinBase"]);
        Assert.Equal(100, player.Output["ColdMaxBase"]);
    }

    [Fact]
    public void BaseDamage_WithBaseMultiplier()
    {
        var skill = MakeSpellSkill(extraSkillData: new()
        {
            ["ColdMin"] = 100,
            ["ColdMax"] = 200,
            ["baseMultiplier"] = 1.5,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(150, player.Output["ColdMinBase"]);
        Assert.Equal(300, player.Output["ColdMaxBase"]);
    }

    [Fact]
    public void BaseDamage_WithDamageEffectiveness()
    {
        var skill = MakeSpellSkill(extraSkillData: new()
        {
            ["ColdMin"] = 100,
            ["ColdMax"] = 200,
            ["damageEffectiveness"] = 0.5,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("ColdMin", ModType.Base, 100, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
            p.NewMod("ColdMax", ModType.Base, 200, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });

        CalcOffence.Offence(player, skill);

        // base = 100*1 + 100*0.5*1 = 150 (min), 200*1 + 200*0.5*1 = 300 (max)
        Assert.Equal(150, player.Output["ColdMinBase"]);
        Assert.Equal(300, player.Output["ColdMaxBase"]);
    }

    [Fact]
    public void BaseDamage_AddedDamage()
    {
        var skill = MakeSpellSkill();
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("FireMin", ModType.Base, 50, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
            p.NewMod("FireMax", ModType.Base, 100, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });

        CalcOffence.Offence(player, skill);

        // No source damage, added 50-100 * effectiveness(1) * addedMult(1)
        Assert.Equal(50, player.Output["FireMinBase"]);
        Assert.Equal(100, player.Output["FireMaxBase"]);
    }

    [Fact]
    public void BaseDamage_EmptySource_NoBaseDamage()
    {
        var skill = MakeSpellSkill();
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["PhysicalMinBase"]);
        Assert.Equal(0, player.Output["PhysicalMaxBase"]);
    }

    [Fact]
    public void BaseDamage_MultipleDamageTypes()
    {
        var skill = MakeSpellSkill(extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 200,
            ["FireMin"] = 50, ["FireMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(100, player.Output["ColdMinBase"]);
        Assert.Equal(200, player.Output["ColdMaxBase"]);
        Assert.Equal(50, player.Output["FireMinBase"]);
        Assert.Equal(100, player.Output["FireMaxBase"]);
    }

    [Fact]
    public void BaseDamage_BonusMinMax()
    {
        var skill = MakeSpellSkill(extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 200,
            ["ColdBonusMin"] = 10, ["ColdBonusMax"] = 20,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(110, player.Output["ColdMinBase"]);
        Assert.Equal(220, player.Output["ColdMaxBase"]);
    }

    // ═══════════════════════════════════════════
    // Hit Damage Loop Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void HitDamage_NoCrit_AverageHitEqualsNonCrit()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 200,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // AverageHit = totalHitAvg * 1 + totalCritAvg * 0
        double avgHit = player.Output["AverageHit"];
        Assert.True(avgHit > 0);
        Assert.Equal(150, avgHit, 0); // (100+200)/2 = 150
    }

    [Fact]
    public void HitDamage_100Crit_AverageHitEqualsCrit()
    {
        var skill = MakeSpellSkill(critChance: 100, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 200,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // With 100% crit and 1.5x multi:
        // critAvg = 150 * 1.5 = 225
        double avgHit = player.Output["AverageHit"];
        Assert.Equal(225, avgHit, 0);
    }

    [Fact]
    public void HitDamage_50Crit_WeightedAverage()
    {
        var skill = MakeSpellSkill(critChance: 50, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 200,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // nonCrit avg = 150, crit avg = 225
        // AverageHit = 150 * 0.5 + 225 * 0.5 = 187.5
        double avgHit = player.Output["AverageHit"];
        Assert.Equal(187.5, avgHit, 0);
    }

    [Fact]
    public void HitDamage_ConversionChain()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["PhysicalMin"] = 100, ["PhysicalMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            // Convert 50% physical to cold
            p.NewMod("PhysicalDamageConvertToCold", ModType.Base, 50, "Test");
        });

        CalcOffence.Offence(player, skill);

        // Physical remains 50%, Cold gets 50%
        double physAvg = player.Output["PhysicalHitAverage"];
        double coldAvg = player.Output["ColdHitAverage"];
        Assert.True(physAvg > 0);
        Assert.True(coldAvg > 0);
        // Total should be close to 100 (before any DR)
        Assert.Equal(100, physAvg + coldAvg, 0);
    }

    [Fact]
    public void HitDamage_CannotDealType()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 200,
        });
        var (player, _) = MakeActors(skill);
        skill.SkillModList.AddMod(new Mod
        {
            Name = "DealNoCold", Type = ModType.Flag, Value = true, Source = "Test"
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["ColdHitAverage"]);
    }

    [Fact]
    public void HitDamage_IncreasedDamage()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("ColdDamage", ModType.Inc, 100, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });

        CalcOffence.Offence(player, skill);

        // 100 * (1+100/100) = 200
        Assert.Equal(200, player.Output["ColdHitAverage"], 0);
    }

    [Fact]
    public void HitDamage_MoreDamage()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("ColdDamage", ModType.More, 50, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });

        CalcOffence.Offence(player, skill);

        // 100 * More(50)=1.5 = 150
        Assert.Equal(150, player.Output["ColdHitAverage"], 0);
    }

    // ═══════════════════════════════════════════
    // Resist / Pen Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Resist_EnemyColdResist_ReducesDamage()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, configureEnemyMods: e =>
        {
            e.NewMod("ColdResist", ModType.Base, 75, "Test");
        });

        CalcOffence.Offence(player, skill);

        // 100 * (1 - 75/100) = 25
        Assert.Equal(25, player.Output["ColdHitAverage"], 0);
    }

    [Fact]
    public void Resist_ColdPenetration_ReducesEffective()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("ColdPenetration", ModType.Base, 20, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        }, e =>
        {
            e.NewMod("ColdResist", ModType.Base, 75, "Test");
        });

        CalcOffence.Offence(player, skill);

        // effective resist = 75 - 20 = 55, dmg = 100 * (1 - 55/100) = 45
        Assert.Equal(45, player.Output["ColdHitAverage"], 0);
    }

    [Fact]
    public void Resist_ElementalPenetration_AppliedToEle()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["FireMin"] = 100, ["FireMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("ElementalPenetration", ModType.Base, 10, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        }, e =>
        {
            e.NewMod("FireResist", ModType.Base, 50, "Test");
        });

        CalcOffence.Offence(player, skill);

        // 100 * (1 - (50-10)/100) = 100 * 0.6 = 60
        Assert.Equal(60, player.Output["FireHitAverage"], 0);
    }

    [Fact]
    public void Resist_NegativeResist_IncreaseDamage()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, configureEnemyMods: e =>
        {
            e.NewMod("ColdResist", ModType.Base, -50, "Test");
        });

        CalcOffence.Offence(player, skill);

        // 100 * (1 - (-50)/100) = 100 * 1.5 = 150
        Assert.Equal(150, player.Output["ColdHitAverage"], 0);
    }

    [Fact]
    public void Resist_PhysicalDR_FromEnemyArmour()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["PhysicalMin"] = 1000, ["PhysicalMax"] = 1000,
        });
        var (player, _) = MakeActors(skill, configureEnemyMods: e =>
        {
            e.NewMod("Armour", ModType.Base, 5000, "Test");
        });

        CalcOffence.Offence(player, skill);

        // Armour reduction reduces physical damage
        Assert.True(player.Output["PhysicalHitAverage"] < 1000);
        Assert.True(player.Output["PhysicalHitAverage"] > 0);
    }

    [Fact]
    public void Resist_IgnorePhysicalDR()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["PhysicalMin"] = 1000, ["PhysicalMax"] = 1000,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("IgnoreEnemyPhysicalDamageReduction", ModType.Flag, true, "Test",
                ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        }, e =>
        {
            e.NewMod("Armour", ModType.Base, 50000, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(1000, player.Output["PhysicalHitAverage"], 0);
    }

    [Fact]
    public void Resist_ChaosPenetration()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ChaosMin"] = 100, ["ChaosMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("ChaosPenetration", ModType.Base, 15, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        }, e =>
        {
            e.NewMod("ChaosResist", ModType.Base, 30, "Test");
        });

        CalcOffence.Offence(player, skill);

        // effective resist = 30 - 15 = 15, dmg = 100 * 0.85 = 85
        Assert.Equal(85, player.Output["ChaosHitAverage"], 0);
    }

    [Fact]
    public void Resist_IgnoreResistanceFlag()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("IgnoreColdResistance", ModType.Flag, true, "Test",
                ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        }, e =>
        {
            e.NewMod("ColdResist", ModType.Base, 75, "Test");
        });

        CalcOffence.Offence(player, skill);

        // Ignored resist = full damage
        Assert.Equal(100, player.Output["ColdHitAverage"], 0);
    }

    [Fact]
    public void Resist_EnemyDamageTaken_Increases()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, configureEnemyMods: e =>
        {
            e.NewMod("ColdDamageTaken", ModType.Inc, 20, "Test");
        });

        CalcOffence.Offence(player, skill);

        // 100 * (1 + 20/100) = 120
        Assert.Equal(120, player.Output["ColdHitAverage"], 0);
    }

    [Fact]
    public void Resist_ResistFloor()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["FireMin"] = 100, ["FireMax"] = 100,
        });
        var (player, _) = MakeActors(skill, configureEnemyMods: e =>
        {
            e.NewMod("FireResist", ModType.Base, -300, "Test");
        });

        CalcOffence.Offence(player, skill);

        // Resist clamped at -200, so effMult = 1 - (-200)/100 = 3
        Assert.Equal(300, player.Output["FireHitAverage"], 0);
    }

    // ═══════════════════════════════════════════
    // DPS Aggregation Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void DPS_BasicTotalDPS()
    {
        var skill = MakeSpellSkill(critChance: 0, castTime: 1.0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // AverageHit = 100, HitChance = 100%, Speed = 1
        // TotalDPS = 100 * 1 * 1 * 1 = 100
        Assert.Equal(100, player.Output["AverageHit"], 0);
        Assert.Equal(100, player.Output["AverageDamage"], 0);
        Assert.Equal(100, player.Output["TotalDPS"], 0);
    }

    [Fact]
    public void DPS_WithSpeed()
    {
        var skill = MakeSpellSkill(critChance: 0, castTime: 0.5, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // Speed = 2, AverageDamage = 100, TotalDPS = 100 * 2 = 200
        Assert.Equal(200, player.Output["TotalDPS"], 0);
    }

    [Fact]
    public void DPS_DpsMultiplier()
    {
        var skill = MakeSpellSkill(critChance: 0, castTime: 1.0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
            ["dpsMultiplier"] = 2,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // TotalDPS = 100 * 1 * 2 = 200
        Assert.Equal(200, player.Output["TotalDPS"], 0);
    }

    [Fact]
    public void DPS_CombinedDPS_EqualsTotal()
    {
        var skill = MakeSpellSkill(critChance: 0, castTime: 1.0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        // No culling or reservation → CombinedDPS = TotalDPS
        Assert.Equal(player.Output["TotalDPS"], player.Output["CombinedDPS"], 1);
    }

    [Fact]
    public void DPS_CullingStrike()
    {
        var skill = MakeSpellSkill(critChance: 0, castTime: 1.0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);
        skill.SkillModList.AddMod(new Mod
        {
            Name = "CullPercent", Type = ModType.Max, Value = 10, Source = "Test"
        });

        CalcOffence.Offence(player, skill);

        // CullMultiplier = 100 / (100 - 10) = 1.1111
        Assert.True(player.Output["CullMultiplier"] > 1);
        Assert.True(player.Output["CullingDPS"] > 0);
        Assert.True(player.Output["CombinedDPS"] > player.Output["TotalDPS"]);
    }

    [Fact]
    public void DPS_HitChance_100()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(100, player.Output["HitChance"]);
    }

    [Fact]
    public void DPS_EnemyBlock_ReducesHitChance()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, configureEnemyMods: e =>
        {
            e.NewMod("BlockChance", ModType.Base, 20, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(80, player.Output["HitChance"], 1);
        // TotalDPS reduced accordingly
        Assert.Equal(80, player.Output["TotalDPS"], 0);
    }

    [Fact]
    public void DPS_WithCritAndResist()
    {
        var skill = MakeSpellSkill(critChance: 50, castTime: 1.0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, configureEnemyMods: e =>
        {
            e.NewMod("ColdResist", ModType.Base, 50, "Test");
        });

        CalcOffence.Offence(player, skill);

        // nonCrit: 100 * 0.5 (resist) = 50; crit: 100 * 1.5 * 0.5 = 75
        // AverageHit = 50 * 0.5 + 75 * 0.5 = 62.5
        Assert.Equal(62.5, player.Output["AverageHit"], 0);
    }

    // ═══════════════════════════════════════════
    // Double/Triple Damage Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void DoubleDamage_IncreasesDPS()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DoubleDamageChance", ModType.Base, 50, "Test");
        });

        CalcOffence.Offence(player, skill);

        // ScaledDamageEffect = 1 + 0.5 = 1.5
        Assert.Equal(50, player.Output["DoubleDamageChance"]);
        Assert.Equal(150, player.Output["AverageHit"], 0);
    }

    [Fact]
    public void TripleDamage_OverridesDouble()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DoubleDamageChance", ModType.Base, 100, "Test");
            p.NewMod("TripleDamageChance", ModType.Base, 50, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(50, player.Output["TripleDamageChance"]);
        // Double reduced by triple overlap: 100 - 50*100/100 = 50
        Assert.Equal(50, player.Output["DoubleDamageChance"]);
        // ScaledDamageEffect = 1 + 0.5 + 1.0 = 2.5
        Assert.Equal(250, player.Output["AverageHit"], 0);
    }

    // ═══════════════════════════════════════════
    // Leech Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Leech_LifeLeech_FromDamage()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 1000, ["ColdMax"] = 1000,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DamageLifeLeech", ModType.Base, 5, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });

        CalcOffence.Offence(player, skill);

        // 1000 * 5/100 = 50 leech per hit
        Assert.Equal(50, player.Output["LifeLeech"], 0);
    }

    [Fact]
    public void Leech_GhostReaver_LifeToES()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 1000, ["ColdMax"] = 1000,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DamageLifeLeech", ModType.Base, 5, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
            p.NewMod("GhostReaver", ModType.Flag, true, "Test");
        });
        player.Output["EnergyShield"] = 500;

        CalcOffence.Offence(player, skill);

        // Ghost Reaver redirects life leech to ES
        Assert.Equal(0, player.Output["LifeLeech"], 0);
        Assert.True(player.Output["EnergyShieldLeech"] > 0);
    }

    [Fact]
    public void Leech_InstantLeech()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 1000, ["ColdMax"] = 1000,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DamageLifeLeech", ModType.Base, 5, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
            p.NewMod("InstantLifeLeech", ModType.Base, 100, "Test");
        });

        CalcOffence.Offence(player, skill);

        // All leech is instant
        Assert.Equal(0, player.Output["LifeLeech"], 1);
        Assert.Equal(50, player.Output["LifeLeechInstant"], 0);
    }

    [Fact]
    public void Leech_ManaLeech()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 1000, ["ColdMax"] = 1000,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DamageManaLeech", ModType.Base, 3, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });

        CalcOffence.Offence(player, skill);

        // 1000 * 3/100 = 30
        Assert.Equal(30, player.Output["ManaLeech"], 0);
    }

    [Fact]
    public void Leech_CappedPerInstance()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100000, ["ColdMax"] = 100000,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DamageLifeLeech", ModType.Base, 50, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });

        CalcOffence.Offence(player, skill);

        // Max life leech instance = 100, leech would be 50000 but capped at 100
        Assert.Equal(100, player.Output["LifeLeech"], 0);
    }

    [Fact]
    public void Leech_CannotLeechLife()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 1000, ["ColdMax"] = 1000,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DamageLifeLeech", ModType.Base, 5, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
            p.NewMod("CannotLeechLife", ModType.Flag, true, "Test", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["LifeLeech"], 0);
    }

    // ═══════════════════════════════════════════
    // Gain on Hit / Kill Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void GainOnHit_LifeOnHit()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("LifeOnHit", ModType.Base, 20, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(20, player.Output["LifeOnHit"]);
        Assert.True(player.Output["LifeOnHitRate"] > 0);
    }

    [Fact]
    public void GainOnHit_MineOrTrap_Zero()
    {
        var skill = MakeSpellSkill(critChance: 0,
            extraSkillData: new() { ["ColdMin"] = 100, ["ColdMax"] = 100 },
            extraFlags: new() { ["trap"] = true });
        skill.SkillFlags.Remove("selfCast");
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("LifeOnHit", ModType.Base, 20, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["LifeOnHit"]);
    }

    [Fact]
    public void GainOnKill_LifeOnKill()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("LifeOnKill", ModType.Base, 30, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(30, player.Output["LifeOnKill"]);
    }

    // ═══════════════════════════════════════════
    // IronWill / Transfiguration Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void StatBonus_IronWill_AddsStrDmgToSpell()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("IronWill", ModType.Flag, true, "Test");
        });
        // Set StrDmgBonus in output (normally computed by DoActorAttributes)
        player.Output["StrDmgBonus"] = 50;

        CalcOffence.Offence(player, skill);

        // 100 * (1 + 50/100) = 150
        Assert.Equal(150, player.Output["ColdHitAverage"], 0);
    }

    [Fact]
    public void StatBonus_TransfigurationOfMind()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("TransfigurationOfMind", ModType.Flag, true, "Test");
            p.NewMod("Mana", ModType.Inc, 100, "Test");
        });

        CalcOffence.Offence(player, skill);

        // bonus = floor(100 * 0.3) = 30% INC damage
        // 100 * (1 + 30/100) = 130
        Assert.Equal(130, player.Output["ColdHitAverage"], 0);
    }

    // ═══════════════════════════════════════════
    // CalcResistForType Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void CalcResistForType_BasicResist()
    {
        var enemyDB = new ModDB();
        enemyDB.NewMod("FireResist", ModType.Base, 40, "Test");

        double resist = CalcOffence.CalcResistForType("Fire", enemyDB, null);

        Assert.Equal(40, resist, 1);
    }

    [Fact]
    public void CalcResistForType_CappedByMaxResist()
    {
        var enemyDB = new ModDB();
        enemyDB.NewMod("FireResist", ModType.Base, 200, "Test");

        double resist = CalcOffence.CalcResistForType("Fire", enemyDB, null);

        Assert.Equal(MiscConstants.EnemyMaxResist, resist, 1);
    }

    [Fact]
    public void CalcResistForType_FlooredAtResistFloor()
    {
        var enemyDB = new ModDB();
        enemyDB.NewMod("ChaosResist", ModType.Base, -500, "Test");

        double resist = CalcOffence.CalcResistForType("Chaos", enemyDB, null);

        Assert.Equal(MiscConstants.ResistFloor, resist, 1);
    }

    [Fact]
    public void CalcResistForType_Override()
    {
        var enemyDB = new ModDB();
        enemyDB.NewMod("ColdResist", ModType.Override, 25, "Test");

        double resist = CalcOffence.CalcResistForType("Cold", enemyDB, null);

        Assert.Equal(25, resist, 1);
    }

    [Fact]
    public void CalcResistForType_ElementalResist()
    {
        var enemyDB = new ModDB();
        enemyDB.NewMod("ElementalResist", ModType.Base, 30, "Test");

        // Lightning is elemental, so includes ElementalResist
        double resist = CalcOffence.CalcResistForType("Lightning", enemyDB, null);

        Assert.Equal(30, resist, 1);
    }

    // ═══════════════════════════════════════════
    // Pipeline Integration Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Pipeline_FullCalculate_NullMainSkill_NoCrash()
    {
        // Without a skill cache, MainSkill is null, should not crash
        var build = new BuildData
        {
            Metadata = new BuildMetadata
            {
                ClassName = "Witch",
                AscendClassName = "None",
                Level = 1,
            },
        };

        var (player, _) = BuildPipeline.Calculate(build);

        Assert.False(player.Output.ContainsKey("CombinedDPS"));
    }

    [Fact]
    public void Pipeline_OffenceOutput_HitChanceIsSet()
    {
        var skill = MakeSpellSkill(critChance: 5, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.True(player.Output.ContainsKey("HitChance"));
        Assert.True(player.Output.ContainsKey("Speed"));
        Assert.True(player.Output.ContainsKey("CritChance"));
        Assert.True(player.Output.ContainsKey("CritMultiplier"));
        Assert.True(player.Output.ContainsKey("AverageHit"));
        Assert.True(player.Output.ContainsKey("TotalDPS"));
        Assert.True(player.Output.ContainsKey("CombinedDPS"));
    }

    [Fact]
    public void Pipeline_OffenceOutput_CritOutputs()
    {
        var skill = MakeSpellSkill(critChance: 10, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.True(player.Output["CritChance"] > 0);
        Assert.True(player.Output["CritMultiplier"] > 1);
        Assert.True(player.Output["CritEffect"] > 1);
    }

    [Fact]
    public void Pipeline_AddedDamageIncreasesDPS()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);
        CalcOffence.Offence(player, skill);
        double baseDPS = player.Output["TotalDPS"];

        // Now add more damage
        var skill2 = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player2, _) = MakeActors(skill2, p =>
        {
            p.NewMod("ColdMin", ModType.Base, 100, "AddedDamage", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
            p.NewMod("ColdMax", ModType.Base, 100, "AddedDamage", ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });
        CalcOffence.Offence(player2, skill2);

        Assert.True(player2.Output["TotalDPS"] > baseDPS);
    }

    [Fact]
    public void Pipeline_EnemyResistReducesDPS()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);
        CalcOffence.Offence(player, skill);
        double baseDPS = player.Output["TotalDPS"];

        var skill2 = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player2, _) = MakeActors(skill2, configureEnemyMods: e =>
        {
            e.NewMod("ColdResist", ModType.Base, 50, "Test");
        });
        CalcOffence.Offence(player2, skill2);

        Assert.True(player2.Output["TotalDPS"] < baseDPS);
    }

    // ═══════════════════════════════════════════
    // Additional Edge Cases
    // ═══════════════════════════════════════════

    [Fact]
    public void Repeats_DefaultIsOne()
    {
        var skill = MakeSpellSkill();
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(1, player.Output["Repeats"]);
    }

    [Fact]
    public void Repeats_WithRepeatCount()
    {
        var skill = MakeSpellSkill();
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("RepeatCount", ModType.Base, 2, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(3, player.Output["Repeats"]);
    }

    [Fact]
    public void DPS_ZeroBaseDamage_ZeroDPS()
    {
        var skill = MakeSpellSkill(critChance: 0);
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["TotalDPS"]);
        Assert.Equal(0, player.Output["AverageHit"]);
    }

    [Fact]
    public void DoubleDamage_CappedAt100()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DoubleDamageChance", ModType.Base, 200, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(100, player.Output["DoubleDamageChance"]);
    }

    [Fact]
    public void Crit_ZeroCritChance_NoCritEffect()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["CritChance"]);
        Assert.Equal(1, player.Output["CritEffect"], 4);
        Assert.Equal(100, player.Output["AverageHit"], 0);
    }

    [Fact]
    public void Resist_ResistCappedByMaxResist()
    {
        // Enemy max resist is typically 75
        var enemyDB = new ModDB();
        enemyDB.NewMod("FireResist", ModType.Base, 80, "Test");

        double resist = CalcOffence.CalcResistForType("Fire", enemyDB, null);

        Assert.True(resist <= MiscConstants.EnemyMaxResist);
    }

    [Fact]
    public void DPS_IncDPS_MultipliesDPS()
    {
        var skill = MakeSpellSkill(critChance: 0, castTime: 1.0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);
        skill.SkillModList.AddMod(new Mod
        {
            Name = "DPS", Type = ModType.Inc, Value = 100, Source = "Test"
        });

        CalcOffence.Offence(player, skill);

        // dpsMultiplier = 1 * (1 + 100/100) * 1 = 2
        Assert.Equal(200, player.Output["TotalDPS"], 0);
    }

    [Fact]
    public void Leech_ESLeech()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 1000, ["ColdMax"] = 1000,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("DamageEnergyShieldLeech", ModType.Base, 2, "Test",
                ModFlag.Spell | ModFlag.Cast | ModFlag.Hit);
        });
        player.Output["EnergyShield"] = 500;

        CalcOffence.Offence(player, skill);

        // 1000 * 2/100 = 20
        Assert.Equal(20, player.Output["EnergyShieldLeech"], 0);
    }

    [Fact]
    public void GainOnHit_EnergyShieldOnHit()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("EnergyShieldOnHit", ModType.Base, 10, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(10, player.Output["EnergyShieldOnHit"]);
    }

    [Fact]
    public void GainOnHit_ManaOnHit()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("ManaOnHit", ModType.Base, 15, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(15, player.Output["ManaOnHit"]);
    }

    [Fact]
    public void GainOnKill_EnergyShieldOnKill()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("EnergyShieldOnKill", ModType.Base, 25, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(25, player.Output["EnergyShieldOnKill"]);
    }

    [Fact]
    public void GainOnKill_ManaOnKill()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill, p =>
        {
            p.NewMod("ManaOnKill", ModType.Base, 40, "Test");
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(40, player.Output["ManaOnKill"]);
    }

    [Fact]
    public void DealNoDamage_AllTypes_ZeroDPS()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);
        skill.SkillModList.AddMod(new Mod
        {
            Name = "DealNoDamage", Type = ModType.Flag, Value = true, Source = "Test"
        });

        CalcOffence.Offence(player, skill);

        Assert.Equal(0, player.Output["AverageHit"]);
        Assert.Equal(0, player.Output["TotalDPS"]);
    }

    [Fact]
    public void CullMultiplier_NoCull_IsOne()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(1, player.Output["CullMultiplier"], 4);
        Assert.Equal(0, player.Output["CullingDPS"], 1);
    }

    [Fact]
    public void ReservationDpsMultiplier_Default_IsOne()
    {
        var skill = MakeSpellSkill(critChance: 0, extraSkillData: new()
        {
            ["ColdMin"] = 100, ["ColdMax"] = 100,
        });
        var (player, _) = MakeActors(skill);

        CalcOffence.Offence(player, skill);

        Assert.Equal(1, player.Output["ReservationDpsMultiplier"], 4);
    }
}
