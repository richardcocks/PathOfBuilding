namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Determines whether a support gem can support an active skill.
/// Evaluates postfix boolean expressions for requireSkillTypes / excludeSkillTypes.
/// Ported from CalcTools.lua canGrantedEffectSupportActiveSkill and doesTypeExpressionMatch.
/// </summary>
public static class SupportCompatibility
{
    /// <summary>
    /// Check if a support skill definition can support the given active skill.
    /// </summary>
    public static bool CanSupport(SkillDefinition support, ActiveSkill activeSkill)
    {
        if (!support.Support)
            return false;

        // Check excludeSkillTypes: if the expression matches, the support is excluded
        if (support.ExcludeSkillTypes.Count > 0 &&
            DoesTypeExpressionMatch(support.ExcludeSkillTypes, activeSkill.SkillTypes))
            return false;

        // Check requireSkillTypes: must match if present
        if (support.RequireSkillTypes.Count > 0 &&
            !DoesTypeExpressionMatch(support.RequireSkillTypes, activeSkill.SkillTypes))
            return false;

        return true;
    }

    /// <summary>
    /// Evaluate a postfix boolean expression of skill types.
    /// Uses SkillType.OR, SkillType.AND, SkillType.NOT as operators.
    /// Terminal values push skillTypes[type] (true if present, false otherwise).
    /// Returns true if any element remaining on the stack is truthy.
    /// Ported from CalcTools.lua doesTypeExpressionMatch.
    /// </summary>
    public static bool DoesTypeExpressionMatch(
        List<SkillType> checkTypes, Dictionary<SkillType, bool> skillTypes,
        Dictionary<SkillType, bool>? minionTypes = null)
    {
        var stack = new List<bool>();

        foreach (var skillType in checkTypes)
        {
            if (skillType == SkillType.OR)
            {
                if (stack.Count >= 2)
                {
                    bool other = stack[^1];
                    stack.RemoveAt(stack.Count - 1);
                    stack[^1] = stack[^1] || other;
                }
            }
            else if (skillType == SkillType.AND)
            {
                if (stack.Count >= 2)
                {
                    bool other = stack[^1];
                    stack.RemoveAt(stack.Count - 1);
                    stack[^1] = stack[^1] && other;
                }
            }
            else if (skillType == SkillType.NOT)
            {
                if (stack.Count >= 1)
                {
                    stack[^1] = !stack[^1];
                }
            }
            else
            {
                bool hasType = skillTypes.ContainsKey(skillType) ||
                               (minionTypes != null && minionTypes.ContainsKey(skillType));
                stack.Add(hasType);
            }
        }

        // Return true if any stack element is truthy
        foreach (var val in stack)
        {
            if (val)
                return true;
        }

        return false;
    }
}
