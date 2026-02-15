using System.Text.RegularExpressions;

namespace PathOfBuilding.Core.Modifiers.Parsing;

/// <summary>
/// Modifier form types — how a modifier's value is expressed in text.
/// Ported from ModParser.lua formList.
/// </summary>
public enum ModForm
{
    INC,            // "X% increased"
    RED,            // "X% reduced"
    MORE,           // "X% more"
    LESS,           // "X% less"
    BASE,           // "+X to" / flat values
    GAIN,           // "gain X"
    LOSE,           // "lose X"
    GRANTS,         // "grants X" (local)
    REMOVES,        // "removes X" (local)
    CHANCE,         // "X% chance"
    PEN,            // "penetrates X%"
    BASECOST,       // "skills cost X"
    TOTALCOST,      // "costs X"
    REGENFLAT,      // "X life regenerated per second"
    REGENPERCENT,   // "X% life regenerated per second"
    DEGENFLAT,      // "X life lost per second"
    DEGENPERCENT,   // "X% life lost per second"
    DEGEN,          // "X fire damage per second"
    DMG,            // "X to Y added damage"
    DMGATTACKS,     // "adds X to Y damage to attacks"
    DMGSPELLS,      // "adds X to Y damage to spells"
    DMGBOTH,        // "adds X to Y damage to attacks and spells"
    FLAG,           // "you have" / "gain"
    OVERRIDE,       // "is X"
    DOUBLED,        // "is doubled"
}

/// <summary>
/// Static form pattern data. Each entry maps a regex to a ModForm value.
/// Patterns are checked in order; the scan algorithm finds the earliest/longest match.
/// </summary>
public static class FormPatterns
{
    private static readonly RegexOptions Opts = RegexOptions.Compiled | RegexOptions.IgnoreCase;

    /// <summary>
    /// The formList patterns ported from ModParser.lua lines 64-150.
    /// Order matters: more specific patterns should come before less specific ones.
    /// The scan algorithm picks the earliest match, then longest, then most specific pattern.
    /// </summary>
    public static readonly (Regex Pattern, ModForm Form)[] List =
    [
        // INC / RED / MORE / LESS
        (new(@"^(\d+)% increased", Opts), ModForm.INC),
        (new(@"^(\d+)% faster", Opts), ModForm.INC),
        (new(@"^(\d+)% reduced", Opts), ModForm.RED),
        (new(@"^(\d+)% slower", Opts), ModForm.RED),
        (new(@"^(\d+)% more", Opts), ModForm.MORE),
        (new(@"^(\d+)% less", Opts), ModForm.LESS),

        // BASE forms (with +/- prefix)
        (new(@"^([+\-][\d.]+)%? to\b", Opts), ModForm.BASE),
        (new(@"^([+\-]?[\d.]+)%? of\b", Opts), ModForm.BASE),
        (new(@"^([+\-][\d.]+)%? base\b", Opts), ModForm.BASE),
        (new(@"^([+\-]?[\d.]+)%? additional\b", Opts), ModForm.BASE),
        (new(@"(\d+) additional hits?", Opts), ModForm.BASE),
        (new(@"(\d+) additional times?", Opts), ModForm.BASE),
        (new(@"^throw up to (\d+)", Opts), ModForm.BASE),

        // GAIN / LOSE
        (new(@"^you gain ([\d.]+)", Opts), ModForm.GAIN),
        (new(@"^gains? ([\d.]+)% of\b", Opts), ModForm.GAIN),
        (new(@"^gain ([\d.]+)", Opts), ModForm.GAIN),
        (new(@"^gain \+(\d+)% to\b", Opts), ModForm.GAIN),
        (new(@"^you lose ([\d.]+)", Opts), ModForm.LOSE),
        (new(@"^loses? ([\d.]+)% of\b", Opts), ModForm.LOSE),
        (new(@"^lose ([\d.]+)", Opts), ModForm.LOSE),
        (new(@"^lose \+(\d+)% to\b", Opts), ModForm.LOSE),

        // GRANTS / REMOVES (local)
        (new(@"^grants ([\d.]+)", Opts), ModForm.GRANTS),
        (new(@"^removes? ([\d.]+) ?o?f? ?y?o?u?r?", Opts), ModForm.REMOVES),

        // CHANCE
        (new(@"^([+\-]?\d+)% chance to gain ", Opts), ModForm.FLAG),
        (new(@"^([+\-]?\d+)% chance\b", Opts), ModForm.CHANCE),
        (new(@"^([+\-]?\d+)% additional chance\b", Opts), ModForm.CHANCE),

        // COST
        (new(@"costs? ([+\-]?\d+)", Opts), ModForm.TOTALCOST),
        (new(@"skills cost ([+\-]?\d+)", Opts), ModForm.BASECOST),

        // PEN
        (new(@"penetrates (\d+)% of enemy", Opts), ModForm.PEN),
        (new(@"penetrates (\d+)% of\b", Opts), ModForm.PEN),
        (new(@"penetrates? (\d+)%", Opts), ModForm.PEN),

        // REGEN (flat and percent)
        (new(@"^([\d.]+)% of (.+) regenerated per second", Opts), ModForm.REGENPERCENT),
        (new(@"^([\d.]+)% (.+) regenerated per second", Opts), ModForm.REGENPERCENT),
        (new(@"^([\d.]+) (.+) regenerated per second", Opts), ModForm.REGENFLAT),
        (new(@"^regenerate ([\d.]+)% of (.+?) per second", Opts), ModForm.REGENPERCENT),
        (new(@"^regenerate ([\d.]+)% of your (.+?) per second", Opts), ModForm.REGENPERCENT),
        (new(@"^regenerate ([\d.]+)% (.+?) per second", Opts), ModForm.REGENPERCENT),
        (new(@"^regenerate ([\d.]+) (.+?) per second", Opts), ModForm.REGENFLAT),
        (new(@"^you regenerate ([\d.]+)% of (.+?) per second", Opts), ModForm.REGENPERCENT),

        // DEGEN (flat and percent)
        (new(@"^([\d.]+)% of (.+) lost per second", Opts), ModForm.DEGENPERCENT),
        (new(@"^([\d.]+)% (.+) lost per second", Opts), ModForm.DEGENPERCENT),
        (new(@"^([\d.]+) (.+) lost per second", Opts), ModForm.DEGENFLAT),
        (new(@"^lose ([\d.]+)% of (.+?) per second", Opts), ModForm.DEGENPERCENT),
        (new(@"^lose ([\d.]+)% of your (.+?) per second", Opts), ModForm.DEGENPERCENT),
        (new(@"^lose ([\d.]+)% (.+?) per second", Opts), ModForm.DEGENPERCENT),
        (new(@"^lose ([\d.]+) (.+?) per second", Opts), ModForm.DEGENFLAT),
        (new(@"^you lose ([\d.]+)% of (.+?) per second", Opts), ModForm.DEGENPERCENT),

        // DEGEN (damage type)
        (new(@"^([\d.]+) (\w+) damage taken per second", Opts), ModForm.DEGEN),
        (new(@"^([\d.]+) (\w+) damage per second", Opts), ModForm.DEGEN),

        // DMG (damage ranges)
        (new(@"adds (\d+) to (\d+) (\w+) damage to attacks and spells", Opts), ModForm.DMGBOTH),
        (new(@"adds (\d+)-(\d+) (\w+) damage to attacks and spells", Opts), ModForm.DMGBOTH),
        (new(@"adds (\d+) to (\d+) (\w+) damage to spells and attacks", Opts), ModForm.DMGBOTH),
        (new(@"adds (\d+)-(\d+) (\w+) damage to spells and attacks", Opts), ModForm.DMGBOTH),
        (new(@"adds (\d+) to (\d+) (\w+) damage to hits", Opts), ModForm.DMGBOTH),
        (new(@"adds (\d+)-(\d+) (\w+) damage to hits", Opts), ModForm.DMGBOTH),
        (new(@"adds (\d+) to (\d+) (\w+) damage to attacks", Opts), ModForm.DMGATTACKS),
        (new(@"adds (\d+)-(\d+) (\w+) damage to attacks", Opts), ModForm.DMGATTACKS),
        (new(@"adds (\d+) to (\d+) (\w+) attack damage", Opts), ModForm.DMGATTACKS),
        (new(@"adds (\d+)-(\d+) (\w+) attack damage", Opts), ModForm.DMGATTACKS),
        (new(@"(\d+) to (\d+) added attack (\w+) damage", Opts), ModForm.DMGATTACKS),
        (new(@"adds (\d+) to (\d+) (\w+) damage to spells", Opts), ModForm.DMGSPELLS),
        (new(@"adds (\d+)-(\d+) (\w+) damage to spells", Opts), ModForm.DMGSPELLS),
        (new(@"adds (\d+) to (\d+) (\w+) spell damage", Opts), ModForm.DMGSPELLS),
        (new(@"adds (\d+)-(\d+) (\w+) spell damage", Opts), ModForm.DMGSPELLS),
        (new(@"(\d+) to (\d+) added spell (\w+) damage", Opts), ModForm.DMGSPELLS),
        (new(@"(\d+) to (\d+) spell (\w+) damage", Opts), ModForm.DMGSPELLS),
        (new(@"adds (\d+) to (\d+) (\w+) damage", Opts), ModForm.DMG),
        (new(@"adds (\d+)-(\d+) (\w+) damage", Opts), ModForm.DMG),
        (new(@"(\d+) to (\d+) additional (\w+) damage", Opts), ModForm.DMG),
        (new(@"(\d+)-(\d+) additional (\w+) damage", Opts), ModForm.DMG),
        (new(@"(\d+) to (\d+) added (\w+) damage", Opts), ModForm.DMG),
        (new(@"(\d+)-(\d+) added (\w+) damage", Opts), ModForm.DMG),
        (new(@"^(\d+) to (\d+) (\w+) damage", Opts), ModForm.DMG),

        // FLAG / OVERRIDE / DOUBLED
        (new(@"^you have ", Opts), ModForm.FLAG),
        (new(@"^have ", Opts), ModForm.FLAG),
        (new(@"^you are ", Opts), ModForm.FLAG),
        (new(@"^are ", Opts), ModForm.FLAG),
        (new(@"^gain ", Opts), ModForm.FLAG),
        (new(@"^you gain ", Opts), ModForm.FLAG),
        (new(@"is (-?\d+)%? ", Opts), ModForm.OVERRIDE),
        (new(@"is doubled", Opts), ModForm.DOUBLED),
        (new(@"doubles?", Opts), ModForm.DOUBLED),
        (new(@"causes? double", Opts), ModForm.DOUBLED),

        // Generic BASE (must be last - catches bare numbers at start)
        (new(@"^([+\-][\d.]+)%?", Opts), ModForm.BASE),
        (new(@"^(\d+)", Opts), ModForm.BASE),
    ];
}
