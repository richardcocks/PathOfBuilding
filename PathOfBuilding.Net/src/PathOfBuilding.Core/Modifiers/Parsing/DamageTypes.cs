namespace PathOfBuilding.Core.Modifiers.Parsing;

/// <summary>
/// Static lookup dictionaries for damage types, regen types, degen types, etc.
/// Ported from ModParser.lua lines ~5607-5692.
/// </summary>
public static class DamageTypes
{
    /// <summary>Damage type names: "physical" → "Physical", etc.</summary>
    public static readonly Dictionary<string, string> DmgTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["physical"] = "Physical",
        ["lightning"] = "Lightning",
        ["cold"] = "Cold",
        ["fire"] = "Fire",
        ["chaos"] = "Chaos",
    };

    /// <summary>Penetration type names: "fire resistance" → "FirePenetration", etc.</summary>
    public static readonly Dictionary<string, string> PenTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lightning resistance"] = "LightningPenetration",
        ["cold resistance"] = "ColdPenetration",
        ["fire resistance"] = "FirePenetration",
        ["elemental resistance"] = "ElementalPenetration",
        ["elemental resistances"] = "ElementalPenetration",
        ["chaos resistance"] = "ChaosPenetration",
    };

    /// <summary>Resource types used to build regen/degen/cost variants.</summary>
    private static readonly Dictionary<string, string[]> ResourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["life"] = ["Life"],
        ["mana"] = ["Mana"],
        ["energy shield"] = ["EnergyShield"],
        ["life and mana"] = ["Life", "Mana"],
        ["life and energy shield"] = ["Life", "EnergyShield"],
        ["life, mana and energy shield"] = ["Life", "Mana", "EnergyShield"],
        ["life, energy shield and mana"] = ["Life", "Mana", "EnergyShield"],
        ["mana and life"] = ["Life", "Mana"],
        ["mana and energy shield"] = ["Mana", "EnergyShield"],
        ["mana, life and energy shield"] = ["Life", "Mana", "EnergyShield"],
        ["mana, energy shield and life"] = ["Life", "Mana", "EnergyShield"],
        ["energy shield and life"] = ["Life", "EnergyShield"],
        ["energy shield and mana"] = ["Mana", "EnergyShield"],
        ["energy shield, life and mana"] = ["Life", "Mana", "EnergyShield"],
        ["energy shield, mana and life"] = ["Life", "Mana", "EnergyShield"],
        ["rage"] = ["Rage"],
        // "maximum X" variants
        ["maximum life"] = ["Life"],
        ["maximum mana"] = ["Mana"],
        ["maximum energy shield"] = ["EnergyShield"],
        ["maximum life and mana"] = ["Life", "Mana"],
        ["maximum life and energy shield"] = ["Life", "EnergyShield"],
        ["maximum life, mana and energy shield"] = ["Life", "Mana", "EnergyShield"],
        ["maximum mana and life"] = ["Life", "Mana"],
        ["maximum mana and energy shield"] = ["Mana", "EnergyShield"],
        ["maximum energy shield and life"] = ["Life", "EnergyShield"],
        ["maximum energy shield and mana"] = ["Mana", "EnergyShield"],
    };

    /// <summary>Regen type lookup: "life" → ["LifeRegen"], etc.</summary>
    public static readonly Dictionary<string, string[]> RegenTypes = BuildAppended("Regen");

    /// <summary>Degen type lookup: "life" → ["LifeDegen"], etc.</summary>
    public static readonly Dictionary<string, string[]> DegenTypes = BuildAppended("Degen");

    /// <summary>Cost type lookup: "life" → ["LifeCost"], etc.</summary>
    public static readonly Dictionary<string, string[]> CostTypes = BuildAppended("Cost");

    /// <summary>Base cost type lookup: "life" → ["LifeCostNoMult"], etc.</summary>
    public static readonly Dictionary<string, string[]> BaseCostTypes = BuildAppended("CostNoMult");

    /// <summary>Flag types for the FLAG form: "phasing" → "Condition:Phasing", etc.</summary>
    public static readonly Dictionary<string, FlagTypeEntry> FlagTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["phasing"] = new("Condition:Phasing"),
        ["onslaught"] = new("Condition:Onslaught"),
        ["rampage"] = new("Condition:Rampage"),
        ["soul eater"] = new("Condition:CanHaveSoulEater"),
        ["adrenaline"] = new("Condition:Adrenaline"),
        ["elusive"] = new("Condition:CanBeElusive"),
        ["arcane surge"] = new("Condition:ArcaneSurge"),
        ["fortify"] = new("Condition:Fortified"),
        ["fortified"] = new("Condition:Fortified"),
        ["unholy might"] = new("Condition:UnholyMight"),
        ["chaotic might"] = new("Condition:ChaoticMight"),
        ["lesser brutal shrine buff"] = new("Condition:LesserBrutalShrine"),
        ["lesser massive shrine buff"] = new("Condition:LesserMassiveShrine"),
        ["tailwind"] = new("Condition:Tailwind"),
        ["intimidated"] = new("Condition:Intimidated"),
        ["crushed"] = new("Condition:Crushed"),
        ["chilled"] = new("Condition:Chilled"),
        ["blinded"] = new("Condition:Blinded"),
        ["no life regeneration"] = new("NoLifeRegen"),
        ["hexproof"] = new("CurseEffectOnSelf", -100, "MORE"),
        ["unnerved"] = new("Condition:Unnerved"),
        ["malediction"] = new("HasMalediction"),
        ["debilitated"] = new("Condition:Debilitated"),
    };

    /// <summary>Suffix types for BASE/GAIN/LOSE forms.</summary>
    public static readonly Dictionary<string, string> SuffixTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["as extra maximum energy shield"] = "GainAsEnergyShield",
        ["converted to energy shield"] = "ConvertToEnergyShield",
        ["as extra armour"] = "GainAsArmour",
        ["as physical damage"] = "AsPhysical",
        ["as lightning damage"] = "AsLightning",
        ["as cold damage"] = "AsCold",
        ["as fire damage"] = "AsFire",
        ["as fire"] = "AsFire",
        ["as chaos damage"] = "AsChaos",
        ["leeched as life and mana"] = "Leech",
        ["leeched as life"] = "LifeLeech",
        ["is leeched as life"] = "LifeLeech",
        ["leeched as mana"] = "ManaLeech",
        ["is leeched as mana"] = "ManaLeech",
        ["leeched as energy shield"] = "EnergyShieldLeech",
        ["is leeched as energy shield"] = "EnergyShieldLeech",
    };

    private static Dictionary<string, string[]> BuildAppended(string suffix)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in ResourceTypes)
        {
            var appended = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                appended[i] = values[i] + suffix;
            result[key] = appended;
        }
        return result;
    }
}

/// <summary>
/// Entry for the flagTypes table. Most are simple condition names,
/// but some (like "hexproof") have custom name/value/type.
/// </summary>
public readonly record struct FlagTypeEntry
{
    public string Name { get; }
    public double? Value { get; }
    public string? Type { get; }

    public FlagTypeEntry(string name)
    {
        Name = name;
        Value = null;
        Type = null;
    }

    public FlagTypeEntry(string name, double value, string type)
    {
        Name = name;
        Value = value;
        Type = type;
    }

    /// <summary>Whether this is a simple condition flag (no custom value/type).</summary>
    public bool IsSimple => Value is null;
}
