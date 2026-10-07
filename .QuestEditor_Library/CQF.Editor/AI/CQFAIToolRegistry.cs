using System.Globalization;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIToolRegistry
    {
        public CQFAIToolRegistry(CQFAIModel model, CQFAIResourceCatalog catalog, CQFAIEditorContext? context, CQFAITransaction? transaction, string command, bool generateText, bool? editingAllowed = null, CQFAIRuntimeJournal? runtimeJournal = null)
        {
            this.model = model;
            this.context = context;
            this.transaction = transaction;
            this.command = command;
            this.generateText = generateText;
            EditingAllowed = editingAllowed ?? transaction != null;
            liveMap = (context?.Read() as CQFAILiveMapInfo)?.backend;
            reader = new CQFAITargetReader(model);
            RuntimeJournal = runtimeJournal ?? new CQFAIRuntimeJournal(model);
            runtimeTools = new CQFAIRuntimeTools(model, RuntimeJournal, generateText, command); runtimeTools.Register(this);
            Register(new CQFAITool("cqf_get_context", "Read current target summary, permissions and resource counts. Discover Mod IDs and Def types separately.", _ =>
                new XElement("context", new XElement("capabilities", new XAttribute("editingAllowed", EditingAllowed),
                    new XAttribute("hasTarget", context != null), new XAttribute("canEditTarget", transaction != null),
                    new XAttribute("liveMapConstruction", liveMap != null), new XAttribute("targetScope", liveMap != null ? "live_map" : "editor_data"),
                    new XAttribute("runtimeQueries", Current.Game != null), new XAttribute("runtimeOperations", EditingAllowed && Current.Game != null), new XAttribute("definitionManagement", EditingAllowed),
                    new XAttribute("unavailableReason", !EditingAllowed ? "editing_disabled" : context == null ? "no_target" : "none")),
                    reader.Summary(context?.Read()), catalog.Summary())));
            Register(new CQFAITool("cqf_list_mods", "Find loaded Mod package IDs and names, 40 per page.",
                arguments => catalog.Discover(true, Text(arguments, "search", ""), Number(arguments, "offset", 0)),
                ("search", "Package ID or name substring, at most 100 characters.", false), ("offset", "Page offset, integer text from 0 to 100000.", false)));
            Register(new CQFAITool("cqf_list_def_types", "Find exact fully qualified loaded Def CLR type names, 40 per page. These include native and Mod Def types.",
                arguments => catalog.Discover(false, Text(arguments, "search", ""), Number(arguments, "offset", 0)),
                ("search", "Type name substring, at most 100 characters.", false), ("offset", "Page offset, integer text from 0 to 100000.", false)));
            Register(new CQFAITool("cqf_list_databases", "List existing Global, Temporary and Quest CQF databases and counts. Does not initialize missing data or modify state.",
                arguments => CQFAIDatabaseCatalog.List(Number(arguments, "offset", 0), Number(arguments, "limit", 20)),
                ("offset", "Page offset from 0 to 100000.", false), ("limit", "Page size from 1 to 40.", false)));
            Register(new CQFAITool("cqf_read_database", "Read existing CQF database values, targets, groups or Lord summaries. Read-only; never creates Quest databases. Lists and group members are paginated.",
                arguments => CQFAIDatabaseCatalog.Read(Text(arguments, "scope", "global"), Number(arguments, "quest_id", -1), Text(arguments, "category", "targets"),
                    Text(arguments, "key", ""), Text(arguments, "search", ""), Number(arguments, "offset", 0), Number(arguments, "limit", 20)),
                ("scope", "global, temporary or quest.", false), ("quest_id", "Required for quest scope: existing integer ID from cqf_list_databases.", false),
                ("category", "targets, bools, ints, groups, group_members or lords; default targets.", false),
                ("key", "Optional exact key; required for group_members, at most 200 characters.", false), ("search", "Key substring or member Thing ID, at most 100 characters.", false),
                ("offset", "Page offset from 0 to 100000.", false), ("limit", "Page size from 1 to 40.", false)));
            Register(new CQFAITool("cqf_list_cqf_things", "Discover actual loaded CQF custom items/buildings and submod subclasses by functionality, without guessing defName prefixes or English labels. Returns exact Def names, source Mod, runtime class, map-draft data type and live editing support, 20 per page. Query placement/material metadata separately before placing anything.",
                arguments => CQFAIThingCatalog.Query(Text(arguments, "kind", ""), Text(arguments, "search", ""), Number(arguments, "offset", 0)),
                ("kind", "Optional functionality: interaction, entrance, exit, loot, trap, container, door, spawner, zone, generation, custom_text, action_worker, facility, custom or resource. Empty lists all.", false),
                ("search", "Optional defName, current label or runtime class substring, at most 100 characters.", false), ("offset", "Page offset from 0 to 100000.", false)));
            Register(new CQFAITool("cqf_query_resources", "Query actual loaded Defs, images, or editable Def data. ThingDefs include rotation values, native anchor-relative footprints and rotated interaction offsets for placement. Uses the documented queries XML; never invent resource names.",
                arguments => catalog.Query(CQFAIChanges.Parse(arguments.Element("queries_xml")!.Value)), ("queries_xml", "XML rooted at queries, containing defs/images/schema/object queries.", true)));
            Register(new CQFAITool("cqf_find_types", "Find supported data CLR types and concrete subclasses before requesting their field schemas.", FindTypes,
                ("search", "Type name substring; empty searches all supported types.", false), ("base_type", "Optional exact supported CLR base type to filter subclasses.", false), ("offset", "Page offset, integer text from 0 to 100000.", false)));
            Register(new CQFAITool("cqf_get_schema", "Read only one data type's editable fields. Discover concrete subclasses with cqf_find_types and request nested schemas as needed.",
                arguments => model.Schema(model.Resolve(arguments.Element("type")!.Value), false), ("type", "Exact fully qualified supported CLR type.", true)));
            Register(new CQFAITool("cqf_read_resource", "Read a field or page from a loaded editable Def, including related map definitions. This never changes the loaded resource.", ReadResource,
                ("type", "Exact fully qualified editable Def CLR type.", true), ("name", "Exact loaded defName.", true),
                ("path", "Field path; / reads the root. Use narrower paths for large resources.", false), ("offset", "Collection page offset as integer text.", false), ("limit", "Page size from 1 to 40 as integer text.", false)));
            if (context != null)
            {
                Register(new CQFAITool("cqf_read_target", "Read current editor data at a field path. Lists and dictionaries are paginated; use narrower paths for large objects.",
                    arguments => reader.Read(context.Read(), Text(arguments, "path", "/"), Number(arguments, "offset", 0), Number(arguments, "limit", 20)),
                    ("path", "Field path, such as /customThings/0 or /nodeMoulds/@0. / reads the root.", false), ("offset", "Collection page offset, integer text from 0 to 100000.", false),
                    ("limit", "Collection page size, integer text from 1 to 40.", false)));
                Register(new CQFAITool("cqf_validate_target", "Check current data, supported CQF live configurations and portal warnings. Does not execute triggers, inspect all running quests, prove signal delivery, assess layout beauty, reachability, room usability or roof support.", _ => ValidateTarget()));
            }
            if (liveMap != null)
            {
                if (liveMap is CQFAILiveMap runtimeMap)
                {
                    Register(new CQFAITool("cqf_read_map_configuration", "Read actual map event areas and map-level damage triggers. Root reads a bounded summary; use /eventAreas, /triggers and narrower paths with pagination for full data. Map trigger targets are exact Building Thing IDs.",
                        arguments => ReadMapConfiguration(runtimeMap.Map, arguments),
                        ("path", "Configuration path, default / summary. /eventAreas/0/cells reads a region's cell list.", false),
                        ("offset", "Collection page offset from 0 to 100000.", false), ("limit", "Page size from 1 to 40.", false)));
                    Register(new CQFAITool("cqf_read_map_targets", "Find current map target keys and their actual Thing IDs. These keys belong to MapComponent_CQFTargets; Quest, Global and Temporary targets are queried separately through database tools.",
                        arguments => CQFAIRuntimeCatalog.Targets(runtimeMap.Map, Text(arguments, "search", ""), Text(arguments, "thing_id", ""), Number(arguments, "offset", 0), Number(arguments, "limit", 20)),
                        ("search", "Key substring, at most 100 characters.", false), ("thing_id", "Optional exact Thing ID to find its keys.", false),
                        ("offset", "Page offset from 0 to 100000.", false), ("limit", "Page size from 1 to 40.", false)));
                    Register(new CQFAITool("cqf_read_map_signals", "Read CQF map object signal references, source Thing IDs, scopes and matching sender/receiver counts. This is configuration reference analysis, not execution history or all running Quest receivers. Names are actual runtime names; do not add prefixes again.",
                        arguments => CQFAIRuntimeCatalog.Signals(runtimeMap.Map, Text(arguments, "search", ""), Number(arguments, "offset", 0), Number(arguments, "limit", 20)),
                        ("search", "Signal, source or Thing ID substring, at most 100 characters.", false), ("offset", "Page offset from 0 to 100000.", false),
                        ("limit", "Page size from 1 to 40.", false)));
                }
                Register(new CQFAITool("cqf_read_map_region", "Read actual terrain, roofs and Thing IDs in the current game map. Inspect a region before changing it. Coordinates are absolute game map cells.", ReadLiveRegion,
                    ("x", "Minimum X coordinate as integer text.", true), ("z", "Minimum Z coordinate as integer text.", true),
                    ("width", "Region width; at most 4096 cells total.", true), ("height", "Region height; at most 4096 cells total.", true),
                    ("offset", "Cell page offset as integer text.", false), ("limit", "Cells per page, from 1 to 400; default 100.", false)));
                Register(new CQFAITool("cqf_read_map_thing", "Read an actual Thing's rotation, footprint, interaction cells, state and editable configuration. Large configurations default to a field summary; read narrower paths and collection pages. Pawns are inspection-only.", ReadLiveThing,
                    ("thing_id", "Exact Thing ID returned by map tools.", true), ("path", "Optional configuration field path, such as /loot/loots or /actionWorkers/0/comps.", false),
                    ("offset", "Collection page offset from 0 to 100000.", false), ("limit", "Page size from 1 to 40, default 20.", false)));
            }
            if (transaction == null) { RegisterTools?.Invoke(this, context); return; }
            Register(new CQFAITool("cqf_apply_changes", "Apply an ordered batch of generic field/map operations atomically, then return the actual editor state. Edits remain visible and can be undone as one task.",
                arguments => Apply(CQFAIChanges.Parse(arguments.Element("changes_xml")!.Value)), ("changes_xml", "XML rooted at changes using set/append/put/remove or map operations. Map placement: <place def='exact ThingDef' x='anchorX' z='anchorZ' rotation='0'/>; rotation 0=North, 1=East, 2=South, 3=West. Query native footprint and interaction offsets first; leave operating cells and entrances clear.", true)));
            if (liveMap != null)
                Register(new CQFAITool("cqf_edit_map_thing", "Edit an existing actual map Thing. Applies instantly, reads back the result, and shares task undo with construction and terrain changes.",
                    arguments => Apply(new XElement("changes", new XElement("editThing", new XAttribute("id", arguments.Element("thing_id")!.Value), CQFAIChanges.Parse(arguments.Element("changes_xml")!.Value)))),
                    ("thing_id", "Exact Thing ID returned by map tools.", true), ("changes_xml", "Generic changes XML for CQFAILiveThingConfiguration: hitPoints, stackCount, interaction/entrance/exit/loot/trap/door/container/spawner/zone/generation, extraInteractions, customTexts and actionWorkers. Read the Thing and schemas first. Component indices are fixed. Container innerThings is the generation recipe, not held objects. Editing does not run actions, open boxes, generate zones or spawn NPCs.", true)));
            if (liveMap is CQFAILiveMap)
                Register(new CQFAITool("cqf_edit_map_configuration", "Edit actual map event areas and map-level damage triggers atomically, with readback and task undo. Updates area caches without executing actions. Use the CQFAIMapConfiguration schema. Remove entries to delete areas or triggers; keep keys unique.",
                    arguments => Apply(new XElement("changes", new XElement("editMap", CQFAIChanges.Parse(arguments.Element("changes_xml")!.Value)))),
                    ("changes_xml", "Generic changes rooted at changes for /eventAreas and /triggers. Areas require key, in-bounds cells, optional faction/onlyHumanlike and actions. Triggers require key, mode=Damaged, exact Building thingIds and actions. Existing data is visible through cqf_read_map_configuration.", true)));
            object target = context!.Read();
            if (target is DialogTreeDef)
                Register(new CQFAITool("cqf_add_dialogue_branch", "Create a dialogue node and add a choice in a source node linking to it. Maintains node identifiers and validates links atomically.", AddDialogueBranch,
                    ("source_node_id", "Existing source node's integer dictionary key as text.", true),
                    ("node_xml", "XML value with DialogNode fields, such as text and editorPos. The new index is allocated automatically.", true),
                    ("option_xml", "XML value with DialogOption fields, including choice text and optional conditions/actions. Its results are linked to the new node.", true)));
            if (liveMap != null || target is CustomMapDataDef || target is CustomThingData_InteractableThing || target is CQFAIInteractableConfiguration)
                Register(new CQFAITool("cqf_add_interaction", "Add an interaction option to an existing interactive object, including its conditions, requirements, results and actions.", AddInteraction,
                    ("object_path", "Path to the interactive object; / for an interactive object's own editor.", false),
                    ("operation_xml", "XML value with Class='QuestEditor_Library.InteractionOperation' and supported fields. Query its schema first.", true),
                    ("thing_id", "Required for a live map target: exact existing interactive Thing ID.", false)));
            if (liveMap != null || target is CustomMapDataDef || target is CustomThingData_CustomMapEntrance || target is CQFAIEntranceConfiguration)
                Register(new CQFAITool("cqf_configure_entrance", "Configure an existing map entrance's destination map, exit name, opened state and enter actions. Does not generate or destroy maps.",
                    arguments => Configure(arguments, true), ("object_path", "Path to the entrance; / for an entrance's own editor.", false),
                    ("configuration_xml", "Partial value XML containing data Def reference, exitName, opended, enterActions or chance pools. Omitted fields remain unchanged.", true),
                    ("thing_id", "Required for a live map target: exact existing entrance Thing ID.", false)));
            if (liveMap != null || target is CustomMapDataDef || target is CustomThingData_CustomMapExit || target is CQFAIExitConfiguration)
                Register(new CQFAITool("cqf_configure_exit", "Configure an existing map exit's exitName and enterActions. The matching entrance refers to this exit name.",
                    arguments => Configure(arguments, false), ("object_path", "Path to the exit; / for an exit's own editor.", false),
                    ("configuration_xml", "Partial value XML containing exitName or enterActions. Omitted fields remain unchanged.", true),
                    ("thing_id", "Required for a live map target: exact existing exit Thing ID.", false)));
            RegisterTools?.Invoke(this, context);
        }
        public bool EditingAllowed { get; }
        public CQFAIRuntimeJournal RuntimeJournal { get; }
        public IReadOnlyCollection<CQFAITool> Tools => tools.Values.ToArray();
        public XElement Definitions => new XElement("tools", tools.Values.Select(tool => tool.Definition));
        public void Register(CQFAITool tool)
        {
            if (!tool.Name.StartsWith("cqf_", StringComparison.Ordinal) || tools.ContainsKey(tool.Name)) throw new InvalidOperationException("CQF_AI_InvalidTool: " + tool.Name);
            tools.Add(tool.Name, tool);
        }
        public void RestrictToInspection()
        {
            foreach (string name in tools.Keys.Where(name => name is not ("cqf_get_context" or "cqf_list_mods" or "cqf_list_def_types" or "cqf_query_resources"
                or "cqf_find_types" or "cqf_get_schema" or "cqf_read_resource" or "cqf_read_target" or "cqf_validate_target" or "cqf_read_map_region" or "cqf_read_map_thing"
                or "cqf_list_targets" or "cqf_select_target" or "cqf_list_cqf_things" or "cqf_list_databases" or "cqf_read_database"
                or "cqf_read_map_targets" or "cqf_read_map_signals" or "cqf_read_map_configuration" or "cqf_runtime_help" or "cqf_read_runtime" or "cqf_check_conditions" or "cqf_inspect_map" or "cqf_export_definition")).ToArray()) tools.Remove(name);
        }
        public void AddInstruction(string text) { command += "\n" + text; runtimeTools.AddInstruction(text); }
        public XElement Execute(CQFAIToolCall call)
        {
            bool independent = call.Name is "cqf_runtime_help" or "cqf_read_runtime" or "cqf_operate_runtime" or "cqf_check_conditions" or "cqf_inspect_map" or "cqf_manage_definition" or "cqf_export_definition";
            if (!independent && (context?.IsValid?.Invoke() == false || transaction != null && !transaction.IsTargetValid)) throw new InvalidOperationException("CQF_AI_StaleTarget");
            if (transaction == null && call.Name is "cqf_apply_changes" or "cqf_edit_map_thing" or "cqf_edit_map_configuration" or "cqf_add_dialogue_branch" or "cqf_add_interaction" or "cqf_configure_entrance" or "cqf_configure_exit")
                throw new InvalidDataException(EditingAllowed && context == null ? "CQF_AI_NoEditingTarget" : "CQF_AI_ReadOnly");
            if (!tools.TryGetValue(call.Name, out CQFAITool tool)) throw new InvalidDataException("CQF_AI_InvalidTool: " + call.Name);
            XElement result = new XElement("tool_result", new XAttribute("id", call.Id), new XAttribute("name", call.Name), new XAttribute("success", true), tool.Execute(call.Arguments));
            if (result.Elements().Any(element => (bool?)element.Attribute("success") == false))
            {
                result.SetAttributeValue("success", false);
                result.Add(new XElement("error", "CQF_AI_RuntimeFailed: " + string.Join("; ", result.Descendants("error").Take(3).Select(error => error.Value))));
            }
            if (call.Name == "cqf_validate_target" && result.Element("validation")?.Attribute("passed")?.Value == "false")
            {
                result.SetAttributeValue("success", false);
                string detail = string.Join("; ", result.Element("validation")!.Elements("error").Take(3)
                    .Select(error => ((string?)error.Attribute("thingId") is string id ? id + ": " : "") + error.Value));
                if (detail.Length > 2000) detail = detail.Substring(0, 2000);
                result.Add(new XElement("error", "CQF_AI_ValidationFailed" + (detail.Length > 0 ? ": " + detail : "")));
            }
            if (result.ToString(SaveOptions.DisableFormatting).Length > 131072) throw new InvalidDataException("CQF_AI_ReadTooLarge");
            return result;
        }
        private XElement ReadMapConfiguration(Verse.Map map, XElement arguments)
        {
            string path = Text(arguments, "path", "/");
            int offset = Number(arguments, "offset", 0), limit = Number(arguments, "limit", 20);
            CQFAIRuntimeCatalog.ValidatePage("", offset, limit);
            if (path == "/" && offset != 0) throw new InvalidDataException("CQF_AI_InvalidTool: root is a summary");
            CQFAIMapConfiguration value = CQFAIMapFeatures.Read(map);
            return path == "/" ? reader.Summary(value) : reader.Read(value, path, offset, limit);
        }
        private XElement FindTypes(XElement arguments)
        {
            string search = Text(arguments, "search", "");
            if (search.Length > 100) throw new InvalidDataException("CQF_AI_InvalidTool: search");
            string baseName = Text(arguments, "base_type", "");
            Type? baseType = baseName.Length == 0 ? null : model.Resolve(baseName);
            int offset = Number(arguments, "offset", 0);
            if (offset < 0 || offset > 100000) throw new InvalidDataException("CQF_AI_InvalidTool: offset");
            Type[] matches = model.Types.Where(type => (baseType == null || baseType.IsAssignableFrom(type)) && type.FullName!.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(type => type.FullName).ToArray();
            return new XElement("types", new XAttribute("total", matches.Length), new XAttribute("offset", offset), matches.Skip(offset).Take(40)
                .Select(type => new XElement("type", new XAttribute("name", type.FullName!), new XAttribute("abstract", type.IsAbstract))));
        }
        private XElement ReadResource(XElement arguments)
        {
            Type type = model.Resolve(arguments.Element("type")!.Value);
            if (!typeof(Def).IsAssignableFrom(type) || !GenDefDatabase.AllDefTypesWithDatabases().Contains(type)) throw new InvalidDataException("CQF_AI_UnknownType: " + type.FullName);
            string name = arguments.Element("name")!.Value;
            Def definition = GenDefDatabase.GetAllDefsInDatabaseForDef(type).FirstOrDefault(def => def.defName == name) ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
            return reader.Read(definition, Text(arguments, "path", "/"), Number(arguments, "offset", 0), Number(arguments, "limit", 20));
        }
        private XElement Apply(XElement changes)
        {
            if (transaction == null) throw new InvalidDataException("CQF_AI_ReadOnly");
            transaction.Build(changes, command, generateText);
            transaction.Apply();
            if (transaction is CQFAILiveMapTransaction live) return live.Receipt;
            return new XElement("applied", new XAttribute("operations", changes.Elements().Count()), new XAttribute("undoAvailable", transaction.CanUndo), reader.Summary(context!.Read()), ValidateTarget());
        }
        private XElement ValidateTarget()
        {
            if (liveMap != null) return liveMap.Validate();
            object value = model.Copy(context!.Read());
            CQFAIChanges.Validate(value);
            context.Validate?.Invoke(value);
            XElement[] warnings = CQFAIContentValidation.PortalWarnings(value).ToArray();
            return new XElement("validation", new XAttribute("passed", true), new XAttribute("warnings", warnings.Length), warnings.Take(40));
        }
        private XElement AddDialogueBranch(XElement arguments)
        {
            if (context!.Read() is not DialogTreeDef tree) throw new InvalidDataException("CQF_AI_InvalidValue: dialogue required");
            int source = Number(arguments, "source_node_id", -1);
            if (!tree.nodeMoulds.ContainsKey(source)) throw new InvalidDataException("CQF_AI_InvalidLinks: source node");
            int index = Math.Max(1, tree.curIndex);
            while (tree.nodeMoulds.ContainsKey(index) && index < int.MaxValue) index++;
            if (index == int.MaxValue) throw new InvalidDataException("CQF_AI_InvalidValue: node index");
            XElement nodeXml = CQFAIChanges.Parse(arguments.Element("node_xml")!.Value);
            XElement optionXml = CQFAIChanges.Parse(arguments.Element("option_xml")!.Value);
            if (nodeXml.Name != "value" || optionXml.Name != "value") throw new InvalidDataException("CQF_AI_InvalidValue: dialogue value");
            if (model.Read(nodeXml, typeof(DialogNode)) is not DialogNode node || model.Read(optionXml, typeof(DialogOption)) is not DialogOption option)
                throw new InvalidDataException("CQF_AI_InvalidValue: dialogue value");
            node.index = index;
            if (option.results == null) throw new InvalidDataException("CQF_AI_InvalidLinks: choice results");
            if (option.results.Count == 0) option.results.Add(new DialogResult());
            foreach (DialogResult result in option.results) result.nextIndex = index;
            tree.ResolveOptions();
            int optionId = Math.Max(0, tree.curOptionIndex);
            while (tree.optionMoulds.ContainsKey(optionId)) optionId = checked(optionId + 1);
            return Apply(new XElement("changes",
                new XElement("put", new XAttribute("path", "/nodeMoulds"), new XElement("key", index), model.Write(node)),
                new XElement("put", new XAttribute("path", "/optionMoulds"), new XElement("key", optionId), model.Write(option)),
                new XElement("append", new XAttribute("path", "/nodeMoulds/@" + source + "/optionIds"), new XElement("value", optionId)),
                new XElement("set", new XAttribute("path", "/curOptionIndex"), new XElement("value", optionId + 1)),
                new XElement("set", new XAttribute("path", "/curIndex"), new XElement("value", index + 1))));
        }
        private XElement AddInteraction(XElement arguments)
        {
            string path = Text(arguments, "object_path", "/");
            string id = Text(arguments, "thing_id", "");
            XElement current = liveMap == null ? CQFAITargetReader.Find(model.Write(context!.Read(), root: true), path)
                : liveMap.ReadThing(id).Element("configuration")?.Element("interaction") ?? throw new InvalidDataException("CQF_AI_InvalidValue: interactive object required");
            Type type = model.Resolve(current.Attribute("Class")?.Value ?? "");
            if (!typeof(CustomThingData_InteractableThing).IsAssignableFrom(type) && type != typeof(CQFAIInteractableConfiguration)) throw new InvalidDataException("CQF_AI_InvalidValue: interactive object required");
            XElement operation = CQFAIChanges.Parse(arguments.Element("operation_xml")!.Value);
            if (operation.Name != "value" || model.Read(operation, typeof(InteractionOperation)) is not InteractionOperation) throw new InvalidDataException("CQF_AI_InvalidValue: interaction");
            XElement changes = new XElement("changes", new XElement("append", new XAttribute("path", liveMap == null ? path.TrimEnd('/') + "/operations" : "/interaction/operations"), operation));
            return Apply(liveMap == null ? changes : new XElement("changes", new XElement("editThing", new XAttribute("id", id), changes)));
        }
        private XElement Configure(XElement arguments, bool entrance)
        {
            string path = Text(arguments, "object_path", "/");
            string id = Text(arguments, "thing_id", "");
            XElement current = liveMap == null ? CQFAITargetReader.Find(model.Write(context!.Read(), root: true), path)
                : liveMap.ReadThing(id).Element("configuration")?.Element(entrance ? "entrance" : "exit") ?? throw new InvalidDataException("CQF_AI_InvalidValue: portal type");
            Type type = model.Resolve(current.Attribute("Class")?.Value ?? "");
            bool matches = entrance ? typeof(CustomThingData_CustomMapEntrance).IsAssignableFrom(type) || type == typeof(CQFAIEntranceConfiguration)
                : typeof(CustomThingData_CustomMapExit).IsAssignableFrom(type) || type == typeof(CQFAIExitConfiguration);
            if (!matches) throw new InvalidDataException("CQF_AI_InvalidValue: portal type");
            XElement value = CQFAIChanges.Parse(arguments.Element("configuration_xml")!.Value);
            string[] allowed = entrance ? new[] { "data", "exitName", "opended", "enterActions", "tagWithChance", "mapDefWithChance" } : new[] { "exitName", "enterActions" };
            if (value.Name != "value" || value.HasAttributes || !value.HasElements || value.Elements().Any(field => !allowed.Contains(field.Name.ToString()))
                || value.Elements().GroupBy(field => field.Name).Any(group => group.Count() > 1)) throw new InvalidDataException("CQF_AI_InvalidValue: portal configuration");
            XElement changes = new XElement("changes", value.Elements().Select(field => new XElement("set", new XAttribute("path", (liveMap == null ? path.TrimEnd('/') : entrance ? "/entrance" : "/exit") + "/" + field.Name),
                new XElement("value", field.Attributes(), field.Nodes()))));
            return Apply(liveMap == null ? changes : new XElement("changes", new XElement("editThing", new XAttribute("id", id), changes)));
        }
        private XElement ReadLiveRegion(XElement arguments)
        {
            int x = Number(arguments, "x", -1), z = Number(arguments, "z", -1), width = Number(arguments, "width", 0), height = Number(arguments, "height", 0);
            var size = liveMap!.Info.size;
            if (x < 0 || z < 0 || width < 1 || height < 1 || (long)x + width > size.x || (long)z + height > size.z || (long)width * height > 4096) throw new InvalidDataException("CQF_AI_MapBounds");
            return liveMap.ReadRegion(new CellRect(x, z, width, height), Number(arguments, "offset", 0), Number(arguments, "limit", 100));
        }
        private XElement ReadLiveThing(XElement arguments)
        {
            int offset = Number(arguments, "offset", 0), limit = Number(arguments, "limit", 20);
            CQFAIRuntimeCatalog.ValidatePage("", offset, limit);
            XElement thing = liveMap!.ReadThing(arguments.Element("thing_id")!.Value);
            XElement? configuration = thing.Element("configuration");
            if (configuration == null)
            {
                if (arguments.Element("path") != null || offset != 0) throw new InvalidDataException("CQF_AI_InvalidValue: Thing has no editable configuration");
                return thing;
            }
            string path = Text(arguments, "path", "/");
            if (path == "/" && offset == 0 && arguments.Element("path") == null)
            {
                if (thing.ToString(SaveOptions.DisableFormatting).Length > 32768) configuration.ReplaceWith(new XElement("configurationSummary", reader.SummaryXml(configuration)));
                return thing;
            }
            XElement value = CQFAITargetReader.Find(configuration, path);
            bool collection = value.Elements().Any(element => element.Name == "li" || element.Name == "entry");
            if (!collection && offset != 0) throw new InvalidDataException("CQF_AI_InvalidTool: offset requires a collection");
            XElement data = new XElement("configurationData", new XAttribute("path", path));
            if (collection)
            {
                data.Add(new XAttribute("total", value.Elements().Count()), new XAttribute("offset", offset));
                data.Add(new XElement(value.Name, value.Attributes(), value.Elements().Skip(offset).Take(limit).Select(element => new XElement(element))));
            }
            else data.Add(new XElement(value));
            configuration.ReplaceWith(data);
            if (thing.ToString(SaveOptions.DisableFormatting).Length > 65536) throw new InvalidDataException("CQF_AI_ReadTooLarge");
            return thing;
        }
        private static string Text(XElement arguments, string name, string fallback) => arguments.Element(name)?.Value ?? fallback;
        private static int Number(XElement arguments, string name, int fallback)
        {
            if (arguments.Element(name) == null) return fallback;
            return int.TryParse(arguments.Element(name)!.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : throw new InvalidDataException("CQF_AI_InvalidTool: " + name);
        }
        public static event Action<CQFAIToolRegistry, CQFAIEditorContext?>? RegisterTools;
        private readonly CQFAIModel model;
        private readonly CQFAIEditorContext? context;
        private readonly CQFAITransaction? transaction;
        private readonly CQFAITargetReader reader;
        private string command;
        private readonly CQFAIRuntimeTools runtimeTools;
        private readonly bool generateText;
        private readonly ICQFAILiveMap? liveMap;
        private readonly Dictionary<string, CQFAITool> tools = new Dictionary<string, CQFAITool>(StringComparer.Ordinal);
    }
}
