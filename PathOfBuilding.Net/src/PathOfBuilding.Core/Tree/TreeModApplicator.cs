using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Modifiers.Parsing;

namespace PathOfBuilding.Core.Tree;

/// <summary>
/// Applies passive tree node modifiers to a player's ModDB.
/// For each allocated node, parses the stat descriptions via ModParser
/// and adds the resulting mods to the player's modifier database.
/// </summary>
public static class TreeModApplicator
{
    /// <summary>
    /// Apply allocated tree node mods to the player ModDB.
    /// </summary>
    public static void Apply(TreeData treeData, TreeSpecData treeSpec, ModDB playerModDB)
    {
        // Apply allocated node mods
        foreach (int nodeId in treeSpec.AllocatedNodes)
        {
            if (!treeData.Nodes.TryGetValue(nodeId, out var node))
                continue;

            // Skip mastery nodes themselves (their effects come from MasterySelections)
            if (node.IsMastery)
                continue;

            ApplyNodeStats(node.Stats, $"Tree:{nodeId}", playerModDB);
        }

        // Apply mastery selections
        foreach (var (nodeId, effectId) in treeSpec.MasterySelections)
        {
            if (!treeData.Nodes.TryGetValue(nodeId, out var node))
                continue;

            if (node.MasteryEffects == null)
                continue;

            if (!node.MasteryEffects.TryGetValue(effectId, out var effectStats))
                continue;

            ApplyNodeStats(effectStats, $"Tree:{nodeId}:Mastery:{effectId}", playerModDB);
        }
    }

    /// <summary>
    /// Parse stat description strings and add resulting mods to ModDB.
    /// </summary>
    private static void ApplyNodeStats(List<string> stats, string source, ModDB modDB)
    {
        foreach (string statText in stats)
        {
            var mods = ModParser.ParseMod(statText);
            if (mods == null)
                continue;

            foreach (var mod in mods)
            {
                var treeMod = new Mod
                {
                    Name = mod.Name,
                    Type = mod.Type,
                    Value = mod.Value,
                    Flags = mod.Flags,
                    KeywordFlags = mod.KeywordFlags,
                    Source = source,
                    Tags = mod.Tags,
                };
                modDB.AddMod(treeMod);
            }
        }
    }
}
