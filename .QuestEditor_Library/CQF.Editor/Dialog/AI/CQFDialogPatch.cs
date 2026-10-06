using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFDialogPatch
    {
        public CQFDialogPatch(CQFDialogEditSession session)
        {
            this.session = session;
            this.types = new Dictionary<string, Type>(StringComparer.Ordinal);
            Queue<Type> pending = new Queue<Type>(new[] { typeof(DialogOption), typeof(DialogNode), typeof(DialogResult), typeof(DialogImage),
                typeof(CQFAction), typeof(DialogCondition), typeof(CQFThingData) });
            HashSet<Type> visited = new HashSet<Type>();
            while (pending.Count > 0)
            {
                Type type = pending.Dequeue();
                if (!visited.Add(type) || typeof(Def).IsAssignableFrom(type)) continue;
                if (!type.IsAbstract) this.types.Add(type.FullName, type);
                foreach (Type child in type.AllSubclassesNonAbstract()) pending.Enqueue(child);
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    Type item = field.FieldType.IsGenericType && typeof(IList).IsAssignableFrom(field.FieldType)
                        ? field.FieldType.GetGenericArguments()[0] : field.FieldType;
                    if (typeof(ISaveable).IsAssignableFrom(item)) pending.Enqueue(item);
                }
            }
        }

        public IEnumerable<Type> SupportedTypes => this.types.Values;

        public DialogTreeDef Build(DialogTreeDef current, string response)
        {
            string xml = response.Trim();
            if (xml.StartsWith("```", StringComparison.Ordinal))
            {
                int line = xml.IndexOf('\n');
                int end = xml.LastIndexOf("```", StringComparison.Ordinal);
                if (line < 0 || end <= line) throw new InvalidDataException("CQF_DialogAI_InvalidPatch");
                xml = xml.Substring(line + 1, end - line - 1);
            }
            XElement patch;
            using (StringReader source = new StringReader(xml))
            using (XmlReader reader = XmlReader.Create(source, new XmlReaderSettings
                { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1048576 }))
            {
                patch = XElement.Load(reader);
            }
            if (patch.Name != "patch" || patch.HasAttributes || !patch.Elements().Any() || patch.Elements().Count() > 500)
            {
                throw new InvalidDataException("CQF_DialogAI_InvalidPatch");
            }
            DialogTreeDef draft = this.session.Copy(current);
            this.NoRawText(patch);
            foreach (XElement operation in patch.Elements())
            {
                string name = operation.Name.LocalName;
                if (operation.Name.Namespace != XNamespace.None) throw new InvalidDataException("CQF_DialogAI_InvalidOperation: " + name);
                string[] allowed = name == "addNode" || name == "setNode" || name == "removeNode" ? new[] { "node" }
                    : name == "addOption" ? new[] { "node" }
                    : name == "setOption" || name == "removeOption" || name == "addResult" ? new[] { "node", "option" }
                    : name == "setResult" || name == "removeResult" ? new[] { "node", "option", "result" }
                    : name == "connect" ? new[] { "node", "option", "result", "next" } : Array.Empty<string>();
                if (allowed.Length == 0 || operation.Attributes().Any(attribute => attribute.Name.Namespace != XNamespace.None || !allowed.Contains(attribute.Name.LocalName)))
                {
                    throw new InvalidDataException("CQF_DialogAI_InvalidOperation: " + name);
                }
                int nodeId = this.Index(operation, "node");
                if (name == "addNode")
                {
                    if (nodeId < 0 || draft.nodeMoulds.ContainsKey(nodeId)) throw new InvalidDataException("CQF_DialogAI_DuplicateNode: " + nodeId);
                    DialogNode added = new DialogNode(nodeId) { text = "CQF_Dialog_Node_" + nodeId };
                    this.SetFields(added, operation, new[] { "text", "extraText", "images", "options", "editorX", "editorY" });
                    added.editorPositionSet = operation.Element("editorX") != null && operation.Element("editorY") != null;
                    draft.nodeMoulds.Add(nodeId, added);
                    continue;
                }
                if (!draft.nodeMoulds.TryGetValue(nodeId, out DialogNode node)) throw new InvalidDataException("CQF_DialogAI_MissingNode: " + nodeId);
                if (name == "setNode")
                {
                    this.SetFields(node, operation, new[] { "text", "extraText", "images", "editorX", "editorY" });
                    if (operation.Element("editorX") != null && operation.Element("editorY") != null) node.editorPositionSet = true;
                    continue;
                }
                if (name == "removeNode")
                {
                    this.NoChildren(operation);
                    if (nodeId == 0) throw new InvalidDataException("CQF_DialogGraph_KeepEntry");
                    draft.nodeMoulds.Remove(nodeId);
                    foreach (DialogResult result in draft.nodeMoulds.Values.SelectMany(value => value.options).SelectMany(option => option.results))
                    {
                        if (result.nextIndex == nodeId) result.nextIndex = null;
                    }
                    continue;
                }
                if (name == "addOption")
                {
                    DialogOption added = new DialogOption { text = "CQF_Dialog_Node_" + nodeId + "_Option_" + node.options.Count };
                    this.SetFields(added, operation);
                    node.options.Add(added);
                    continue;
                }
                int optionId = this.Index(operation, "option");
                if (optionId < 0 || optionId >= node.options.Count) throw new InvalidDataException("CQF_DialogAI_MissingOption: " + optionId);
                DialogOption selectedOption = node.options[optionId];
                if (name == "setOption")
                {
                    this.SetFields(selectedOption, operation);
                    continue;
                }
                if (name == "removeOption")
                {
                    this.NoChildren(operation);
                    node.options.RemoveAt(optionId);
                    continue;
                }
                if (name == "addResult")
                {
                    DialogResult added = new DialogResult { resultName = "CQF_Dialog_Result_" + selectedOption.results.Count };
                    this.SetFields(added, operation);
                    selectedOption.results.Add(added);
                    continue;
                }
                int resultId = this.Index(operation, "result");
                if (resultId < 0 || resultId >= selectedOption.results.Count) throw new InvalidDataException("CQF_DialogAI_MissingResult: " + resultId);
                if (name == "setResult") this.SetFields(selectedOption.results[resultId], operation);
                else if (name == "removeResult")
                {
                    this.NoChildren(operation);
                    selectedOption.results.RemoveAt(resultId);
                }
                else if (name == "connect")
                {
                    this.NoChildren(operation);
                    string target = operation.Attribute("next")?.Value ?? throw new InvalidDataException("CQF_DialogAI_InvalidPatch");
                    selectedOption.results[resultId].nextIndex = target == "end" ? (int?)null : int.Parse(target, CultureInfo.InvariantCulture);
                }
            }
            draft.curIndex = draft.nodeMoulds.Keys.Max() + 1;
            draft.Update();
            this.Validate(draft);
            draft.SaveToXElement("tree");
            return draft;
        }

        public void Validate(DialogTreeDef tree)
        {
            if (!tree.nodeMoulds.ContainsKey(0)) throw new InvalidDataException("CQF_DialogGraph_KeepEntry");
            foreach (KeyValuePair<int, DialogNode> pair in tree.nodeMoulds)
            {
                DialogNode node = pair.Value;
                if (pair.Key < 0 || node == null || node.index != pair.Key || node.options == null || node.images == null || node.extraText == null
                    || float.IsNaN(node.editorX) || float.IsNaN(node.editorY) || float.IsInfinity(node.editorX) || float.IsInfinity(node.editorY))
                {
                    throw new InvalidDataException("CQF_DialogAI_InvalidNode: " + pair.Key);
                }
                foreach (DialogImage image in node.images)
                {
                    if (image == null || float.IsNaN(image.scale) || float.IsInfinity(image.scale) || image.scale < 0f)
                        throw new InvalidDataException("CQF_DialogAI_InvalidField: images");
                    if (!string.IsNullOrEmpty(image.imagePath) && ContentFinder<UnityEngine.Texture2D>.Get(image.imagePath, false) == null)
                        throw new InvalidDataException("CQF_DialogAI_MissingImage: " + image.imagePath);
                }
                foreach (DialogOption option in node.options)
                {
                    if (option == null || option.results == null || option.conditions == null || option.requiredThings == null)
                        throw new InvalidDataException("CQF_DialogAI_InvalidNode: " + pair.Key);
                    foreach (DialogResult result in option.results)
                    {
                        if (result == null || result.conditions == null || result.actions == null)
                            throw new InvalidDataException("CQF_DialogAI_InvalidNode: " + pair.Key);
                        if (result.nextIndex.HasValue && !tree.nodeMoulds.ContainsKey(result.nextIndex.Value))
                            throw new InvalidDataException("CQF_DialogAI_MissingNode: " + result.nextIndex.Value);
                    }
                }
            }
        }

        public DialogTreeDef SelectChanges(DialogTreeDef current, DialogTreeDef draft, IEnumerable<int> nodeIds)
        {
            DialogTreeDef accepted = this.session.Copy(current);
            DialogTreeDef draftCopy = this.session.Copy(draft);
            foreach (int id in nodeIds.Distinct())
            {
                if (draftCopy.nodeMoulds.TryGetValue(id, out DialogNode node)) accepted.nodeMoulds[id] = node;
                else
                {
                    if (id == 0) throw new InvalidDataException("CQF_DialogGraph_KeepEntry");
                    accepted.nodeMoulds.Remove(id);
                    foreach (DialogResult result in accepted.nodeMoulds.Values.SelectMany(value => value.options).SelectMany(option => option.results))
                    {
                        if (result.nextIndex == id) result.nextIndex = null;
                    }
                }
            }
            accepted.curIndex = accepted.nodeMoulds.Keys.Max() + 1;
            accepted.Update();
            this.Validate(accepted);
            return accepted;
        }

        private void SetFields(object target, XElement element, string[]? allowed = null)
        {
            this.NoRawText(element);
            HashSet<string> used = new HashSet<string>();
            foreach (XElement child in element.Elements())
            {
                string name = child.Name.LocalName;
                FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
                if (child.Name.Namespace != XNamespace.None || field == null || field.IsInitOnly || !used.Add(name)
                    || (allowed != null && !allowed.Contains(name)) || typeof(Delegate).IsAssignableFrom(field.FieldType)
                    || typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    throw new InvalidDataException("CQF_DialogAI_InvalidField: " + target.GetType().Name + "." + name);
                }
                field.SetValue(target, this.ReadValue(field.FieldType, child, 0));
            }
        }

        private object? ReadValue(Type expected, XElement element, int depth)
        {
            if (depth > 24) throw new InvalidDataException("CQF_DialogAI_InvalidPatch");
            if (element.Attributes().Any(attribute => attribute.Name != "Class" && attribute.Name != "IsNull"))
                throw new InvalidDataException("CQF_DialogAI_InvalidField: " + element.Name);
            if (element.Attribute("IsNull")?.Value == "true")
            {
                this.NoChildren(element);
                if (!string.IsNullOrWhiteSpace(element.Value)) throw new InvalidDataException("CQF_DialogAI_InvalidField: " + element.Name);
                if (expected.IsValueType && Nullable.GetUnderlyingType(expected) == null) throw new InvalidDataException("CQF_DialogAI_InvalidField: " + element.Name);
                return null;
            }
            Type type = Nullable.GetUnderlyingType(expected) ?? expected;
            XAttribute className = element.Attribute("Class");
            if (className != null)
            {
                if (!this.types.TryGetValue(className.Value, out Type actual) || !type.IsAssignableFrom(actual))
                    throw new InvalidDataException("CQF_DialogAI_InvalidType: " + className.Value);
                type = actual;
            }
            if (typeof(Def).IsAssignableFrom(type))
            {
                this.NoChildren(element);
                Type databaseType = GenDefDatabase.AllDefTypesWithDatabases().FirstOrDefault(database => database.IsAssignableFrom(type)) ?? type;
                Def definition = GenDefDatabase.GetDef(databaseType, element.Value, false);
                if (definition == null || !type.IsInstanceOfType(definition)) throw new InvalidDataException("CQF_DialogAI_MissingDef: " + type.Name + "/" + element.Value);
                return definition;
            }
            if (type == typeof(string))
            {
                this.NoChildren(element);
                return element.Value;
            }
            if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
            {
                this.NoRawText(element);
                IList list = (IList)Activator.CreateInstance(type);
                foreach (XElement item in element.Elements())
                {
                    if (item.Name != "li") throw new InvalidDataException("CQF_DialogAI_InvalidField: " + item.Name);
                    list.Add(this.ReadValue(type.GetGenericArguments()[0], item, depth + 1));
                }
                return list;
            }
            if (type.IsValueType || (type.IsGenericType && type.Name.StartsWith("SlateRef", StringComparison.Ordinal)))
            {
                this.NoChildren(element);
                object parsed = ParseHelper.FromString(element.Value, type);
                if (parsed is float number && (float.IsNaN(number) || float.IsInfinity(number)))
                    throw new InvalidDataException("CQF_DialogAI_InvalidField: " + element.Name);
                return parsed;
            }
            if (type.IsAbstract || !typeof(ISaveable).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidDataException("CQF_DialogAI_InvalidType: " + type.FullName);
            object value = Activator.CreateInstance(type);
            this.NoRawText(element);
            HashSet<string> used = new HashSet<string>();
            foreach (XElement child in element.Elements())
            {
                FieldInfo field = type.GetField(child.Name.LocalName, BindingFlags.Instance | BindingFlags.Public);
                if (child.Name.Namespace != XNamespace.None || field == null || field.IsInitOnly || !used.Add(child.Name.LocalName) || typeof(Delegate).IsAssignableFrom(field.FieldType))
                    throw new InvalidDataException("CQF_DialogAI_InvalidField: " + type.Name + "." + child.Name);
                field.SetValue(value, this.ReadValue(field.FieldType, child, depth + 1));
            }
            return value;
        }

        private int Index(XElement operation, string name)
        {
            if (!int.TryParse(operation.Attribute(name)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
                throw new InvalidDataException("CQF_DialogAI_InvalidIndex: " + name);
            return value;
        }

        private void NoChildren(XElement element)
        {
            if (element.HasElements) throw new InvalidDataException("CQF_DialogAI_InvalidField: " + element.Name);
        }

        private void NoRawText(XElement element)
        {
            if (element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                throw new InvalidDataException("CQF_DialogAI_InvalidField: " + element.Name);
        }

        private readonly CQFDialogEditSession session;
        private readonly Dictionary<string, Type> types;
    }
}
