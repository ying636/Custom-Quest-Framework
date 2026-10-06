using System.Globalization;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAITokenUsage
    {
        public CQFAITokenUsage(long input, long output, long total, long? cached = null, long? reasoning = null)
        {
            if (input < 0 || output < 0 || total < 0 || input > long.MaxValue - output || total != input + output
                || cached < 0 || cached > input || reasoning < 0 || reasoning > output) throw new InvalidDataException("CQF_AI_InvalidUsage");
            Input = input; Output = output; Total = total; Cached = cached; Reasoning = reasoning;
        }
        public long Input { get; }
        public long Output { get; }
        public long Total { get; }
        public long? Cached { get; }
        public long? Reasoning { get; }
        public static CQFAITokenUsage? Read(XElement? value)
        {
            if (value == null || value.Attribute("type")?.Value == "null") return null;
            return new CQFAITokenUsage(Number(value.Element("prompt_tokens"))!.Value, Number(value.Element("completion_tokens"))!.Value,
                Number(value.Element("total_tokens"))!.Value, Number(value.Element("prompt_tokens_details")?.Element("cached_tokens"), true),
                Number(value.Element("completion_tokens_details")?.Element("reasoning_tokens"), true));
        }
        private static long? Number(XElement? element, bool optional = false)
        {
            if (optional && (element == null || element.Attribute("type")?.Value == "null")) return null;
            if (element?.Attribute("type")?.Value != "number" || !long.TryParse(element.Value, NumberStyles.None, CultureInfo.InvariantCulture, out long value) || value < 0)
                throw new InvalidDataException("CQF_AI_InvalidUsage");
            return value;
        }
    }
}
