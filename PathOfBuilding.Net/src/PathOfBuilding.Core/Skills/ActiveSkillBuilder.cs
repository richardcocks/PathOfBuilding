using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Creates ActiveSkill instances from socket groups in a build.
/// Resolves gems, filters supports, and builds the skill list.
/// Ported from CalcSetup.lua socket group processing and CalcActiveSkill.lua createActiveSkill.
/// </summary>
public static class ActiveSkillBuilder
{
    /// <summary>
    /// Build all active skills from the build's socket groups.
    /// Populates player.ActiveSkillList and sets player.MainSkill.
    /// </summary>
    public static void BuildFromSocketGroups(
        BuildData build, Actor player, SkillDataCache skillCache)
    {
        // Find socket groups from all skill sets
        var socketGroups = new List<SocketGroupData>();
        foreach (var skillSet in build.SkillSets)
        {
            socketGroups.AddRange(skillSet.SocketGroups);
        }

        if (socketGroups.Count == 0)
            return;

        int mainSocketGroupIndex = build.Metadata.MainSocketGroup; // 1-based

        for (int groupIndex = 0; groupIndex < socketGroups.Count; groupIndex++)
        {
            var group = socketGroups[groupIndex];
            if (!group.Enabled)
                continue;

            // Separate support gems from active gems
            var supportEffects = new List<ActiveSkillEffect>();
            var activeGems = new List<(GemInstanceData gem, SkillDefinition skill, GemDefinition? gemDef)>();

            foreach (var gem in group.Gems)
            {
                if (!gem.Enabled)
                    continue;

                var (skill, gemDef) = ResolveGem(gem, skillCache);
                if (skill == null)
                    continue;

                if (skill.Support)
                {
                    supportEffects.Add(new ActiveSkillEffect
                    {
                        GrantedEffect = skill,
                        GemInstance = gem,
                        GemData = gemDef,
                    });
                }
                else
                {
                    activeGems.Add((gem, skill, gemDef));
                }
            }

            // Create an ActiveSkill for each active gem in the group
            int activeIndex = 0;
            foreach (var (gem, skill, gemDef) in activeGems)
            {
                activeIndex++;

                var activeSkill = CreateActiveSkill(skill, gem, gemDef, supportEffects, player, group);

                player.ActiveSkillList.Add(activeSkill);

                // Select main skill from the main socket group
                if (groupIndex + 1 == mainSocketGroupIndex &&
                    activeIndex == group.MainActiveSkill)
                {
                    player.MainSkill = activeSkill;
                }
            }
        }

        // Fallback: if no main skill was set, use the first active skill
        if (player.MainSkill == null && player.ActiveSkillList.Count > 0)
            player.MainSkill = player.ActiveSkillList[0];

        // Set MainSkillName for downstream use
        if (player.MainSkill != null)
            player.MainSkillName = player.MainSkill.GrantedEffect.Name;
    }

    /// <summary>
    /// Create an ActiveSkill from an active gem with support filtering.
    /// Implements the two-pass support cascading from CalcActiveSkill.lua.
    /// </summary>
    internal static ActiveSkill CreateActiveSkill(
        SkillDefinition skill, GemInstanceData gem, GemDefinition? gemDef,
        List<ActiveSkillEffect> supportEffects, Actor player, SocketGroupData group)
    {
        // Copy skill types and base flags from the skill definition
        var skillTypes = new Dictionary<SkillType, bool>(skill.SkillTypes);
        var skillFlags = new Dictionary<string, bool>(skill.BaseFlags);

        // Set hit flag based on skill types
        if (!skillFlags.ContainsKey("hit"))
        {
            if (skillTypes.ContainsKey(SkillType.Attack) ||
                skillTypes.ContainsKey(SkillType.Damage) ||
                skillTypes.ContainsKey(SkillType.Projectile))
            {
                skillFlags["hit"] = true;
            }
        }

        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            GemData = gemDef,
            SkillTypes = skillTypes,
            SkillFlags = skillFlags,
            Actor = player,
            SocketGroup = group,
            SkillPart = gem.SkillPart ?? 1,
        };

        // Add the active effect as first in the effect list
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
            GemData = gemDef,
        });

        // Pass 1: Add skill types from compatible supports
        var rejectedIndices = new List<int>();

        for (int i = 0; i < supportEffects.Count; i++)
        {
            var support = supportEffects[i];
            if (SupportCompatibility.CanSupport(support.GrantedEffect, activeSkill))
            {
                foreach (var addType in support.GrantedEffect.AddSkillTypes)
                {
                    activeSkill.SkillTypes[addType] = true;
                }
            }
            else
            {
                rejectedIndices.Add(i);
            }
        }

        // Cascading loop: retry rejected supports until stable
        bool addedNew;
        do
        {
            addedNew = false;
            for (int j = rejectedIndices.Count - 1; j >= 0; j--)
            {
                int idx = rejectedIndices[j];
                var support = supportEffects[idx];
                if (SupportCompatibility.CanSupport(support.GrantedEffect, activeSkill))
                {
                    addedNew = true;
                    rejectedIndices.RemoveAt(j);
                    foreach (var addType in support.GrantedEffect.AddSkillTypes)
                    {
                        activeSkill.SkillTypes[addType] = true;
                    }
                }
            }
        }
        while (addedNew);

        // Pass 2: Add all compatible supports to the effect list and flags
        foreach (var support in supportEffects)
        {
            if (SupportCompatibility.CanSupport(support.GrantedEffect, activeSkill))
            {
                activeSkill.EffectList.Add(support);

                // Add flags from support (e.g., Remote Mine adds 'mine')
                if (support.GrantedEffect.BaseFlags.Count > 0)
                {
                    foreach (var (flag, value) in support.GrantedEffect.BaseFlags)
                    {
                        if (value)
                            activeSkill.SkillFlags[flag] = true;
                    }
                }
            }
        }

        return activeSkill;
    }

    /// <summary>
    /// Resolve a GemInstanceData to its SkillDefinition and optional GemDefinition.
    /// Tries SkillId first, then falls back to GemId → GemDefinition.GrantedEffectId.
    /// </summary>
    internal static (SkillDefinition? skill, GemDefinition? gem) ResolveGem(
        GemInstanceData gem, SkillDataCache skillCache)
    {
        // Try direct skill ID lookup
        if (!string.IsNullOrEmpty(gem.SkillId))
        {
            var skill = skillCache.GetSkillById(gem.SkillId);
            if (skill != null)
            {
                // Also try to find the gem definition
                GemDefinition? gemDef = !string.IsNullOrEmpty(gem.GemId)
                    ? skillCache.GetGemById(gem.GemId)
                    : null;
                return (skill, gemDef);
            }
        }

        // Fall back to GemId → GemDefinition → GrantedEffectId → SkillDefinition
        if (!string.IsNullOrEmpty(gem.GemId))
        {
            var gemDef = skillCache.GetGemById(gem.GemId);
            if (gemDef != null)
            {
                var skill = skillCache.GetSkillById(gemDef.GrantedEffectId);
                return (skill, gemDef);
            }
        }

        return (null, null);
    }
}
