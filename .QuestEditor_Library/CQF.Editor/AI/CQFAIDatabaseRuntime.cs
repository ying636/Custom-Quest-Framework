using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public sealed class CQFAIDatabaseRuntime
    {
        public CQFAIDatabaseRuntime(CQFAIRuntimeJournal journal) { Journal = journal; }
        public CQFAIRuntimeJournal Journal { get; }
        public XElement Help() => new XElement("database",
            new XElement("operation", new XAttribute("kind", "database_value"), "Fields: scope=global|temporary|quest, quest_id for quest, key, value_type=bool|int|target, value for bool/int, targets with exactly one target for target, remove=false. Requires an existing initialized database; query databases first."),
            new XElement("operation", new XAttribute("kind", "database_create"), "Fields: scope, quest_id for quest. Creates only missing Global/Temporary/Quest data; existing databases are preserved. Quest ID must be a current Quest."),
            new XElement("operation", new XAttribute("kind", "database_group"), "Fields: scope, quest_id, key, members (list of <id>exact spawned Thing ID</id>), remove=false. Replaces only the exact group's members; no implicit append or Pawn generation."),
            new XElement("operation", new XAttribute("kind", "database_lord"), "Fields: scope, quest_id, key, map_id, lord_id from lords query, remove=false. Binds an existing Lord, never creates one."),
            new XElement("operation", new XAttribute("kind", "map_target"), "Fields: map_id, key, thing_id, remove=false. Registers an exact spawned Thing on that map or removes the exact key. Reserved keys and existing key conflicts are rejected."),
            new XElement("operation", new XAttribute("kind", "duty_value"), "Fields: pawn_id, key, value_type=bool|int|float|string|target, value or targets with one target, remove=false. Requires an existing DutyMap runtime; does not create it."));
        public XElement Operate(XElement request)
        {
            string kind = CQFAIRuntimeRequest.Text(request, "kind");
            if (kind == "map_target") return MapTarget(request);
            bool duty = kind == "duty_value";
            CQFAIRuntimeRequest.Fields(request, kind switch
            {
                "database_create" => new[] { "kind", "scope", "quest_id" },
                "database_value" => new[] { "kind", "scope", "quest_id", "key", "value_type", "value", "targets", "remove" },
                "database_group" => new[] { "kind", "scope", "quest_id", "key", "members", "remove" },
                "database_lord" => new[] { "kind", "scope", "quest_id", "key", "map_id", "lord_id", "remove" },
                "duty_value" => new[] { "kind", "pawn_id", "key", "value_type", "value", "targets", "remove" },
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: database operation")
            });
            object component = duty ? CQFAIPawnRuntime.Runtime(CQFAIRuntimeRequest.Pawn(request)) ?? throw new InvalidDataException("CQF_AI_MissingResource: duty runtime")
                : Current.Game?.components?.OfType<GameComponent_Editor>().FirstOrDefault() ?? throw new InvalidDataException("CQF_AI_MissingResource: database component");
            if (kind == "database_create") return Create(request, (GameComponent_Editor)component);
            object data = duty ? component : Resolve(request, (GameComponent_Editor)component);
            string key = CQFAIRuntimeRequest.Text(request, "key");
            if (string.IsNullOrWhiteSpace(key) || key.Length > 200) throw new InvalidDataException("CQF_AI_InvalidTool: database key");
            bool remove = CQFAIRuntimeRequest.Bool(request, "remove");
            string valueType = CQFAIRuntimeRequest.Text(request, "value_type");
            string field = kind switch
            {
                "database_group" => "pawnGroups", "database_lord" => "lords",
                "database_value" => valueType switch { "bool" => "values_B", "int" => "values", "target" => "targetDatas", _ => throw new InvalidDataException("CQF_AI_InvalidTool: database value type") },
                "duty_value" => valueType switch { "bool" => "bools", "int" => "ints", "float" => "floats", "string" => "strings", "target" => "targets", _ => throw new InvalidDataException("CQF_AI_InvalidTool: duty value type") },
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: database operation")
            };
            FieldInfo slot = data.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new MissingFieldException(data.GetType().FullName, field);
            object? before = slot.GetValue(data);
            object after;
            object? value = null;
            if (!remove)
            {
                value = valueType switch
                {
                    "bool" => CQFAIRuntimeRequest.Bool(request, "value"), "int" => CQFAIRuntimeRequest.Int(request, "value"), "float" => CQFAIRuntimeRequest.Float(request, "value"),
                    "string" => CQFAIRuntimeRequest.Text(request, "value"), "target" => SingleTarget(request), _ => null
                };
                if (kind == "database_lord") value = CQFAIPawnRuntime.Lord(request);
            }
            if (field is "targets" or "targetDatas")
            {
                List<TargetWithKey> values = before is List<TargetWithKey> targets ? new List<TargetWithKey>(targets) : new List<TargetWithKey>();
                values.RemoveAll(entry => entry.key == key);
                if (!remove) values.Add(new TargetWithKey { key = key, target = (TargetInfo)value! });
                after = values;
            }
            else if (field == "pawnGroups")
            {
                List<QuestPawnGroup> groups = before is List<QuestPawnGroup> old ? new List<QuestPawnGroup>(old) : new List<QuestPawnGroup>();
                groups.RemoveAll(group => group.groupName == key);
                if (!remove)
                {
                    XElement members = request.Element("members") ?? throw new InvalidDataException("CQF_AI_InvalidTool: members");
                    if (members.HasAttributes || members.Elements().Count() > 256 || members.Elements().Any(element => element.Name != "id" || element.HasAttributes || element.HasElements)) throw new InvalidDataException("CQF_AI_InvalidTool: members");
                    List<Thing> things = members.Elements().Select(element => CQFAIRuntimeRequest.Thing(element.Value)).ToList();
                    if (things.Distinct().Count() != things.Count) throw new InvalidDataException("CQF_AI_InvalidTool: duplicate members");
                    groups.Add(new QuestPawnGroup { groupName = key, inner = things });
                }
                after = groups;
            }
            else
            {
                IDictionary values = (IDictionary)Activator.CreateInstance(slot.FieldType)!;
                if (before is IDictionary old) foreach (DictionaryEntry entry in old) values.Add(entry.Key, entry.Value);
                if (remove) values.Remove(key); else values[key] = value ?? throw new InvalidDataException("CQF_AI_InvalidTool: value required");
                after = values;
            }
            string identity = duty ? "duty:" + CQFAIRuntimeRequest.Text(request, "pawn_id") : "database:" + CQFAIRuntimeRequest.Text(request, "scope", "global") + ":" + CQFAIRuntimeRequest.Int(request, "quest_id");
            return Journal.Edit(identity + ":" + field, () => slot.SetValue(data, after), () => slot.SetValue(data, before), () => Snapshot(slot.GetValue(data), field));
        }
        public static XElement Snapshot(object? data, string name)
        {
            XElement result = new XElement(name, new XAttribute("initialized", data != null));
            if (data is IDictionary values) foreach (DictionaryEntry entry in values.Keys.Cast<object>().Select(key => new DictionaryEntry(key, values[key])).OrderBy(entry => entry.Key?.ToString(), StringComparer.Ordinal))
                result.Add(new XElement("entry", new XAttribute("key", entry.Key), entry.Value is Lord lord ? new XAttribute("lordId", lord.loadID) : new XAttribute("value", Convert.ToString(entry.Value, CultureInfo.InvariantCulture) ?? "")));
            if (data is IEnumerable<TargetWithKey> targets) foreach (TargetWithKey entry in targets.OrderBy(entry => entry.key, StringComparer.Ordinal)) result.Add(new XElement("entry", new XAttribute("key", entry.key), CQFAIRuntimeCatalog.Target(entry.target)));
            if (data is IEnumerable<QuestPawnGroup> groups) foreach (QuestPawnGroup group in groups.OrderBy(group => group.groupName, StringComparer.Ordinal))
                result.Add(new XElement("group", new XAttribute("key", group.groupName), (group.inner ?? new List<Thing>()).Select(thing => new XElement("member", thing?.ThingID ?? ""))));
            return result;
        }
        private object Resolve(XElement request, GameComponent_Editor component)
        {
            string scope = CQFAIRuntimeRequest.Text(request, "scope", "global");
            if (scope == "quest")
            {
                int id = CQFAIRuntimeRequest.Int(request, "quest_id");
                Dictionary<int, QuestData>? quests = (Dictionary<int, QuestData>?)DatabaseField("datas").GetValue(component);
                return quests != null && quests.TryGetValue(id, out QuestData value) ? value : throw new InvalidDataException("CQF_AI_MissingResource: quest database");
            }
            string field = scope switch { "global" => "globalData", "temporary" => "temporaryData", _ => throw new InvalidDataException("CQF_AI_InvalidTool: database scope") };
            return DatabaseField(field).GetValue(component) ?? throw new InvalidDataException("CQF_AI_MissingResource: uninitialized database; create explicitly");
        }
        private XElement Create(XElement request, GameComponent_Editor component)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "scope", "quest_id");
            string scope = CQFAIRuntimeRequest.Text(request, "scope", "global");
            string field = scope switch { "global" => "globalData", "temporary" => "temporaryData", "quest" => "datas", _ => throw new InvalidDataException("CQF_AI_InvalidTool: database scope") };
            FieldInfo slot = DatabaseField(field);
            object? before = slot.GetValue(component);
            object after;
            if (scope == "quest")
            {
                int id = CQFAIRuntimeRequest.Quest(request, true)!.id;
                Dictionary<int, QuestData> copy = before is Dictionary<int, QuestData> values ? new Dictionary<int, QuestData>(values) : new Dictionary<int, QuestData>();
                if (copy.ContainsKey(id)) return new XElement("databaseCreated", new XAttribute("existing", true));
                copy.Add(id, new QuestData()); after = copy;
            }
            else { if (before != null) return new XElement("databaseCreated", new XAttribute("existing", true)); after = new QuestData(); }
            return Journal.Edit("database:create:" + field, () => slot.SetValue(component, after), () => slot.SetValue(component, before),
                () => slot.GetValue(component) is Dictionary<int, QuestData> values ? new XElement("questDatabases", values.OrderBy(pair => pair.Key).Select(pair => new XElement("quest", new XAttribute("id", pair.Key), DatabaseSnapshot(pair.Value)))) : DatabaseSnapshot(slot.GetValue(component) as QuestData));
        }
        private XElement MapTarget(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "map_id", "key", "thing_id", "remove");
            Map map = CQFAIRuntimeRequest.Map(request);
            MapComponent_CQFTargets component = map.GetComponent<MapComponent_CQFTargets>() ?? throw new InvalidDataException("CQF_AI_MissingResource: map targets component");
            string key = CQFAIRuntimeRequest.Text(request, "key");
            if (string.IsNullOrWhiteSpace(key) || key.Length > 200 || key != key.Trim() || CQFTargetNames.Reserved.Contains(key)) throw new InvalidDataException("CQF_AI_InvalidTool: target key");
            FieldInfo slot = typeof(MapComponent_CQFTargets).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)!;
            object? before = slot.GetValue(component);
            List<TargetWithKey> copy = before is List<TargetWithKey> old ? new List<TargetWithKey>(old) : new List<TargetWithKey>();
            Thing? thing = CQFAIRuntimeRequest.Bool(request, "remove") ? null : CQFAIRuntimeRequest.Thing(CQFAIRuntimeRequest.Text(request, "thing_id"));
            if (thing != null && (thing.Map != map || copy.Any(entry => entry.key == key && entry.target.Thing != thing && entry.target.Thing?.Spawned == true))) throw new InvalidDataException("CQF_AI_InvalidValue: map target conflict");
            copy.RemoveAll(entry => entry.key == key);
            if (thing != null) copy.Add(new TargetWithKey { key = key, target = new TargetInfo(thing) });
            return Journal.Edit("mapTargets:" + map.uniqueID, () => slot.SetValue(component, copy), () => slot.SetValue(component, before), () => Snapshot(slot.GetValue(component), "targets"));
        }
        private static TargetInfo SingleTarget(XElement request)
        {
            Dictionary<string, TargetInfo> values = CQFAIRuntimeRequest.Targets(request);
            return values.Count == 1 ? values.Single().Value : throw new InvalidDataException("CQF_AI_InvalidTool: exactly one target required");
        }
        private static FieldInfo DatabaseField(string name) => typeof(GameComponent_Editor).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new MissingFieldException(typeof(GameComponent_Editor).FullName, name);
        private static XElement DatabaseSnapshot(QuestData? data) => new XElement("databaseSlot", new XAttribute("initialized", data != null), data == null ? null : new[] { "values_B", "values", "targetDatas", "pawnGroups", "lords" }
            .Select(name => Snapshot(typeof(QuestData).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(data), name)));
    }
}
