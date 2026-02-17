namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Definition of a skill gem from Gems.lua.
/// Maps gem metadata ID to granted effect IDs and requirements.
/// </summary>
public sealed class GemDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? BaseTypeName { get; init; }
    public required string GameId { get; init; }
    public required string VariantId { get; init; }
    public required string GrantedEffectId { get; init; }
    public string? SecondaryGrantedEffectId { get; init; }
    public bool VaalGem { get; init; }
    public Dictionary<string, bool> Tags { get; init; } = new();
    public string? TagString { get; init; }
    public int ReqStr { get; init; }
    public int ReqDex { get; init; }
    public int ReqInt { get; init; }
    public int NaturalMaxLevel { get; init; } = 20;
}
