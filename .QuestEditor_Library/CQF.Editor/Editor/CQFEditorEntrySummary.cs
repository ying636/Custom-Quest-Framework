using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFEditorEntrySummary
    {
        public static string Describe(object entry)
        {
            if (entry == null)
            {
                return "None".Translate();
            }
            string title = entry.GetType().Name.Translate();
            string details = Details(entry);
            return details.Length == 0 ? title : title + " · " + details;
        }

        public static string Details(object entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }
            Type type = entry.GetType();
            if (!fields.TryGetValue(type, out FieldInfo[] entryFields))
            {
                entryFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(field => !field.Name.StartsWith("buffer", StringComparison.OrdinalIgnoreCase)
                        && field.Name != "failReason" && field.Name != "show" && field.Name != "foldout")
                    .OrderBy(field => fieldNames.TryGetValue(field.Name, out string key) ? 0 : 1)
                    .ToArray();
                fields.Add(type, entryFields);
            }
            List<string> parts = new List<string>();
            foreach (FieldInfo field in entryFields)
            {
                object value = field.GetValue(entry);
                string text = Format(value, field.Name);
                if (text.NullOrEmpty())
                {
                    continue;
                }
                if (fieldNames.TryGetValue(field.Name, out string key))
                {
                    text = key.Translate() + " " + text;
                }
                parts.Add(text);
                if (parts.Count == 4)
                {
                    break;
                }
            }
            return string.Join(" · ", parts);
        }

        private static string Format(object value, string name)
        {
            if (value is string text)
            {
                return text.Replace('\n', ' ').Replace('\r', ' ');
            }
            if (value is Def def)
            {
                return def.LabelCap;
            }
            if (value is IEnumerable<string> targets)
            {
                return string.Join(", ", targets);
            }
            if (value is bool flag)
            {
                return name == "valueOfBool" ? (flag ? "✓" : "×") : null;
            }
            if (value is int || value is float || value is double)
            {
                return fieldNames.ContainsKey(name) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
            }
            if (value is IList list && list.Count > 0 && name == "actions")
            {
                return "InteractionActions".Translate() + " " + list.Count;
            }
            if (value is IList conditions && conditions.Count > 0 && (name == "conditions" || name == "condition"))
            {
                return "Conditions".Translate() + " " + conditions.Count;
            }
            return null;
        }

        private static readonly Dictionary<Type, FieldInfo[]> fields = new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<string, string> fieldNames = new Dictionary<string, string>
        {
            { "targetsText", "TargetKey" },
            { "targetText", "TargetKey" },
            { "targetKey", "TargetKey" },
            { "signal", "OutSignal" },
            { "delayTime", "DelayTime" },
            { "loopCount", "LoopCount" },
            { "chance", "chance" },
            { "keyOfBool", "keyOfBoolValue" },
            { "boolName", "keyOfBoolValue" },
            { "valueOfBool", "valueOfBool" }
        };
    }
}
