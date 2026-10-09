using System;

namespace QuestEditor_Library
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CQFLocalizableTextAttribute : Attribute
    {
        public CQFLocalizableTextAttribute(string? preserveAs = null)
        {
            this.PreserveAs = preserveAs;
        }

        public string? PreserveAs { get; }
    }
}
