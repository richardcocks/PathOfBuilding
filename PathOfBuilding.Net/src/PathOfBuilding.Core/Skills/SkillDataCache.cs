namespace PathOfBuilding.Core.Skills;

/// <summary>
/// Lazy-loading cache for skill, gem, and stat map data.
/// Supports testing via Set* methods to preload data without files.
/// </summary>
public class SkillDataCache
{
    private Dictionary<string, SkillDefinition>? _skills;
    private Dictionary<string, GemDefinition>? _gems;
    private Dictionary<string, StatMapEntry>? _skillStatMap;

    /// <summary>
    /// Base path to the src/Data directory. Used for lazy loading from files.
    /// </summary>
    public string? DataBasePath { get; set; }

    public Dictionary<string, SkillDefinition> Skills
    {
        get
        {
            if (_skills == null)
            {
                if (DataBasePath == null)
                    throw new InvalidOperationException("DataBasePath not set and Skills not preloaded");
                var skillsDir = Path.Combine(DataBasePath, "Skills");
                _skills = SkillDataLoader.LoadAllFromDirectory(skillsDir);
            }
            return _skills;
        }
    }

    public Dictionary<string, GemDefinition> Gems
    {
        get
        {
            if (_gems == null)
            {
                if (DataBasePath == null)
                    throw new InvalidOperationException("DataBasePath not set and Gems not preloaded");
                var gemsFile = Path.Combine(DataBasePath, "Gems.lua");
                _gems = GemDataLoader.LoadFromFile(gemsFile);
            }
            return _gems;
        }
    }

    public Dictionary<string, StatMapEntry> SkillStatMap
    {
        get
        {
            if (_skillStatMap == null)
            {
                if (DataBasePath == null)
                    throw new InvalidOperationException("DataBasePath not set and SkillStatMap not preloaded");
                var statMapFile = Path.Combine(DataBasePath, "SkillStatMap.lua");
                _skillStatMap = SkillStatMapLoader.LoadFromFile(statMapFile);
            }
            return _skillStatMap;
        }
    }

    public SkillDefinition? GetSkillById(string id)
    {
        return Skills.GetValueOrDefault(id);
    }

    public GemDefinition? GetGemById(string id)
    {
        return Gems.GetValueOrDefault(id);
    }

    public SkillDefinition? GetSkillForGem(GemDefinition gem)
    {
        return Skills.GetValueOrDefault(gem.GrantedEffectId);
    }

    /// <summary>Preload skills for testing.</summary>
    public void SetSkills(Dictionary<string, SkillDefinition> skills)
    {
        _skills = skills;
    }

    /// <summary>Preload gems for testing.</summary>
    public void SetGems(Dictionary<string, GemDefinition> gems)
    {
        _gems = gems;
    }

    /// <summary>Preload stat map for testing.</summary>
    public void SetSkillStatMap(Dictionary<string, StatMapEntry> statMap)
    {
        _skillStatMap = statMap;
    }
}
