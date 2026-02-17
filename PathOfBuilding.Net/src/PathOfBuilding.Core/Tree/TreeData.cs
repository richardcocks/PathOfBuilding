namespace PathOfBuilding.Core.Tree;

/// <summary>
/// Parsed passive skill tree data for a specific game version.
/// Contains all nodes indexed by their ID.
/// </summary>
public class TreeData
{
    /// <summary>All nodes in the tree indexed by node ID.</summary>
    public Dictionary<int, TreeNode> Nodes { get; init; } = new();

    /// <summary>The tree version string (e.g., "3_13", "3_25").</summary>
    public string Version { get; init; } = "";
}
