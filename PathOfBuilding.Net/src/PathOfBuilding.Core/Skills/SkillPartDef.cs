namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Definition of a skill part (for multi-part skills like Fireball projectile vs explosion).
/// </summary>
public sealed class SkillPartDef
{
    public required string Name { get; init; }
    public Dictionary<string, bool> Flags { get; init; } = new();
}
