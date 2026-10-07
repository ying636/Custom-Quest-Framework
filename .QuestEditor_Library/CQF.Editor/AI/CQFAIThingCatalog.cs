using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIThingCatalog
    {
        public static XElement Summary()
        {
            return new XElement("cqfThings", new XAttribute("discoveryTool", "cqf_list_cqf_things"), DefDatabase<ThingDef>.AllDefsListForReading
                .Select(def => Kind(def)).Where(kind => kind.Length > 0).GroupBy(kind => kind).OrderBy(group => group.Key)
                .Select(group => new XElement("kind", new XAttribute("name", group.Key), new XAttribute("count", group.Count()))));
        }
        public static XElement Query(string kind, string search, int offset)
        {
            if (kind.Length > 100 || search.Length > 100 || offset < 0 || offset > 100000) throw new InvalidDataException("CQF_AI_InvalidTool: pagination/search");
            if (kind.Length > 0 && kind is not ("interaction" or "entrance" or "exit" or "loot" or "trap" or "container" or "door" or "spawner" or "zone" or "generation" or "custom_text" or "action_worker" or "facility" or "custom" or "resource"))
                throw new InvalidDataException("CQF_AI_InvalidTool: kind");
            ThingDef[] matches = DefDatabase<ThingDef>.AllDefsListForReading.Where(def =>
            {
                string family = Kind(def);
                return family.Length > 0 && (kind.Length == 0 || kind == family || kind == "facility" && Facilities(def)) && (search.Length == 0
                    || def.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || (def.label ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || (def.thingClass?.FullName ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }).OrderBy(def => def.defName).ToArray();
            return new XElement("cqfThings", new XAttribute("total", matches.Length), new XAttribute("offset", offset), matches.Skip(offset).Take(20).Select(def =>
                new XElement("def", new XAttribute("name", def.defName), new XAttribute("label", def.label ?? def.defName), new XAttribute("mod", def.modContentPack?.PackageId ?? ""),
                    new XElement("description", def.description ?? ""), Describe(def))));
        }
        public static XElement? Describe(ThingDef def)
        {
            string kind = Kind(def);
            if (kind.Length == 0) return null;
            Type? data = kind switch
            {
                "interaction" => typeof(CustomThingData_InteractableThing), "entrance" => typeof(CustomThingData_CustomMapEntrance), "exit" => typeof(CustomThingData_CustomMapExit),
                "loot" => typeof(CustomThingData_LootBox), "trap" => typeof(CustomThingData_CustomTrap), "container" => typeof(CustomThingData_CustomContainer),
                "door" => typeof(CustomThingData_CustomDoor), "zone" => typeof(CustomThingData_ZoneCore), _ => null
            };
            string[] tools = kind switch
            {
                "interaction" => new[] { "cqf_read_map_thing", "cqf_edit_map_thing", "cqf_add_interaction" },
                "entrance" => new[] { "cqf_read_map_thing", "cqf_edit_map_thing", "cqf_configure_entrance" },
                "exit" => new[] { "cqf_read_map_thing", "cqf_edit_map_thing", "cqf_configure_exit" },
                "loot" or "trap" or "door" or "container" or "spawner" or "zone" or "generation" or "custom_text" or "action_worker" => new[] { "cqf_read_map_thing", "cqf_edit_map_thing" }, _ => new[] { "cqf_read_map_thing" }
            };
            bool worker = def.comps?.Any(comp => comp.compClass != null && typeof(CompActionWorker).IsAssignableFrom(comp.compClass)) == true;
            bool text = def.comps?.Any(comp => comp.compClass != null && typeof(CompCustomText).IsAssignableFrom(comp.compClass)) == true;
            bool pawn = def.thingClass != null && typeof(Pawn).IsAssignableFrom(def.thingClass);
            bool facility = Facilities(def);
            bool editable = !pawn && (kind is "interaction" or "entrance" or "exit" or "loot" or "trap" or "door" or "container" or "spawner" or "zone" or "generation" or "custom_text" or "action_worker" || worker || text);
            if ((worker || text) && !tools.Contains("cqf_edit_map_thing")) tools = tools.Concat(new[] { "cqf_edit_map_thing" }).ToArray();
            if (pawn) tools = new[] { "cqf_read_map_thing" };
            if (facility || pawn) tools = tools.Concat(new[] { "cqf_read_runtime", "cqf_operate_runtime", "cqf_runtime_help" }).Distinct().ToArray();
            Type? liveType = kind switch
            {
                "interaction" => typeof(CQFAIInteractableConfiguration), "entrance" => typeof(CQFAIEntranceConfiguration), "exit" => typeof(CQFAIExitConfiguration),
                "loot" => typeof(CQFAILootConfiguration), "trap" => typeof(CQFAITrapConfiguration), "door" => typeof(CQFAIDoorConfiguration),
                "container" => typeof(CQFAIContainerConfiguration), "spawner" => typeof(CQFAISpawnerConfiguration), "zone" => typeof(CQFAIZoneConfiguration),
                "generation" => typeof(CQFAIGenerationActionsConfiguration), _ => null
            };
            return new XElement("cqf", new XAttribute("kind", kind), new XAttribute("thingClass", def.thingClass?.FullName ?? ""), new XAttribute("frameworkOwned", IsOwned(def)),
                new XAttribute("liveFeatureEditing", editable),
                facility ? new XElement("runtimeComponentConfiguration", new XAttribute("type", typeof(CQFAIFacilityConfiguration).FullName!), new XAttribute("helpGroup", "facilities")) : null,
                pawn ? new XElement("runtimeOperations", new XAttribute("helpGroup", "pawns")) : null,
                liveType == null ? null : new XElement("liveConfiguration", new XAttribute("type", liveType.FullName!), new XAttribute("path", "/" + kind)),
                worker && !pawn ? new XElement("liveComponentConfiguration", new XAttribute("type", typeof(CQFAIActionWorkerConfiguration).FullName!), new XAttribute("path", "/actionWorkers")) : null,
                text && !pawn ? new XElement("liveComponentConfiguration", new XAttribute("type", typeof(CQFAICustomTextConfiguration).FullName!), new XAttribute("path", "/customTexts")) : null,
                data == null ? null : new XElement("draftDataType", data.FullName), tools.Select(tool => new XElement("liveTool", tool)),
                def.comps?.Where(comp => comp.compClass != null && (typeof(CompActionWorker).IsAssignableFrom(comp.compClass) || typeof(CompCustomText).IsAssignableFrom(comp.compClass)))
                    .Select(comp => new XElement("component", new XAttribute("type", comp.compClass.FullName!))));
        }
        private static string Kind(ThingDef def)
        {
            Type? type = def.thingClass;
            if (type != null)
            {
                if (typeof(InteractableThing).IsAssignableFrom(type)) return "interaction";
                if (typeof(CustomMapEntrance).IsAssignableFrom(type)) return "entrance";
                if (typeof(CustomMapExit).IsAssignableFrom(type)) return "exit";
                if (typeof(LootBox).IsAssignableFrom(type)) return "loot";
                if (typeof(CustomTrap).IsAssignableFrom(type)) return "trap";
                if (typeof(CustomContainer).IsAssignableFrom(type)) return "container";
                if (typeof(CustomDoor).IsAssignableFrom(type)) return "door";
                if (typeof(Spawner).IsAssignableFrom(type)) return "spawner";
                if (typeof(ZoneCore).IsAssignableFrom(type)) return "zone";
                if (typeof(GenerationActionWorker).IsAssignableFrom(type)) return "generation";
                if (typeof(ICustomThing).IsAssignableFrom(type)) return "custom";
            }
            if (def.comps?.Any(comp => comp.compClass != null && typeof(CompActionWorker).IsAssignableFrom(comp.compClass)) == true) return "action_worker";
            if (def.comps?.Any(comp => comp.compClass != null && typeof(CompCustomText).IsAssignableFrom(comp.compClass)) == true) return "custom_text";
            if (Facilities(def)) return "facility";
            return IsOwned(def) ? "resource" : "";
        }
        private static bool IsOwned(ThingDef def) => def.modContentPack?.assemblies?.loadedAssemblies.Contains(typeof(InteractableThing).Assembly) == true;
        private static bool Facilities(ThingDef def) => def.comps?.Any(comp => comp.compClass != null && new[] { typeof(CompTransmit), typeof(CompPower_Level), typeof(CompLandFillable), typeof(CompTriggerDialog), typeof(CompHackOutcome) }.Any(type => type.IsAssignableFrom(comp.compClass))) == true;
    }
}
