namespace PathOfBuilding.Core.Modifiers;

/// <summary>
/// Bitmask flags for modifier applicability based on damage mode, source, and weapon type.
/// Ported from Global.lua ModFlag table.
/// </summary>
[Flags]
public enum ModFlag : uint
{
    None = 0,

    // Damage modes (0x000000FF)
    Attack     = 0x00000001,
    Spell      = 0x00000002,
    Hit        = 0x00000004,
    Dot        = 0x00000008,
    Cast       = 0x00000010,

    // Damage sources (0x00003F00)
    Melee      = 0x00000100,
    Area       = 0x00000200,
    Projectile = 0x00000400,
    SourceMask = 0x00000600,
    Ailment    = 0x00000800,
    MeleeHit   = 0x00001000,
    Weapon     = 0x00002000,

    // Weapon types (0x03FF0000)
    Axe        = 0x00010000,
    Bow        = 0x00020000,
    Claw       = 0x00040000,
    Dagger     = 0x00080000,
    Mace       = 0x00100000,
    Staff      = 0x00200000,
    Sword      = 0x00400000,
    Wand       = 0x00800000,
    Unarmed    = 0x01000000,
    Fishing    = 0x02000000,

    // Weapon classes (0x3C000000)
    WeaponMelee  = 0x04000000,
    WeaponRanged = 0x08000000,
    Weapon1H     = 0x10000000,
    Weapon2H     = 0x20000000,

    /// <summary>Combined mask for all weapon-related flags.</summary>
    WeaponMask = 0x2FFF0000,
}
