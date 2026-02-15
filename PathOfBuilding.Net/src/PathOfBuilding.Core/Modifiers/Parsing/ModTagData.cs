using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Modifiers.Parsing;

/// <summary>
/// Entry in the modTagList table. Contains optional tags, flags, keywordFlags, and aura markers.
/// </summary>
public record ModTagEntry(
    List<TagSpec>? Tags = null,
    ModFlag Flags = ModFlag.None,
    KeywordFlag KeywordFlags = KeywordFlag.None,
    bool NewAura = false,
    bool NewAuraOnlyAllies = false
);

/// <summary>
/// Static modifier tag data ported from ModParser.lua modTagList (lines 1249-1895).
/// Maps text fragments to tag specifications used for conditional modifier evaluation.
/// </summary>
public static class ModTagData
{
    /// <summary>
    /// Plain text entries (no regex captures needed). Case-insensitive lookup.
    /// </summary>
    public static readonly Dictionary<string, ModTagEntry> PlainEntries = new(StringComparer.OrdinalIgnoreCase)
    {
        // Empty tags
        ["on enemies"] = new(),
        ["while active"] = new(),
        ["when you hit a unique enemy"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "RareOrUnique")]),
        [" on critical strike"] = new(Tags: [new TagSpec("Condition", Var: "CriticalStrike")]),
        ["from critical strikes"] = new(Tags: [new TagSpec("Condition", Var: "CriticalStrike")]),
        ["with critical strikes"] = new(Tags: [new TagSpec("Condition", Var: "CriticalStrike")]),
        ["while affected by auras you cast"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByAura")]),
        ["for you and nearby allies"] = new(NewAura: true),
        ["to you and allies"] = new(NewAura: true),

        // Multipliers - charges
        ["per power charge"] = new(Tags: [new TagSpec("Multiplier", Var: "PowerCharge")]),
        ["per frenzy charge"] = new(Tags: [new TagSpec("Multiplier", Var: "FrenzyCharge")]),
        ["per endurance charge"] = new(Tags: [new TagSpec("Multiplier", Var: "EnduranceCharge")]),
        ["per siphoning charge"] = new(Tags: [new TagSpec("Multiplier", Var: "SiphoningCharge")]),
        ["per spirit charge"] = new(Tags: [new TagSpec("Multiplier", Var: "SpiritCharge")]),
        ["per challenger charge"] = new(Tags: [new TagSpec("Multiplier", Var: "ChallengerCharge")]),
        ["per gale force"] = new(Tags: [new TagSpec("Multiplier", Var: "GaleForce")]),
        ["per intensity"] = new(Tags: [new TagSpec("Multiplier", Var: "Intensity")]),
        ["per brand"] = new(Tags: [new TagSpec("Multiplier", Var: "ActiveBrand")]),
        ["per blitz charge"] = new(Tags: [new TagSpec("Multiplier", Var: "BlitzCharge")]),
        ["per ghost shroud"] = new(Tags: [new TagSpec("Multiplier", Var: "GhostShroud")]),
        ["per crab barrier"] = new(Tags: [new TagSpec("Multiplier", Var: "CrabBarrier")]),
        ["per rage"] = new(Tags: [new TagSpec("Multiplier", Var: "Rage")]),
        ["per rage while you are not losing rage"] = new(Tags: [new TagSpec("Multiplier", Var: "Rage")]),
        ["per mana burn"] = new(Tags: [new TagSpec("Multiplier", Var: "ManaBurnStacks")]),
        ["per mana burn on you"] = new(Tags: [new TagSpec("Multiplier", Var: "ManaBurnStacks")]),
        ["per level"] = new(Tags: [new TagSpec("Multiplier", Var: "Level")]),
        ["per defiance"] = new(Tags: [new TagSpec("Multiplier", Var: "Defiance")]),

        // Multipliers - equipped items
        ["for each equipped normal item"] = new(Tags: [new TagSpec("Multiplier", Var: "NormalItem")]),
        ["for each normal item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "NormalItem")]),
        ["for each normal item you have equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "NormalItem")]),
        ["for each equipped magic item"] = new(Tags: [new TagSpec("Multiplier", Var: "MagicItem")]),
        ["for each magic item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "MagicItem")]),
        ["for each magic item you have equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "MagicItem")]),
        ["for each equipped rare item"] = new(Tags: [new TagSpec("Multiplier", Var: "RareItem")]),
        ["for each rare item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "RareItem")]),
        ["for each rare item you have equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "RareItem")]),
        ["for each equipped unique item"] = new(Tags: [new TagSpec("Multiplier", Var: "UniqueItem")]),
        ["for each unique item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "UniqueItem")]),
        ["for each unique item you have equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "UniqueItem")]),
        ["per elder item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "ElderItem")]),
        ["per shaper item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "ShaperItem")]),
        ["per elder or shaper item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "ShaperOrElderItem")]),
        ["for each corrupted item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "CorruptedItem")]),
        ["for each equipped corrupted item"] = new(Tags: [new TagSpec("Multiplier", Var: "CorruptedItem")]),
        ["for each uncorrupted item equipped"] = new(Tags: [new TagSpec("Multiplier", Var: "NonCorruptedItem")]),

        // Multipliers - per weapon type
        ["per equipped claw"] = new(Tags: [new TagSpec("Multiplier", Var: "ClawItem")]),
        ["per equipped dagger"] = new(Tags: [new TagSpec("Multiplier", Var: "DaggerItem")]),
        ["per equipped axe"] = new(Tags: [new TagSpec("Multiplier", Var: "AxeItem")]),
        ["per equipped ring"] = new(Tags: [new TagSpec("Multiplier", Var: "RingItem")]),
        ["per equipped flask"] = new(Tags: [new TagSpec("Multiplier", Var: "FlaskItem")]),
        ["per equipped sword"] = new(Tags: [new TagSpec("Multiplier", Var: "SwordItem")]),
        ["per equipped jewel"] = new(Tags: [new TagSpec("Multiplier", Var: "JewelItem")]),
        ["per equipped mace"] = new(Tags: [new TagSpec("Multiplier", Var: "MaceItem")]),
        ["per equipped sceptre"] = new(Tags: [new TagSpec("Multiplier", Var: "SceptreItem")]),
        ["per equipped wand"] = new(Tags: [new TagSpec("Multiplier", Var: "WandItem")]),
        ["per claw"] = new(Tags: [new TagSpec("Multiplier", Var: "ClawItem")]),
        ["per dagger"] = new(Tags: [new TagSpec("Multiplier", Var: "DaggerItem")]),
        ["per axe"] = new(Tags: [new TagSpec("Multiplier", Var: "AxeItem")]),
        ["per ring"] = new(Tags: [new TagSpec("Multiplier", Var: "RingItem")]),
        ["per flask"] = new(Tags: [new TagSpec("Multiplier", Var: "FlaskItem")]),
        ["per sword"] = new(Tags: [new TagSpec("Multiplier", Var: "SwordItem")]),
        ["per jewel"] = new(Tags: [new TagSpec("Multiplier", Var: "JewelItem")]),
        ["per mace"] = new(Tags: [new TagSpec("Multiplier", Var: "MaceItem")]),
        ["per sceptre"] = new(Tags: [new TagSpec("Multiplier", Var: "SceptreItem")]),
        ["per wand"] = new(Tags: [new TagSpec("Multiplier", Var: "WandItem")]),

        // Multipliers - herald / aura / abyss
        ["for each of your aura or herald skills affecting you"] = new(Tags: [new TagSpec("Multiplier", VarList: ["Herald", "AuraAffectingSelf"])]),
        ["per sextant affecting the area"] = new(Tags: [new TagSpec("Multiplier", Var: "Sextant")]),
        ["per buff on you"] = new(Tags: [new TagSpec("Multiplier", Var: "BuffOnSelf")]),
        ["per hit suppressed recently"] = new(Tags: [new TagSpec("Multiplier", Var: "HitsSuppressedRecently")]),
        ["per curse on enemy"] = new(Tags: [new TagSpec("Multiplier", Var: "CurseOnEnemy")]),
        ["for each curse on enemy"] = new(Tags: [new TagSpec("Multiplier", Var: "CurseOnEnemy")]),
        ["for each curse on the enemy"] = new(Tags: [new TagSpec("Multiplier", Var: "CurseOnEnemy")]),
        ["per curse on you"] = new(Tags: [new TagSpec("Multiplier", Var: "CurseOnSelf")]),
        ["per poison on you"] = new(Tags: [new TagSpec("Multiplier", Var: "PoisonStack")]),
        ["for each poison on you"] = new(Tags: [new TagSpec("Multiplier", Var: "PoisonStack")]),
        ["for each poison you have inflicted recently"] = new(Tags: [new TagSpec("Multiplier", Var: "PoisonAppliedRecently")]),
        ["per withered debuff on enemy"] = new(Tags: [new TagSpec("Multiplier", Var: "WitheredStack", Actor: "enemy", Limit: 15)]),
        ["for each shocked enemy you've killed recently"] = new(Tags: [new TagSpec("Multiplier", Var: "ShockedEnemyKilledRecently")]),
        ["per minion from your non-vaal skills"] = new(Tags: [new TagSpec("Multiplier", Var: "NonVaalSummonedMinion")]),
        ["per minion"] = new(Tags: [new TagSpec("Multiplier", Var: "SummonedMinion")]),
        ["for each time you have blocked in the past 10 seconds"] = new(Tags: [new TagSpec("Multiplier", Var: "BlockedPast10Sec")]),
        ["per enemy killed by you or your totems recently"] = new(Tags: [new TagSpec("Multiplier", VarList: ["EnemyKilledRecently", "EnemyKilledByTotemsRecently"])]),
        ["per enemy in close range"] = new(Tags: [new TagSpec("Condition", Var: "AtCloseRange"), new TagSpec("Multiplier", Var: "NearbyEnemies")]),

        // Multipliers - sockets
        ["per red socket"] = new(Tags: [new TagSpec("Multiplier", Var: "RedSocketIn{SlotName}")]),
        ["per green socket on main hand weapon"] = new(Tags: [new TagSpec("Multiplier", Var: "GreenSocketInWeapon 1")]),
        ["per green socket on"] = new(Tags: [new TagSpec("Multiplier", Var: "GreenSocketInWeapon 1")]),
        ["per red socket on main hand weapon"] = new(Tags: [new TagSpec("Multiplier", Var: "RedSocketInWeapon 1")]),
        ["per red socket on equipped staff"] = new(Tags: [new TagSpec("Multiplier", Var: "RedSocketInWeapon 1"), new TagSpec("Condition", Var: "UsingStaff")]),
        ["per blue socket on equipped staff"] = new(Tags: [new TagSpec("Multiplier", Var: "BlueSocketInWeapon 1"), new TagSpec("Condition", Var: "UsingStaff")]),
        ["per green socket"] = new(Tags: [new TagSpec("Multiplier", Var: "GreenSocketIn{SlotName}")]),
        ["per blue socket"] = new(Tags: [new TagSpec("Multiplier", Var: "BlueSocketIn{SlotName}")]),
        ["per white socket"] = new(Tags: [new TagSpec("Multiplier", Var: "WhiteSocketIn{SlotName}")]),
        ["for each unlinked socket in equipped two handed weapon"] = new(Tags: [new TagSpec("Multiplier", Var: "UnlinkedSocketInWeapon 1"), new TagSpec("Condition", Var: "UsingTwoHandedWeapon")]),
        ["for each empty red socket on any equipped item"] = new(Tags: [new TagSpec("Multiplier", Var: "EmptyRedSocketsInAnySlot")]),
        ["for each empty green socket on any equipped item"] = new(Tags: [new TagSpec("Multiplier", Var: "EmptyGreenSocketsInAnySlot")]),
        ["for each empty blue socket on any equipped item"] = new(Tags: [new TagSpec("Multiplier", Var: "EmptyBlueSocketsInAnySlot")]),
        ["for each empty white socket on any equipped item"] = new(Tags: [new TagSpec("Multiplier", Var: "EmptyWhiteSocketsInAnySlot")]),
        ["per socketed gem"] = new(Tags: [new TagSpec("Multiplier", Var: "SocketedGemsIn{SlotName}")]),

        // Multipliers - impale, animated weapon, vines, etc.
        ["for each impale on enemy"] = new(Tags: [new TagSpec("Multiplier", Var: "ImpaleStacks", Actor: "enemy")]),
        ["per impale on enemy"] = new(Tags: [new TagSpec("Multiplier", Var: "ImpaleStacks", Actor: "enemy")]),
        ["per animated weapon"] = new(Tags: [new TagSpec("Multiplier", Var: "AnimatedWeapon", Actor: "parent")]),
        ["per grasping vine"] = new(Tags: [new TagSpec("Multiplier", Var: "GraspingVinesCount")]),
        ["per fragile regrowth"] = new(Tags: [new TagSpec("Multiplier", Var: "FragileRegrowthCount")]),
        ["per bark"] = new(Tags: [new TagSpec("Multiplier", Var: "BarkskinStacks")]),
        ["per bark below maximum"] = new(Tags: [new TagSpec("Multiplier", Var: "MissingBarkskinStacks")]),
        ["per allocated mastery passive skill"] = new(Tags: [new TagSpec("Multiplier", Var: "AllocatedMastery")]),
        ["per allocated notable passive skill"] = new(Tags: [new TagSpec("Multiplier", Var: "AllocatedNotable")]),
        ["for each different type of mastery you have allocated"] = new(Tags: [new TagSpec("Multiplier", Var: "AllocatedMasteryType")]),
        ["per grand spectrum"] = new(Tags: [new TagSpec("Multiplier", Var: "GrandSpectrum")]),
        ["per elemental ailment you've inflicted recently"] = new(Tags: [new TagSpec("Multiplier", Var: "AppliedAilmentsRecently")]),

        // Per stat - plain entries
        ["per dexterity"] = new(Tags: [new TagSpec("PerStat", Stat: "Dex")]),
        ["per soul required"] = new(Tags: [new TagSpec("PerStat", Stat: "SoulCost")]),
        ["per endurance, frenzy or power charge"] = new(Tags: [new TagSpec("PerStat", Stat: "TotalCharges")]),
        ["per fortification"] = new(Tags: [new TagSpec("PerStat", Stat: "FortificationStacks")]),
        ["per two fortification on you"] = new(Tags: [new TagSpec("PerStat", Stat: "FortificationStacks", Div: 2, Actor: "player")]),
        ["per fortification above 20"] = new(Tags: [new TagSpec("PerStat", Stat: "FortificationStacksOver20")]),
        ["per totem"] = new(Tags: [new TagSpec("PerStat", Stat: "TotemsSummoned")]),
        ["per summoned totem"] = new(Tags: [new TagSpec("PerStat", Stat: "TotemsSummoned")]),
        ["for each summoned totem"] = new(Tags: [new TagSpec("PerStat", Stat: "TotemsSummoned")]),
        ["for each time they have chained"] = new(Tags: [new TagSpec("PerStat", Stat: "Chain")]),
        ["for each time it has chained"] = new(Tags: [new TagSpec("PerStat", Stat: "Chain")]),
        ["for each summoned golem"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveGolemLimit")]),
        ["for each golem you have summoned"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveGolemLimit")]),
        ["per summoned golem"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveGolemLimit")]),
        ["per summoned sentinel of purity"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveSentinelOfPurityLimit")]),
        ["per summoned void spawn"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveVoidSpawnLimit")]),
        ["per summoned skeleton"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveSkeletonLimit")]),
        ["per skeleton you own"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveSkeletonLimit", Actor: "parent")]),
        ["per summoned raging spirit"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveRagingSpiritLimit")]),
        ["per summoned phantasm"] = new(Tags: [new TagSpec("PerStat", Stat: "ActivePhantasmLimit")]),
        ["for each raised zombie"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveZombieLimit")]),
        ["per zombie you own"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveZombieLimit", Actor: "parent")]),
        ["per raised zombie"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveZombieLimit")]),
        ["per raised spectre"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveSpectreLimit")]),
        ["per spectre you own"] = new(Tags: [new TagSpec("PerStat", Stat: "ActiveSpectreLimit", Actor: "parent")]),
        ["for each remaining chain"] = new(Tags: [new TagSpec("PerStat", Stat: "ChainRemaining")]),
        ["for each enemy pierced"] = new(Tags: [new TagSpec("PerStat", Stat: "PiercedCount")]),
        ["for each time they've pierced"] = new(Tags: [new TagSpec("PerStat", Stat: "PiercedCount")]),

        // Stat conditions - plain entries
        ["if dexterity is higher than intelligence"] = new(Tags: [new TagSpec("Condition", Var: "DexHigherThanInt")]),
        ["if strength is higher than intelligence"] = new(Tags: [new TagSpec("Condition", Var: "StrHigherThanInt")]),
        ["against targets they pierce"] = new(Tags: [new TagSpec("StatThreshold", Stat: "PierceCount", Threshold: 1)]),
        ["against pierced targets"] = new(Tags: [new TagSpec("StatThreshold", Stat: "PierceCount", Threshold: 1)]),
        ["to targets they pierce"] = new(Tags: [new TagSpec("StatThreshold", Stat: "PierceCount", Threshold: 1)]),
        ["that fire a single projectile"] = new(Tags: [new TagSpec("StatThreshold", Stat: "ProjectileCount", Threshold: 1, Upper: true)]),
        ["while affected by a unique abyss jewel"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "UniqueAbyssJewels", Threshold: 1)]),
        ["while affected by a rare abyss jewel"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "RareAbyssJewels", Threshold: 1)]),
        ["while affected by a magic abyss jewel"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "MagicAbyssJewels", Threshold: 1)]),
        ["while affected by a normal abyss jewel"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "NormalAbyssJewels", Threshold: 1)]),

        // Slot conditions
        ["when in main hand"] = new(Tags: [new TagSpec("SlotNumber", Num: 1)]),
        ["in main hand"] = new(Tags: [new TagSpec("InSlot", Num: 1)]),
        ["in off hand"] = new(Tags: [new TagSpec("InSlot", Num: 2)]),
        ["of skills supported by spellslinger"] = new(Tags: [new TagSpec("Condition", Var: "SupportedBySpellslinger")]),

        // Equipment conditions - plain entries
        ["while holding a fishing rod"] = new(Tags: [new TagSpec("Condition", Var: "UsingFishing")]),
        ["while your off hand is empty"] = new(Tags: [new TagSpec("Condition", Var: "OffHandIsEmpty")]),
        ["with shields"] = new(Tags: [new TagSpec("Condition", Var: "UsingShield")]),
        ["while dual wielding"] = new(Tags: [new TagSpec("Condition", Var: "DualWielding")]),
        ["while dual wielding claws"] = new(Tags: [new TagSpec("Condition", Var: "DualWieldingClaws")]),
        ["while dual wielding or holding a shield"] = new(Tags: [new TagSpec("Condition", VarList: ["DualWielding", "UsingShield"])]),
        ["while wielding an axe"] = new(Tags: [new TagSpec("Condition", Var: "UsingAxe")]),
        ["while wielding an axe or sword"] = new(Tags: [new TagSpec("Condition", VarList: ["UsingAxe", "UsingSword"])]),
        ["while wielding a bow"] = new(Tags: [new TagSpec("Condition", Var: "UsingBow")]),
        ["while wielding a claw"] = new(Tags: [new TagSpec("Condition", Var: "UsingClaw")]),
        ["while wielding a dagger"] = new(Tags: [new TagSpec("Condition", Var: "UsingDagger")]),
        ["while wielding a claw or dagger"] = new(Tags: [new TagSpec("Condition", VarList: ["UsingClaw", "UsingDagger"])]),
        ["while wielding a mace"] = new(Tags: [new TagSpec("Condition", Var: "UsingMace")]),
        ["while wielding a mace or sceptre"] = new(Tags: [new TagSpec("Condition", Var: "UsingMace")]),
        ["while wielding a mace, sceptre or staff"] = new(Tags: [new TagSpec("Condition", VarList: ["UsingMace", "UsingStaff"])]),
        ["while wielding a staff"] = new(Tags: [new TagSpec("Condition", Var: "UsingStaff")]),
        ["while wielding a sword"] = new(Tags: [new TagSpec("Condition", Var: "UsingSword")]),
        ["while wielding a melee weapon"] = new(Tags: [new TagSpec("Condition", Var: "UsingMeleeWeapon")]),
        ["while wielding a one handed weapon"] = new(Tags: [new TagSpec("Condition", Var: "UsingOneHandedWeapon")]),
        ["while wielding a two handed weapon"] = new(Tags: [new TagSpec("Condition", Var: "UsingTwoHandedWeapon")]),
        ["while wielding a two handed melee weapon"] = new(Tags: [new TagSpec("Condition", Var: "UsingTwoHandedWeapon"), new TagSpec("Condition", Var: "UsingMeleeWeapon")]),
        ["while wielding a wand"] = new(Tags: [new TagSpec("Condition", Var: "UsingWand")]),
        ["while wielding two different weapon types"] = new(Tags: [new TagSpec("Condition", Var: "WieldingDifferentWeaponTypes")]),
        ["while unarmed"] = new(Tags: [new TagSpec("Condition", Var: "Unarmed")]),
        ["while you are unencumbered"] = new(Tags: [new TagSpec("Condition", Var: "Unencumbered")]),
        ["equipped bow"] = new(Tags: [new TagSpec("Condition", Var: "UsingBow")]),
        ["if corrupted"] = new(Tags: [new TagSpec("ItemCondition", ItemSlot: "{SlotName}", CorruptedCond: true)]),
        ["with a normal item equipped"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "NormalItem", Threshold: 1)]),
        ["with a magic item equipped"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "MagicItem", Threshold: 1)]),
        ["with a rare item equipped"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "RareItem", Threshold: 1)]),
        ["with a unique item equipped"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "UniqueItem", Threshold: 1)]),
        ["if you wear no corrupted items"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "CorruptedItem", Threshold: 0, Upper: true)]),
        ["if no worn items are corrupted"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "CorruptedItem", Threshold: 0, Upper: true)]),
        ["if no equipped items are corrupted"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "CorruptedItem", Threshold: 0, Upper: true)]),
        ["if all worn items are corrupted"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "NonCorruptedItem", Threshold: 0, Upper: true)]),
        ["if all equipped items are corrupted"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "NonCorruptedItem", Threshold: 0, Upper: true)]),
        ["if equipped helmet, body armour, gloves, and boots all have armour"] = new(Tags: [
            new TagSpec("StatThreshold", Stat: "ArmourOnHelmet", Threshold: 1),
            new TagSpec("StatThreshold", Stat: "ArmourOnBody Armour", Threshold: 1),
            new TagSpec("StatThreshold", Stat: "ArmourOnGloves", Threshold: 1),
            new TagSpec("StatThreshold", Stat: "ArmourOnBoots", Threshold: 1),
        ]),
        ["if equipped helmet, body armour, gloves, and boots all have evasion rating"] = new(Tags: [
            new TagSpec("StatThreshold", Stat: "EvasionOnHelmet", Threshold: 1),
            new TagSpec("StatThreshold", Stat: "EvasionOnBody Armour", Threshold: 1),
            new TagSpec("StatThreshold", Stat: "EvasionOnGloves", Threshold: 1),
            new TagSpec("StatThreshold", Stat: "EvasionOnBoots", Threshold: 1),
        ]),

        // Player status conditions - plain entries
        ["if used while on low life"] = new(Tags: [new TagSpec("Condition", Var: "LowLife")]),
        ["on reaching low life"] = new(Tags: [new TagSpec("Condition", Var: "LowLife")]),
        ["while stationary"] = new(Tags: [new TagSpec("Condition", Var: "Stationary")]),
        ["while you are stationary"] = new(Tags: [new TagSpec("ActorCondition", Actor: "player", Var: "Stationary")]),
        ["while moving"] = new(Tags: [new TagSpec("Condition", Var: "Moving")]),
        ["while channelling"] = new(Tags: [new TagSpec("Condition", Var: "Channelling")]),
        ["while channelling snipe"] = new(Tags: [new TagSpec("Condition", Var: "Channelling")]),
        ["if you've inflicted exposure recently"] = new(Tags: [new TagSpec("Condition", Var: "AppliedExposureRecently")]),
        ["while you have no power charges"] = new(Tags: [new TagSpec("StatThreshold", Stat: "PowerCharges", Threshold: 0, Upper: true)]),
        ["while you have no frenzy charges"] = new(Tags: [new TagSpec("StatThreshold", Stat: "FrenzyCharges", Threshold: 0, Upper: true)]),
        ["while you have no endurance charges"] = new(Tags: [new TagSpec("StatThreshold", Stat: "EnduranceCharges", Threshold: 0, Upper: true)]),
        ["while you have a power charge"] = new(Tags: [new TagSpec("StatThreshold", Stat: "PowerCharges", Threshold: 1)]),
        ["while you have a frenzy charge"] = new(Tags: [new TagSpec("StatThreshold", Stat: "FrenzyCharges", Threshold: 1)]),
        ["while you have an endurance charge"] = new(Tags: [new TagSpec("StatThreshold", Stat: "EnduranceCharges", Threshold: 1)]),
        ["while at maximum power charges"] = new(Tags: [new TagSpec("StatThreshold", Stat: "PowerCharges", ThresholdStat: "PowerChargesMax")]),
        ["while at maximum frenzy charges"] = new(Tags: [new TagSpec("StatThreshold", Stat: "FrenzyCharges", ThresholdStat: "FrenzyChargesMax")]),
        ["while on full frenzy charges"] = new(Tags: [new TagSpec("StatThreshold", Stat: "FrenzyCharges", ThresholdStat: "FrenzyChargesMax")]),
        ["while at maximum endurance charges"] = new(Tags: [new TagSpec("StatThreshold", Stat: "EnduranceCharges", ThresholdStat: "EnduranceChargesMax")]),
        ["while at maximum rage"] = new(Tags: [new TagSpec("Condition", Var: "HaveMaximumRage")]),
        ["while at maximum fortification"] = new(Tags: [new TagSpec("Condition", Var: "HaveMaximumFortification")]),
        ["while you have a totem"] = new(Tags: [new TagSpec("Condition", Var: "HaveTotem")]),
        ["while you have at least one nearby ally"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "NearbyAlly", Threshold: 1)]),
        ["while you have a linked target"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "LinkedTargets", Threshold: 1)]),
        ["while you have fortify"] = new(Tags: [new TagSpec("Condition", Var: "Fortified")]),
        ["while you have phasing"] = new(Tags: [new TagSpec("Condition", Var: "Phasing")]),
        ["while you have elusive"] = new(Tags: [new TagSpec("Condition", Var: "Elusive")]),
        ["while physical aegis is depleted"] = new(Tags: [new TagSpec("Condition", Var: "PhysicalAegisDepleted")]),
        ["during onslaught"] = new(Tags: [new TagSpec("Condition", Var: "Onslaught")]),
        ["while you have onslaught"] = new(Tags: [new TagSpec("Condition", Var: "Onslaught")]),
        ["while phasing"] = new(Tags: [new TagSpec("Condition", Var: "Phasing")]),
        ["while you have tailwind"] = new(Tags: [new TagSpec("Condition", Var: "Tailwind")]),
        ["while elusive"] = new(Tags: [new TagSpec("Condition", Var: "Elusive")]),
        ["gain elusive"] = new(Tags: [new TagSpec("Condition", VarList: ["CanBeElusive", "Elusive"])]),
        ["while you have arcane surge"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByArcaneSurge")]),
        ["while you have cat's stealth"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByCat'sStealth")]),
        ["while you have cat's agility"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByCat'sAgility")]),
        ["while you have avian's might"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByAvian'sMight")]),
        ["while you have avian's flight"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByAvian'sFlight")]),
        ["while affected by aspect of the cat"] = new(Tags: [new TagSpec("Condition", VarList: ["AffectedByCat'sStealth", "AffectedByCat'sAgility"])]),
        ["while affected by a non-vaal guard skill"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByNonVaalGuardSkill")]),
        ["if a non-vaal guard buff was lost recently"] = new(Tags: [new TagSpec("Condition", Var: "LostNonVaalBuffRecently")]),
        ["while affected by a guard skill buff"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByGuardSkill")]),
        ["while affected by a herald"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByHerald")]),
        ["while fortified"] = new(Tags: [new TagSpec("Condition", Var: "Fortified")]),
        ["while in blood stance"] = new(Tags: [new TagSpec("Condition", Var: "BloodStance")]),
        ["while in sand stance"] = new(Tags: [new TagSpec("Condition", Var: "SandStance")]),
        ["while you have a bestial minion"] = new(Tags: [new TagSpec("Condition", Var: "HaveBestialMinion")]),
        ["while you have infusion"] = new(Tags: [new TagSpec("Condition", Var: "InfusionActive")]),
        ["while leeching"] = new(Tags: [new TagSpec("Condition", Var: "Leeching")]),
        ["while leeching life"] = new(Tags: [new TagSpec("Condition", Var: "LeechingLife")]),
        ["while leeching energy shield"] = new(Tags: [new TagSpec("Condition", Var: "LeechingEnergyShield")]),
        ["while leeching mana"] = new(Tags: [new TagSpec("Condition", Var: "LeechingMana")]),
        ["while using a flask"] = new(Tags: [new TagSpec("Condition", Var: "UsingFlask")]),
        ["during effect"] = new(Tags: [new TagSpec("Condition", Var: "UsingFlask")]),
        ["during flask effect"] = new(Tags: [new TagSpec("Condition", Var: "UsingFlask")]),
        ["during any flask effect"] = new(Tags: [new TagSpec("Condition", Var: "UsingFlask")]),
        ["while under no flask effects"] = new(Tags: [new TagSpec("Condition", Var: "UsingFlask", Neg: true)]),
        ["during effect of any mana flask"] = new(Tags: [new TagSpec("Condition", Var: "UsingManaFlask")]),
        ["during effect of any life flask"] = new(Tags: [new TagSpec("Condition", Var: "UsingLifeFlask")]),
        ["if you've used a life flask in the past 10 seconds"] = new(Tags: [new TagSpec("Condition", Var: "UsingLifeFlask")]),
        ["if you've used a mana flask in the past 10 seconds"] = new(Tags: [new TagSpec("Condition", Var: "UsingManaFlask")]),
        ["during effect of any life or mana flask"] = new(Tags: [new TagSpec("Condition", VarList: ["UsingManaFlask", "UsingLifeFlask"])]),
        ["while you have an active tincture"] = new(Tags: [new TagSpec("Condition", Var: "UsingTincture")]),
        ["while you have a tincture active"] = new(Tags: [new TagSpec("Condition", Var: "UsingTincture")]),
        ["while on consecrated ground"] = new(Tags: [new TagSpec("Condition", Var: "OnConsecratedGround")]),
        ["while on caustic ground"] = new(Tags: [new TagSpec("Condition", Var: "OnCausticGround")]),
        ["when you create consecrated ground"] = new(),
        ["on burning ground"] = new(Tags: [new TagSpec("Condition", Var: "OnBurningGround")]),
        ["while on burning ground"] = new(Tags: [new TagSpec("Condition", Var: "OnBurningGround")]),
        ["on chilled ground"] = new(Tags: [new TagSpec("Condition", Var: "OnChilledGround")]),
        ["on shocked ground"] = new(Tags: [new TagSpec("Condition", Var: "OnShockedGround")]),
        ["while in a caustic cloud"] = new(Tags: [new TagSpec("Condition", Var: "OnCausticCloud")]),
        ["while blinded"] = new(Tags: [new TagSpec("Condition", Var: "Blinded"), new TagSpec("Condition", Var: "CannotBeBlinded", Neg: true)]),
        ["while burning"] = new(Tags: [new TagSpec("Condition", Var: "Burning")]),
        ["while ignited"] = new(Tags: [new TagSpec("Condition", Var: "Ignited")]),
        ["while you are ignited"] = new(Tags: [new TagSpec("Condition", Var: "Ignited")]),
        ["while chilled"] = new(Tags: [new TagSpec("Condition", Var: "Chilled")]),
        ["while you are chilled"] = new(Tags: [new TagSpec("Condition", Var: "Chilled")]),
        ["while frozen"] = new(Tags: [new TagSpec("Condition", Var: "Frozen")]),
        ["while shocked"] = new(Tags: [new TagSpec("Condition", Var: "Shocked")]),
        ["while you are shocked"] = new(Tags: [new TagSpec("Condition", Var: "Shocked")]),
        ["while you are bleeding"] = new(Tags: [new TagSpec("Condition", Var: "Bleeding")]),
        ["while not ignited, frozen or shocked"] = new(Tags: [new TagSpec("Condition", VarList: ["Ignited", "Frozen", "Shocked"], Neg: true)]),
        ["while bleeding"] = new(Tags: [new TagSpec("Condition", Var: "Bleeding")]),
        ["while poisoned"] = new(Tags: [new TagSpec("Condition", Var: "Poisoned")]),
        ["while you are poisoned"] = new(Tags: [new TagSpec("Condition", Var: "Poisoned")]),
        ["while cursed"] = new(Tags: [new TagSpec("Condition", Var: "Cursed")]),
        ["while not cursed"] = new(Tags: [new TagSpec("Condition", Var: "Cursed", Neg: true)]),
        ["while there is only one nearby enemy"] = new(Tags: [new TagSpec("Multiplier", Var: "NearbyEnemies", Limit: 1), new TagSpec("Condition", Var: "OnlyOneNearbyEnemy")]),
        ["when you or your totems hit an enemy with a spell"] = new(Tags: [new TagSpec("Condition", VarList: ["HitSpellRecently", "TotemsHitSpellRecently"])]),
        ["on hit with spells"] = new(Tags: [new TagSpec("Condition", Var: "HitSpellRecently")]),
        ["if you haven't crit recently"] = new(Tags: [new TagSpec("Condition", Var: "CritRecently", Neg: true)]),
        ["if you haven't dealt a critical strike recently"] = new(Tags: [new TagSpec("Condition", Var: "CritRecently", Neg: true)]),
        ["if your skills have dealt a critical strike recently"] = new(Tags: [new TagSpec("Condition", Var: "SkillCritRecently")]),
        ["if you dealt a critical strike with a herald skill recently"] = new(Tags: [new TagSpec("Condition", Var: "CritWithHeraldSkillRecently")]),
        ["on killing taunted enemies"] = new(Tags: [new TagSpec("Condition", Var: "KilledTauntedEnemyRecently")]),
        ["on kill"] = new(Tags: [new TagSpec("Condition", Var: "KilledRecently")]),
        ["on melee kill"] = new(Flags: ModFlag.WeaponMelee, Tags: [new TagSpec("Condition", Var: "KilledRecently")]),
        ["when you kill an enemy"] = new(Tags: [new TagSpec("Condition", Var: "KilledRecently")]),
        ["if you haven't killed recently"] = new(Tags: [new TagSpec("Condition", Var: "KilledRecently", Neg: true)]),
        ["if you or your totems have killed recently"] = new(Tags: [new TagSpec("Condition", VarList: ["KilledRecently", "TotemsKilledRecently"])]),
        ["on throwing a trap"] = new(Tags: [new TagSpec("Condition", Var: "TrapOrMineThrownRecently")]),
        ["if you've impaled an enemy recently"] = new(Tags: [new TagSpec("Condition", Var: "ImpaledRecently")]),
        ["if you've changed stance recently"] = new(Tags: [new TagSpec("Condition", Var: "ChangedStanceRecently")]),
        ["if you've gained a power charge recently"] = new(Tags: [new TagSpec("Condition", Var: "GainedPowerChargeRecently")]),
        ["if you haven't gained a power charge recently"] = new(Tags: [new TagSpec("Condition", Var: "GainedPowerChargeRecently", Neg: true)]),
        ["if you haven't gained a frenzy charge recently"] = new(Tags: [new TagSpec("Condition", Var: "GainedFrenzyChargeRecently", Neg: true)]),
        ["if you've stopped taking damage over time recently"] = new(Tags: [new TagSpec("Condition", Var: "StoppedTakingDamageOverTimeRecently")]),
        ["during soul gain prevention"] = new(Tags: [new TagSpec("Condition", Var: "SoulGainPrevention")]),
        ["if you detonated mines recently"] = new(Tags: [new TagSpec("Condition", Var: "DetonatedMinesRecently")]),
        ["if you detonated a mine recently"] = new(Tags: [new TagSpec("Condition", Var: "DetonatedMinesRecently")]),
        ["when your mine is detonated targeting an enemy"] = new(Tags: [new TagSpec("Condition", Var: "DetonatedMinesRecently")]),
        ["when your trap is triggered by an enemy"] = new(Tags: [new TagSpec("Condition", Var: "TriggeredTrapsRecently")]),
        ["if energy shield recharge has started recently"] = new(Tags: [new TagSpec("Condition", Var: "EnergyShieldRechargeRecently")]),
        ["if energy shield recharge has started in the past 2 seconds"] = new(Tags: [new TagSpec("Condition", Var: "EnergyShieldRechargePastTwoSec")]),
        ["when cast on frostbolt"] = new(Tags: [new TagSpec("Condition", Var: "CastOnFrostbolt")]),
        ["branded enemy's"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "BrandsAttachedToEnemy", Threshold: 1)]),
        ["to enemies they're attached to"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "BrandsAttachedToEnemy", Threshold: 1)]),
        ["while you have iron reflexes"] = new(Tags: [new TagSpec("Condition", Var: "HaveIronReflexes")]),
        ["while you do not have iron reflexes"] = new(Tags: [new TagSpec("Condition", Var: "HaveIronReflexes", Neg: true)]),
        ["while you have elemental overload"] = new(Tags: [new TagSpec("Condition", Var: "HaveElementalOverload")]),
        ["while you do not have elemental overload"] = new(Tags: [new TagSpec("Condition", Var: "HaveElementalOverload", Neg: true)]),
        ["while you have resolute technique"] = new(Tags: [new TagSpec("Condition", Var: "HaveResoluteTechnique")]),
        ["while you do not have resolute technique"] = new(Tags: [new TagSpec("Condition", Var: "HaveResoluteTechnique", Neg: true)]),
        ["while you have avatar of fire"] = new(Tags: [new TagSpec("Condition", Var: "HaveAvatarOfFire")]),
        ["while you do not have avatar of fire"] = new(Tags: [new TagSpec("Condition", Var: "HaveAvatarOfFire", Neg: true)]),
        ["if you have a summoned golem"] = new(Tags: [new TagSpec("Condition", VarList: ["HavePhysicalGolem", "HaveLightningGolem", "HaveColdGolem", "HaveFireGolem", "HaveChaosGolem", "HaveCarrionGolem"])]),
        ["while you have a summoned golem"] = new(Tags: [new TagSpec("Condition", VarList: ["HavePhysicalGolem", "HaveLightningGolem", "HaveColdGolem", "HaveFireGolem", "HaveChaosGolem", "HaveCarrionGolem"])]),
        ["if a minion has died recently"] = new(Tags: [new TagSpec("Condition", Var: "MinionsDiedRecently")]),
        ["if a minion has been killed recently"] = new(Tags: [new TagSpec("Condition", Var: "MinionsDiedRecently")]),
        ["while you have sacrificial zeal"] = new(Tags: [new TagSpec("Condition", Var: "SacrificialZeal")]),
        ["while sane"] = new(Tags: [new TagSpec("Condition", Var: "Insane", Neg: true)]),
        ["while insane"] = new(Tags: [new TagSpec("Condition", Var: "Insane")]),
        ["while you have defiance"] = new(Tags: [new TagSpec("MultiplierThreshold", Var: "Defiance", Threshold: 1)]),
        ["while affected by glorious madness"] = new(Tags: [new TagSpec("Condition", Var: "AffectedByGloriousMadness")]),
        ["if you have reserved life and mana"] = new(Tags: [
            new TagSpec("StatThreshold", Stat: "LifeReserved", Threshold: 1),
            new TagSpec("StatThreshold", Stat: "ManaReserved", Threshold: 1),
        ]),
        ["if you've shattered an enemy recently"] = new(Tags: [new TagSpec("Condition", Var: "ShatteredEnemyRecently")]),
        ["if you haven't cast dash recently"] = new(Tags: [new TagSpec("Condition", Var: "CastDashRecently", Neg: true)]),
        ["if you haven't used a brand skill recently"] = new(Tags: [new TagSpec("Condition", Var: "UsedBrandRecently", Neg: true)]),
        ["if you haven't summoned a totem in the past 2 seconds"] = new(Tags: [new TagSpec("Condition", Var: "NoSummonedTotemsInPastTwoSeconds")]),
        ["when you use a movement skill"] = new(Tags: [new TagSpec("Condition", Var: "UsedMovementSkillRecently")]),
        ["when you warcry"] = new(Tags: [new TagSpec("Condition", Var: "UsedWarcryRecently")]),
        ["when you summon a totem"] = new(Tags: [new TagSpec("Condition", Var: "SummonedTotemRecently")]),
        ["if you summoned a golem in the past 8 seconds"] = new(Tags: [new TagSpec("Condition", Var: "SummonedGolemInPast8Sec")]),
        ["when you use a vaal skill"] = new(Tags: [new TagSpec("Condition", Var: "UsedVaalSkillRecently")]),
        ["for each corpse consumed recently"] = new(Tags: [new TagSpec("Multiplier", Var: "CorpseConsumedRecently")]),
        ["for each warcry exerting them"] = new(Tags: [new TagSpec("Multiplier", Var: "ExertingWarcryCount")]),
        ["for each different non-instant spell you've cast recently"] = new(Tags: [new TagSpec("Multiplier", Var: "NonInstantSpellCastRecently")]),
        ["if you have energy shield"] = new(Tags: [new TagSpec("Condition", Var: "HaveEnergyShield")]),

        // Enemy status conditions - plain entries
        ["at close range"] = new(Tags: [new TagSpec("Condition", Var: "AtCloseRange")]),
        ["not at close range"] = new(Tags: [new TagSpec("Condition", Var: "AtCloseRange", Neg: true)]),
        ["against rare and unique enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "RareOrUnique")]),
        ["against unique enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "RareOrUnique")]),
        ["against enemies on full life"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "FullLife")]),
        ["against enemies that are on full life"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "FullLife")]),
        ["against enemies on low life"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "LowLife")]),
        ["against enemies that are on low life"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "LowLife")]),
        ["against enemies that are not on low life"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "LowLife", Neg: true)]),
        ["to enemies which have energy shield"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "HaveEnergyShield")], KeywordFlags: KeywordFlag.Hit | KeywordFlag.Ailment),
        ["against cursed enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Cursed")]),
        ["against stunned enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Stunned")]),
        ["on cursed enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Cursed")]),
        ["of cursed enemies'"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Cursed")]),
        ["when hitting cursed enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Cursed")], KeywordFlags: KeywordFlag.Hit),
        ["from cursed enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Cursed")]),
        ["against marked enemy"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Marked")]),
        ["when hitting marked enemy"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Marked")], KeywordFlags: KeywordFlag.Hit),
        ["from marked enemy"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Marked")]),
        ["against taunted enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Taunted")]),
        ["against bleeding enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Bleeding")]),
        ["you inflict on bleeding enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Bleeding")]),
        ["to bleeding enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Bleeding")]),
        ["from bleeding enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Bleeding")]),
        ["against poisoned enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Poisoned")]),
        ["you inflict on poisoned enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Poisoned")]),
        ["to poisoned enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Poisoned")]),
        ["against hindered enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Hindered")]),
        ["against maimed enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Maimed")]),
        ["you inflict on maimed enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Maimed")]),
        ["against blinded enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Blinded")]),
        ["from blinded enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Blinded")]),
        ["against burning enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Burning")]),
        ["against ignited enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Ignited")]),
        ["to ignited enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Ignited")]),
        ["against shocked enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Shocked")]),
        ["you inflict on shocked enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Shocked")]),
        ["to shocked enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Shocked")]),
        ["inflicted on shocked enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Shocked")]),
        ["enemies which are shocked"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Shocked")]),
        ["against frozen enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Frozen")]),
        ["to frozen enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Frozen")]),
        ["against chilled enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Chilled")]),
        ["you inflict on chilled enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Chilled")]),
        ["to chilled enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Chilled")]),
        ["inflicted on chilled enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Chilled")]),
        ["enemies which are chilled"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Chilled")]),
        ["against chilled or frozen enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", VarList: ["Chilled", "Frozen"])]),
        ["against frozen, shocked or ignited enemies"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", VarList: ["Frozen", "Shocked", "Ignited"])]),
        ["against enemies affected by elemental ailments"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", VarList: ["Frozen", "Chilled", "Shocked", "Ignited", "Scorched", "Brittle", "Sapped"])]),
        ["against enemies affected by ailments"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", VarList: ["Frozen", "Chilled", "Shocked", "Ignited", "Scorched", "Brittle", "Sapped", "Poisoned", "Bleeding"])]),
        ["against enemies that are affected by elemental ailments"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", VarList: ["Frozen", "Chilled", "Shocked", "Ignited", "Scorched", "Brittle", "Sapped"])]),
        ["against enemies that are affected by no elemental ailments"] = new(Tags: [
            new TagSpec("ActorCondition", Actor: "enemy", VarList: ["Frozen", "Chilled", "Shocked", "Ignited", "Scorched", "Brittle", "Sapped"], Neg: true),
            new TagSpec("Condition", Var: "Effective"),
        ]),
        ["against enemies on consecrated ground"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "OnConsecratedGround")]),
        ["against enemies with a higher percentage of their life remaining than you"] = new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "HigherLifePercentThanPlayer")]),

        // Enemy multipliers - plain entries
        ["per freeze, shock and ignite on enemy"] = new(Tags: [new TagSpec("Multiplier", Var: "FreezeShockIgniteOnEnemy")]),
        ["per poison affecting enemy"] = new(Tags: [new TagSpec("Multiplier", Actor: "enemy", Var: "PoisonStack")]),
        ["for each spider's web on the enemy"] = new(Tags: [new TagSpec("Multiplier", Actor: "enemy", Var: "Spider's WebStack")]),
    };

    /// <summary>
    /// Helper to capitalize first letter of a string (equivalent to Lua's firstToUpper).
    /// </summary>
    private static string FirstToUpper(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    /// <summary>
    /// Regex-based entries for patterns with captures or Lua character classes.
    /// Each tuple is (pattern, factory function that receives captured groups and returns a ModTagEntry).
    /// </summary>
    public static readonly (string Pattern, Func<string[], ModTagEntry> Factory)[] RegexEntries =
    [
        // Empty/simple regex entries
        (@"for (\d+) seconds", _ => new()),

        // Multipliers with captures
        (@"per brand, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "ActiveBrand", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"per (\d+) rage",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "Rage", Div: double.Parse(caps[0]))])),

        (@"per mana burn, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "ManaBurnStacks", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"per (\d+) player levels",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "Level", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% (\w+) effect on enemy",
            caps => new(Tags: [new TagSpec("Multiplier", Var: FirstToUpper(caps[1]) + "Effect", Div: double.Parse(caps[0]), Actor: "enemy")])),

        (@"if (\d+) (\w+) items are equipped",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: FirstToUpper(caps[1]) + "Item", Threshold: double.Parse(caps[0]))])),

        // Abyss jewels with regex
        (@"per abyssa?l? jewel affecting you",
            _ => new(Tags: [new TagSpec("Multiplier", Var: "AbyssJewel")])),

        (@"for each herald b?u?f?f?s?k?i?l?l? ?affecting you",
            _ => new(Tags: [new TagSpec("Multiplier", Var: "Herald")])),

        (@"for each type of abyssa?l? jewel affecting you",
            _ => new(Tags: [new TagSpec("Multiplier", Var: "AbyssJewelType")])),

        (@"per (.+) eye jewel affecting you, up to a maximum of \+?(\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: FirstToUpper(caps[0]) + "EyeJewel", Limit: double.Parse(caps[1]), LimitTotal: true)])),

        // Poison stacks with limits
        (@"for each poison on you up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "PoisonStack", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"per poison on you, up to (\d+) per second",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "PoisonStack", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"for each poison you have inflicted recently, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "PoisonAppliedRecently", GlobalLimit: double.Parse(caps[0]), GlobalLimitKey: "DurationPerPoisonRecently")])),

        (@"for each time you have shocked a non-shocked enemy recently, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "ShockedNonShockedEnemyRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"per enemy killed recently, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "EnemyKilledRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"per (\d+) rampage kills",
            caps => { var d = double.Parse(caps[0]); return new(Tags: [new TagSpec("Multiplier", Var: "Rampage", Div: d, Limit: 1000 / d, LimitTotal: true)]); }),

        (@"per minion, up to ?a? ?m?a?x?i?m?u?m? ?o?f? (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "SummonedMinion", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"for each enemy you or your minions have killed recently, up to (\d+)%$",
            caps => new(Tags: [new TagSpec("Multiplier", VarList: ["EnemyKilledRecently", "EnemyKilledByMinionsRecently"], Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"for each enemy you or your minions have killed recently, up to (\d+)% per second",
            caps => new(Tags: [new TagSpec("Multiplier", VarList: ["EnemyKilledRecently", "EnemyKilledByMinionsRecently"], Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"for each (\d+) total mana y?o?u? ?h?a?v?e? ?spent recently$",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "ManaSpentRecently", Div: double.Parse(caps[0]))])),

        (@"for each (\d+) total mana you have spent recently, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "ManaSpentRecently", Div: double.Parse(caps[0]), Limit: double.Parse(caps[1]), LimitTotal: true)])),

        (@"per (\d+) mana spent recently, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "ManaSpentRecently", Div: double.Parse(caps[0]), Limit: double.Parse(caps[1]), LimitTotal: true)])),

        (@"per nearby enemy, up to \+?(\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "NearbyEnemies", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"per second you've been stationary, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "StationarySeconds", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        // Per stat with captures
        (@"per (\d+)% of maximum mana they reserve",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ManaReservedPercent", Div: double.Parse(caps[0]))])),

        (@"per (\d+) strength",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Str", Div: double.Parse(caps[0]))])),

        (@"per (\d+) dexterity",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Dex", Div: double.Parse(caps[0]))])),

        (@"per (\d+) intelligence",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Int", Div: double.Parse(caps[0]))])),

        (@"per (\d+) omniscience",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Omni", Div: double.Parse(caps[0]))])),

        (@"per (\d+) total attributes",
            caps => new(Tags: [new TagSpec("PerStat", StatList: ["Str", "Dex", "Int"], Div: double.Parse(caps[0]))])),

        (@"per (\d+) of your lowest attribute",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "LowestAttribute", Div: double.Parse(caps[0]))])),

        (@"per (\d+) reserved life",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "LifeReserved", Div: double.Parse(caps[0]))])),

        (@"per (\d+) unreserved maximum mana, up to (\d+)%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ManaUnreserved", Div: double.Parse(caps[0]), Limit: double.Parse(caps[1]), LimitTotal: true)])),

        (@"per (\d+) unreserved maximum mana$",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ManaUnreserved", Div: double.Parse(caps[0]))])),

        (@"per (\d+) armour$",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Armour", Div: double.Parse(caps[0]))])),

        (@"per (\d+) evasion rating, up to (\d+)%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Evasion", Div: double.Parse(caps[0]), Limit: double.Parse(caps[1]), LimitTotal: true)])),

        (@"per (\d+) evasion rating$",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Evasion", Div: double.Parse(caps[0]))])),

        (@"per (\d+) maximum energy shield$",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EnergyShield", Div: double.Parse(caps[0]))])),

        (@"per (\d+) maximum life$",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Life", Div: double.Parse(caps[0]))])),

        (@"per (\d+) of maximum life or maximum mana, whichever is lower",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "LowestOfMaximumLifeAndMaximumMana", Div: double.Parse(caps[0]))])),

        (@"per (\d+) player maximum life",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Life", Div: double.Parse(caps[0]), Actor: "parent")])),

        (@"per (\d+) maximum mana, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Mana", Div: double.Parse(caps[0]), Limit: double.Parse(caps[1]), LimitTotal: true)])),

        (@"per (\d+) maximum mana, up to (\d+)%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Mana", Div: double.Parse(caps[0]), Limit: double.Parse(caps[1]), LimitTotal: true)])),

        (@"per (\d+) maximum mana$",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Mana", Div: double.Parse(caps[0]))])),

        (@"per (\d+) accuracy rating",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Accuracy", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% block chance",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "BlockChance", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% chance to block on equipped shield",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ShieldBlockChance", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% chance to block attack damage",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "BlockChance", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% chance to block spell damage",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "SpellBlockChance", Div: double.Parse(caps[0]))])),

        (@"per (\d+) of the lowest of armour and evasion rating",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "LowestOfArmourAndEvasion", Div: double.Parse(caps[0]))])),

        (@"per (\d+) maximum energy shield on equipped helmet",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EnergyShieldOnHelmet", Div: double.Parse(caps[0]))])),

        (@"per (\d+) maximum energy shield on helmet",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EnergyShieldOnHelmet", Div: double.Parse(caps[0]))])),

        (@"per (\d+) evasion rating on body armour",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EvasionOnBody Armour", Div: double.Parse(caps[0]))])),

        (@"per (\d+) evasion rating on equipped body armour",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EvasionOnBody Armour", Div: double.Parse(caps[0]))])),

        (@"per (\d+) armour on equipped shield",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ArmourOnWeapon 2", Div: double.Parse(caps[0]))])),

        (@"per (\d+) armour or evasion rating on shield$",
            caps => new(Tags: [new TagSpec("PerStat", StatList: ["ArmourOnWeapon 2", "EvasionOnWeapon 2"], Div: double.Parse(caps[0]))])),

        (@"per (\d+) armour or evasion rating on equipped shield",
            caps => new(Tags: [new TagSpec("PerStat", StatList: ["ArmourOnWeapon 2", "EvasionOnWeapon 2"], Div: double.Parse(caps[0]))])),

        (@"per (\d+) evasion rating on equipped shield",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EvasionOnWeapon 2", Div: double.Parse(caps[0]))])),

        (@"per (\d+) maximum energy shield on equipped shield",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EnergyShieldOnWeapon 2", Div: double.Parse(caps[0]))])),

        (@"per (\d+) maximum energy shield on shield",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EnergyShieldOnWeapon 2", Div: double.Parse(caps[0]))])),

        (@"per (\d+) evasion on equipped boots",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EvasionOnBoots", Div: double.Parse(caps[0]))])),

        (@"per (\d+) evasion on boots",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "EvasionOnBoots", Div: double.Parse(caps[0]))])),

        (@"per (\d+) armour on equipped gloves",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ArmourOnGloves", Div: double.Parse(caps[0]))])),

        (@"per (\d+) armour on gloves",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ArmourOnGloves", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% chaos resistance",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ChaosResist", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% cold resistance above 75%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "ColdResistOver75", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% lightning resistance above 75%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "LightningResistOver75", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% fire resistance above 75%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "FireResistOver75", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% fire, cold, or lightning resistance above 75%",
            caps => new(Tags: [new TagSpec("PerStat", StatList: ["FireResistOver75", "ColdResistOver75", "LightningResistOver75"], Div: double.Parse(caps[0]))])),

        (@"per (\d+) devotion",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "Devotion", Actor: "parent", Div: double.Parse(caps[0]))])),

        (@"per (\d+)% missing fire resistance, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "MissingFireResist", Div: double.Parse(caps[0]), GlobalLimit: double.Parse(caps[1]), GlobalLimitKey: "ReplicaNebulisFire")])),

        (@"per (\d+)% missing cold resistance, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("PerStat", Stat: "MissingColdResist", Div: double.Parse(caps[0]), GlobalLimit: double.Parse(caps[1]), GlobalLimitKey: "ReplicaNebulisCold")])),

        (@"per (\d+)% missing fire, cold, or lightning resistance, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("PerStat", StatList: ["MissingFireResist", "MissingColdResist", "MissingLightningResist"], Div: double.Parse(caps[0]), GlobalLimit: double.Parse(caps[1]), GlobalLimitKey: "ReplicaNebulisCold")])),

        // Stat conditions with captures
        (@"with (\d+) or more strength",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Str", Threshold: double.Parse(caps[0]))])),

        (@"with at least (\d+) strength",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Str", Threshold: double.Parse(caps[0]))])),

        (@"w?h?i[lf]e? you have at least (\d+) strength",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Str", Threshold: double.Parse(caps[0]))])),

        (@"w?h?i[lf]e? you have at least (\d+) dexterity",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Dex", Threshold: double.Parse(caps[0]))])),

        (@"w?h?i[lf]e? you have at least (\d+) intelligence",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Int", Threshold: double.Parse(caps[0]))])),

        (@"w?h?i[lf]e? strength is below (\d+)",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Str", Threshold: double.Parse(caps[0]) - 1, Upper: true)])),

        (@"w?h?i[lf]e? dexterity is below (\d+)",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Dex", Threshold: double.Parse(caps[0]) - 1, Upper: true)])),

        (@"w?h?i[lf]e? intelligence is below (\d+)",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Int", Threshold: double.Parse(caps[0]) - 1, Upper: true)])),

        (@"at least (\d+) intelligence",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Int", Threshold: double.Parse(caps[0]))])),

        (@"w?h?i[lf]e? you have at least (\d+) maximum energy shield",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "EnergyShield", Threshold: double.Parse(caps[0]))])),

        (@"w?h?i[lf]e? you have at least (\d+) devotion",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "Devotion", Threshold: double.Parse(caps[0]))])),

        (@"while you have at least (\d+) rage",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "Rage", Threshold: double.Parse(caps[0]))])),

        // Slot conditions with regex
        (@"whi?l?en? in off hand",
            _ => new(Tags: [new TagSpec("SlotNumber", Num: 2)])),

        (@"w?i?t?h? main hand",
            _ => new(Tags: [new TagSpec("Condition", Var: "MainHandAttack"), new TagSpec("SkillType", SkillTypeValue: SkillType.Attack)])),

        (@"w?i?t?h? off ?hand",
            _ => new(Tags: [new TagSpec("Condition", Var: "OffHandAttack"), new TagSpec("SkillType", SkillTypeValue: SkillType.Attack)])),

        (@"[fi]?[rn]?[of]?[ml]?[ i]?[hc]?[it]?[te]?[sd]? ? with this weapon",
            _ => new(Tags: [new TagSpec("Condition", Var: "{Hand}Attack"), new TagSpec("SkillType", SkillTypeValue: SkillType.Attack)])),

        (@"if your o[tp][hp][eo][rs]i?t?e? ring is a shaper item",
            _ => new(Tags: [new TagSpec("ItemCondition", ItemSlot: "Ring {OtherSlotNum}", ShaperCond: true)])),

        (@"if your o[tp][hp][eo][rs]i?t?e? ring is an elder item",
            _ => new(Tags: [new TagSpec("ItemCondition", ItemSlot: "Ring {OtherSlotNum}", ElderCond: true)])),

        (@"if you have a (\w+) (\w+) in (\w+) slot",
            caps => new(Tags: [new TagSpec("Condition", Var: FirstToUpper(caps[0]) + "ItemIn" + FirstToUpper(caps[1]) + " " + (caps[2] == "right" ? "2" : caps[2] == "left" ? "1" : caps[2]))])),

        // Equipment conditions with regex captures
        (@"while holding a (\w+) or (\w+)",
            caps => new(Tags: [new TagSpec("Condition", VarList: ["Using" + FirstToUpper(caps[0]), "Using" + FirstToUpper(caps[1])])])),

        (@"while holding a (\w+)",
            caps => new(Tags: [new TagSpec("Condition", VarList: ["Using" + FirstToUpper(caps[0])])])),

        (@"if equipped ([\w\s]+) has an ([\w\s]+) modifier",
            caps => new(Tags: [new TagSpec("ItemCondition", SearchCond: caps[1], ItemSlot: caps[0])])),

        (@"if both equipped ([\w\s]+) have a?n? ?([\w\s]+) modifiers?",
            caps => new(Tags: [new TagSpec("ItemCondition", SearchCond: caps[1], ItemSlot: caps[0][..^1], BothSlots: true)])),

        (@"if both equipped left and right ([\w\s]+) have a?n? ?([\w\s]+) modifiers?",
            caps => new(Tags: [new TagSpec("ItemCondition", SearchCond: caps[1], ItemSlot: caps[0][..^1], BothSlots: true)])),

        (@"if there are no ([\w\s]+) modifiers on equipped ([\w\s]+)",
            caps => new(Tags: [new TagSpec("ItemCondition", SearchCond: caps[0], ItemSlot: caps[1], Neg: true)])),

        (@"if there are no (\w+) modifiers on other equipped items",
            caps => new(Tags: [new TagSpec("ItemCondition", SearchCond: caps[0], ItemSlot: "{SlotName}", AllSlots: true, ExcludeSelf: true, Neg: true)])),

        (@"if equipped shield has at least (\d+)% chance to block",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "ShieldBlockChance", Threshold: double.Parse(caps[0]))])),

        (@"if you have (\d+) primordial items socketed or equipped",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "PrimordialItem", Threshold: double.Parse(caps[0]))])),

        (@"with at least one (\w+) grafted to you",
            caps => new(Tags: [new TagSpec("Condition", Var: "Using" + FirstToUpper(caps[0]))])),

        // Player status conditions with regex (wh[ie][ln]e? patterns)
        (@"wh[ie][ln]e? on low life",
            _ => new(Tags: [new TagSpec("Condition", Var: "LowLife")])),

        (@"wh[ie][ln]e? not on low life",
            _ => new(Tags: [new TagSpec("Condition", Var: "LowLife", Neg: true)])),

        (@"wh[ie][ln]e? on low mana",
            _ => new(Tags: [new TagSpec("Condition", Var: "LowMana")])),

        (@"wh[ie][ln]e? not on low mana",
            _ => new(Tags: [new TagSpec("Condition", Var: "LowMana", Neg: true)])),

        (@"wh[ie][ln]e? on full life",
            _ => new(Tags: [new TagSpec("Condition", Var: "FullLife")])),

        (@"wh[ie][ln]e? not on full life",
            _ => new(Tags: [new TagSpec("Condition", Var: "FullLife", Neg: true)])),

        (@"wh[ie][ln]e? no life is reserved",
            _ => new(Tags: [new TagSpec("StatThreshold", Stat: "LifeReserved", Threshold: 0, Upper: true)])),

        (@"wh[ie][ln]e? no mana is reserved",
            _ => new(Tags: [new TagSpec("StatThreshold", Stat: "ManaReserved", Threshold: 0, Upper: true)])),

        (@"wh[ie][ln]e? on full energy shield",
            _ => new(Tags: [new TagSpec("Condition", Var: "FullEnergyShield")])),

        (@"wh[ie][ln]e? not on full energy shield",
            _ => new(Tags: [new TagSpec("Condition", Var: "FullEnergyShield", Neg: true)])),

        (@"wh[ie][ln]e? you have energy shield",
            _ => new(Tags: [new TagSpec("Condition", Var: "HaveEnergyShield")])),

        (@"wh[ie][ln]e? you have no energy shield",
            _ => new(Tags: [new TagSpec("Condition", Var: "HaveEnergyShield", Neg: true)])),

        (@"while focus?sed",
            _ => new(Tags: [new TagSpec("Condition", Var: "Focused")])),

        // Channelling time thresholds
        (@"after channelling for (\d+) seconds?",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "ChannellingTime", Threshold: double.Parse(caps[0]))])),

        (@"if you've been channelling for at least (\d+) seconds?",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "ChannellingTime", Threshold: double.Parse(caps[0]))])),

        // Charge thresholds
        (@"while you have at least (\d+) crab barriers",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "CrabBarriers", Threshold: double.Parse(caps[0]))])),

        (@"while you have at least (\d+) fortification",
            caps => new(Tags: [new TagSpec("StatThreshold", Stat: "FortificationStacks", Threshold: double.Parse(caps[0]))])),

        (@"while you have at least (\d+) total endurance, frenzy and power charges",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "TotalCharges", Threshold: double.Parse(caps[0]))])),

        // Recently conditions with regex (if you[' ]h?a?ve patterns)
        (@"if you[' ]h?a?ve suppressed spell damage recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "SuppressedRecently")])),

        (@"while t?h?e?r?e? ?i?s? ?a rare or unique enemy i?s? ?nearby",
            _ => new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", VarList: ["NearbyRareOrUniqueEnemy", "RareOrUnique"])])),

        (@"while at least (\d+) enemies are nearby",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "NearbyEnemies", Threshold: double.Parse(caps[0]))])),

        (@"if you[' ]h?a?ve hit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "HitRecently")])),

        (@"if you[' ]h?a?ve hit an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "HitRecently")])),

        (@"if you[' ]h?a?ve hit with your main hand weapon recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "HitRecentlyWithWeapon")])),

        (@"if you[' ]h?a?ve hit with your off hand weapon recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "HitRecentlyWithWeapon"), new TagSpec("Condition", Var: "DualWielding")])),

        (@"if you[' ]h?a?ve hit a cursed enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "HitRecently"), new TagSpec("ActorCondition", Actor: "enemy", Var: "Cursed")])),

        (@"if you[' ]h?a?ve crit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "CritRecently")])),

        (@"if you[' ]h?a?ve dealt a critical strike recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "CritRecently")])),

        (@"when you deal a critical strike",
            _ => new(Tags: [new TagSpec("Condition", Var: "CritRecently")])),

        (@"if you[' ]h?a?ve dealt a critical strike with this weapon recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "CritRecently")])),

        (@"if you[' ]h?a?ve crit in the past 8 seconds",
            _ => new(Tags: [new TagSpec("Condition", Var: "CritInPast8Sec")])),

        (@"if you[' ]h?a?ve dealt a crit in the past 8 seconds",
            _ => new(Tags: [new TagSpec("Condition", Var: "CritInPast8Sec")])),

        (@"if you[' ]h?a?ve dealt a critical strike in the past 8 seconds",
            _ => new(Tags: [new TagSpec("Condition", Var: "CritInPast8Sec")])),

        (@"if you[' ]h?a?ve dealt a non-critical strike recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "NonCritRecently")])),

        (@"if you[' ]h?a?ve dealt a critical strike with a two handed melee weapon recently",
            _ => new(Flags: ModFlag.Weapon2H | ModFlag.WeaponMelee, Tags: [new TagSpec("Condition", Var: "CritRecently")])),

        (@"if you[' ]h?a?ve killed recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "KilledRecently")])),

        (@"if you[' ]h?a?ve killed an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "KilledRecently")])),

        (@"if you[' ]h?a?ve killed at least (\d) enemies recently",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "EnemyKilledRecently", Threshold: double.Parse(caps[0]))])),

        (@"if you[' ]h?a?ve thrown a trap or mine recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "TrapOrMineThrownRecently")])),

        (@"if you[' ]h?a?ve killed a maimed enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "KilledRecently"), new TagSpec("ActorCondition", Actor: "enemy", Var: "Maimed")])),

        (@"if you[' ]h?a?ve killed a cursed enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "KilledRecently"), new TagSpec("ActorCondition", Actor: "enemy", Var: "Cursed")])),

        (@"if you[' ]h?a?ve killed a bleeding enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "KilledRecently"), new TagSpec("ActorCondition", Actor: "enemy", Var: "Bleeding")])),

        (@"if you[' ]h?a?ve killed an enemy affected by your damage over time recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "KilledAffectedByDotRecently")])),

        (@"if you[' ]h?a?ve frozen an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "FrozenEnemyRecently")])),

        (@"if you[' ]h?a?ve chilled an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "ChilledEnemyRecently")])),

        (@"if you[' ]h?a?ve ignited an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "IgnitedEnemyRecently")])),

        (@"if you[' ]h?a?ve shocked an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "ShockedEnemyRecently")])),

        (@"if you[' ]h?a?ve stunned an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "StunnedEnemyRecently")])),

        (@"if you[' ]h?a?ve stunned an enemy with a two handed melee weapon recently",
            _ => new(Flags: ModFlag.Weapon2H | ModFlag.WeaponMelee, Tags: [new TagSpec("Condition", Var: "StunnedEnemyRecently")])),

        (@"if you[' ]h?a?ve been hit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenHitRecently")])),

        (@"if you[' ]h?a?ve been hit by an attack recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenHitByAttackRecently")])),

        (@"if you were hit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenHitRecently")])),

        (@"if you were damaged by a hit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenHitRecently")])),

        (@"if you[' ]h?a?ve taken a critical strike recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenCritRecently")])),

        (@"if you[' ]h?a?ve taken a savage hit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenSavageHitRecently")])),

        (@"if you have ?n[o']t been hit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenHitRecently", Neg: true)])),

        (@"if you have ?n[o']t been hit by an attack recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenHitByAttackRecently", Neg: true)])),

        (@"if you[' ]h?a?ve taken no damage from hits recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenHitRecently", Neg: true)])),

        (@"if you[' ]h?a?ve taken fire damage from a hit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "HitByFireDamageRecently")])),

        (@"if you[' ]h?a?ve taken fire damage from an enemy hit recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "TakenFireDamageFromEnemyHitRecently")])),

        (@"if you[' ]h?a?ve taken spell damage recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "HitBySpellDamageRecently")])),

        (@"if you haven't taken damage recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BeenHitRecently", Neg: true)])),

        (@"if you[' ]h?a?ve blocked recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BlockedRecently")])),

        (@"if you haven't blocked recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BlockedRecently", Neg: true)])),

        (@"if you[' ]h?a?ve blocked an attack recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BlockedAttackRecently")])),

        (@"if you[' ]h?a?ve blocked attack damage recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BlockedAttackRecently")])),

        (@"if you[' ]h?a?ve blocked a spell recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BlockedSpellRecently")])),

        (@"if you[' ]h?a?ve blocked spell damage recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "BlockedSpellRecently")])),

        (@"if you[' ]h?a?ve blocked damage from a unique enemy in the past 10 seconds",
            _ => new(Tags: [new TagSpec("Condition", Var: "BlockedHitFromUniqueEnemyInPast10Sec")])),

        (@"if you[' ]h?a?ve attacked recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "AttackedRecently")])),

        (@"if you[' ]h?a?ve cast a spell recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "CastSpellRecently")])),

        (@"if you[' ]h?a?ve been stunned while casting recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "StunnedWhileCastingRecently")])),

        (@"if you[' ]h?a?ve consumed a corpse recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "ConsumedCorpseRecently")])),

        (@"if you[' ]h?a?ve cursed an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "CursedEnemyRecently")])),

        (@"if you[' ]h?a?ve cast a mark spell recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "CastMarkRecently")])),

        (@"if you have ?n[o']t consumed a corpse recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "ConsumedCorpseRecently", Neg: true)])),

        (@"if you[' ]h?a?ve taunted an enemy recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "TauntedEnemyRecently")])),

        (@"if you[' ]h?a?ve used a skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedSkillRecently")])),

        (@"if you[' ]h?a?ve used a travel skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedTravelSkillRecently")])),

        (@"for each skill you've used recently, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "SkillUsedRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"if you[' ]h?a?ve used a warcry recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedWarcryRecently")])),

        (@"if you[' ]h?a?ve warcried recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedWarcryRecently")])),

        (@"if you[' ]h?a?ve not warcried recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedWarcryRecently", Neg: true)])),

        (@"for each time you[' ]h?a?ve warcried recently",
            _ => new(Tags: [new TagSpec("Multiplier", Var: "WarcryUsedRecently")])),

        (@"if you[' ]h?a?ve warcried in the past 8 seconds",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedWarcryInPast8Seconds")])),

        (@"for each second you've been affected by a warcry buff, up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "AffectedByWarcryBuffDuration", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"for each of your mines detonated recently, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "MineDetonatedRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"[fp][oe]r ?e?a?c?h? mine detonated recently, up to (\d+)% per second",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "MineDetonatedRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"[fp][oe]r ?e?a?c?h? mine detonated recently, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "MineDetonatedRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"[fp][oe]r ?e?a?c?h? mine detonated recently",
            _ => new(Tags: [new TagSpec("Multiplier", Var: "MineDetonatedRecently")])),

        (@"for each of your traps triggered recently, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "TrapTriggeredRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"for each trap triggered recently, up to (\d+)% per second",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "TrapTriggeredRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"for each trap triggered recently, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "TrapTriggeredRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"if you[' ]h?a?ve used a fire skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedFireSkillRecently")])),

        (@"if you[' ]h?a?ve used a cold skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedColdSkillRecently")])),

        (@"if you[' ]h?a?ve used a fire skill in the past 10 seconds",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedFireSkillInPast10Sec")])),

        (@"if you[' ]h?a?ve used a cold skill in the past 10 seconds",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedColdSkillInPast10Sec")])),

        (@"if you[' ]h?a?ve used a lightning skill in the past 10 seconds",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedLightningSkillInPast10Sec")])),

        (@"if you[' ]h?a?ve summoned a totem recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "SummonedTotemRecently")])),

        (@"if you[' ]h?a?ve used a minion skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedMinionSkillRecently")])),

        (@"if you[' ]h?a?ve used a movement skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedMovementSkillRecently")])),

        (@"if you[' ]h?a?ve cast dash recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "CastDashRecently")])),

        (@"if you[' ]h?a?ve used a vaal skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedVaalSkillRecently")])),

        (@"if you[' ]h?a?ve used a socketed vaal skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedVaalSkillRecently")])),

        (@"if you[' ]h?a?ve used a brand skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedBrandRecently")])),

        (@"if you[' ]h?a?ve used a retaliation skill recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "UsedRetaliationRecently")])),

        (@"if you[' ]h?a?ve spent (\d+) total mana recently",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "ManaSpentRecently", Threshold: double.Parse(caps[0]))])),

        (@"if you[' ]h?a?ve spent life recently",
            _ => new(Tags: [new TagSpec("MultiplierThreshold", Var: "LifeSpentRecently", Threshold: 1)])),

        (@"for \d+ seconds after spending a total of (\d+) mana",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Var: "ManaSpentRecently", Threshold: double.Parse(caps[0]))])),

        (@"if you[' ]h?a?ve detonated a mine recently",
            _ => new(Tags: [new TagSpec("Condition", Var: "DetonatedMinesRecently")])),

        (@"for each hit you've taken recently up to a maximum of (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "BeenHitRecently", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        (@"for each nearby enemy, up to (\d+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Var: "NearbyEnemies", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        // Enemy status conditions with regex
        (@"by s?l?a?i?n? rare [ao][nr]d? unique enemies",
            _ => new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "RareOrUnique")])),

        (@"against enemies affected by (\d+) or more poisons",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Actor: "enemy", Var: "PoisonStack", Threshold: double.Parse(caps[0]))])),

        (@"against enemies affected by at least (\d+) poisons",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Actor: "enemy", Var: "PoisonStack", Threshold: double.Parse(caps[0]))])),

        (@"against enemies affected by (\d+) spider's webs",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Actor: "enemy", Var: "Spider's WebStack", Threshold: double.Parse(caps[0]))])),

        (@"if (\d+)% of curse duration expired",
            caps => new(Tags: [new TagSpec("MultiplierThreshold", Actor: "enemy", Var: "CurseExpired", Threshold: double.Parse(caps[0]))])),

        (@"against enemies with (\w+) exposure",
            caps => new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Has" + FirstToUpper(caps[0]) + "Exposure")])),

        (@"by s?l?a?i?n? ?frozen enemies",
            _ => new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Frozen")])),

        (@"by s?l?a?i?n? ?shocked enemies",
            _ => new(Tags: [new TagSpec("ActorCondition", Actor: "enemy", Var: "Shocked")])),

        // Enemy multipliers with regex
        (@"per freeze, shock [ao][nr]d? ignite on enemy",
            _ => new(Tags: [new TagSpec("Multiplier", Var: "FreezeShockIgniteOnEnemy")])),

        (@"per poison affecting enemy, up to \+?([\d.]+)%",
            caps => new(Tags: [new TagSpec("Multiplier", Actor: "enemy", Var: "PoisonStack", Limit: double.Parse(caps[0]), LimitTotal: true)])),

        // "for each different non-instant spell you[' ]h?a?ve cast recently" regex variant
        (@"for each different non-instant spell you[' ]h?a?ve cast recently",
            _ => new(Tags: [new TagSpec("Multiplier", Var: "NonInstantSpellCastRecently")])),
    ];
}
