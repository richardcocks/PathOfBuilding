using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Builds the skill modifier list and config for an ActiveSkill.
/// Merges skill+support mods, creates skillCfg, and extracts skillData.
/// Ported from CalcActiveSkill.lua buildActiveSkillModList (lines 227-650).
/// </summary>
public static class SkillModListBuilder
{
    /// <summary>
    /// Build the complete modifier list for an active skill.
    /// </summary>
    public static void Build(ActiveSkill activeSkill, ModDB playerModDB,
        SkillDataCache skillCache)
    {
        var skillTypes = activeSkill.SkillTypes;
        var skillFlags = activeSkill.SkillFlags;

        // Handle multipart skills
        var parts = activeSkill.GrantedEffect.Parts;
        if (parts.Count > 1)
        {
            activeSkill.SkillPart = Math.Clamp(activeSkill.SkillPart, 1, parts.Count);
            var part = parts[activeSkill.SkillPart - 1]; // 1-based to 0-based
            activeSkill.SkillPartName = part.Name;
        }

        // Build skillModFlags from skillFlags
        var skillModFlags = BuildSkillModFlags(skillFlags);

        // Build skillKeywordFlags from skillTypes and skillFlags
        var skillKeywordFlags = BuildSkillKeywordFlags(skillTypes, skillFlags);

        // Create skillCfg
        string skillName = activeSkill.GrantedEffect.Name;
        // Strip "Vaal " prefix so modifiers targeting a skill also apply to its Vaal version
        if (skillName.StartsWith("Vaal ", StringComparison.Ordinal))
            skillName = skillName[5..];

        activeSkill.SkillCfg = new ModConfig
        {
            Flags = skillModFlags,
            KeywordFlags = skillKeywordFlags,
            SkillName = skillName,
            SkillPart = activeSkill.SkillPart,
            SkillTypes = new HashSet<SkillType>(skillTypes.Keys),
            SkillCond = new Dictionary<string, bool>(),
        };

        // Create skill mod list with player ModDB as parent
        var skillModList = new ModList(playerModDB);
        activeSkill.SkillModList = skillModList;

        // Merge support mods first (supports in effectList after index 0)
        for (int i = 1; i < activeSkill.EffectList.Count; i++)
        {
            var effect = activeSkill.EffectList[i];
            MergeSkillInstanceMods(skillModList, effect.GemInstance, effect.GrantedEffect,
                skillCache, effect.GrantedEffect.Name);
        }

        // Merge active skill mods (first effect in list)
        if (activeSkill.EffectList.Count > 0)
        {
            var activeEffect = activeSkill.EffectList[0];
            MergeSkillInstanceMods(skillModList, activeEffect.GemInstance,
                activeEffect.GrantedEffect, skillCache, activeEffect.GrantedEffect.Name);

            // Add level-specific data to skillData
            if (activeEffect.GrantedEffect.Levels.TryGetValue(
                    activeEffect.GemInstance.Level, out var levelData))
            {
                if (levelData.CritChance > 0)
                    activeSkill.SkillData["CritChance"] = levelData.CritChance;
                if (levelData.Cooldown > 0)
                    activeSkill.SkillData["cooldown"] = levelData.Cooldown;
                if (levelData.Duration > 0)
                    activeSkill.SkillData["duration"] = levelData.Duration;
                if (levelData.StoredUses > 0)
                    activeSkill.SkillData["storedUses"] = levelData.StoredUses;
                if (levelData.BaseMultiplier > 0)
                    activeSkill.SkillData["baseMultiplier"] = levelData.BaseMultiplier;
                if (levelData.AttackSpeedMultiplier != 0)
                    activeSkill.SkillData["attackSpeedMultiplier"] = levelData.AttackSpeedMultiplier;
                if (levelData.DamageEffectiveness > 0)
                    activeSkill.SkillData["damageEffectiveness"] = levelData.DamageEffectiveness;
                if (levelData.ManaReservationPercent > 0)
                    activeSkill.SkillData["manaReservationPercent"] = levelData.ManaReservationPercent;
                if (levelData.ManaReservationFlat > 0)
                    activeSkill.SkillData["manaReservationFlat"] = levelData.ManaReservationFlat;
                if (levelData.LifeReservationPercent > 0)
                    activeSkill.SkillData["lifeReservationPercent"] = levelData.LifeReservationPercent;
                if (levelData.LifeReservationFlat > 0)
                    activeSkill.SkillData["lifeReservationFlat"] = levelData.LifeReservationFlat;
            }
        }

        // Extract skillData from SkillData LIST mods
        ExtractSkillData(activeSkill, playerModDB, skillModList);
    }

    /// <summary>
    /// Build ModFlag bitmask from skill flag dictionary.
    /// Ported from CalcActiveSkill.lua lines 334-354.
    /// </summary>
    internal static ModFlag BuildSkillModFlags(Dictionary<string, bool> skillFlags)
    {
        var flags = ModFlag.None;

        if (skillFlags.ContainsKey("hit"))
            flags |= ModFlag.Hit;

        if (skillFlags.ContainsKey("attack"))
        {
            flags |= ModFlag.Attack;
        }
        else
        {
            flags |= ModFlag.Cast;
            if (skillFlags.ContainsKey("spell"))
                flags |= ModFlag.Spell;
        }

        if (skillFlags.ContainsKey("melee"))
        {
            flags |= ModFlag.Melee;
        }
        else if (skillFlags.ContainsKey("projectile"))
        {
            flags |= ModFlag.Projectile;
        }

        if (skillFlags.ContainsKey("area"))
            flags |= ModFlag.Area;

        return flags;
    }

    /// <summary>
    /// Build KeywordFlag bitmask from skill types and skill flags.
    /// Ported from CalcActiveSkill.lua lines 356-411.
    /// </summary>
    internal static KeywordFlag BuildSkillKeywordFlags(
        Dictionary<SkillType, bool> skillTypes, Dictionary<string, bool> skillFlags)
    {
        var flags = KeywordFlag.None;

        if (skillFlags.ContainsKey("hit"))
            flags |= KeywordFlag.Hit;

        if (skillTypes.ContainsKey(SkillType.Aura))
            flags |= KeywordFlag.Aura;
        if (skillTypes.ContainsKey(SkillType.AppliesCurse))
            flags |= KeywordFlag.Curse;
        if (skillTypes.ContainsKey(SkillType.Warcry))
            flags |= KeywordFlag.Warcry;
        if (skillTypes.ContainsKey(SkillType.Movement))
            flags |= KeywordFlag.Movement;
        if (skillTypes.ContainsKey(SkillType.Vaal))
            flags |= KeywordFlag.Vaal;
        if (skillTypes.ContainsKey(SkillType.Lightning))
            flags |= KeywordFlag.Lightning;
        if (skillTypes.ContainsKey(SkillType.Cold))
            flags |= KeywordFlag.Cold;
        if (skillTypes.ContainsKey(SkillType.Fire))
            flags |= KeywordFlag.Fire;
        if (skillTypes.ContainsKey(SkillType.Chaos))
            flags |= KeywordFlag.Chaos;
        if (skillTypes.ContainsKey(SkillType.Physical))
            flags |= KeywordFlag.Physical;

        if (skillFlags.ContainsKey("brand"))
            flags |= KeywordFlag.Brand;

        if (skillFlags.ContainsKey("totem"))
            flags |= KeywordFlag.Totem;
        else if (skillFlags.ContainsKey("trap"))
            flags |= KeywordFlag.Trap;
        else if (skillFlags.ContainsKey("mine"))
            flags |= KeywordFlag.Mine;

        if (skillTypes.ContainsKey(SkillType.Attack))
            flags |= KeywordFlag.Attack;
        if (skillTypes.ContainsKey(SkillType.Spell) && !skillFlags.ContainsKey("cast"))
            flags |= KeywordFlag.Spell;

        return flags;
    }

    /// <summary>
    /// Merge stat-map mods from a skill effect into the mod list.
    /// Ported from CalcActiveSkill.lua mergeSkillInstanceMods (lines 52-78).
    /// </summary>
    internal static void MergeSkillInstanceMods(
        ModList modList, GemInstanceData gemInstance,
        SkillDefinition skill, SkillDataCache skillCache,
        string source)
    {
        // Build instance stats
        var stats = SkillInstanceStats.Build(gemInstance, skill);

        // Map stats through the stat map
        foreach (var (statName, statValue) in stats)
        {
            // Check skill-specific stat map first, then global
            StatMapEntry? mapEntry = null;
            if (skill.StatMap.TryGetValue(statName, out var skillMap))
                mapEntry = skillMap;
            else if (skillCache.SkillStatMap.TryGetValue(statName, out var globalMap))
                mapEntry = globalMap;

            if (mapEntry == null)
                continue;

            // Calculate the effective value using the map's div/mult/base
            double effectiveValue = statValue * (mapEntry.Mult ?? 1) / (mapEntry.Div ?? 1) + (mapEntry.Base ?? 0);

            // Apply to each mod in the entry
            foreach (var mod in mapEntry.Mods)
            {
                var newMod = new Mod
                {
                    Name = mod.Name,
                    Type = mod.Type,
                    Value = mod.Value.Kind == ModValueKind.Number ? effectiveValue : mod.Value,
                    Flags = mod.Flags,
                    KeywordFlags = mod.KeywordFlags,
                    Source = source,
                    Tags = mod.Tags,
                };
                modList.AddMod(newMod);
            }
        }

        // Add base mods from the skill definition
        foreach (var mod in skill.BaseMods)
        {
            modList.AddMod(mod);
        }
    }

    /// <summary>
    /// Extract SkillData from LIST mods on both the player ModDB and skill mod list.
    /// Ported from CalcActiveSkill.lua lines 624-630.
    /// </summary>
    private static void ExtractSkillData(ActiveSkill activeSkill, ModDB playerModDB,
        ModList skillModList)
    {
        var cfg = activeSkill.SkillCfg;

        // From player ModDB
        var playerSkillData = playerModDB.List(cfg, "SkillData");
        foreach (var value in playerSkillData)
        {
            ExtractSkillDataValue(activeSkill, value);
        }

        // From skill mod list
        var skillData = skillModList.List(cfg, "SkillData");
        foreach (var value in skillData)
        {
            ExtractSkillDataValue(activeSkill, value);
        }
    }

    /// <summary>
    /// Extract a single SkillData value from a LIST mod value.
    /// SkillData mods carry complex values with key/value fields.
    /// </summary>
    private static void ExtractSkillDataValue(ActiveSkill activeSkill, ModValue value)
    {
        if (value.Kind != ModValueKind.Complex || value.Complex == null)
            return;

        // Handle Dictionary<string, object?> format (key/value pairs)
        if (value.Complex is Dictionary<string, object?> dict)
        {
            if (dict.TryGetValue("key", out var keyObj) && keyObj is string key &&
                dict.TryGetValue("value", out var valObj))
            {
                double numVal = valObj switch
                {
                    double d => d,
                    int i => i,
                    float f => f,
                    bool b => b ? 1 : 0,
                    _ => 0,
                };
                activeSkill.SkillData[key] = numVal;
            }
        }
    }
}
