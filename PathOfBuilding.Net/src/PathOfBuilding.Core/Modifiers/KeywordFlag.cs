namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Bitmask flags for skill keyword matching.
/// Ported from Global.lua KeywordFlag table.
/// Default matching is OR (any flag matches). Include MatchAll for AND matching.
/// </summary>
[Flags]
public enum KeywordFlag : uint
{
    None = 0,

    // Skill keywords
    Aura      = 0x00000001,
    Curse     = 0x00000002,
    Warcry    = 0x00000004,
    Movement  = 0x00000008,
    Physical  = 0x00000010,
    Fire      = 0x00000020,
    Cold      = 0x00000040,
    Lightning = 0x00000080,
    Chaos     = 0x00000100,
    Vaal      = 0x00000200,
    Bow       = 0x00000400,

    // Skill types
    Trap    = 0x00001000,
    Mine    = 0x00002000,
    Totem   = 0x00004000,
    Minion  = 0x00008000,
    Attack  = 0x00010000,
    Spell   = 0x00020000,
    Hit     = 0x00040000,
    Ailment = 0x00080000,
    Brand   = 0x00100000,

    // Other effects
    Poison = 0x00200000,
    Bleed  = 0x00400000,
    Ignite = 0x00800000,

    // Damage over Time types
    PhysicalDot  = 0x01000000,
    LightningDot = 0x02000000,
    ColdDot      = 0x04000000,
    FireDot      = 0x08000000,
    ChaosDot     = 0x10000000,

    /// <summary>
    /// When set, ALL specified flags must match (AND logic) instead of ANY (OR logic).
    /// </summary>
    MatchAll = 0x40000000,
}
