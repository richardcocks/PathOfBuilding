namespace PathOfBuilding.Core.Tree;

/// <summary>
/// Caches loaded tree data by version to avoid re-parsing large tree.lua files.
/// Also manages the base path for tree data files.
/// </summary>
public class TreeDataCache
{
    private readonly Dictionary<string, TreeData> _cache = new();

    /// <summary>
    /// Base directory containing tree version subdirectories (e.g., "3_13/tree.lua").
    /// </summary>
    public string? TreeDataBasePath { get; set; }

    /// <summary>
    /// Get tree data for a given version string (e.g., "3_13").
    /// Loads from disk on first access, caches for subsequent requests.
    /// Returns null if the version is not found or no base path is configured.
    /// </summary>
    public TreeData? GetTreeData(string version)
    {
        if (string.IsNullOrEmpty(version))
            return null;

        if (_cache.TryGetValue(version, out var cached))
            return cached;

        var data = LoadTreeData(version);
        if (data != null)
            _cache[version] = data;

        return data;
    }

    /// <summary>
    /// Pre-load tree data from a TreeData instance (useful for testing).
    /// </summary>
    public void SetTreeData(string version, TreeData data)
    {
        _cache[version] = data;
    }

    private TreeData? LoadTreeData(string version)
    {
        if (string.IsNullOrEmpty(TreeDataBasePath))
            return null;

        var filePath = Path.Combine(TreeDataBasePath, version, "tree.lua");
        if (!File.Exists(filePath))
            return null;

        return TreeDataLoader.LoadFromFile(filePath, version);
    }
}
