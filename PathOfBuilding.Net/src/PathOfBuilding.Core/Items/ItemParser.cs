using System.Globalization;
using System.Text.RegularExpressions;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers.Parsing;

namespace PathOfBuilding.Core.Items;

/// <summary>
/// Parses ItemData.RawText into structured ParsedItem objects.
/// State machine mirrors Lua Item:ParseRaw.
/// </summary>
public static class ItemParser
{
    private static readonly Regex FlagPattern = new(@"\{([a-z]+)(?::.*?)?\}", RegexOptions.Compiled);

    public static ParsedItem Parse(ItemData itemData)
    {
        return Parse(itemData.Id, itemData.RawText);
    }

    public static ParsedItem Parse(int id, string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new ParsedItem { Id = id };
        }

        var lines = rawText.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
            return new ParsedItem { Id = id };

        // Phase 1: Extract rarity
        var rarity = ItemRarity.Normal;
        int lineIdx = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("Rarity:", StringComparison.OrdinalIgnoreCase))
            {
                var rarityStr = lines[i]["Rarity:".Length..].Trim().ToUpperInvariant();
                rarity = rarityStr switch
                {
                    "NORMAL" => ItemRarity.Normal,
                    "MAGIC" => ItemRarity.Magic,
                    "RARE" => ItemRarity.Rare,
                    "UNIQUE" => ItemRarity.Unique,
                    "RELIC" => ItemRarity.Relic,
                    _ => ItemRarity.Normal,
                };
                lineIdx = i + 1;
                break;
            }
        }

        // Phase 2: Extract name and base name
        string name = "";
        string baseName = "";

        if (lineIdx < lines.Count)
        {
            if (rarity is ItemRarity.Rare or ItemRarity.Unique or ItemRarity.Relic)
            {
                name = lines[lineIdx];
                lineIdx++;
                if (lineIdx < lines.Count)
                {
                    baseName = lines[lineIdx];
                    lineIdx++;
                }
            }
            else
            {
                // Magic/Normal: single name line is both name and baseName
                name = lines[lineIdx];
                baseName = lines[lineIdx];
                lineIdx++;
            }
        }

        // Phase 3: Extract properties
        int itemLevel = 0;
        int quality = 0;
        string sockets = "";
        int levelReq = 0;
        int implicitCount = 0;
        bool corrupted = false;

        // Scan property lines until we hit the Implicits line or run out of property-like lines
        int modStartIdx = lineIdx;
        for (int i = lineIdx; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.StartsWith("Unique ID:", StringComparison.OrdinalIgnoreCase))
            {
                modStartIdx = i + 1;
                continue;
            }
            if (TryExtractProperty(line, "Item Level:", out int il))
            {
                itemLevel = il;
                modStartIdx = i + 1;
                continue;
            }
            if (TryExtractQuality(line, out int q))
            {
                quality = q;
                modStartIdx = i + 1;
                continue;
            }
            if (line.StartsWith("Sockets:", StringComparison.OrdinalIgnoreCase))
            {
                sockets = line["Sockets:".Length..].Trim();
                modStartIdx = i + 1;
                continue;
            }
            if (TryExtractProperty(line, "LevelReq:", out int lr))
            {
                levelReq = lr;
                modStartIdx = i + 1;
                continue;
            }
            if (TryExtractProperty(line, "Implicits:", out int ic))
            {
                implicitCount = ic;
                modStartIdx = i + 1;
                break; // Implicits line signals start of mod lines
            }
        }

        // Phase 4: Parse mod lines
        var modLines = new List<ParsedModLine>();
        int implicitsParsed = 0;

        for (int i = modStartIdx; i < lines.Count; i++)
        {
            var line = lines[i];

            // Skip "Corrupted" marker
            if (line.Equals("Corrupted", StringComparison.OrdinalIgnoreCase))
            {
                corrupted = true;
                continue;
            }

            // Strip flags and determine category from flags
            var (strippedText, flagCategory) = StripFlags(line);

            // Determine if we're still in the implicit section (by position)
            bool inImplicitSection = implicitsParsed < implicitCount;

            // Flag overrides category; otherwise use position
            ModLineCategory category;
            if (flagCategory != null)
                category = flagCategory.Value;
            else if (inImplicitSection)
                category = ModLineCategory.Implicit;
            else
                category = ModLineCategory.Explicit;

            // Lines in the implicit section always count toward the position counter
            if (inImplicitSection)
                implicitsParsed++;

            if (string.IsNullOrWhiteSpace(strippedText))
                continue;

            // Parse mod text
            var mods = ModParser.ParseMod(strippedText);
            modLines.Add(new ParsedModLine
            {
                RawText = strippedText,
                Category = category,
                Mods = mods ?? new(),
                ParseFailed = mods == null,
            });
        }

        return new ParsedItem
        {
            Id = id,
            Rarity = rarity,
            Name = name,
            BaseName = baseName,
            ItemLevel = itemLevel,
            Quality = quality,
            Sockets = sockets,
            LevelReq = levelReq,
            ImplicitCount = implicitCount,
            Corrupted = corrupted,
            ModLines = modLines,
        };
    }

    private static bool TryExtractProperty(string line, string prefix, out int value)
    {
        value = 0;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var numStr = line[prefix.Length..].Trim().TrimStart('+').TrimEnd('%');
        return int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryExtractQuality(string line, out int value)
    {
        value = 0;
        if (!line.StartsWith("Quality:", StringComparison.OrdinalIgnoreCase))
            return false;

        var numStr = line["Quality:".Length..].Trim().TrimStart('+').TrimEnd('%');
        return int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static (string Text, ModLineCategory? Category) StripFlags(string line)
    {
        ModLineCategory? category = null;
        var stripped = FlagPattern.Replace(line, m =>
        {
            var flag = m.Groups[1].Value.ToLowerInvariant();
            switch (flag)
            {
                case "crafted":
                    category = ModLineCategory.Crafted;
                    break;
                case "fractured":
                    category = ModLineCategory.Fractured;
                    break;
                case "enchant":
                    category = ModLineCategory.Enchant;
                    break;
                case "implicit":
                    category = ModLineCategory.Implicit;
                    break;
                case "scourge":
                    category = ModLineCategory.Scourge;
                    break;
                case "crucible":
                    category = ModLineCategory.Crucible;
                    break;
                    // {custom}, {tags:...}, {range:...} → strip but don't set category
            }
            return "";
        });

        return (stripped.Trim(), category);
    }
}
