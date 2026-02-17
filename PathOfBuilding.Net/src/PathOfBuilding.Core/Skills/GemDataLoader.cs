using System.Globalization;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Loads gem definitions from Gems.lua.
/// </summary>
public static class GemDataLoader
{
    public static Dictionary<string, GemDefinition> LoadFromFile(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return LoadFromText(text);
    }

    public static Dictionary<string, GemDefinition> LoadFromText(string lua)
    {
        var parsed = LuaSkillParser.ParseReturnTable(lua);
        if (parsed is not Dictionary<string, object?> topTable)
            throw new FormatException("Gems.lua must return a table");

        var result = new Dictionary<string, GemDefinition>(topTable.Count);

        foreach (var (id, entry) in topTable)
        {
            if (entry is not Dictionary<string, object?> gemData)
                continue;

            var gem = ParseGem(id, gemData);
            result[id] = gem;
        }

        return result;
    }

    private static GemDefinition ParseGem(string id, Dictionary<string, object?> d)
    {
        return new GemDefinition
        {
            Id = id,
            Name = GetString(d, "name") ?? id,
            BaseTypeName = GetString(d, "baseTypeName"),
            GameId = GetString(d, "gameId") ?? id,
            VariantId = GetString(d, "variantId") ?? "",
            GrantedEffectId = GetString(d, "grantedEffectId") ?? "",
            SecondaryGrantedEffectId = GetString(d, "secondaryGrantedEffectId"),
            VaalGem = GetBool(d, "vaalGem"),
            Tags = ParseBoolDict(d, "tags"),
            TagString = GetString(d, "tagString"),
            ReqStr = GetInt(d, "reqStr"),
            ReqDex = GetInt(d, "reqDex"),
            ReqInt = GetInt(d, "reqInt"),
            NaturalMaxLevel = GetInt(d, "naturalMaxLevel", 20),
        };
    }

    private static string? GetString(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;
        if (v is string s) return s;
        if (v is DottedIdentifier di && string.IsNullOrEmpty(di.Namespace)) return di.Name;
        return v.ToString();
    }

    private static bool GetBool(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return false;
        return v is true;
    }

    private static int GetInt(Dictionary<string, object?> d, string key, int defaultValue = 0)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return defaultValue;
        if (v is double dv) return (int)dv;
        return defaultValue;
    }

    private static Dictionary<string, bool> ParseBoolDict(Dictionary<string, object?> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return new();
        if (v is not Dictionary<string, object?> dict) return new();

        var result = new Dictionary<string, bool>();
        foreach (var (k, val) in dict)
        {
            if (val is true)
                result[k] = true;
        }
        return result;
    }
}
