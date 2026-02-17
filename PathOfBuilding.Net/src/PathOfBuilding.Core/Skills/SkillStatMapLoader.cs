using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Loads the global stat→mod mapping table from SkillStatMap.lua.
/// </summary>
public static class SkillStatMapLoader
{
    public static Dictionary<string, StatMapEntry> LoadFromFile(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return LoadFromText(text);
    }

    public static Dictionary<string, StatMapEntry> LoadFromText(string lua)
    {
        var parsed = LuaSkillParser.ParseReturnTable(lua);
        if (parsed is not Dictionary<string, object?> topTable)
            throw new FormatException("SkillStatMap.lua must return a table");

        var result = new Dictionary<string, StatMapEntry>(topTable.Count);

        foreach (var (stat, entry) in topTable)
        {
            if (entry is Dictionary<string, object?> entryDict)
            {
                // Mixed table: has div/mult/base as named keys + mods as numeric keys
                result[stat] = SkillDataLoader.ParseStatMapEntry(entryDict);
            }
            else if (entry is List<object?> entryList)
            {
                // Pure list: only mod calls, no div/mult/base
                var mods = new List<Mod>();
                foreach (var item in entryList)
                {
                    var resolved = LuaDslInterpreter.Resolve(item);
                    if (resolved is Mod mod)
                        mods.Add(mod);
                }
                result[stat] = new StatMapEntry { Mods = mods };
            }
        }

        return result;
    }
}
