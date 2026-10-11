using System.Collections.Generic;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFConfigurationReference
    {
        public CQFConfigurationReference(string kind, string value)
        {
            this.Kind = kind;
            this.Original = value;
            this.Value = value;
        }

        public string Kind { get; }
        public string Original { get; }
        public string Value { get; set; }
        public List<XElement> Elements { get; } = new List<XElement>();
    }
}
