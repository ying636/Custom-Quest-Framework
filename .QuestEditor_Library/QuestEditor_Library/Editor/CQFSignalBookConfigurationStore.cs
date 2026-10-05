using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFSignalBookConfigurationStore
    {
        public CQFSignalBookConfigurationStore(string directory)
        {
            this.directory = Path.GetFullPath(directory);
        }

        public string[] ConfigurationNames => Directory.Exists(this.directory)
            ? Directory.GetFiles(this.directory, "CQF_*.xml").Select(path => Path.GetFileNameWithoutExtension(path)!.Substring(4))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()
            : Array.Empty<string>();

        public string DefaultConfigurationName
        {
            get
            {
                string path = Path.Combine(this.directory, "default.xml");
                if (!File.Exists(path))
                {
                    return string.Empty;
                }
                XElement root = Read(path);
                XElement? nameElement = root.Element("name");
                if (root.Name != "CQFSignalBookDefault" || nameElement == null || root.Elements("name").Count() != 1)
                {
                    throw new InvalidDataException("CQF_SignalBook_InvalidConfiguration");
                }
                string name = nameElement.Value;
                return name.Length == 0 ? name : ValidateName(name);
            }
        }

        public void Save(string name, IEnumerable<string> signals)
        {
            name = ValidateName(name);
            string[] values = ValidateSignals(signals);
            Write(this.ConfigurationPath(name), new XElement("CQFSignalBookConfiguration",
                new XElement("name", name), new XElement("signals", values.Select(value => new XElement("li", value)))));
        }

        public string[] Load(string name)
        {
            name = ValidateName(name);
            XElement root = Read(this.ConfigurationPath(name));
            XElement? nameElement = root.Element("name");
            XElement? signalElements = root.Element("signals");
            if (root.Name != "CQFSignalBookConfiguration" || nameElement == null || signalElements == null || root.Elements("name").Count() != 1
                || root.Elements("signals").Count() != 1
                || !string.Equals(nameElement.Value, name, StringComparison.OrdinalIgnoreCase)
                || signalElements.Elements().Any(element => element.Name != "li" || element.HasElements))
            {
                throw new InvalidDataException("CQF_SignalBook_InvalidConfiguration");
            }
            return ValidateSignals(signalElements.Elements("li").Select(element => element.Value));
        }

        public void SetDefault(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = ValidateName(name);
                this.Load(name);
            }
            Write(Path.Combine(this.directory, "default.xml"),
                new XElement("CQFSignalBookDefault", new XElement("name", name ?? string.Empty)));
        }

        public static string[] ValidateSignals(IEnumerable<string> signals)
        {
            string[] values = signals.ToArray();
            if (values.Any(string.IsNullOrWhiteSpace) || values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            {
                throw new ArgumentException("CQF_SignalBook_InvalidSignal");
            }
            foreach (string value in values)
            {
                XmlConvert.VerifyXmlChars(value);
            }
            return values;
        }

        private string ConfigurationPath(string name)
        {
            return Path.Combine(this.directory, "CQF_" + ValidateName(name) + ".xml");
        }

        private static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || name == "." || name == ".."
                || name.EndsWith(".", StringComparison.Ordinal) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("CQF_SignalBook_InvalidConfigurationName");
            }
            XmlConvert.VerifyXmlChars(name);
            return name;
        }

        private static XElement Read(string path)
        {
            using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                return XElement.Load(reader);
            }
        }

        private void Write(string path, XElement root)
        {
            Directory.CreateDirectory(this.directory);
            string temporaryPath = Path.Combine(this.directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                new XDocument(root).Save(temporaryPath);
                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private readonly string directory;
    }
}
