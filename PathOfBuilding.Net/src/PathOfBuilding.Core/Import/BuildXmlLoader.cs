using System.Globalization;
using System.Xml.Linq;
using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Import;

public static class BuildXmlLoader
{
    public static BuildData Load(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new InvalidOperationException("Missing root element");
        return ParseRoot(root);
    }

    public static BuildData LoadFromFile(string path)
    {
        var doc = XDocument.Load(path);
        var root = doc.Root ?? throw new InvalidOperationException("Missing root element");
        return ParseRoot(root);
    }

    public static BuildData LoadFromCode(string code)
    {
        var xml = BuildCodec.Decode(code);
        return Load(xml);
    }

    private static BuildData ParseRoot(XElement root)
    {
        var data = new BuildData();

        var buildEl = root.Element("Build");
        if (buildEl != null)
        {
            data.Metadata = ParseBuildMetadata(buildEl);
            data.PlayerStats = ParsePlayerStats(buildEl);
        }

        var configEl = root.Element("Config");
        if (configEl != null)
            data.ConfigSets = ParseConfig(configEl);

        var skillsEl = root.Element("Skills");
        if (skillsEl != null)
            data.SkillSets = ParseSkills(skillsEl);

        var treeEl = root.Element("Tree");
        if (treeEl != null)
        {
            data.ActiveSpec = ParseInt(treeEl.Attribute("activeSpec")?.Value, 1);
            data.TreeSpecs = ParseTreeSpecs(treeEl);
        }

        var itemsEl = root.Element("Items");
        if (itemsEl != null)
            ParseItems(itemsEl, data);

        var notesEl = root.Element("Notes");
        if (notesEl != null)
            data.Notes = notesEl.Value;

        return data;
    }

    private static BuildMetadata ParseBuildMetadata(XElement el)
    {
        return new BuildMetadata
        {
            Level = ParseInt(el.Attribute("level")?.Value, 1),
            TargetVersion = el.Attribute("targetVersion")?.Value ?? "3_0",
            ClassName = el.Attribute("className")?.Value ?? "",
            AscendClassName = el.Attribute("ascendClassName")?.Value ?? "None",
            Bandit = el.Attribute("bandit")?.Value ?? "None",
            PantheonMajorGod = el.Attribute("pantheonMajorGod")?.Value ?? "None",
            PantheonMinorGod = el.Attribute("pantheonMinorGod")?.Value ?? "None",
            MainSocketGroup = ParseInt(el.Attribute("mainSocketGroup")?.Value, 1),
            ViewMode = el.Attribute("viewMode")?.Value ?? "TREE",
            CharacterLevelAutoMode = ParseBool(el.Attribute("characterLevelAutoMode")?.Value),
        };
    }

    private static List<PlayerStatData> ParsePlayerStats(XElement buildEl)
    {
        var stats = new List<PlayerStatData>();
        foreach (var el in buildEl.Elements("PlayerStat"))
        {
            stats.Add(new PlayerStatData
            {
                Name = el.Attribute("stat")?.Value ?? "",
                Value = ParseDouble(el.Attribute("value")?.Value, 0),
            });
        }
        return stats;
    }

    private static List<ConfigSetData> ParseConfig(XElement configEl)
    {
        var sets = new List<ConfigSetData>();

        // Check for new-format ConfigSet wrappers
        var configSetEls = configEl.Elements("ConfigSet").ToList();
        if (configSetEls.Count > 0)
        {
            foreach (var setEl in configSetEls)
            {
                sets.Add(new ConfigSetData
                {
                    Id = ParseInt(setEl.Attribute("id")?.Value, 0),
                    Title = setEl.Attribute("title")?.Value,
                    Inputs = ParseConfigInputs(setEl),
                });
            }
        }
        else
        {
            // Old format: flat Input elements directly under Config
            var inputs = ParseConfigInputs(configEl);
            if (inputs.Count > 0)
            {
                sets.Add(new ConfigSetData { Id = 1, Inputs = inputs });
            }
        }

        return sets;
    }

    private static List<ConfigInput> ParseConfigInputs(XElement parent)
    {
        var inputs = new List<ConfigInput>();
        foreach (var el in parent.Elements("Input"))
        {
            var input = new ConfigInput
            {
                Name = el.Attribute("name")?.Value ?? "",
            };

            var boolAttr = el.Attribute("boolean");
            var numAttr = el.Attribute("number");
            var strAttr = el.Attribute("string");

            if (boolAttr != null)
            {
                input.Kind = ConfigInputKind.Boolean;
                input.BooleanValue = ParseBool(boolAttr.Value);
            }
            else if (numAttr != null)
            {
                input.Kind = ConfigInputKind.Number;
                input.NumberValue = ParseDouble(numAttr.Value, 0);
            }
            else if (strAttr != null)
            {
                input.Kind = ConfigInputKind.String;
                input.StringValue = strAttr.Value;
            }

            inputs.Add(input);
        }
        return inputs;
    }

    private static List<SkillSetData> ParseSkills(XElement skillsEl)
    {
        var sets = new List<SkillSetData>();

        // Check for new-format SkillSet wrappers
        var skillSetEls = skillsEl.Elements("SkillSet").ToList();
        if (skillSetEls.Count > 0)
        {
            foreach (var setEl in skillSetEls)
            {
                sets.Add(new SkillSetData
                {
                    Id = ParseInt(setEl.Attribute("id")?.Value, 0),
                    Title = setEl.Attribute("title")?.Value,
                    SocketGroups = ParseSocketGroups(setEl),
                });
            }
        }
        else
        {
            // Old format: flat Skill elements directly under Skills
            var groups = ParseSocketGroups(skillsEl);
            if (groups.Count > 0)
            {
                sets.Add(new SkillSetData { Id = 1, SocketGroups = groups });
            }
        }

        return sets;
    }

    private static List<SocketGroupData> ParseSocketGroups(XElement parent)
    {
        var groups = new List<SocketGroupData>();
        foreach (var el in parent.Elements("Skill"))
        {
            var group = new SocketGroupData
            {
                Enabled = ParseBool(el.Attribute("enabled")?.Value, true),
                Slot = NilToNull(el.Attribute("slot")?.Value),
                Label = NilToNull(el.Attribute("label")?.Value),
                Source = NilToNull(el.Attribute("source")?.Value),
                MainActiveSkill = ParseInt(el.Attribute("mainActiveSkill")?.Value, 1),
                MainActiveSkillCalcs = ParseInt(el.Attribute("mainActiveSkillCalcs")?.Value, 1),
                IncludeInFullDPS = ParseBool(el.Attribute("includeInFullDPS")?.Value),
                GroupCount = ParseNullableInt(el.Attribute("groupCount")?.Value),
                Gems = ParseGems(el),
            };
            groups.Add(group);
        }
        return groups;
    }

    private static List<GemInstanceData> ParseGems(XElement skillEl)
    {
        var gems = new List<GemInstanceData>();
        foreach (var el in skillEl.Elements("Gem"))
        {
            gems.Add(new GemInstanceData
            {
                NameSpec = el.Attribute("nameSpec")?.Value ?? "",
                GemId = el.Attribute("gemId")?.Value ?? "",
                SkillId = el.Attribute("skillId")?.Value ?? "",
                QualityId = el.Attribute("qualityId")?.Value ?? "Default",
                Level = ParseInt(el.Attribute("level")?.Value, 1),
                Quality = ParseInt(el.Attribute("quality")?.Value, 0),
                Enabled = ParseBool(el.Attribute("enabled")?.Value, true),
                EnableGlobal1 = ParseBool(el.Attribute("enableGlobal1")?.Value, true),
                EnableGlobal2 = ParseBool(el.Attribute("enableGlobal2")?.Value, true),
                SkillPart = ParseNullableInt(el.Attribute("skillPart")?.Value),
                SkillPartCalcs = ParseNullableInt(el.Attribute("skillPartCalcs")?.Value),
                SkillMinion = NilToNull(el.Attribute("skillMinion")?.Value),
            });
        }
        return gems;
    }

    private static List<TreeSpecData> ParseTreeSpecs(XElement treeEl)
    {
        var specs = new List<TreeSpecData>();
        foreach (var el in treeEl.Elements("Spec"))
        {
            var spec = new TreeSpecData
            {
                ClassId = ParseInt(el.Attribute("classId")?.Value, 0),
                AscendClassId = ParseInt(el.Attribute("ascendClassId")?.Value, 0),
                TreeVersion = el.Attribute("treeVersion")?.Value ?? "",
            };

            // Parse allocated nodes from comma-separated attribute
            var nodesAttr = el.Attribute("nodes")?.Value;
            if (!string.IsNullOrEmpty(nodesAttr))
            {
                foreach (var part in nodesAttr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(part.Trim(), out var nodeId))
                        spec.AllocatedNodes.Add(nodeId);
                }
            }

            // Parse mastery selections
            foreach (var masteryEl in el.Elements("MasteryEffect"))
            {
                var nodeId = ParseInt(masteryEl.Attribute("nodeId")?.Value, 0);
                var effectId = ParseInt(masteryEl.Attribute("effectId")?.Value, 0);
                if (nodeId != 0)
                    spec.MasterySelections[nodeId] = effectId;
            }

            // Parse jewel sockets
            var socketsEl = el.Element("Sockets");
            if (socketsEl != null)
            {
                foreach (var socketEl in socketsEl.Elements("Socket"))
                {
                    var nodeId = ParseInt(socketEl.Attribute("nodeId")?.Value, 0);
                    var itemId = ParseInt(socketEl.Attribute("itemId")?.Value, 0);
                    if (nodeId != 0)
                        spec.JewelSockets[nodeId] = itemId;
                }
            }

            // Parse URL
            var urlEl = el.Element("URL");
            if (urlEl != null)
            {
                var url = urlEl.Value.Trim();
                if (!string.IsNullOrEmpty(url))
                    spec.Url = url;
            }

            specs.Add(spec);
        }
        return specs;
    }

    private static void ParseItems(XElement itemsEl, BuildData data)
    {
        data.ActiveItemSet = ParseInt(itemsEl.Attribute("activeItemSet")?.Value, 1);
        data.UseSecondWeaponSet = ParseBool(itemsEl.Attribute("useSecondWeaponSet")?.Value);

        // Parse Item elements
        foreach (var el in itemsEl.Elements("Item"))
        {
            var item = new ItemData
            {
                Id = ParseInt(el.Attribute("id")?.Value, 0),
                RawText = el.Value.Trim(),
                Variant = ParseNullableInt(el.Attribute("variant")?.Value),
                VariantAlt = ParseNullableInt(el.Attribute("variantAlt")?.Value),
                VariantAlt2 = ParseNullableInt(el.Attribute("variantAlt2")?.Value),
                VariantAlt3 = ParseNullableInt(el.Attribute("variantAlt3")?.Value),
            };

            foreach (var rangeEl in el.Elements("ModRange"))
            {
                var id = ParseInt(rangeEl.Attribute("id")?.Value, 0);
                var range = ParseDouble(rangeEl.Attribute("range")?.Value, 0);
                item.ModRanges[id] = range;
            }

            data.Items.Add(item);
        }

        // Parse top-level Slot elements (default slots outside ItemSets)
        foreach (var el in itemsEl.Elements("Slot"))
        {
            data.DefaultSlots.Add(ParseSlotAssignment(el));
        }

        // Parse ItemSet elements
        foreach (var el in itemsEl.Elements("ItemSet"))
        {
            var set = new ItemSetData
            {
                Id = ParseInt(el.Attribute("id")?.Value, 0),
                Title = NilToNull(el.Attribute("title")?.Value),
                UseSecondWeaponSet = ParseBool(el.Attribute("useSecondWeaponSet")?.Value),
            };

            foreach (var slotEl in el.Elements("Slot"))
            {
                set.Slots.Add(ParseSlotAssignment(slotEl));
            }

            data.ItemSets.Add(set);
        }
    }

    private static SlotAssignment ParseSlotAssignment(XElement el)
    {
        return new SlotAssignment
        {
            SlotName = el.Attribute("name")?.Value ?? "",
            ItemId = ParseInt(el.Attribute("itemId")?.Value, 0),
            Active = ParseBool(el.Attribute("active")?.Value),
        };
    }

    // --- Parsing helpers ---

    private static bool ParseBool(string? value, bool defaultValue = false)
    {
        if (value == null || value == "nil") return defaultValue;
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        if (value == null || value == "nil") return defaultValue;
        return int.TryParse(value, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    private static int? ParseNullableInt(string? value)
    {
        if (value == null || value == "nil") return null;
        return int.TryParse(value, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static double ParseDouble(string? value, double defaultValue)
    {
        if (value == null || value == "nil") return defaultValue;
        return double.TryParse(value, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    private static string? NilToNull(string? value)
    {
        if (value == null || value == "nil") return null;
        return value;
    }
}
