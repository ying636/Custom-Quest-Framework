using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public static class CQFAIDefDocument
    {
        public static string Save(Def definition)
        {
            CQFAIChanges.Validate(definition);
            System.Xml.XmlConvert.VerifyNCName(definition.defName);
            if (!definition.defName.StartsWith("CQF_", StringComparison.Ordinal) && GenDefDatabase.GetAllDefsInDatabaseForDef(definition.GetType()).All(def => def.defName != definition.defName))
                throw new InvalidDataException("CQF_AI_InvalidName");
            XElement xml = Export(definition);
            string category = definition switch { CustomMapDataDef or MainMapDef => "Map", DialogTreeDef or DialogManagerDef => "DialogTree", ComplexPawnDef => "Pawn", DutyMapDef or DutyDef => "Duty", QuestBookDef => "QuestBook", QuestScriptDef => string.Empty, _ => "AI" };
            string directory = Path.Combine(CQFContentPaths.Quests, category);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, definition.defName + ".xml");
            new XDocument(new XElement("Defs", xml)).Save(path);
            CQFQuestDefBootstrap.HotLoadDefinition(definition);
            return path;
        }

        public static XElement Export(Def definition)
        {
            CQFAIChanges.Validate(definition);
            Type type = definition.GetType();
            if (type.Assembly != typeof(DialogTreeDef).Assembly && type != typeof(DutyDef) && type != typeof(QuestScriptDef))
                throw new InvalidDataException("CQF_AI_UnknownType: " + type.FullName);
            string name = definition.GetType().Assembly == typeof(DialogTreeDef).Assembly ? definition.GetType().FullName! : definition.GetType().Name;
            if (definition is CustomMapDataDef map)
            {
                CustomMapDataDef copy = (CustomMapDataDef)new CQFAIModel().Copy(map);
                foreach (var pair in copy.terrains)
                {
                    CellRect[] occupied = copy.terrainsRect.Values.SelectMany(rects => rects).ToArray();
                    if (!copy.terrainsRect.TryGetValue(pair.Key, out List<CellRect> rects)) copy.terrainsRect.Add(pair.Key, rects = new List<CellRect>());
                    rects.AddRange(pair.Value.Where(cell => !occupied.Any(rect => rect.Contains(cell))).Select(cell => new CellRect(cell.x, cell.z, 1, 1)));
                }
                foreach (var pair in copy.roofs)
                {
                    CellRect[] occupied = copy.roofRects.Values.SelectMany(rects => rects).ToArray();
                    if (!copy.roofRects.TryGetValue(pair.Key, out List<CellRect> rects)) copy.roofRects.Add(pair.Key, rects = new List<CellRect>());
                    rects.AddRange(pair.Value.Where(cell => !occupied.Any(rect => rect.Contains(cell))).Select(cell => new CellRect(cell.x, cell.z, 1, 1)));
                }
                definition = copy;
            }
            XElement xml = Write(definition, definition.GetType(), name, true, new HashSet<object>(), 0)!;
            foreach (string field in new[] { "defName", "label", "description" })
            {
                object? value = typeof(Def).GetField(field)!.GetValue(definition);
                xml.Elements(field).Remove();
                if (value != null) xml.Add(new XElement(field, value));
            }
            return xml;
        }

        private static XElement? Write(object? value, Type declared, string name, bool root, HashSet<object> ancestors, int depth)
        {
            if (depth > 64) throw new InvalidDataException("CQF_AI_Depth");
            if (value == null) return new XElement(name, new XAttribute("IsNull", "True"));
            if (value is Def reference && !root) return new XElement(name, reference.defName);
            if (value is ISlateRef slate) return slate.SlateRef == null ? null : new XElement(name, slate.SlateRef);
            if (value is Type runtimeType) return new XElement(name, runtimeType.FullName);
            if (value is IntVec3 cell) return new XElement(name, $"({cell.x},{cell.y},{cell.z})");
            Type type = value.GetType();
            if (type.IsEnum || type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return new XElement(name, Convert.ToString(value, CultureInfo.InvariantCulture));
            if (DirectXmlSaver.IsSimpleTextType(type)) return DirectXmlSaver.XElementFromObject(value, declared, name);
            if (!type.IsValueType && !ancestors.Add(value)) throw new InvalidDataException("CQF_AI_Cycle: " + type.FullName);
            try
            {
                MethodInfo? save = type.GetMethod("SaveToXElement", new[] { typeof(string) });
                if (save != null)
                {
                    try { return (XElement)save.Invoke(value, new object[] { name })!; }
                    catch (TargetInvocationException error) when (error.InnerException != null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
                }
                XElement xml = new XElement(name);
                if (value is IDictionary dictionary)
                {
                    Type[] arguments = type.GetGenericArguments();
                    foreach (DictionaryEntry entry in dictionary)
                        xml.Add(new XElement("li", Write(entry.Key, arguments[0], "key", false, ancestors, depth + 1), Write(entry.Value, arguments[1], "value", false, ancestors, depth + 1)));
                }
                else if (value is IList list)
                {
                    Type itemType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
                    foreach (object? item in list) xml.Add(Write(item, itemType, "li", false, ancestors, depth + 1));
                }
                else
                {
                    if (!root && type != (Nullable.GetUnderlyingType(declared) ?? declared)) xml.SetAttributeValue("Class", type.FullName);
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(field => field.MetadataToken))
                    {
                        if (field.IsDefined(typeof(UnsavedAttribute), true) || field.DeclaringType == typeof(Def) && field.Name is not ("defName" or "label" or "description" or "modExtensions")) continue;
                        object? fieldValue = field.GetValue(value);
                        if (fieldValue != null) xml.Add(Write(fieldValue, field.FieldType, field.Name, false, ancestors, depth + 1));
                    }
                }
                return xml;
            }
            finally { if (!type.IsValueType) ancestors.Remove(value); }
        }
    }
}
