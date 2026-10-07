using System.Collections;
using System.Globalization;
using System.Reflection;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIModel
    {
        public CQFAIModel()
        {
            types = GenTypes.AllTypes.Where(type => type.FullName != null && IsDataType(type))
                .GroupBy(type => type.FullName).ToDictionary(group => group.Key!, group => group.First(), StringComparer.Ordinal);
        }
        public IEnumerable<Type> Types => types.Values;
        public IEnumerable<FieldInfo> Fields(Type type)
        {
            IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (typeof(ThinkNode).IsAssignableFrom(type) || typeof(QuestNode).IsAssignableFrom(type))
            {
                List<FieldInfo> nodeFields = new List<FieldInfo>();
                for (Type? current = type; current != null && current != typeof(object); current = current.BaseType)
                    nodeFields.AddRange(current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                fields = nodeFields.GroupBy(field => field.Name).Select(group => group.First());
            }
            return fields
                .Where(field => !field.IsInitOnly && !field.IsDefined(typeof(UnsavedAttribute), true)
                    && !field.Name.Contains("k__BackingField")
                    && (field.DeclaringType != typeof(Def) || field.Name is "defName" or "label" or "description")
                    && !ExcludedFields.Contains(field.Name) && IsFieldType(field.FieldType)).OrderBy(field => field.Name);
        }
        public XElement Write(object? value, string name = "value", bool root = false)
        {
            if (value is DialogTreeDef tree && root) tree.ResolveOptions();
            return WriteValue(value, name, root, new HashSet<object>(), 0);
        }
        public object? Read(XElement xml, Type declared, object? template = null, bool root = false)
        {
            object? value = ReadValue(xml, declared, template, root, 0);
            if (value is DialogTreeDef tree && root) tree.Update();
            return value;
        }
        public object Copy(object source)
        {
            return Read(Write(source, root: true), source.GetType(), source, true)!;
        }
        public Type Resolve(string name)
        {
            return types.TryGetValue(name, out Type type) ? type : throw new InvalidDataException("CQF_AI_UnknownType: " + name);
        }
        public XElement Schema(Type root, bool recursive = true)
        {
            Queue<Type> pending = new Queue<Type>();
            HashSet<Type> visited = new HashSet<Type>();
            pending.Enqueue(root);
            XElement schema = new XElement("schemas");
            while (pending.Count > 0)
            {
                Type type = pending.Dequeue();
                if (!visited.Add(type)) continue;
                XElement entry = new XElement("type", new XAttribute("name", type.FullName ?? type.Name));
                foreach (FieldInfo field in Fields(type))
                {
                    XElement info = new XElement("field", new XAttribute("name", field.Name), new XAttribute("type", field.FieldType.ToString()));
                    if (type == typeof(DialogNode) && field.Name == "optionIds")
                        info.Add(new XAttribute("description", "Ordered references to DialogTreeDef.optionMoulds keys. Several nodes can reference the same choice. Removing one reference does not delete the choice."));
                    if (type == typeof(DialogTreeDef) && field.Name == "optionMoulds")
                        info.Add(new XAttribute("description", "Canonical choice definitions, including unused choices. Editing a choice updates every node that references its key. Delete all node optionIds references before deleting a choice."));
                    Type item = field.FieldType.IsArray ? field.FieldType.GetElementType()! : field.FieldType;
                    if (recursive && !IsScalar(item))
                    {
                        if (item.IsGenericType) foreach (Type argument in item.GetGenericArguments()) Enqueue(argument);
                        else Enqueue(item);
                    }
                    if (item.IsEnum) info.Add(new XAttribute("values", string.Join(",", Enum.GetNames(item))));
                    entry.Add(info);
                }
                schema.Add(entry);
            }
            return schema;
            void Enqueue(Type type)
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
                if (typeof(Def).IsAssignableFrom(type) || IsScalar(type) || typeof(IList).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type)) return;
                foreach (Type candidate in Types.Where(candidate => type.IsAssignableFrom(candidate) && !candidate.IsAbstract))
                    pending.Enqueue(candidate);
            }
        }
        private XElement WriteValue(object? value, string name, bool root, HashSet<object> ancestors, int depth)
        {
            if (depth > 64) throw new InvalidDataException("CQF_AI_Depth");
            XElement xml = new XElement(name);
            if (value == null) { xml.Add(new XAttribute("null", true)); return xml; }
            Type type = value is Type ? typeof(Type) : value.GetType();
            if (value is ISlateRef slate && slate.SlateRef == null) { xml.Add(new XAttribute("slateNull", true)); return xml; }
            if (value is Def definition && !root)
            {
                xml.Add(new XAttribute("def", type.FullName!), definition.defName);
                return xml;
            }
            if (IsScalar(type)) { xml.Value = ScalarText(value); return xml; }
            if (!type.IsValueType && !ancestors.Add(value)) throw new InvalidDataException("CQF_AI_Cycle: " + type.Name);
            try
            {
                if (value is IDictionary dictionary)
                {
                    foreach (DictionaryEntry entry in dictionary)
                        xml.Add(new XElement("entry", WriteValue(entry.Key, "key", false, ancestors, depth + 1), WriteValue(entry.Value, "value", false, ancestors, depth + 1)));
                }
                else if (value is IList list)
                {
                    foreach (object? item in list) xml.Add(WriteValue(item, "li", false, ancestors, depth + 1));
                }
                else
                {
                    if (!IsDataType(type)) throw new InvalidDataException("CQF_AI_UnknownType: " + type.FullName);
                    xml.Add(new XAttribute("Class", type.FullName!));
                    foreach (FieldInfo field in Fields(type)) xml.Add(WriteValue(field.GetValue(value), field.Name, false, ancestors, depth + 1));
                }
            }
            finally { if (!type.IsValueType) ancestors.Remove(value); }
            return xml;
        }
        private object? ReadValue(XElement xml, Type declared, object? template, bool root, int depth)
        {
            if (depth > 64) throw new InvalidDataException("CQF_AI_Depth");
            if (xml.Attribute("null")?.Value == "true")
            {
                if (xml.HasElements || xml.Value.Length != 0 || xml.Attributes().Any(attribute => attribute.Name != "null")) throw new InvalidDataException("CQF_AI_InvalidValue: null");
                if (declared.IsValueType && Nullable.GetUnderlyingType(declared) == null) throw new InvalidDataException("CQF_AI_InvalidValue: " + declared.Name);
                return null;
            }
            Type type = Nullable.GetUnderlyingType(declared) ?? declared;
            if (typeof(Def).IsAssignableFrom(type) && !root)
            {
                if (xml.HasElements || xml.Attribute("Class") != null) throw new InvalidDataException("CQF_AI_InvalidValue: Def");
                Type actual = xml.Attribute("def") == null ? type : GenDefDatabase.AllDefTypesWithDatabases().FirstOrDefault(value => value.FullName == xml.Attribute("def")!.Value)
                    ?? throw new InvalidDataException("CQF_AI_UnknownType");
                if (!type.IsAssignableFrom(actual)) throw new InvalidDataException("CQF_AI_InvalidValue: Def type");
                return GenDefDatabase.GetAllDefsInDatabaseForDef(actual).FirstOrDefault(def => def.defName == xml.Value)
                    ?? throw new InvalidDataException("CQF_AI_MissingResource: " + xml.Value);
            }
            if (IsScalar(type))
            {
                if (xml.HasElements || xml.Attributes().Any(attribute => attribute.Name != "null" && !(typeof(ISlateRef).IsAssignableFrom(type) && attribute.Name == "slateNull"))) throw new InvalidDataException("CQF_AI_InvalidValue: " + type.Name);
                if (typeof(ISlateRef).IsAssignableFrom(type) && xml.Attribute("slateNull")?.Value == "true")
                {
                    if (xml.Value.Length != 0) throw new InvalidDataException("CQF_AI_InvalidValue: SlateRef");
                    return Activator.CreateInstance(type);
                }
                try { return ParseScalar(xml.Value, type); }
                catch (Exception error) when (error is FormatException || error is OverflowException || error is ArgumentException)
                { throw new InvalidDataException("CQF_AI_InvalidValue: " + type.Name + ": " + error.Message, error); }
            }
            if (xml.Attribute("Class") is XAttribute className)
            {
                Type actual = Resolve(className.Value);
                if (!type.IsAssignableFrom(actual) || actual.IsAbstract) throw new InvalidDataException("CQF_AI_InvalidValue: " + className.Value);
                type = actual;
            }
            if (xml.Attributes().Any(attribute => attribute.Name != "Class" && attribute.Name != "null")) throw new InvalidDataException("CQF_AI_InvalidValue: attributes");
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                IDictionary result = (IDictionary)Activator.CreateInstance(type)!;
                Type[] arguments = type.GetGenericArguments();
                foreach (XElement entry in xml.Elements())
                {
                    if (entry.Name != "entry" || entry.HasAttributes || entry.Elements().Count() != 2 || entry.Element("key") == null || entry.Element("value") == null) throw new InvalidDataException("CQF_AI_InvalidValue: dictionary");
                    object key = ReadValue(entry.Element("key")!, arguments[0], null, false, depth + 1)!;
                    object? previous = template is IDictionary old && old.Contains(key) ? old[key] : null;
                    result.Add(key, ReadValue(entry.Element("value")!, arguments[1], previous, false, depth + 1));
                }
                return result;
            }
            if (typeof(IList).IsAssignableFrom(type))
            {
                Type itemType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
                IList result = type.IsArray ? Array.CreateInstance(itemType, xml.Elements().Count()) : (IList)Activator.CreateInstance(type)!;
                int index = 0;
                foreach (XElement item in xml.Elements())
                {
                    if (item.Name != "li") throw new InvalidDataException("CQF_AI_InvalidValue: list");
                    object? old = template is IList list && index < list.Count ? list[index] : null;
                    object? value = ReadValue(item, itemType, old, false, depth + 1);
                    if (type.IsArray) result[index] = value; else result.Add(value);
                    index++;
                }
                return result;
            }
            if (!IsDataType(type) || type.IsAbstract) throw new InvalidDataException("CQF_AI_UnknownType: " + type.FullName);
            object copy = template != null && template.GetType() == type ? Memberwise.Invoke(template, null)! : Activator.CreateInstance(type)!;
            if (copy is DialogNode dialogNode)
            {
                dialogNode.options = new List<DialogOption>();
                dialogNode.optionsResolved = false;
            }
            Dictionary<string, FieldInfo> fields = Fields(type).ToDictionary(field => field.Name);
            HashSet<string> seen = new HashSet<string>();
            foreach (XElement element in xml.Elements())
            {
                if (!seen.Add(element.Name.LocalName) || !fields.TryGetValue(element.Name.LocalName, out FieldInfo field)) throw new InvalidDataException("CQF_AI_UnknownField: " + element.Name);
                field.SetValue(copy, ReadValue(element, field.FieldType, field.GetValue(copy), false, depth + 1));
            }
            if (copy is ThinkNode node)
            {
                if (node.subNodes == null || node.subNodes.Any(child => child == null)) throw new InvalidDataException("CQF_AI_InvalidValue: subNodes");
                node.parent = null!;
                foreach (ThinkNode child in node.subNodes) child.parent = node;
                float priority = (float)typeof(ThinkNode).GetField("priority", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(node);
                typeof(ThinkNode).GetField("hasPriority", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(node,
                    priority >= 0f || type.GetMethod(nameof(ThinkNode.GetPriority))!.DeclaringType != typeof(ThinkNode));
            }
            return copy;
        }
        private static bool IsDataType(Type type)
        {
            return !type.IsGenericTypeDefinition && !type.IsInterface && !typeof(Thing).IsAssignableFrom(type)
                && !typeof(ThingComp).IsAssignableFrom(type) && !typeof(GameComponent).IsAssignableFrom(type) && !typeof(MapComponent).IsAssignableFrom(type)
                && (type.Assembly == typeof(DialogTreeDef).Assembly || typeof(ICQFAIEditableData).IsAssignableFrom(type) || typeof(ThinkNode).IsAssignableFrom(type)
                    || typeof(QuestNode).IsAssignableFrom(type) || typeof(IExposable).IsAssignableFrom(type)
                    || type == typeof(DutyDef) || type == typeof(RimWorld.QuestScriptDef));
        }
        private static bool IsFieldType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (IsScalar(type) || typeof(Def).IsAssignableFrom(type)) return true;
            if (type.IsArray) return IsFieldType(type.GetElementType()!);
            if (type.IsGenericType && (typeof(IList).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type)))
                return type.GetGenericArguments().All(IsFieldType);
            return IsDataType(type);
        }
        private static bool IsScalar(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(Type) || typeof(ISlateRef).IsAssignableFrom(type)
                || type == typeof(IntVec3) || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Color)
                || type == typeof(Rot4) || type == typeof(CellRect) || type == typeof(IntRange) || type == typeof(FloatRange);
        }
        private static string ScalarText(object value)
        {
            float[]? floats = value switch { Vector2 v => new[] { v.x, v.y }, Vector3 v => new[] { v.x, v.y, v.z }, Color c => new[] { c.r, c.g, c.b, c.a }, FloatRange r => new[] { r.min, r.max }, _ => null };
            if (floats != null) return string.Join(",", floats.Select(number => number.ToString("R", CultureInfo.InvariantCulture)));
            int[]? integers = value switch { IntVec3 v => new[] { v.x, v.y, v.z }, CellRect r => new[] { r.minX, r.minZ, r.Width, r.Height }, IntRange r => new[] { r.min, r.max }, Rot4 r => new[] { r.AsInt }, _ => null };
            if (integers != null) return string.Join(",", integers);
            if (value is Type type) return type.FullName!;
            if (value is ISlateRef slate) return slate.SlateRef;
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
        private object ParseScalar(string text, Type type)
        {
            if (type == typeof(string)) return text;
            if (type == typeof(Type)) return Resolve(text);
            if (typeof(ISlateRef).IsAssignableFrom(type))
            {
                object value = Activator.CreateInstance(type)!;
                ((ISlateRef)value).SlateRef = text;
                Type item = Nullable.GetUnderlyingType(type.GetGenericArguments()[0]) ?? type.GetGenericArguments()[0];
                if (typeof(Def).IsAssignableFrom(item) && text.Length > 0 && !text.Contains("$")
                    && !GenDefDatabase.GetAllDefsInDatabaseForDef(item).Any(def => def.defName == text)) throw new InvalidDataException("CQF_AI_MissingResource: " + text);
                return value;
            }
            if (type.IsEnum)
            {
                object value = Enum.Parse(type, text, false);
                if (!Enum.IsDefined(type, value) && type.GetCustomAttribute<FlagsAttribute>() == null) throw new InvalidDataException("CQF_AI_InvalidValue: enum");
                return value;
            }
            string[] pieces = text.Split(',');
            int[]? integers = type == typeof(IntVec3) || type == typeof(CellRect) || type == typeof(IntRange) || type == typeof(Rot4) ? pieces.Select(piece => int.Parse(piece, CultureInfo.InvariantCulture)).ToArray() : null;
            float[]? floats = type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Color) || type == typeof(FloatRange) ? pieces.Select(piece => float.Parse(piece, CultureInfo.InvariantCulture)).ToArray() : null;
            if (floats?.Any(value => float.IsNaN(value) || float.IsInfinity(value)) == true) throw new InvalidDataException("CQF_AI_InvalidValue: finite");
            if (type == typeof(IntVec3) && integers!.Length == 3) return new IntVec3(integers[0], integers[1], integers[2]);
            if (type == typeof(CellRect) && integers!.Length == 4) return new CellRect(integers[0], integers[1], integers[2], integers[3]);
            if (type == typeof(IntRange) && integers!.Length == 2) return new IntRange(integers[0], integers[1]);
            if (type == typeof(Rot4) && integers!.Length == 1 && (integers[0] >= 0 && integers[0] <= 3 || integers[0] == Rot4.Invalid.AsInt)) return integers[0] == Rot4.Invalid.AsInt ? Rot4.Invalid : new Rot4(integers[0]);
            if (type == typeof(Vector2) && floats!.Length == 2) return new Vector2(floats[0], floats[1]);
            if (type == typeof(Vector3) && floats!.Length == 3) return new Vector3(floats[0], floats[1], floats[2]);
            if (type == typeof(Color) && floats!.Length == 4) return new Color(floats[0], floats[1], floats[2], floats[3]);
            if (type == typeof(FloatRange) && floats!.Length == 2) return new FloatRange(floats[0], floats[1]);
            if (integers != null || floats != null) throw new InvalidDataException("CQF_AI_InvalidValue: " + type.Name);
            object scalar = Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            if (scalar is float single && (float.IsNaN(single) || float.IsInfinity(single)) || scalar is double number && (double.IsNaN(number) || double.IsInfinity(number))) throw new InvalidDataException("CQF_AI_InvalidValue: finite");
            return scalar;
        }
        private readonly Dictionary<string, Type> types;
        private static readonly MethodInfo Memberwise = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly HashSet<string> ExcludedFields = new HashSet<string> { "origin", "extraDataByDirection", "extraDataByOrigin", "idleNodes", "parentIndex", "subNodeIndexs", "heights", "scrollPos", "buffer" };
    }
}
