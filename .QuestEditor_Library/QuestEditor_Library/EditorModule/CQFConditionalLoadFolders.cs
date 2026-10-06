using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFConditionalLoadFolders
    {
        public CQFConditionalLoadFolders(ModContentPack content, CustomQuestFramework_ModSetting settings)
        {
            this.content = content;
            string file = Path.Combine(content.RootDir, "LoadFolders.xml");
            if (!File.Exists(file)) return;
            XmlDocument xml = new XmlDocument { XmlResolver = null };
            using (XmlReader reader = XmlReader.Create(file, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                xml.Load(reader);
            string root = Path.GetFullPath(content.RootDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Dictionary<string, string> conditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlNode entry in xml.SelectNodes("/loadFolders/*/li[@IfCQFSetting]"))
            {
                string relative = entry.InnerText.Trim();
                string folder = Path.GetFullPath(Path.Combine(root, relative));
                if (Path.IsPathRooted(relative) || !folder.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("CQF conditional folder must be inside the Mod directory: " + relative);
                string condition = entry.Attributes["IfCQFSetting"].Value.Trim();
                if (conditions.TryGetValue(folder, out string previous) && previous != condition)
                    throw new InvalidDataException("Conflicting CQF folder conditions: " + relative);
                conditions[folder] = condition;
            }
            List<string> original = content.foldersToLoadDescendingOrder.ToList();
            content.foldersToLoadDescendingOrder.RemoveAll(folder => conditions.ContainsKey(Path.GetFullPath(folder)));
            List<string> accepted = new List<string>();
            foreach (string folder in original)
            {
                string fullPath = Path.GetFullPath(folder);
                if (!conditions.TryGetValue(fullPath, out string condition))
                {
                    accepted.Add(folder);
                    continue;
                }
                FieldInfo field = typeof(CustomQuestFramework_ModSetting).GetField(condition, BindingFlags.Public | BindingFlags.Instance);
                if (field == null || field.FieldType != typeof(bool))
                    throw new InvalidDataException("Unknown CQF boolean setting: " + condition);
                if (!(bool)field.GetValue(settings)) continue;
                if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException(fullPath);
                string assemblies = Path.Combine(fullPath, "Assemblies");
                if (Directory.Exists(assemblies) && Directory.GetFiles(assemblies, "*.dll", SearchOption.AllDirectories).Length != 0)
                    throw new InvalidDataException("CQF conditional DLLs must be placed at the module folder root: " + fullPath);
                accepted.Add(folder);
                if (!folders.TryGetValue(condition, out List<string> selected))
                    folders.Add(condition, selected = new List<string>());
                selected.Add(folder);
            }
            content.foldersToLoadDescendingOrder.Clear();
            content.foldersToLoadDescendingOrder.AddRange(accepted);
        }

        public IReadOnlyList<string> GetFolders(string setting)
        {
            return folders.TryGetValue(setting, out List<string> selected) ? selected.ToArray() : Array.Empty<string>();
        }

        public void Exclude(string setting)
        {
            if (!folders.TryGetValue(setting, out List<string> selected)) return;
            content.foldersToLoadDescendingOrder.RemoveAll(folder => selected.Contains(folder));
            folders.Remove(setting);
        }

        private readonly ModContentPack content;
        private readonly Dictionary<string, List<string>> folders = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    }
}
