using System.Diagnostics;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIOperation
    {
        public CQFAIOperation(CQFAIToolCall call)
        {
            Id = call.Id; Name = call.Name;
            Arguments = string.Join(", ", call.Arguments.Elements().Where(element => !element.Name.LocalName.EndsWith("_xml", StringComparison.Ordinal))
                .Select(element => element.Name.LocalName + "=" + (element.Value.Length <= 120 ? element.Value : element.Value.Substring(0, 120))));
            XElement? request = call.Arguments.Elements().FirstOrDefault(element => element.Name.LocalName is "operation_xml" or "query_xml" or "request_xml");
            if (request != null)
            {
                try
                {
                    XElement value = CQFAIChanges.Parse(request.Value);
                    Arguments = string.Join(", ", value.Elements().Where(element => !element.HasElements && !element.Name.LocalName.EndsWith("_xml", StringComparison.Ordinal)).Select(element => element.Name.LocalName + "=" + (element.Value.Length <= 120 ? element.Value : element.Value.Substring(0, 120))));
                }
                catch (Exception error) when (error is InvalidDataException || error is System.Xml.XmlException) { Arguments = "CQF_AI_InvalidTool".Translate() + ": " + error.Message; }
            }
        }
        public string Id { get; }
        public string Name { get; }
        public string Arguments { get; private set; }
        public bool? Succeeded { get; private set; }
        public string Error { get; private set; } = string.Empty;
        public int? Count { get; private set; }
        public double ElapsedSeconds => elapsed + timer.Elapsed.TotalSeconds;
        public string Label => "CQF_AI_OperationLine".Translate(ToolLabel(Name), (Succeeded == null ? "CQF_AI_OperationRunning" : Succeeded.Value ? "CQF_AI_OperationDone" : "CQF_AI_OperationFailed").Translate(), ElapsedSeconds < 0.1 ? "<0.1" : ElapsedSeconds.ToString("0.0")).ToString()
            + (Count == null ? string.Empty : " " + "CQF_AI_OperationCount".Translate(Count.Value).ToString());
        public string Details => string.Join("\n", new[] { Arguments, Error }.Where(value => value.Length > 0));
        public void Complete(XElement result, string secret)
        {
            timer.Stop();
            Succeeded = result.Attribute("success")?.Value == "true";
            string error = result.Element("error")?.Value ?? string.Empty;
            if (error.Length > 0)
            {
                string key = error.Split(new[] { ':', ' ' }, 2)[0];
                error = key.CanTranslate() ? key.Translate().ToString() + error.Substring(key.Length) : error;
                Error = secret.Length > 0 ? error.Replace(secret, "***") : error;
            }
            string? count = result.Descendants().FirstOrDefault(element => element.Name.LocalName is "applied" or "liveMapApplied")?.Attribute("operations")?.Value;
            if (int.TryParse(count, out int number)) Count = number;
        }
        public static string ToolLabel(string name)
        {
            string key = name switch
            {
                "cqf_get_context" => "CQF_AI_ToolContext",
                "cqf_list_targets" or "cqf_select_target" => "CQF_AI_ToolTargets",
                "cqf_list_mods" => "CQF_AI_ToolMods",
                "cqf_list_def_types" or "cqf_find_types" => "CQF_AI_ToolTypes",
                "cqf_query_resources" or "cqf_read_resource" => "CQF_AI_ToolResources",
                "cqf_list_cqf_things" => "CQF_AI_ToolCQFThings",
                "cqf_read_map_targets" => "CQF_AI_ToolMapTargets",
                "cqf_read_map_signals" => "CQF_AI_ToolMapSignals",
                "cqf_read_map_configuration" or "cqf_edit_map_configuration" => "CQF_AI_ToolMapConfiguration",
                "cqf_list_databases" or "cqf_read_database" => "CQF_AI_ToolDatabase",
                "cqf_get_schema" => "CQF_AI_ToolSchema",
                "cqf_read_target" => "CQF_AI_ToolReadTarget",
                "cqf_validate_target" => "CQF_AI_ToolValidate",
                "cqf_read_map_region" => "CQF_AI_ToolReadMap",
                "cqf_read_map_thing" => "CQF_AI_ToolReadThing",
                "cqf_apply_changes" => "CQF_AI_ToolApply",
                "cqf_edit_map_thing" => "CQF_AI_ToolEditThing",
                "cqf_add_dialogue_branch" => "CQF_AI_ToolBranch",
                "cqf_add_interaction" => "CQF_AI_ToolInteraction",
                "cqf_configure_entrance" or "cqf_configure_exit" => "CQF_AI_ToolPortal",
                "cqf_get_task" or "cqf_set_goal" or "cqf_finish_goal" => "CQF_AI_ToolGoal",
                "cqf_update_plan" => "CQF_AI_ToolPlan",
                "cqf_spawn_agent" => "CQF_AI_ToolDelegate",
                "cqf_wait_agents" => "CQF_AI_ToolWaitAgents",
                "cqf_get_agent_result" => "CQF_AI_ToolAgentResult",
                "cqf_runtime_help" => "CQF_AI_ToolRuntimeHelp",
                "cqf_read_runtime" => "CQF_AI_ToolRuntimeRead",
                "cqf_operate_runtime" => "CQF_AI_ToolRuntimeOperate",
                "cqf_check_conditions" => "CQF_AI_ToolConditions",
                "cqf_inspect_map" => "CQF_AI_ToolDiagnostics",
                "cqf_manage_definition" or "cqf_export_definition" => "CQF_AI_ToolDefinition",
                _ => "CQF_AI_ToolExtension"
            };
            return key.Translate();
        }
        public XElement Save() => new XElement("operation", new XAttribute("id", Id), new XAttribute("name", Name), new XAttribute("elapsed", ElapsedSeconds),
            Succeeded == null ? null : new XAttribute("success", Succeeded.Value), Count == null ? null : new XAttribute("count", Count.Value), new XElement("arguments", Arguments), new XElement("error", Error));
        public static CQFAIOperation Restore(XElement value)
        {
            string id = (string?)value.Attribute("id") ?? "", name = (string?)value.Attribute("name") ?? "";
            double seconds = (double)value.Attribute("elapsed")!;
            int? count = (int?)value.Attribute("count");
            if (id.Length is < 1 or > 256 || !name.StartsWith("cqf_", StringComparison.Ordinal) || name.Length > 100 || seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || count < 0)
                throw new InvalidDataException("CQF_AI_InvalidHistory");
            CQFAIOperation operation = new CQFAIOperation(new CQFAIToolCall(id, name, new XElement("arguments")))
            { elapsed = seconds, Succeeded = (bool?)value.Attribute("success"), Count = count, Error = value.Element("error")?.Value ?? "", Arguments = value.Element("arguments")?.Value ?? "" };
            operation.timer.Stop(); operation.timer.Reset();
            return operation;
        }
        private readonly Stopwatch timer = Stopwatch.StartNew();
        private double elapsed;
    }
}
