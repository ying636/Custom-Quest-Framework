using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AIQueryChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog, string modRoot)
    {
        var invalid = new (string xml, string code, string detail)[]
        {
            ("<query/>", "CQF_AI_InvalidQuery", "CQF_AI_QueryRoot"),
            ("<queries mode='read'><images/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryRoot"),
            ("<queries>unexpected<images/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryRoot"),
            ("<queries/>", "CQF_AI_InvalidQuery", "CQF_AI_QueryCount"),
            ("<queries>" + string.Concat(Enumerable.Repeat("<images/>", 9)) + "</queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryCount"),
            ("<queries><mods/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryUnknown"),
            ("<queries><query type='Verse.ThingDef'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryUnknown"),
            ("<queries><defs><type>Verse.ThingDef</type></defs></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryBody"),
            ("<queries><images>unexpected</images></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryBody"),
            ("<queries><defs type='Verse.ThingDef' limit='20'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryAttributes"),
            ("<queries><schema type='QuestEditor_Library.DialogNode' search='text'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryAttributes"),
            ("<queries><defs search='Wall'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryType"),
            ("<queries><defs type='Verse.ThingDef' offset='next'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryOffset"),
            ("<queries><defs type='Verse.ThingDef' offset='9999999999999999'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryOffset"),
            ("<queries><defs type='Verse.ThingDef' offset='-1'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryOffset"),
            ("<queries><images search='" + new string('x', 101) + "'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QuerySearch"),
            ("<queries><defs type='ThingDef'/></queries>", "CQF_AI_UnknownType", "ThingDef"),
            ("<queries><schema type='CQF_Unknown'/></queries>", "CQF_AI_UnknownType", "CQF_Unknown"),
            ("<queries><object type='QuestEditor_Library.DialogTreeDef'/></queries>", "CQF_AI_InvalidQuery", "CQF_AI_QueryName"),
            ("<queries><object type='QuestEditor_Library.DialogTreeDef' name='CQF_MissingQueryObject'/></queries>", "CQF_AI_MissingResource", "CQF_MissingQueryObject")
        };
        foreach (var item in invalid)
        {
            XElement query = XElement.Parse(item.xml);
            try { catalog.Query(query); throw new InvalidOperationException("Invalid query was accepted: " + item.xml); }
            catch (InvalidDataException error)
            {
                Check(error.Message.StartsWith(item.code, StringComparison.Ordinal) && error.Message.Contains(item.detail), "invalid query identifies its cause: " + item.detail);
            }
            XElement result = catalog.QueryForAssistant(query);
            Check(result.Name == "results" && result.Elements().Count() == 1 && result.Element("error")?.Attribute("code")?.Value == item.code,
                "invalid query becomes actionable tool feedback: " + item.detail);
        }
        XElement valid = XElement.Parse("<queries><defs type='Verse.ThingDef' search='CQF_Check_Item' mod='cqf.checks' offset='0'/></queries>");
        Check(catalog.Query(valid).Descendants("def").Any(def => def.Attribute("name")?.Value == "CQF_Check_Item"), "Mod package query is case insensitive");
        Check(XNode.DeepEquals(catalog.Query(valid), catalog.QueryForAssistant(valid)), "valid queries retain their original resource results");
        Check(catalog.Query(XElement.Parse("<queries><schema type='QuestEditor_Library.DialogNode'/></queries>")).Descendants("field").Any(field => field.Attribute("name")?.Value == "text"),
            "schema query returns actual editable fields");
        DialogTreeDef tree = new DialogTreeDef { defName = "CQF_Check_QueryObject" };
        DefDatabase<DialogTreeDef>.Add(tree);
        XElement queried = catalog.Query(XElement.Parse("<queries><object type='QuestEditor_Library.DialogTreeDef' name='CQF_Check_QueryObject'/></queries>"));
        Check(XNode.DeepEquals(queried.Element("object"), model.Write(tree, "object", true)), "object queries return the actual CQF definition");
        Check(catalog.Query(XElement.Parse("<queries><images mod='CQF_MissingPackage'/></queries>")).Element("images")?.Attribute("total")?.Value == "0", "image search keeps an empty result distinct from query failure");
        ThingDef directional = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        directional.defName = "CQF_Check_DirectionalBuilding"; directional.label = "CQF_Check_DirectionalBuilding"; directional.category = ThingCategory.Building;
        directional.size = new IntVec2(2, 3); directional.rotatable = true; directional.passability = Traversability.Impassable;
        directional.hasInteractionCell = true; directional.interactionCellOffset = new IntVec3(0, 0, -2);
        DefDatabase<ThingDef>.Add(directional);
        XElement placement = catalog.Query(XElement.Parse("<queries><defs type='Verse.ThingDef' search='CQF_Check_DirectionalBuilding'/></queries>")).Descendants("placement").Single();
        Check(placement.Attribute("passability")?.Value == "Impassable" && placement.Elements("rotation").Count() == 4, "directional buildings expose passability and all four placement orientations");
        foreach (XElement rotation in placement.Elements("rotation"))
        {
            int value = (int)rotation.Attribute("value")!;
            Rot4 facing = new(value);
            CellRect footprint = GenAdj.OccupiedRect(IntVec3.Zero, facing, directional.size);
            IntVec3 interaction = directional.interactionCellOffset.RotatedBy(facing);
            Check(rotation.Attribute("footprint")?.Value == $"{footprint.minX},{footprint.minZ},{footprint.Width},{footprint.Height}"
                && (int)rotation.Element("interactionOffset")!.Attribute("x")! == interaction.x && (int)rotation.Element("interactionOffset")!.Attribute("z")! == interaction.z,
                "placement reports native even-sized anchor offsets and operating cell for rotation " + value);
        }
        directional.hasInteractionCell = false;
        directional.multipleInteractionCellOffsets = new List<IntVec3> { new(1, 0, 2), new(-1, 0, 2) };
        placement = catalog.Query(XElement.Parse("<queries><defs type='Verse.ThingDef' search='CQF_Check_DirectionalBuilding'/></queries>")).Descendants("placement").Single();
        Check(placement.Elements("rotation").All(rotation => rotation.Elements("interactionOffset").Count() == 2), "resource metadata preserves multiple operating cells for every orientation");
        Thing described = new Thing { def = directional, Position = new IntVec3(10, 0, 12), Rotation = Rot4.West };
        XElement actual = (XElement)typeof(CQFAILiveMap).GetMethod("Describe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, new object[] { described })!;
        CellRect actualFootprint = described.OccupiedRect();
        Check((int)actual.Attribute("rotation")! == 3 && (int)actual.Element("footprint")!.Attribute("x")! == actualFootprint.minX
            && (int)actual.Element("footprint")!.Attribute("z")! == actualFootprint.minZ && (int)actual.Element("footprint")!.Attribute("width")! == 3,
            "live map descriptions report actual rotated footprint at absolute coordinates");
        Check(actual.Elements("interactionCell").Select(cell => new IntVec3((int)cell.Attribute("x")!, 0, (int)cell.Attribute("z")!))
            .SequenceEqual(directional.multipleInteractionCellOffsets.Select(cell => cell.RotatedBy(Rot4.West) + described.Position)), "live map descriptions rotate and translate multiple operating cells correctly");
        Dictionary<string, LoadedLanguage.KeyedReplacement> previous = LanguageDatabase.activeLanguage.keyedReplacements;
        try
        {
            foreach (string language in new[] { "English", "ChineseSimplified (简体中文)" })
            {
                XElement translations = XElement.Parse(File.ReadAllText(Path.Combine(modRoot, "Languages", language, "Keyed", "CQF_AI.xml"), System.Text.Encoding.UTF8));
                LanguageDatabase.activeLanguage.keyedReplacements = translations.Elements().ToDictionary(element => element.Name.LocalName,
                    element => new LoadedLanguage.KeyedReplacement { key = element.Name.LocalName, value = element.Value });
                string unknown = catalog.QueryForAssistant(XElement.Parse("<queries><mods/></queries>")).Element("error")!.Value;
                Check(unknown.Contains("mods") && unknown.Contains("defs") && !unknown.Contains("{0}"), "localized query errors include the rejected command: " + language);
                string attribute = catalog.QueryForAssistant(XElement.Parse("<queries><defs type='Verse.ThingDef' limit='20'/></queries>")).Element("error")!.Value;
                Check(attribute.Contains("limit") && attribute.Contains("offset") && !attribute.Contains("{1}") && !attribute.Contains("{2}"), "localized query errors identify the bad attribute and accepted attributes: " + language);
                CQFAIActivity timing = CQFAIActivity.Restore(XElement.Parse("<activity index='1' state='CQF_AI_ActivityDone' elapsed='321.8'/>"));
                CQFAIOperation fast = CQFAIOperation.Restore(XElement.Parse("<operation id='CQF_Check_Fast' name='cqf_query_resources' elapsed='0.02' success='true'/>"));
                Check(timing.Header.Contains("322") && !timing.Header.Contains("{1}") && fast.Label.Contains("<0.1"), "localized timing distinguishes full task duration from sub-tenth-second tool execution: " + language);
                Check(Math.Abs((double)timing.Save().Attribute("elapsed")! - 321.8) < 0.000001 && fast.ElapsedSeconds == 0.02,
                    "restored task and tool timings preserve independent precision without restarting clocks: " + language);
            }
        }
        finally { LanguageDatabase.activeLanguage.keyedReplacements = previous; }
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
