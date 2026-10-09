using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFContentTextExport
    {
        public CQFContentTextExport(string defName, Func<string, string?>? translate = null,
            Func<string, Type?>? resolveType = null)
        {
            XmlConvert.VerifyNCName(defName);
            this.defName = defName;
            this.translate = translate ?? (text => text.CanTranslate() ? text.Translate().ToString() : null);
            this.resolveType = resolveType ?? (name => GenTypes.GetTypeInAnyAssembly(name));
        }

        public XElement Keyed { get; } = new XElement("LanguageData");

        public XElement DefInjected { get; } = new XElement("LanguageData");

        public XElement Compile(XElement source, Type rootType)
        {
            this.Keyed.RemoveNodes();
            this.DefInjected.RemoveNodes();
            this.prefix = "CQF_" + rootType.Name + "_" + XmlConvert.EncodeLocalName(this.defName) + "_";
            XElement document = new XElement(source);
            this.Visit(document, rootType, "", true);
            return document;
        }

        public void Save(XElement source, Type rootType, string path, bool? exportDefInjected = null,
            XElement? additionalTranslations = null)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
            string stem = Path.GetFileNameWithoutExtension(path);
            string languagePath = Path.Combine(directory, stem + "_Text.xml");
            this.existingTranslations.Clear();
            if (File.Exists(languagePath))
            {
                XElement previous = XElement.Load(languagePath);
                if (previous.Name != "LanguageData") throw new InvalidDataException(languagePath);
                foreach (XElement entry in previous.Elements())
                {
                    if (this.existingTranslations.ContainsKey(entry.Name.LocalName))
                        throw new InvalidDataException("Duplicate translation: " + entry.Name);
                    this.existingTranslations.Add(entry.Name.LocalName, entry.Value);
                }
            }
            XElement document = this.Compile(source, rootType);
            if (additionalTranslations != null)
            {
                if (additionalTranslations.Name != "LanguageData") throw new InvalidDataException(additionalTranslations.Name.ToString());
                foreach (XElement entry in additionalTranslations.Elements())
                    this.AddTranslation(entry.Name.LocalName, this.existingTranslations.TryGetValue(entry.Value, out string previous) ? previous : entry.Value);
            }
            foreach (XElement element in document.Descendants().Where(element => !element.HasElements))
                if (this.existingTranslations.TryGetValue(element.Value, out string previous) && this.Keyed.Element(element.Value) == null)
                    this.AddTranslation(element.Value, previous);
            Directory.CreateDirectory(directory);
            this.Keyed.Save(languagePath);
            if (exportDefInjected ?? !string.Equals(LanguageDatabase.activeLanguage?.folderName, "English", StringComparison.OrdinalIgnoreCase))
                this.DefInjected.Save(Path.Combine(directory, stem + "_DefInjected.xml"));
            new XElement("Defs", document).Save(path);
        }

        private void Visit(XElement node, Type declaredType, string path, bool root = false)
        {
            string? className = node.Attribute("Class")?.Value;
            Type type = className == null ? declaredType : this.resolveType(className)
                ?? throw new InvalidDataException("Unknown exported class: " + className);
            if (!declaredType.IsAssignableFrom(type)) throw new InvalidDataException("Invalid exported class: " + className);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type valueType = type.GetGenericArguments()[1];
                int index = 0;
                foreach (XElement entry in node.Elements("li"))
                {
                    XElement? value = entry.Element("value");
                    if (value != null) this.Visit(value, valueType, path + "_" + index + "_Value");
                    index++;
                }
                return;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                int index = 0;
                foreach (XElement item in node.Elements("li")) this.Visit(item, itemType, path + "_" + index++);
                return;
            }
            if (!node.HasElements || (!root && typeof(Def).IsAssignableFrom(type))) return;
            foreach (XElement child in node.Elements())
            {
                FieldInfo? field = type.GetField(child.Name.LocalName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) continue;
                if (root && field.DeclaringType == typeof(Def) && (field.Name == "label" || field.Name == "description"))
                {
                    if (!string.IsNullOrWhiteSpace(child.Value))
                    {
                        child.Value = this.translate(child.Value) ?? child.Value;
                        this.DefInjected.Add(new XElement(this.defName + "." + field.Name, child.Value));
                    }
                    continue;
                }
                string childPath = path.Length == 0 ? child.Name.LocalName : path + "_" + child.Name.LocalName;
                CQFLocalizableTextAttribute? textField = field.GetCustomAttribute<CQFLocalizableTextAttribute>();
                if (field.FieldType == typeof(string) && textField != null)
                {
                    if (textField.PreserveAs != null)
                    {
                        XElement? preserved = node.Element(textField.PreserveAs);
                        if (preserved == null) node.Add(new XElement(textField.PreserveAs, child.Value));
                        else if (string.IsNullOrEmpty(preserved.Value)) preserved.Value = child.Value;
                    }
                    this.CompileText(child, this.prefix + XmlConvert.EncodeLocalName(childPath));
                }
                else if (child.HasElements) this.Visit(child, field.FieldType, childPath);
            }
        }

        private void CompileText(XElement element, string key)
        {
            string text = element.Value;
            if (string.IsNullOrWhiteSpace(text)) return;
            if (this.existingTranslations.TryGetValue(text, out string previous))
            {
                this.AddTranslation(text, previous);
                return;
            }
            string? translated = this.translate(text);
            if (translated != null)
            {
                if (text.StartsWith(this.prefix, StringComparison.Ordinal)) this.AddTranslation(text, translated);
                return;
            }
            element.Value = key;
            this.AddTranslation(key, text);
        }

        private void AddTranslation(string key, string text)
        {
            XElement? entry = this.Keyed.Element(key);
            if (entry != null && entry.Value != text) throw new InvalidDataException("Conflicting translation: " + key);
            if (entry == null) this.Keyed.Add(new XElement(key, text));
        }

        private readonly string defName;
        private readonly Func<string, string?> translate;
        private readonly Func<string, Type?> resolveType;
        private readonly Dictionary<string, string> existingTranslations = new Dictionary<string, string>(StringComparer.Ordinal);
        private string prefix = "";
    }
}
