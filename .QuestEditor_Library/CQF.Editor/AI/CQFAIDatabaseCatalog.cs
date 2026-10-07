using System.Reflection;
using System.Xml.Linq;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class CQFAIDatabaseCatalog
    {
        public static XElement List(int offset, int limit)
        {
            CQFAIRuntimeCatalog.ValidatePage("", offset, limit);
            GameComponent_Editor component = Component();
            Dictionary<int, QuestData>? quests = (Dictionary<int, QuestData>?)QuestDatabases.GetValue(component);
            return CQFAIRuntimeCatalog.Page("databases", new[] { Summary("global", -1, (QuestData?)GlobalDatabase.GetValue(component)),
                Summary("temporary", -1, (QuestData?)TemporaryDatabase.GetValue(component)) }.Concat((quests ?? new Dictionary<int, QuestData>())
                    .OrderBy(pair => pair.Key).Select(pair => Summary("quest", pair.Key, pair.Value))), offset, limit);
        }
        public static XElement Read(string scope, int questId, string category, string key, string search, int offset, int limit)
        {
            CQFAIRuntimeCatalog.ValidatePage(search, offset, limit);
            GameComponent_Editor component = Component();
            QuestData? data = scope switch
            {
                "global" => (QuestData?)GlobalDatabase.GetValue(component), "temporary" => (QuestData?)TemporaryDatabase.GetValue(component),
                "quest" => questId >= 0 && QuestDatabases.GetValue(component) is Dictionary<int, QuestData> quests && quests.TryGetValue(questId, out QuestData found)
                    ? found : throw new InvalidDataException("CQF_AI_MissingResource: quest database; list current database IDs first"),
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: database scope")
            };
            XElement result = ReadData(data, category, key, search, offset, limit);
            result.Add(new XAttribute("scope", scope));
            if (scope == "quest") result.Add(new XAttribute("questId", questId));
            return result;
        }
        public static XElement ReadData(QuestData? data, string category, string key, string search, int offset, int limit)
        {
            CQFAIRuntimeCatalog.ValidatePage(search, offset, limit);
            if (key.Length > 200 || category is not ("targets" or "bools" or "ints" or "groups" or "group_members" or "lords"))
                throw new InvalidDataException("CQF_AI_InvalidTool: database category/key");
            IEnumerable<XElement> entries = Array.Empty<XElement>();
            bool Match(string name) => (key.Length == 0 || name == key) && (search.Length == 0 || name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            if (data != null)
            {
                if (category == "targets") entries = Values<List<TargetWithKey>>(Targets, data).Where(entry => entry != null && Match(entry.key))
                    .OrderBy(entry => entry.key).Select(entry => new XElement("entry", new XAttribute("key", entry.key), CQFAIRuntimeCatalog.Target(entry.target)));
                if (category == "bools") entries = Values<Dictionary<string, bool>>(Bools, data).Where(entry => Match(entry.Key)).OrderBy(entry => entry.Key)
                    .Select(entry => new XElement("entry", new XAttribute("key", entry.Key), new XAttribute("value", entry.Value)));
                if (category == "ints") entries = Values<Dictionary<string, int>>(Ints, data).Where(entry => Match(entry.Key)).OrderBy(entry => entry.Key)
                    .Select(entry => new XElement("entry", new XAttribute("key", entry.Key), new XAttribute("value", entry.Value)));
                if (category == "groups") entries = Values<List<QuestPawnGroup>>(Groups, data).Where(group => group != null && Match(group.groupName)).OrderBy(group => group.groupName)
                    .Select(group => new XElement("entry", new XAttribute("key", group.groupName), new XAttribute("count", group.inner?.Count ?? 0)));
                if (category == "group_members")
                {
                    if (key.Length == 0) throw new InvalidDataException("CQF_AI_InvalidTool: group_members requires an exact key");
                    QuestPawnGroup group = Values<List<QuestPawnGroup>>(Groups, data).FirstOrDefault(group => group?.groupName == key)
                        ?? throw new InvalidDataException("CQF_AI_MissingResource: database group");
                    entries = (group.inner ?? new List<Thing>()).Select((thing, index) => new { thing, index })
                        .Where(entry => search.Length == 0 || (entry.thing?.ThingID ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Select(entry => new XElement("member", new XAttribute("index", entry.index), entry.thing == null
                            ? new XElement("target", new XAttribute("valid", false)) : CQFAIRuntimeCatalog.Target(new TargetInfo(entry.thing))));
                }
                if (category == "lords") entries = Values<Dictionary<string, Lord>>(Lords, data).Where(entry => Match(entry.Key)).OrderBy(entry => entry.Key)
                    .Select(entry => new XElement("entry", new XAttribute("key", entry.Key), new XAttribute("valid", entry.Value != null),
                        new XAttribute("jobType", entry.Value?.LordJob?.GetType().FullName ?? ""), new XAttribute("pawnCount", entry.Value?.ownedPawns?.Count ?? 0)));
            }
            else if (category == "group_members") throw new InvalidDataException("CQF_AI_MissingResource: database group");
            XElement result = CQFAIRuntimeCatalog.Page("database", entries, offset, limit);
            result.Add(new XAttribute("category", category), new XAttribute("initialized", data != null));
            return result;
        }
        private static XElement Summary(string scope, int questId, QuestData? data) => new XElement("database", new XAttribute("scope", scope),
            questId < 0 ? null : new XAttribute("questId", questId), new XAttribute("initialized", data != null),
            new XAttribute("targets", data == null ? 0 : Values<List<TargetWithKey>>(Targets, data).Count),
            new XAttribute("bools", data == null ? 0 : Values<Dictionary<string, bool>>(Bools, data).Count),
            new XAttribute("ints", data == null ? 0 : Values<Dictionary<string, int>>(Ints, data).Count),
            new XAttribute("groups", data == null ? 0 : Values<List<QuestPawnGroup>>(Groups, data).Count),
            new XAttribute("lords", data == null ? 0 : Values<Dictionary<string, Lord>>(Lords, data).Count));
        private static GameComponent_Editor Component() => Current.Game != null && GameComponent_Editor.Instance != null ? GameComponent_Editor.Instance
            : throw new InvalidDataException("CQF_AI_MissingResource: no active game database");
        private static T Values<T>(FieldInfo field, QuestData data) where T : class, new() => (T?)field.GetValue(data) ?? new T();
        private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(type.FullName, name);
        private static readonly FieldInfo Targets = Field(typeof(QuestData), "targetDatas");
        private static readonly FieldInfo Bools = Field(typeof(QuestData), "values_B");
        private static readonly FieldInfo Ints = Field(typeof(QuestData), "values");
        private static readonly FieldInfo Groups = Field(typeof(QuestData), "pawnGroups");
        private static readonly FieldInfo Lords = Field(typeof(QuestData), "lords");
        private static readonly FieldInfo GlobalDatabase = Field(typeof(GameComponent_Editor), "globalData");
        private static readonly FieldInfo TemporaryDatabase = Field(typeof(GameComponent_Editor), "temporaryData");
        private static readonly FieldInfo QuestDatabases = Field(typeof(GameComponent_Editor), "datas");
    }
}
