using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Items;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Represents a game actor (player, enemy, minion) in the calculation engine.
/// Holds the actor's ModDB, output values, and cross-references to other actors.
/// Ported from CalcSetup.lua actor creation and CalcPerform.lua actor usage.
/// </summary>
public class Actor
{
    /// <summary>The actor's modifier database.</summary>
    public ModDB ModDB { get; set; }

    /// <summary>Numeric calculation results (e.g., "Life" → 4500).</summary>
    public Dictionary<string, double> Output { get; } = new();

    /// <summary>Complex output values (e.g., tables, lists).</summary>
    public Dictionary<string, object?> OutputTable { get; } = new();

    /// <summary>Cross-reference to the enemy actor (player ↔ enemy).</summary>
    public Actor? Enemy { get; set; }

    /// <summary>Parent actor (minion → summoner).</summary>
    public Actor? Parent { get; set; }

    /// <summary>The currently selected main skill name.</summary>
    public string? MainSkillName { get; set; }

    /// <summary>Calculation configuration (combat mode, enemy damage, conditions).</summary>
    public CalcConfig Config { get; set; } = new();

    /// <summary>
    /// Damage shift table: damageShiftTable[fromType][toType] = percentage.
    /// Built by CalcEHP.BuildDamageShiftTables.
    /// </summary>
    public Dictionary<string, Dictionary<string, double>> DamageShiftTable { get; set; } = new();

    // Reservation tracking (simplified — full aura system deferred to later phases)
    public double ReservedLifeBase { get; set; }
    public double ReservedLifePercent { get; set; }
    public double ReservedManaBase { get; set; }
    public double ReservedManaPercent { get; set; }

    /// <summary>Equipped items resolved from active item set, keyed by slot name.</summary>
    public Dictionary<string, ParsedItem>? EquippedItems { get; set; }

    /// <summary>All active skills built from socket groups.</summary>
    public List<ActiveSkill> ActiveSkillList { get; } = new();

    /// <summary>The currently selected main active skill.</summary>
    public ActiveSkill? MainSkill { get; set; }

    public double GetReservedBase(string pool) => pool == "Life" ? ReservedLifeBase : ReservedManaBase;
    public double GetReservedPercent(string pool) => pool == "Life" ? ReservedLifePercent : ReservedManaPercent;

    public Actor()
    {
        ModDB = new ModDB();
        ModDB.Actor = this;
    }

    /// <summary>
    /// Resolves a named actor reference to its ModDB.
    /// Used by tag evaluation to look up "enemy", "parent", or "player" actors.
    /// </summary>
    public virtual ModStore? ResolveModDB(string actorName)
    {
        return actorName.ToLowerInvariant() switch
        {
            "enemy" => Enemy?.ModDB,
            "parent" => Parent?.ModDB,
            "player" => Parent?.ModDB ?? ModDB, // minion's player is parent; player is self
            _ => null,
        };
    }
}
