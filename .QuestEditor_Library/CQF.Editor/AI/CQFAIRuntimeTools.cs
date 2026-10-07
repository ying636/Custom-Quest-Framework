using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIRuntimeTools
    {
        public CQFAIRuntimeTools(CQFAIModel model, CQFAIRuntimeJournal journal, bool text, string command = "")
        {
            pawns = new CQFAIPawnRuntime(model, journal, text, command);
            quests = new CQFAIQuestRuntime(model, journal);
            maps = new CQFAIWorldRuntime(model, journal, text, command);
            inventory = new CQFAIInventoryRuntime(journal);
            facilities = new CQFAIFacilityRuntime(model, journal, text, command);
            database = new CQFAIDatabaseRuntime(journal);
            execution = new CQFAIExecutionRuntime(model, journal, text, command);
            documents = new CQFAIDefinitionRuntime(model, journal, text, command);
        }
        public void Register(CQFAIToolRegistry registry)
        {
            registry.Register(new CQFAITool("cqf_runtime_help", "Discover runtime query/operation schemas by group. Groups: pawns, duties, quests, world, database, inventory, facilities, execution, definitions, diagnostics. Read help before operating. Operations do not require an editor target; permissions still apply.",
                arguments => Help(CQFAIRuntimeRequest.Text(arguments, "group")), ("group", "Exact group; empty returns group names.", false)));
            registry.Register(new CQFAITool("cqf_read_runtime", "Read paginated actual game state without creating missing data. Query XML specifies kind and identifiers. Discover query fields with cqf_runtime_help. Read current state before and after writes.",
                arguments => Trace(() => Read(CQFAIRuntimeRequest.Parse(arguments.Element("query_xml")!.Value, "query"))), ("query_xml", "<query><kind>kind from runtime help</kind>...fields...</query>; offset defaults 0, limit 20 (maximum 40).", true)));
            registry.Register(new CQFAITool("cqf_check_conditions", "Evaluate CQF DialogCondition instances using explicit targets and optional current Quest. Returns actual satisfied result and failure reason for each condition. This does not execute CQFActions.",
                arguments => Trace(() => execution.Check(CQFAIRuntimeRequest.Parse(arguments.Element("request_xml")!.Value, "request"))), ("request_xml", "Request schema in execution help. Conditions use supported model value XML, not source Def XML.", true)));
            registry.Register(new CQFAITool("cqf_inspect_map", "Inspect real reachability, rooms, roofs, support and active game conditions. Diagnostics are scoped to requested cells/Pawn; they do not prove layout quality or all action side effects.",
                arguments => Trace(() => maps.Inspect(CQFAIRuntimeRequest.Parse(arguments.Element("query_xml")!.Value, "query"))), ("query_xml", "Query schema in diagnostics help.", true)));
            registry.Register(new CQFAITool("cqf_export_definition", "Export source XML for an exact loaded CQF editable definition. Large XML is paginated as text; this performs no file writes.", documents.Export,
                ("type", "Exact fully qualified supported Def type.", true), ("name", "Exact loaded defName.", true), ("offset", "Character offset, default 0.", false), ("limit", "Characters 1 to 16000, default 8000.", false)));
            if (!registry.EditingAllowed) return;
            registry.Register(new CQFAITool("cqf_operate_runtime", "Execute one validated runtime operation using exact current IDs. Consult runtime help first. Scalar/configuration/database edits support task undo; operations that trigger game effects report undoSupported=false. Read back effects separately; invocation alone is not proof of the requested outcome.",
                arguments => Operate(CQFAIRuntimeRequest.Parse(arguments.Element("operation_xml")!.Value, "operation")), ("operation_xml", "<operation><kind>kind from runtime help</kind>...fields...</operation>. Exact field schema is returned by cqf_runtime_help.", true)));
            registry.Register(new CQFAITool("cqf_manage_definition", "Create/copy/register or save a CQF definition, capture a map, or refresh existing runtime bindings. New names must start CQF_. Save paths are generated inside the CQF content directory; overwrites require explicit overwrite=true. File writes and hot reload are not fully undoable.",
                arguments => Trace(() => documents.Manage(CQFAIRuntimeRequest.Parse(arguments.Element("request_xml")!.Value, "request"))), ("request_xml", "Request schema in definitions help.", true)));
        }
        public void AddInstruction(string text) { pawns.AddInstruction(text); maps.AddInstruction(text); facilities.AddInstruction(text); execution.AddInstruction(text); documents.AddInstruction(text); }
        private XElement Help(string group) => group switch
        {
            "" => new XElement("groups", new[] { "pawns", "duties", "quests", "world", "database", "inventory", "facilities", "execution", "definitions", "diagnostics" }.Select(name => new XElement("group", name))),
            "pawns" or "duties" => pawns.Help(group), "quests" => quests.Help(), "world" or "diagnostics" => maps.Help(group),
            "database" => database.Help(), "inventory" => inventory.Help(), "facilities" => facilities.Help(), "execution" => execution.Help(),
            "definitions" => documents.Help(), _ => throw new InvalidDataException("CQF_AI_InvalidTool: runtime help group")
        };
        private XElement Read(XElement query)
        {
            if (Current.Game == null) throw new InvalidDataException("CQF_AI_MissingResource: no active game");
            CQFAIRuntimeRequest.Offset(query);
            string kind = CQFAIRuntimeRequest.Text(query, "kind");
            return kind switch
            {
                "pawns" or "factions" or "pawn" or "pawn_skills" or "pawn_health" or "pawn_traits" or "pawn_genes" or "pawn_abilities" or "pawn_needs" or "pawn_profile" or "pawn_dialogue" or "duty" or "duty_database" or "lords" or "lord" or "lord_route" => pawns.Read(query),
                "quests" or "quest" or "quest_parts" or "quest_part_fields" or "quest_books" or "quest_book" or "quest_steps" or "quest_objectives" => quests.Read(query),
                "maps" or "main_sites" or "main_site_cache" or "world_map" or "spawn_queues" or "map_routes" => maps.Read(query),
                "inventory" => inventory.Read(query), "facilities" or "facility" => facilities.Read(query),
                "signal_receivers" => execution.Read(query), _ => throw new InvalidDataException("CQF_AI_InvalidTool: runtime query kind")
            };
        }
        private XElement Operate(XElement operation) => Trace(() => OperateCore(operation));
        private XElement Trace(Func<XElement> execute)
        {
            var before = Log.Messages.ToDictionary(message => message, message => message.repeats);
            XElement result;
            try { result = execute(); }
            catch (Exception error) when (error is not InvalidDataException && (error is not InvalidOperationException || !error.Message.StartsWith("CQF_", StringComparison.Ordinal)))
            {
                Log.Error("CQF AI runtime operation: " + error);
                string detail = error.ToString();
                result = new XElement("runtimeFailure", new XAttribute("success", false), new XAttribute("effectsMayHaveOccurred", true), new XElement("error", detail.Length > 4000 ? detail.Substring(0, 4000) : detail));
            }
            LogMessage[] errors = Log.Messages.Where(message => message.type == LogMessageType.Error && (!before.TryGetValue(message, out int repeats) || message.repeats > repeats)).Take(8).ToArray();
            if (errors.Length > 0)
            {
                result.SetAttributeValue("success", false); result.SetAttributeValue("effectsMayHaveOccurred", true);
                result.Add(new XElement("runtimeErrors", errors.Select(error => new XElement("error", error.text.Length > 2000 ? error.text.Substring(0, 2000) : error.text))));
            }
            return result;
        }
        private XElement OperateCore(XElement operation)
        {
            if (Current.Game == null) throw new InvalidDataException("CQF_AI_MissingResource: no active game");
            if (!journalValid()) throw new InvalidDataException("CQF_AI_StaleTarget");
            string kind = CQFAIRuntimeRequest.Text(operation, "kind");
            if (kind.StartsWith("database_", StringComparison.Ordinal) || kind is "map_target" or "duty_value") return database.Operate(operation);
            if (kind.StartsWith("pawn_", StringComparison.Ordinal) || kind.StartsWith("duty_", StringComparison.Ordinal) || kind.StartsWith("lord_", StringComparison.Ordinal)) return pawns.Operate(operation);
            if (kind.StartsWith("quest_", StringComparison.Ordinal)) return quests.Operate(operation);
            if (kind.StartsWith("inventory_", StringComparison.Ordinal)) return inventory.Operate(operation);
            if (kind.StartsWith("facility_", StringComparison.Ordinal)) return facilities.Operate(operation);
            if (kind is "send_signal" or "run_actions") return execution.Operate(operation);
            return maps.Operate(operation);
        }
        private bool journalValid() => database.Journal.IsTargetValid;
        private readonly CQFAIPawnRuntime pawns;
        private readonly CQFAIQuestRuntime quests;
        private readonly CQFAIWorldRuntime maps;
        private readonly CQFAIInventoryRuntime inventory;
        private readonly CQFAIFacilityRuntime facilities;
        private readonly CQFAIDatabaseRuntime database;
        private readonly CQFAIExecutionRuntime execution;
        private readonly CQFAIDefinitionRuntime documents;
    }
}
