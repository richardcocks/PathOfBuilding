using System.Globalization;
using System.Xml.Linq;
using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Import;

public static class BuildXmlSaver
{
    public static string Save(BuildData build)
    {
        var doc = SaveToDocument(build);
        return doc.Declaration + Environment.NewLine + doc.ToString();
    }

    public static XDocument SaveToDocument(BuildData build)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("PathOfBuilding",
                SaveBuild(build),
                new XElement("Import"),
                SaveSkills(build),
                SaveTree(build),
                new XElement("Notes", build.Notes),
                SaveItems(build),
                SaveConfig(build)
            )
        );
        return doc;
    }

    private static XElement SaveBuild(BuildData build)
    {
        var m = build.Metadata;
        var el = new XElement("Build",
            new XAttribute("level", m.Level),
            new XAttribute("targetVersion", m.TargetVersion),
            new XAttribute("className", m.ClassName),
            new XAttribute("ascendClassName", m.AscendClassName),
            new XAttribute("bandit", m.Bandit),
            new XAttribute("mainSocketGroup", m.MainSocketGroup),
            new XAttribute("viewMode", m.ViewMode),
            new XAttribute("pantheonMajorGod", m.PantheonMajorGod),
            new XAttribute("pantheonMinorGod", m.PantheonMinorGod)
        );

        if (m.CharacterLevelAutoMode)
            el.Add(new XAttribute("characterLevelAutoMode", "true"));

        foreach (var stat in build.PlayerStats)
        {
            el.Add(new XElement("PlayerStat",
                new XAttribute("stat", stat.Name),
                new XAttribute("value", stat.Value.ToString(CultureInfo.InvariantCulture))
            ));
        }

        return el;
    }

    private static XElement SaveConfig(BuildData build)
    {
        var el = new XElement("Config");

        if (build.ConfigSets.Count == 1 && build.ConfigSets[0].Title == null)
        {
            // Old format: flat inputs
            foreach (var input in build.ConfigSets[0].Inputs)
                el.Add(SaveConfigInput(input));
        }
        else
        {
            foreach (var set in build.ConfigSets)
            {
                var setEl = new XElement("ConfigSet",
                    new XAttribute("id", set.Id)
                );
                if (set.Title != null)
                    setEl.Add(new XAttribute("title", set.Title));
                foreach (var input in set.Inputs)
                    setEl.Add(SaveConfigInput(input));
                el.Add(setEl);
            }
        }

        return el;
    }

    private static XElement SaveConfigInput(ConfigInput input)
    {
        var el = new XElement("Input",
            new XAttribute("name", input.Name)
        );

        switch (input.Kind)
        {
            case ConfigInputKind.Boolean:
                el.Add(new XAttribute("boolean", input.BooleanValue ? "true" : "false"));
                break;
            case ConfigInputKind.Number:
                el.Add(new XAttribute("number", input.NumberValue.ToString(CultureInfo.InvariantCulture)));
                break;
            case ConfigInputKind.String:
                el.Add(new XAttribute("string", input.StringValue));
                break;
        }

        return el;
    }

    private static XElement SaveSkills(BuildData build)
    {
        var el = new XElement("Skills");

        if (build.SkillSets.Count == 1 && build.SkillSets[0].Title == null)
        {
            // Old format: flat socket groups
            foreach (var group in build.SkillSets[0].SocketGroups)
                el.Add(SaveSocketGroup(group));
        }
        else
        {
            foreach (var set in build.SkillSets)
            {
                var setEl = new XElement("SkillSet",
                    new XAttribute("id", set.Id)
                );
                if (set.Title != null)
                    setEl.Add(new XAttribute("title", set.Title));
                foreach (var group in set.SocketGroups)
                    setEl.Add(SaveSocketGroup(group));
                el.Add(setEl);
            }
        }

        return el;
    }

    private static XElement SaveSocketGroup(SocketGroupData group)
    {
        var el = new XElement("Skill",
            new XAttribute("enabled", group.Enabled ? "true" : "false"),
            new XAttribute("mainActiveSkill", group.MainActiveSkill),
            new XAttribute("mainActiveSkillCalcs", group.MainActiveSkillCalcs)
        );

        if (group.Slot != null)
            el.Add(new XAttribute("slot", group.Slot));
        if (group.Label != null)
            el.Add(new XAttribute("label", group.Label));
        if (group.Source != null)
            el.Add(new XAttribute("source", group.Source));
        if (group.IncludeInFullDPS)
            el.Add(new XAttribute("includeInFullDPS", "true"));
        if (group.GroupCount != null)
            el.Add(new XAttribute("groupCount", group.GroupCount.Value));

        foreach (var gem in group.Gems)
            el.Add(SaveGem(gem));

        return el;
    }

    private static XElement SaveGem(GemInstanceData gem)
    {
        var el = new XElement("Gem",
            new XAttribute("nameSpec", gem.NameSpec),
            new XAttribute("gemId", gem.GemId),
            new XAttribute("skillId", gem.SkillId),
            new XAttribute("qualityId", gem.QualityId),
            new XAttribute("level", gem.Level),
            new XAttribute("quality", gem.Quality),
            new XAttribute("enabled", gem.Enabled ? "true" : "false"),
            new XAttribute("enableGlobal1", gem.EnableGlobal1 ? "true" : "false"),
            new XAttribute("enableGlobal2", gem.EnableGlobal2 ? "true" : "false")
        );

        if (gem.SkillPart != null)
            el.Add(new XAttribute("skillPart", gem.SkillPart.Value));
        if (gem.SkillPartCalcs != null)
            el.Add(new XAttribute("skillPartCalcs", gem.SkillPartCalcs.Value));
        if (gem.SkillMinion != null)
            el.Add(new XAttribute("skillMinion", gem.SkillMinion));

        return el;
    }

    private static XElement SaveTree(BuildData build)
    {
        var el = new XElement("Tree",
            new XAttribute("activeSpec", build.ActiveSpec)
        );

        foreach (var spec in build.TreeSpecs)
        {
            var specEl = new XElement("Spec",
                new XAttribute("classId", spec.ClassId),
                new XAttribute("ascendClassId", spec.AscendClassId),
                new XAttribute("treeVersion", spec.TreeVersion),
                new XAttribute("nodes", string.Join(",", spec.AllocatedNodes))
            );

            // Mastery selections
            foreach (var (nodeId, effectId) in spec.MasterySelections)
            {
                specEl.Add(new XElement("MasteryEffect",
                    new XAttribute("nodeId", nodeId),
                    new XAttribute("effectId", effectId)
                ));
            }

            // URL
            if (spec.Url != null)
                specEl.Add(new XElement("URL", spec.Url));

            // Jewel sockets
            if (spec.JewelSockets.Count > 0)
            {
                var socketsEl = new XElement("Sockets");
                foreach (var (nodeId, itemId) in spec.JewelSockets)
                {
                    socketsEl.Add(new XElement("Socket",
                        new XAttribute("nodeId", nodeId),
                        new XAttribute("itemId", itemId)
                    ));
                }
                specEl.Add(socketsEl);
            }

            el.Add(specEl);
        }

        return el;
    }

    private static XElement SaveItems(BuildData build)
    {
        var el = new XElement("Items",
            new XAttribute("activeItemSet", build.ActiveItemSet),
            new XAttribute("useSecondWeaponSet", build.UseSecondWeaponSet ? "true" : "false")
        );

        // Items
        foreach (var item in build.Items)
        {
            var itemEl = new XElement("Item",
                new XAttribute("id", item.Id)
            );

            if (item.Variant != null)
                itemEl.Add(new XAttribute("variant", item.Variant.Value));
            if (item.VariantAlt != null)
                itemEl.Add(new XAttribute("variantAlt", item.VariantAlt.Value));
            if (item.VariantAlt2 != null)
                itemEl.Add(new XAttribute("variantAlt2", item.VariantAlt2.Value));
            if (item.VariantAlt3 != null)
                itemEl.Add(new XAttribute("variantAlt3", item.VariantAlt3.Value));

            foreach (var (id, range) in item.ModRanges)
            {
                itemEl.Add(new XElement("ModRange",
                    new XAttribute("id", id),
                    new XAttribute("range", range.ToString(CultureInfo.InvariantCulture))
                ));
            }

            // Raw text as content (add newline prefix/suffix for formatting)
            itemEl.Add(new XText("\n" + item.RawText + "\n"));

            el.Add(itemEl);
        }

        // Default slots
        foreach (var slot in build.DefaultSlots)
            el.Add(SaveSlot(slot));

        // Item sets
        foreach (var set in build.ItemSets)
        {
            var setEl = new XElement("ItemSet",
                new XAttribute("id", set.Id),
                new XAttribute("useSecondWeaponSet", set.UseSecondWeaponSet ? "true" : "false")
            );
            if (set.Title != null)
                setEl.Add(new XAttribute("title", set.Title));
            foreach (var slot in set.Slots)
                setEl.Add(SaveSlot(slot));
            el.Add(setEl);
        }

        return el;
    }

    private static XElement SaveSlot(SlotAssignment slot)
    {
        var el = new XElement("Slot",
            new XAttribute("name", slot.SlotName),
            new XAttribute("itemId", slot.ItemId)
        );
        if (slot.Active)
            el.Add(new XAttribute("active", "true"));
        return el;
    }
}
