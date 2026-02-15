using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Per-type entry in the conversion table: conversion fractions, gain fractions,
/// combined multipliers, and remaining multiplier after conversions out.
/// </summary>
public class ConversionEntry
{
    /// <summary>Conversion fractions: conversion[destType] = fraction converted.</summary>
    public Dictionary<string, double> Conversion { get; } = new();

    /// <summary>Gain fractions: gain[destType] = fraction gained as.</summary>
    public Dictionary<string, double> Gain { get; } = new();

    /// <summary>Combined: this[destType] = conversion + gain fraction.</summary>
    private readonly Dictionary<string, double> _combined = new();

    /// <summary>Remaining multiplier after conversions out (1 - total conversion %).</summary>
    public double Mult { get; internal set; } = 1;

    public double GetCombined(string destType)
        => _combined.TryGetValue(destType, out double v) ? v : 0;

    internal void SetCombined(string destType, double value)
        => _combined[destType] = value;
}

/// <summary>
/// Damage conversion table: stores per-source-type conversion/gain fractions.
/// Ported from CalcOffence.lua buildConversionTable (lines 1839-1884).
/// </summary>
public class ConversionTable
{
    private readonly Dictionary<string, ConversionEntry> _entries = new();

    public ConversionEntry this[string damageType]
    {
        get
        {
            if (!_entries.TryGetValue(damageType, out var entry))
            {
                entry = new ConversionEntry();
                _entries[damageType] = entry;
            }
            return entry;
        }
    }

    /// <summary>
    /// Build a conversion table by reading conversion and gain mods from a ModStore.
    /// </summary>
    /// <param name="modList">The modifier store to query.</param>
    /// <param name="cfg">Optional modifier config for conditional evaluation.</param>
    public static ConversionTable Build(ModStore modList, ModConfig? cfg = null)
    {
        var table = new ConversionTable();
        var dmgTypeList = DamageTypeFlags.DmgTypeList;

        // Process first 4 types (Physical, Lightning, Cold, Fire) — Chaos has no conversions
        for (int srcIdx = 0; srcIdx < 4; srcIdx++)
        {
            string srcType = dmgTypeList[srcIdx];
            bool srcIsElemental = DamageTypeFlags.IsElemental(srcType);
            bool srcIsNotChaos = srcType != "Chaos";

            var globalConv = new Dictionary<string, double>();
            var skillConv = new Dictionary<string, double>();
            var gain = new Dictionary<string, double>();
            double globalTotal = 0;
            double skillTotal = 0;

            for (int destIdx = srcIdx + 1; destIdx < 5; destIdx++)
            {
                string destType = dmgTypeList[destIdx];

                // Global conversions
                double gc = modList.Sum(ModType.Base, cfg,
                    $"{srcType}DamageConvertTo{destType}");
                if (srcIsElemental)
                    gc = Math.Max(gc + modList.Sum(ModType.Base, cfg, $"ElementalDamageConvertTo{destType}"), 0);
                else if (srcIsNotChaos)
                    gc = Math.Max(gc + modList.Sum(ModType.Base, cfg, $"NonChaosDamageConvertTo{destType}"), 0);
                else
                    gc = Math.Max(gc, 0);

                globalConv[destType] = gc;
                globalTotal += gc;

                // Skill-specific conversions
                double sc = Math.Max(modList.Sum(ModType.Base, cfg, $"Skill{srcType}DamageConvertTo{destType}"), 0);
                skillConv[destType] = sc;
                skillTotal += sc;

                // Gain-as
                double g = modList.Sum(ModType.Base, cfg, $"{srcType}DamageGainAs{destType}");
                if (srcIsElemental)
                    g += modList.Sum(ModType.Base, cfg, $"ElementalDamageGainAs{destType}");
                if (srcIsNotChaos)
                    g += modList.Sum(ModType.Base, cfg, $"NonChaosDamageGainAs{destType}");
                gain[destType] = Math.Max(g, 0);
            }

            // Cap conversions at 100%
            if (skillTotal > 100)
            {
                double factor = 100 / skillTotal;
                foreach (var key in new List<string>(skillConv.Keys))
                    skillConv[key] *= factor;
                foreach (var key in new List<string>(globalConv.Keys))
                    globalConv[key] = 0;
                globalTotal = 0;
                skillTotal = 100;
            }
            else if (globalTotal + skillTotal > 100)
            {
                double factor = (100 - skillTotal) / globalTotal;
                foreach (var key in new List<string>(globalConv.Keys))
                    globalConv[key] *= factor;
                globalTotal *= factor;
            }

            var entry = table[srcType];
            foreach (var destType in globalConv.Keys)
            {
                entry.Conversion[destType] = (globalConv[destType] + skillConv[destType]) / 100;
                entry.Gain[destType] = gain[destType] / 100;
                entry.SetCombined(destType, (globalConv[destType] + skillConv[destType] + gain[destType]) / 100);
            }
            entry.Mult = 1 - Math.Min((globalTotal + skillTotal) / 100, 1);
        }

        // Chaos has no conversions
        table["Chaos"].Mult = 1;

        return table;
    }
}
