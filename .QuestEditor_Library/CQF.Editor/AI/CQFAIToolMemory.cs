using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed partial class CQFAIConversation
    {
        private XElement CompactToolResult(XElement result, int maximumCharacters = 8192)
        {
            result.Descendants("defs").Elements("def").Elements("description").Remove();
            foreach (XElement schemas in result.Descendants("schemas").ToArray())
            {
                XElement[] types = schemas.Elements("type").ToArray();
                schemas.ReplaceWith(new XElement("schemas", new XAttribute("historical", true), new XAttribute("total", types.Length),
                    new XAttribute("detailsOmitted", true), types.Take(20).Select(type => new XElement("type", type.Attributes(), new XAttribute("fields", type.Elements("field").Count()))),
                    new XElement("notice", "Historical schema field details omitted. Request cqf_get_schema for the exact type before editing.")));
            }
            if (result.ToString(SaveOptions.DisableFormatting).Length <= maximumCharacters) return result;
            XElement summary = new XElement(result.Name, result.Attributes(),
                new XElement("notice", "Detailed historical data omitted to fit the request. Successful writes have already executed and must not be replayed. Re-read current data using narrow paths and pagination."));
            summary.SetAttributeValue("detailsOmitted", true);
            XElement outcomes = new XElement("execution_outcomes");
            foreach (XElement error in result.Descendants("error").Take(4))
                outcomes.Add(new XElement("error", error.Attributes(), error.Value.Substring(0, Math.Min(error.Value.Length, Math.Min(512, maximumCharacters / 8)))));
            int outcomeBudget = Math.Min(1500, maximumCharacters / 4);
            foreach (XElement applied in result.Descendants().Where(element => element.Name == "applied" || element.Name == "liveMapApplied" || element.Name == "runtimeEdited").Take(4))
            {
                if (outcomeBudget <= 0) break;
                outcomes.Add(this.SummarizeToolData(applied, ref outcomeBudget, 0));
            }
            if (outcomes.HasElements) summary.Add(outcomes);
            int budget = Math.Max(0, maximumCharacters - 1200 - summary.ToString(SaveOptions.DisableFormatting).Length);
            foreach (XElement child in result.Elements())
            {
                if (budget <= 0) break;
                summary.Add(this.SummarizeToolData(child, ref budget, 0));
            }
            if (summary.ToString(SaveOptions.DisableFormatting).Length <= maximumCharacters) return summary;
            XElement receipt = new XElement(result.Name, result.Attributes(),
                new XElement("notice", "Historical result details omitted. Use current read tools if needed; do not repeat successful writes."));
            receipt.SetAttributeValue("detailsOmitted", true);
            foreach (XElement error in result.Descendants("error").Take(4))
                receipt.Add(new XElement("error", error.Attributes(), error.Value.Substring(0, Math.Min(error.Value.Length, Math.Min(512, maximumCharacters / 8)))));
            foreach (XElement applied in result.Descendants().Where(element => element.Name == "applied" || element.Name == "liveMapApplied" || element.Name == "runtimeEdited").Take(4))
                receipt.Add(new XElement(applied.Name, applied.Attributes()));
            return receipt;
        }

        private XElement SummarizeToolData(XElement value, ref int budget, int depth)
        {
            XElement summary = new XElement(value.Name, value.Attributes());
            budget -= summary.ToString(SaveOptions.DisableFormatting).Length;
            if (!value.HasElements)
            {
                int length = Math.Min(value.Value.Length, Math.Max(0, Math.Min(512, budget)));
                summary.Value = value.Value.Substring(0, length);
                budget -= length;
                if (length < value.Value.Length) summary.SetAttributeValue("textOmitted", true);
                return summary;
            }
            XElement[] children = value.Elements().ToArray();
            int kept = 0;
            if (depth < 4)
                foreach (XElement child in children.Take(12))
                {
                    if (budget <= 0) break;
                    summary.Add(this.SummarizeToolData(child, ref budget, depth + 1));
                    kept++;
                }
            if (kept < children.Length) summary.SetAttributeValue("childrenOmitted", children.Length - kept);
            return summary;
        }
    }
}
