using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIChanges
    {
        public CQFAIChanges(CQFAIModel model) { this.model = model; }
        public object Build(object source, XElement changes, string command, bool generateText)
        {
            if (changes.Name != "changes" || changes.HasAttributes || changes.Elements().Count() > 500) throw new InvalidDataException("CQF_AI_InvalidChanges");
            XElement snapshot = model.Write(source, root: true);
            HashSet<string> existing = new HashSet<string>(snapshot.Descendants().Where(element => !element.HasElements).Select(element => element.Value));
            foreach (XElement operation in changes.Elements())
            {
                if (operation.Name.LocalName is "mapResize" or "terrain" or "place" or "erase" or "roof")
                {
                    object current = model.Read(snapshot, source.GetType(), source, true)!;
                    if (current is not CustomMapDataDef map) throw new InvalidDataException("CQF_AI_MapRequired");
                    CQFAIMapPlan.Apply(map, operation);
                    snapshot = model.Write(map, root: true);
                    continue;
                }
                string path = operation.Attribute("path")?.Value ?? throw new InvalidDataException("CQF_AI_InvalidPath");
                if (operation.Attributes().Any(attribute => attribute.Name != "path")) throw new InvalidDataException("CQF_AI_InvalidChanges");
                XElement target = Find(snapshot, path);
                if (operation.Name == "set")
                {
                    XElement value = SingleValue(operation);
                    Merge(target, value);
                }
                else if (operation.Name == "append")
                {
                    RequireCollection(source, snapshot, path, false);
                    XElement value = SingleValue(operation);
                    target.Add(new XElement("li", value.Attributes(), value.Nodes()));
                }
                else if (operation.Name == "put")
                {
                    RequireCollection(source, snapshot, path, true);
                    if (operation.Elements().Count() != 2 || operation.Element("key") == null || operation.Element("value") == null) throw new InvalidDataException("CQF_AI_InvalidChanges");
                    XElement key = operation.Element("key")!;
                    XElement? old = target.Elements("entry").FirstOrDefault(entry => XNode.DeepEquals(entry.Element("key"), key));
                    if (old != null) throw new InvalidDataException("CQF_AI_DuplicateKey");
                    target.Add(new XElement("entry", new XElement(key), new XElement(operation.Element("value")!)));
                }
                else if (operation.Name == "remove")
                {
                    if (operation.HasElements || target.Name != "li" && target.Name != "value") throw new InvalidDataException("CQF_AI_InvalidChanges");
                    if (target.Name == "value" && target.Parent?.Name == "entry") target.Parent.Remove();
                    else if (target.Name == "li") target.Remove();
                    else throw new InvalidDataException("CQF_AI_InvalidChanges");
                }
                else throw new InvalidDataException("CQF_AI_InvalidChanges: " + operation.Name);
            }
            if (!generateText)
            {
                foreach (XElement element in snapshot.Descendants().Where(element => TextFields.Contains(element.Name.LocalName) || element.Name == "li" && element.Parent != null && TextFields.Contains(element.Parent.Name.LocalName)))
                {
                    if (element.HasElements || element.Attribute("null") != null || string.IsNullOrEmpty(element.Value) || existing.Contains(element.Value)
                        || element.Value.CanTranslate() || command.Contains(element.Value)
                        || System.Text.RegularExpressions.Regex.IsMatch(element.Value, "^CQF_[A-Za-z0-9_]+$")) continue;
                    throw new InvalidDataException("CQF_AI_TextNotAllowed: " + element.Name);
                }
            }
            object draft = model.Read(snapshot, source.GetType(), source, true)!;
            Validate(draft);
            return draft;
        }
        public static XElement Parse(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                int first = text.IndexOf('\n'), last = text.LastIndexOf("```", StringComparison.Ordinal);
                if (first < 0 || last <= first) throw new InvalidDataException("CQF_AI_InvalidResponse");
                text = text.Substring(first + 1, last - first - 1);
            }
            using StringReader source = new StringReader(text);
            using XmlReader reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2097152 });
            return XElement.Load(reader);
        }
        public static void Validate(object value)
        {
            CQFAIContentValidation.Validate(value);
            if (value is Def def && (string.IsNullOrWhiteSpace(def.defName) || def.defName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)) throw new InvalidDataException("CQF_AI_InvalidName");
            if (value is DialogTreeDef tree)
            {
                tree.ResolveOptions();
                if (!tree.nodeMoulds.ContainsKey(0)) throw new InvalidDataException("CQF_AI_InvalidLinks: entry");
                foreach (var pair in tree.nodeMoulds)
                {
                    pair.Value.index = pair.Key;
                    foreach (DialogResult result in pair.Value.options.SelectMany(option => option.results))
                        if (result.nextIndex.HasValue && !tree.nodeMoulds.ContainsKey(result.nextIndex.Value)) throw new InvalidDataException("CQF_AI_InvalidLinks: dialogue");
                }
                tree.Update();
                foreach (DialogOption option in tree.optionMoulds.Values)
                    if (option.results == null || option.conditions == null || option.requiredThings == null
                        || float.IsNaN(option.editorX) || float.IsInfinity(option.editorX) || float.IsNaN(option.editorY) || float.IsInfinity(option.editorY)
                        || option.results.Any(result => result == null || result.conditions == null || result.actions == null
                            || result.nextIndex.HasValue && !tree.nodeMoulds.ContainsKey(result.nextIndex.Value)))
                        throw new InvalidDataException("CQF_DialogGraph_InvalidOptions");
            }
            if (value is DutyMapDef duty)
            {
                if (duty.nodes.Count == 0 || duty.nodes.Select(node => node.nodeId).Distinct().Count() != duty.nodes.Count || duty.nodes.Any(node => string.IsNullOrWhiteSpace(node.nodeId))
                    || !duty.nodes.Any(node => node.nodeId == duty.startNodeId)
                    || duty.transitions.Any(link => !duty.nodes.Any(node => node.nodeId == link.fromNodeId) || !duty.nodes.Any(node => node.nodeId == link.toNodeId)))
                    throw new InvalidDataException("CQF_AI_InvalidLinks: duty");
            }
            if (value is QuestBookDef book)
            {
                var steps = book.chapters.SelectMany(chapter => chapter.steps).ToList();
                if (steps.Select(step => step.id).Distinct().Count() != steps.Count || steps.Any(step => string.IsNullOrWhiteSpace(step.id))
                    || steps.Any(step => step.nextStepIds.Any(id => !steps.Any(other => other.id == id)))) throw new InvalidDataException("CQF_AI_InvalidLinks: quest book");
            }
            if (value is CustomMapDataDef map) CQFAIMapPlan.Validate(map);
        }
        private void RequireCollection(object source, XElement snapshot, string path, bool dictionary)
        {
            object copy = model.Read(snapshot, source.GetType(), source, true, resolveDialogue: false)!;
            foreach (string segment in Segments(path))
            {
                if (copy is System.Collections.IDictionary values)
                {
                    object key = values.Keys.Cast<object>().FirstOrDefault(key => model.Write(key).Value == Uri.UnescapeDataString(segment.TrimStart('@'))) ?? throw new InvalidDataException("CQF_AI_InvalidPath");
                    copy = values[key]!;
                }
                else if (copy is System.Collections.IList list) copy = list[int.Parse(segment)]!;
                else copy = model.Fields(copy.GetType()).First(field => field.Name == segment).GetValue(copy)!;
            }
            if (dictionary ? copy is not System.Collections.IDictionary : copy is not System.Collections.IList) throw new InvalidDataException("CQF_AI_InvalidPath: collection");
        }
        private static void Merge(XElement target, XElement value)
        {
            if (target.Attribute("Class") != null && value.HasElements && value.Attributes().All(attribute => attribute.Name == "Class")
                && (value.Attribute("Class") == null || value.Attribute("Class")!.Value == target.Attribute("Class")!.Value))
            {
                if (value.Elements().GroupBy(element => element.Name).Any(group => group.Count() > 1)) throw new InvalidDataException("CQF_AI_InvalidChanges");
                foreach (XElement field in value.Elements())
                {
                    XElement? previous = target.Element(field.Name);
                    if (previous == null) target.Add(new XElement(field));
                    else Merge(previous, field);
                }
            }
            else target.ReplaceWith(new XElement(target.Name, value.Attributes(), value.Nodes()));
        }
        private static XElement SingleValue(XElement operation)
        {
            return operation.Elements().Count() == 1 && operation.Element("value") != null ? operation.Element("value")! : throw new InvalidDataException("CQF_AI_InvalidChanges");
        }
        private static XElement Find(XElement root, string path)
        {
            XElement current = root;
            foreach (string segment in Segments(path))
            {
                if (segment.StartsWith("@", StringComparison.Ordinal))
                    current = current.Elements("entry").FirstOrDefault(entry => entry.Element("key")?.Value == Uri.UnescapeDataString(segment.Substring(1)))?.Element("value") ?? throw new InvalidDataException("CQF_AI_InvalidPath: " + path);
                else if (int.TryParse(segment, out int index))
                    current = current.Elements("li").ElementAtOrDefault(index) ?? throw new InvalidDataException("CQF_AI_InvalidPath: " + path);
                else current = current.Element(segment) ?? throw new InvalidDataException("CQF_AI_InvalidPath: " + path);
            }
            return current;
        }
        private static string[] Segments(string path)
        {
            if (!path.StartsWith("/", StringComparison.Ordinal) || path.Length < 2 || path.Length > 500 || path.Contains("//")) throw new InvalidDataException("CQF_AI_InvalidPath");
            return path.Substring(1).Split('/');
        }
        private readonly CQFAIModel model;
        private static readonly HashSet<string> TextFields = new HashSet<string> { "text", "extraText", "title", "label", "description", "labelKey", "descriptionKey", "customName", "customDescription", "customInspectText", "dialogReportKey", "openReport", "trapName", "lootBoxName", "failReason", "reason", "report", "message", "interactionText", "disableReason", "disarmReport", "spawnMessage", "landfillText", "firstName", "nickName", "lastName" };
    }
}
