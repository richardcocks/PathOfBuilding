using System.Globalization;

namespace PathOfBuilding.Core.Tree;

/// <summary>
/// Loads passive skill tree data from tree.lua files.
/// Parses the Lua table structure and extracts node definitions.
/// </summary>
public static class TreeDataLoader
{
    /// <summary>
    /// Load tree data from a tree.lua file path.
    /// </summary>
    public static TreeData LoadFromFile(string filePath, string version = "")
    {
        var parsed = LuaTableParser.ParseFile(filePath);
        return BuildTreeData(parsed, version);
    }

    /// <summary>
    /// Load tree data from a Lua table string.
    /// </summary>
    public static TreeData LoadFromString(string lua, string version = "")
    {
        var parsed = LuaTableParser.Parse(lua);
        return BuildTreeData(parsed, version);
    }

    private static TreeData BuildTreeData(object? parsed, string version)
    {
        if (parsed is not Dictionary<string, object?> root)
            throw new FormatException("Tree data root must be a table");

        var nodes = new Dictionary<int, TreeNode>();

        if (root.TryGetValue("nodes", out var nodesObj) && nodesObj is Dictionary<string, object?> nodesDict)
        {
            foreach (var (key, value) in nodesDict)
            {
                if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int nodeId))
                    continue;

                if (value is not Dictionary<string, object?> nodeDict)
                    continue;

                var node = ParseNode(nodeId, nodeDict);
                nodes[nodeId] = node;
            }
        }

        return new TreeData
        {
            Nodes = nodes,
            Version = version,
        };
    }

    private static TreeNode ParseNode(int nodeId, Dictionary<string, object?> dict)
    {
        var stats = new List<string>();
        if (dict.TryGetValue("stats", out var statsObj) && statsObj is List<object?> statsList)
        {
            foreach (var stat in statsList)
            {
                if (stat is string s)
                    stats.Add(s);
            }
        }

        Dictionary<int, List<string>>? masteryEffects = null;
        if (dict.TryGetValue("masteryEffects", out var meObj) && meObj is List<object?> meList)
        {
            masteryEffects = new Dictionary<int, List<string>>();
            foreach (var entry in meList)
            {
                if (entry is not Dictionary<string, object?> meDict)
                    continue;

                int effectId = GetInt(meDict, "effect");
                var effectStats = new List<string>();
                if (meDict.TryGetValue("stats", out var esObj) && esObj is List<object?> esList)
                {
                    foreach (var es in esList)
                    {
                        if (es is string s)
                            effectStats.Add(s);
                    }
                }
                masteryEffects[effectId] = effectStats;
            }
        }

        return new TreeNode
        {
            Id = nodeId,
            Name = GetString(dict, "name"),
            Stats = stats,
            IsNotable = GetBool(dict, "isNotable"),
            IsKeystone = GetBool(dict, "isKeystone"),
            IsJewelSocket = GetBool(dict, "isJewelSocket"),
            IsMastery = GetBool(dict, "isMastery"),
            AscendancyName = dict.TryGetValue("ascendancyName", out var asc) ? asc as string : null,
            Group = GetInt(dict, "group"),
            MasteryEffects = masteryEffects,
        };
    }

    private static string GetString(Dictionary<string, object?> dict, string key)
        => dict.TryGetValue(key, out var val) && val is string s ? s : "";

    private static bool GetBool(Dictionary<string, object?> dict, string key)
        => dict.TryGetValue(key, out var val) && val is true;

    private static int GetInt(Dictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var val))
        {
            if (val is double d)
                return (int)d;
            if (val is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                return i;
        }
        return 0;
    }
}
