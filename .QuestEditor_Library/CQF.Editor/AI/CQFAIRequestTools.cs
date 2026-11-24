using System.Xml.Linq;

namespace QuestEditor_Library
{
    internal sealed class CQFAIRequestTools
    {
        public IReadOnlyCollection<CQFAITool> Select(CQFAIToolRegistry registry)
        {
            return registry.Tools.Where(tool => !OnDemand.Contains(tool.Name) || discovered.Contains(tool.Name)).ToArray();
        }

        public void Register(CQFAIToolRegistry registry)
        {
            if (registry.Tools.Any(tool => tool.Name == "cqf_discover_tools")) return;
            registry.Register(new CQFAITool("cqf_discover_tools",
                "Load specialized tool schemas for later calls. Search by name or capability, such as database, definitions, conditions, or spatial diagnostics.",
                arguments =>
                {
                    string search = arguments.Element("search")?.Value.Trim() ?? "";
                    if (search.Length > 100) throw new InvalidDataException("CQF_AI_InvalidTool");
                    CQFAITool[] matches = registry.Tools.Where(tool => OnDemand.Contains(tool.Name) &&
                        (tool.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         tool.Description.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                    foreach (CQFAITool tool in matches) discovered.Add(tool.Name);
                    return new XElement("enabled_tools", matches.Select(tool => new XElement("tool", new XAttribute("name", tool.Name))));
                }, ("search", "Capability or tool name. Empty lists all specialized tools.", false)));
        }

        private static readonly HashSet<string> OnDemand = new HashSet<string>(StringComparer.Ordinal)
        {
            "cqf_list_databases", "cqf_read_database", "cqf_check_conditions", "cqf_inspect_map",
            "cqf_export_definition", "cqf_manage_definition", "cqf_read_resource",
            "cqf_scene_assets", "cqf_scene_preview", "cqf_scene_checks"
        };
        private readonly HashSet<string> discovered = new HashSet<string>(StringComparer.Ordinal);
    }
}
