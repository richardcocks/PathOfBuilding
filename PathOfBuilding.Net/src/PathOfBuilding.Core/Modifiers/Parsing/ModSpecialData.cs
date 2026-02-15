using System.Text.RegularExpressions;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Modifiers.Parsing;

/// <summary>
/// Special modifier patterns that bypass the normal parse pipeline
/// and return complete mod lists directly.
/// Ported from ModParser.lua specialModList (core subset ~100 patterns).
/// </summary>
public static class ModSpecialData
{
    /// <summary>
    /// Represents a special mod value that is either a static mod list or a function.
    /// </summary>
    public class SpecialModValue
    {
        public bool IsFunction { get; init; }
        public List<Mod>? Mods { get; init; }
        public Func<string[], List<Mod>>? Function { get; init; }

        public static SpecialModValue FromMods(params Mod[] mods) => new()
        {
            IsFunction = false,
            Mods = [.. mods],
        };

        public static SpecialModValue FromFunc(Func<string[], List<Mod>> func) => new()
        {
            IsFunction = true,
            Function = func,
        };
    }

    private static Mod M(string name, ModType type, ModValue value, params ModTag[] tags) =>
        ModHelper.CreateMod(name, type, value, "", ModFlag.None, KeywordFlag.None, tags);

    private static Mod MF(string name, ModType type, ModValue value, ModFlag flags, params ModTag[] tags) =>
        ModHelper.CreateMod(name, type, value, "", flags, KeywordFlag.None, tags);

    private static Mod MK(string name, ModType type, ModValue value, KeywordFlag kw, params ModTag[] tags) =>
        ModHelper.CreateMod(name, type, value, "", ModFlag.None, kw, tags);

    private static readonly RegexOptions Opts = RegexOptions.Compiled | RegexOptions.IgnoreCase;

    /// <summary>
    /// Core subset of special modifier patterns.
    /// Each entry is (regex pattern, SpecialModValue).
    /// </summary>
    public static readonly (Regex Pattern, SpecialModValue Value)[] SpecialModList =
    [
        // ─── Keystones ───
        (new(@"^iron reflexes$", Opts),
            SpecialModValue.FromMods(M("Keystone:IronReflexes", ModType.Flag, true))),
        (new(@"^resolute technique$", Opts),
            SpecialModValue.FromMods(M("Keystone:ResoluteTechnique", ModType.Flag, true))),
        (new(@"^acrobatics$", Opts),
            SpecialModValue.FromMods(M("Keystone:Acrobatics", ModType.Flag, true))),
        (new(@"^ancestral bond$", Opts),
            SpecialModValue.FromMods(M("Keystone:AncestralBond", ModType.Flag, true))),
        (new(@"^avatar of fire$", Opts),
            SpecialModValue.FromMods(M("Keystone:AvatarOfFire", ModType.Flag, true))),
        (new(@"^blood magic$", Opts),
            SpecialModValue.FromMods(M("Keystone:BloodMagic", ModType.Flag, true))),
        (new(@"^chaos inoculation$", Opts),
            SpecialModValue.FromMods(M("Keystone:ChaosInoculation", ModType.Flag, true))),
        (new(@"^conduit$", Opts),
            SpecialModValue.FromMods(M("Keystone:Conduit", ModType.Flag, true))),
        (new(@"^crimson dance$", Opts),
            SpecialModValue.FromMods(M("Keystone:CrimsonDance", ModType.Flag, true))),
        (new(@"^eldritch battery$", Opts),
            SpecialModValue.FromMods(M("Keystone:EldritchBattery", ModType.Flag, true))),
        (new(@"^elemental equilibrium$", Opts),
            SpecialModValue.FromMods(M("Keystone:ElementalEquilibrium", ModType.Flag, true))),
        (new(@"^elemental overload$", Opts),
            SpecialModValue.FromMods(M("Keystone:ElementalOverload", ModType.Flag, true))),
        (new(@"^ghost reaver$", Opts),
            SpecialModValue.FromMods(M("Keystone:GhostReaver", ModType.Flag, true))),
        (new(@"^ghost dance$", Opts),
            SpecialModValue.FromMods(M("Keystone:GhostDance", ModType.Flag, true))),
        (new(@"^mind over matter$", Opts),
            SpecialModValue.FromMods(M("Keystone:MindOverMatter", ModType.Flag, true))),
        (new(@"^necromantic aegis$", Opts),
            SpecialModValue.FromMods(M("Keystone:NecromanticAegis", ModType.Flag, true))),
        (new(@"^pain attunement$", Opts),
            SpecialModValue.FromMods(M("Keystone:PainAttunement", ModType.Flag, true))),
        (new(@"^perfect agony$", Opts),
            SpecialModValue.FromMods(M("Keystone:PerfectAgony", ModType.Flag, true))),
        (new(@"^point blank$", Opts),
            SpecialModValue.FromMods(M("Keystone:PointBlank", ModType.Flag, true))),
        (new(@"^runebinder$", Opts),
            SpecialModValue.FromMods(M("Keystone:Runebinder", ModType.Flag, true))),
        (new(@"^unwavering stance$", Opts),
            SpecialModValue.FromMods(M("Keystone:UnwaveringStance", ModType.Flag, true))),
        (new(@"^vaal pact$", Opts),
            SpecialModValue.FromMods(M("Keystone:VaalPact", ModType.Flag, true))),
        (new(@"^zealot's oath$", Opts),
            SpecialModValue.FromMods(M("Keystone:ZealotsOath", ModType.Flag, true))),
        (new(@"^wicked ward$", Opts),
            SpecialModValue.FromMods(M("Keystone:WickedWard", ModType.Flag, true))),
        (new(@"^eternal youth$", Opts),
            SpecialModValue.FromMods(M("Keystone:EternalYouth", ModType.Flag, true))),
        (new(@"^the agnostic$", Opts),
            SpecialModValue.FromMods(M("Keystone:TheAgnostic", ModType.Flag, true))),
        (new(@"^imbalanced guard$", Opts),
            SpecialModValue.FromMods(M("Keystone:ImbalancedGuard", ModType.Flag, true))),
        (new(@"^supreme ego$", Opts),
            SpecialModValue.FromMods(M("Keystone:SupremeEgo", ModType.Flag, true))),
        (new(@"^glancing blows$", Opts),
            SpecialModValue.FromMods(M("Keystone:GlancingBlows", ModType.Flag, true))),
        (new(@"^wind dancer$", Opts),
            SpecialModValue.FromMods(M("Keystone:WindDancer", ModType.Flag, true))),
        (new(@"^divine shield$", Opts),
            SpecialModValue.FromMods(M("Keystone:DivineShield", ModType.Flag, true))),
        (new(@"^lethe shade$", Opts),
            SpecialModValue.FromMods(M("Keystone:LetheShade", ModType.Flag, true))),
        (new(@"^precise technique$", Opts),
            SpecialModValue.FromMods(M("Keystone:PreciseTechnique", ModType.Flag, true))),
        (new(@"^iron will$", Opts),
            SpecialModValue.FromMods(M("Keystone:IronWill", ModType.Flag, true))),
        (new(@"^call to arms$", Opts),
            SpecialModValue.FromMods(M("Keystone:CallToArms", ModType.Flag, true))),
        (new(@"^the impaler$", Opts),
            SpecialModValue.FromMods(M("Keystone:TheImpaler", ModType.Flag, true))),
        (new(@"^arrow dancing$", Opts),
            SpecialModValue.FromMods(M("Keystone:ArrowDancing", ModType.Flag, true))),
        (new(@"^versatile combatant$", Opts),
            SpecialModValue.FromMods(M("Keystone:VersatileCombatant", ModType.Flag, true))),
        (new(@"^magebane$", Opts),
            SpecialModValue.FromMods(M("Keystone:Magebane", ModType.Flag, true))),

        // ─── Damage conversions ───
        (new(@"^(\d+)% of physical damage converted to fire damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("PhysicalDamageConvertToFire", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^(\d+)% of physical damage converted to cold damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("PhysicalDamageConvertToCold", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^(\d+)% of physical damage converted to lightning damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("PhysicalDamageConvertToLightning", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^(\d+)% of physical damage converted to chaos damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("PhysicalDamageConvertToChaos", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^(\d+)% of lightning damage converted to fire damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("LightningDamageConvertToFire", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^(\d+)% of lightning damage converted to cold damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("LightningDamageConvertToCold", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^(\d+)% of cold damage converted to fire damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("ColdDamageConvertToFire", ModType.Base, double.Parse(caps[0]))])),

        // ─── Gain as extra damage ───
        (new(@"^gain (\d+)% of physical damage as extra fire damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("PhysicalDamageGainAsFire", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^gain (\d+)% of physical damage as extra cold damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("PhysicalDamageGainAsCold", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^gain (\d+)% of physical damage as extra lightning damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("PhysicalDamageGainAsLightning", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^gain (\d+)% of physical damage as extra chaos damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("PhysicalDamageGainAsChaos", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^gain (\d+)% of fire damage as extra chaos damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("FireDamageGainAsChaos", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^gain (\d+)% of cold damage as extra chaos damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("ColdDamageGainAsChaos", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^gain (\d+)% of lightning damage as extra chaos damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("LightningDamageGainAsChaos", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^gain (\d+)% of elemental damage as extra chaos damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("ElementalDamageGainAsChaos", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^gain (\d+)% of non-chaos damage as extra chaos damage$", Opts),
            SpecialModValue.FromFunc(caps => [M("NonChaosDamageGainAsChaos", ModType.Base, double.Parse(caps[0]))])),

        // ─── Penetration ───
        (new(@"^damage penetrates (\d+)% fire resistance$", Opts),
            SpecialModValue.FromFunc(caps => [M("FirePenetration", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^damage penetrates (\d+)% cold resistance$", Opts),
            SpecialModValue.FromFunc(caps => [M("ColdPenetration", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^damage penetrates (\d+)% lightning resistance$", Opts),
            SpecialModValue.FromFunc(caps => [M("LightningPenetration", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^damage penetrates (\d+)% elemental resistances?$", Opts),
            SpecialModValue.FromFunc(caps => [M("ElementalPenetration", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^damage penetrates (\d+)% chaos resistance$", Opts),
            SpecialModValue.FromFunc(caps => [M("ChaosPenetration", ModType.Base, double.Parse(caps[0]))])),

        // ─── Resist reductions (enemy) ───
        (new(@"^nearby enemies have (-?\d+)% to fire resistance$", Opts),
            SpecialModValue.FromFunc(caps => [M("EnemyModifier", ModType.List,
                ModValue.FromComplex(M("FireResist", ModType.Base, double.Parse(caps[0]))))])),
        (new(@"^nearby enemies have (-?\d+)% to cold resistance$", Opts),
            SpecialModValue.FromFunc(caps => [M("EnemyModifier", ModType.List,
                ModValue.FromComplex(M("ColdResist", ModType.Base, double.Parse(caps[0]))))])),
        (new(@"^nearby enemies have (-?\d+)% to lightning resistance$", Opts),
            SpecialModValue.FromFunc(caps => [M("EnemyModifier", ModType.List,
                ModValue.FromComplex(M("LightningResist", ModType.Base, double.Parse(caps[0]))))])),
        (new(@"^nearby enemies have (-?\d+)% to chaos resistance$", Opts),
            SpecialModValue.FromFunc(caps => [M("EnemyModifier", ModType.List,
                ModValue.FromComplex(M("ChaosResist", ModType.Base, double.Parse(caps[0]))))])),
        (new(@"^nearby enemies have (-?\d+)% to all resistances$", Opts),
            SpecialModValue.FromFunc(caps =>
            {
                double v = double.Parse(caps[0]);
                return [
                    M("EnemyModifier", ModType.List, ModValue.FromComplex(M("ElementalResist", ModType.Base, v))),
                    M("EnemyModifier", ModType.List, ModValue.FromComplex(M("ChaosResist", ModType.Base, v))),
                ];
            })),

        // ─── Leech ───
        (new(@"^(\d+)% of physical attack damage leeched as life$", Opts),
            SpecialModValue.FromFunc(caps => [MF("PhysicalDamageLifeLeech", ModType.Base, double.Parse(caps[0]), ModFlag.Attack)])),
        (new(@"^(\d+)% of physical attack damage leeched as mana$", Opts),
            SpecialModValue.FromFunc(caps => [MF("PhysicalDamageManaLeech", ModType.Base, double.Parse(caps[0]), ModFlag.Attack)])),
        (new(@"^(\d+)% of attack damage leeched as life$", Opts),
            SpecialModValue.FromFunc(caps => [MF("DamageLifeLeech", ModType.Base, double.Parse(caps[0]), ModFlag.Attack)])),
        (new(@"^(\d+)% of attack damage leeched as mana$", Opts),
            SpecialModValue.FromFunc(caps => [MF("DamageManaLeech", ModType.Base, double.Parse(caps[0]), ModFlag.Attack)])),
        (new(@"^(\d+)% of damage leeched as life$", Opts),
            SpecialModValue.FromFunc(caps => [M("DamageLifeLeech", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^(\d+)% of damage leeched as mana$", Opts),
            SpecialModValue.FromFunc(caps => [M("DamageManaLeech", ModType.Base, double.Parse(caps[0]))])),

        // ─── Crit ───
        (new(@"^your critical strikes do not deal extra damage$", Opts),
            SpecialModValue.FromMods(M("NoCritMultiplier", ModType.Flag, true))),
        (new(@"^critical strikes deal no damage$", Opts),
            SpecialModValue.FromMods(M("NoCritDealsNoDmg", ModType.Flag, true))),
        (new(@"^never deal critical strikes$", Opts),
            SpecialModValue.FromMods(M("NeverCrit", ModType.Flag, true))),

        // ─── Immunity / avoidance ───
        (new(@"^immune to freeze$", Opts),
            SpecialModValue.FromMods(M("AvoidFreeze", ModType.Base, 100))),
        (new(@"^immune to chill$", Opts),
            SpecialModValue.FromMods(M("AvoidChill", ModType.Base, 100))),
        (new(@"^immune to shock$", Opts),
            SpecialModValue.FromMods(M("AvoidShock", ModType.Base, 100))),
        (new(@"^immune to ignite$", Opts),
            SpecialModValue.FromMods(M("AvoidIgnite", ModType.Base, 100))),
        (new(@"^you cannot be shocked$", Opts),
            SpecialModValue.FromMods(M("AvoidShock", ModType.Base, 100))),
        (new(@"^you cannot be frozen$", Opts),
            SpecialModValue.FromMods(M("AvoidFreeze", ModType.Base, 100))),
        (new(@"^you cannot be chilled$", Opts),
            SpecialModValue.FromMods(M("AvoidChill", ModType.Base, 100))),
        (new(@"^you cannot be ignited$", Opts),
            SpecialModValue.FromMods(M("AvoidIgnite", ModType.Base, 100))),
        (new(@"^cannot be stunned$", Opts),
            SpecialModValue.FromMods(M("AvoidStun", ModType.Base, 100))),
        (new(@"^you cannot be stunned$", Opts),
            SpecialModValue.FromMods(M("AvoidStun", ModType.Base, 100))),
        (new(@"^immune to elemental ailments$", Opts),
            SpecialModValue.FromMods(M("AvoidElementalAilments", ModType.Base, 100))),
        (new(@"^immune to poison$", Opts),
            SpecialModValue.FromMods(M("AvoidPoison", ModType.Base, 100))),
        (new(@"^immune to bleeding$", Opts),
            SpecialModValue.FromMods(M("AvoidBleed", ModType.Base, 100))),
        (new(@"^immune to curses$", Opts),
            SpecialModValue.FromMods(M("CurseEffectOnSelf", ModType.More, -100))),

        // ─── Cannot / disable ───
        (new(@"^cannot leech$", Opts),
            SpecialModValue.FromMods(
                M("CannotLeechLife", ModType.Flag, true),
                M("CannotLeechMana", ModType.Flag, true))),
        (new(@"^cannot leech life$", Opts),
            SpecialModValue.FromMods(M("CannotLeechLife", ModType.Flag, true))),
        (new(@"^cannot leech mana$", Opts),
            SpecialModValue.FromMods(M("CannotLeechMana", ModType.Flag, true))),
        (new(@"^cannot evade enemy attacks$", Opts),
            SpecialModValue.FromMods(M("CannotEvade", ModType.Flag, true))),
        (new(@"^cannot block$", Opts),
            SpecialModValue.FromMods(M("CannotBlockAttacks", ModType.Flag, true), M("CannotBlockSpells", ModType.Flag, true))),

        // ─── Misc common patterns ───
        (new(@"^hits can't be evaded$", Opts),
            SpecialModValue.FromMods(M("CannotBeEvaded", ModType.Flag, true))),
        (new(@"^your hits can't be evaded$", Opts),
            SpecialModValue.FromMods(M("CannotBeEvaded", ModType.Flag, true))),

        (new(@"^socketed gems are supported by level (\d+) (.+)$", Opts),
            SpecialModValue.FromFunc(caps =>
            {
                // This is a complex mod that requires the skill system
                // For now, create a placeholder
                return [M("ExtraSupport", ModType.List,
                    ModValue.FromComplex(new Dictionary<string, object?> { ["level"] = double.Parse(caps[0]), ["name"] = caps[1] }))];
            })),

        (new(@"^\+(\d+) to level of socketed gems$", Opts),
            SpecialModValue.FromFunc(caps => [M("GemProperty", ModType.List,
                ModValue.FromComplex(new Dictionary<string, object?> { ["key"] = "level", ["value"] = double.Parse(caps[0]) }))])),

        (new(@"^\+(\d+) to level of all (.+) skill gems$", Opts),
            SpecialModValue.FromFunc(caps => [M("GemProperty", ModType.List,
                ModValue.FromComplex(new Dictionary<string, object?> { ["key"] = "level", ["keywordString"] = caps[1], ["value"] = double.Parse(caps[0]) }))])),

        // ─── Curse/aura special ───
        (new(@"^enemies can have 1 additional curse$", Opts),
            SpecialModValue.FromMods(M("EnemyCurseLimit", ModType.Base, 1))),
        (new(@"^enemies can have (\d+) additional curses?$", Opts),
            SpecialModValue.FromFunc(caps => [M("EnemyCurseLimit", ModType.Base, double.Parse(caps[0]))])),
        (new(@"^you can apply an additional curse$", Opts),
            SpecialModValue.FromMods(M("EnemyCurseLimit", ModType.Base, 1))),
        (new(@"^you can apply (\d+) additional curses?$", Opts),
            SpecialModValue.FromFunc(caps => [M("EnemyCurseLimit", ModType.Base, double.Parse(caps[0]))])),
    ];
}
