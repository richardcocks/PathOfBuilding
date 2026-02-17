namespace PathOfBuilding.Core.Tree;

/// <summary>
/// A single node in the passive skill tree.
/// Contains the node's stats (modifier text) and metadata.
/// </summary>
public class TreeNode
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public List<string> Stats { get; init; } = new();
    public bool IsNotable { get; init; }
    public bool IsKeystone { get; init; }
    public bool IsJewelSocket { get; init; }
    public bool IsMastery { get; init; }
    public string? AscendancyName { get; init; }
    public int Group { get; init; }

    /// <summary>
    /// Mastery effects available on this node (only for mastery nodes in 3.16+).
    /// Maps effect ID → list of stat description strings.
    /// </summary>
    public Dictionary<int, List<string>>? MasteryEffects { get; init; }
}
