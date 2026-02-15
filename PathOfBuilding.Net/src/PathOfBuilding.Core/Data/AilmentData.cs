namespace PathOfBuilding.Core.Data;

/// <summary>
/// Properties for a non-damaging ailment.
/// Ported from Data.lua data.nonDamagingAilment table.
/// </summary>
public record NonDamagingAilmentInfo(
    string AssociatedType,
    bool Alt,
    double? Default,
    double Min,
    double Max,
    int Precision,
    double? Duration
);

/// <summary>
/// Ailment type lists and non-damaging ailment properties.
/// Ported from Data.lua lines 343-355.
/// </summary>
public static class AilmentData
{
    /// <summary>All ailment types.</summary>
    public static readonly string[] AilmentTypeList =
        ["Bleed", "Poison", "Ignite", "Chill", "Freeze", "Shock", "Scorch", "Brittle", "Sap"];

    /// <summary>Elemental ailment types only.</summary>
    public static readonly string[] ElementalAilmentTypeList =
        ["Ignite", "Chill", "Freeze", "Shock", "Scorch", "Brittle", "Sap"];

    /// <summary>Non-damaging ailment types (elemental ailments that don't deal damage).</summary>
    public static readonly string[] NonDamagingAilmentTypeList =
        ["Chill", "Freeze", "Shock", "Scorch", "Brittle", "Sap"];

    /// <summary>Non-elemental ailment types.</summary>
    public static readonly string[] NonElementalAilmentTypeList =
        ["Bleed", "Poison"];

    /// <summary>Per-ailment properties for non-damaging ailments.</summary>
    public static readonly Dictionary<string, NonDamagingAilmentInfo> NonDamagingAilment = new()
    {
        ["Chill"] = new("Cold", false, 10, 5, 30, 0, 2),
        ["Freeze"] = new("Cold", false, null, 0.3, 3, 2, null),
        ["Shock"] = new("Lightning", false, 15, 5, 50, 0, 2),
        ["Scorch"] = new("Fire", true, 10, 0, 30, 0, 4),
        ["Brittle"] = new("Cold", true, 2, 0, 6, 2, 4),
        ["Sap"] = new("Lightning", true, 6, 0, 20, 0, 4),
    };
}
