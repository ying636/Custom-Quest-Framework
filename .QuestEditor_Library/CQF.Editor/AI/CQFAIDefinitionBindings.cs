using System.Collections;
using System.Reflection;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public sealed class CQFAIDefinitionBindings
    {
        public int Refresh(Def definition)
        {
            visited.Clear(); changes.Clear(); replacement = definition;
            foreach (Type type in GenDefDatabase.AllDefTypesWithDatabases().Where(type => type.Assembly == typeof(DialogTreeDef).Assembly || type == typeof(DutyDef) || type == typeof(QuestScriptDef)))
                foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(type)) Visit(def);
            if (Current.Game != null)
            {
                foreach (GameComponent component in Current.Game.components ?? new List<GameComponent>()) Visit(component);
                foreach (Map map in Current.Game.Maps)
                {
                    Visit(map.Parent);
                    foreach (MapComponent component in map.components ?? new List<MapComponent>()) Visit(component);
                    foreach (Thing thing in map.listerThings.AllThings)
                    {
                        Visit(thing);
                        if (thing is ThingWithComps withComps) foreach (ThingComp comp in withComps.AllComps) { Visit(comp); Visit(comp.props); }
                    }
                }
                Visit(GameComponent_ComplexDuty.Component); Visit(GameComponent_Editor.Instance); Visit(GameComponent_QuestBook.Instance);
                if (Current.Game.World != null)
                {
                    Visit(MainMapWorldComponent.Component);
                    foreach (var worldObject in Current.Game.World.worldObjects.AllWorldObjects) Visit(worldObject);
                }
                foreach (Quest quest in Find.QuestManager?.QuestsListForReading ?? new List<Quest>()) foreach (QuestPart part in quest.PartsListForReading) Visit(part);
            }
            foreach (Action change in changes) change();
            if (Current.Game != null)
            {
                if (definition is QuestBookDef book) GameComponent_QuestBook.Instance?.RefreshDefinition(book);
                if (Current.Game.World != null) MainMapWorldComponent.Component?.RebuildMainSiteIndex();
                if (definition is DutyMapDef duty)
                    foreach (Pawn pawn in Current.Game.Maps.SelectMany(map => map.mapPawns.AllPawnsSpawned))
                    {
                        CustomDutyMap? runtime = CQFAIPawnRuntime.Runtime(pawn);
                        if (runtime?.dutyMap != duty) continue;
                        if (duty.GetNode(runtime.currentNodeId) == null) runtime.currentNodeId = duty.StartNode?.nodeId ?? "";
                        LordJob_ComplexCustom.GetForPawn(pawn)?.ApplyDuty(pawn);
                    }
            }
            return changes.Count;
        }
        private void Visit(object? value)
        {
            if (value == null || value is string || value.GetType().IsValueType || !visited.Add(value)) return;
            if (visited.Count > 100000) throw new InvalidDataException("CQF_AI_ReadTooLarge: binding graph");
            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary.Keys.Cast<object>().Select(key => new DictionaryEntry(key, dictionary[key])).ToArray())
                {
                    object key = Match(entry.Key) ? replacement! : entry.Key;
                    object? next = Match(entry.Value) ? replacement : entry.Value;
                    if (!ReferenceEquals(key, entry.Key))
                    {
                        if (dictionary.Contains(key) && !ReferenceEquals(dictionary[key], entry.Value)) throw new InvalidDataException("CQF_AI_DuplicateKey: refreshed definition dictionary key");
                        changes.Add(() => { dictionary.Remove(entry.Key); dictionary[key] = next; });
                    }
                    else if (!ReferenceEquals(next, entry.Value)) changes.Add(() => dictionary[key] = next);
                    else Visit(entry.Value);
                }
                return;
            }
            if (value is IList list)
            {
                for (int index = 0; index < list.Count; index++)
                {
                    int slot = index;
                    if (Match(list[slot])) changes.Add(() => list[slot] = replacement); else Visit(list[slot]);
                }
                return;
            }
            Type type = value.GetType();
            if (type.Assembly != typeof(DialogTreeDef).Assembly && type != typeof(DutyDef) && type != typeof(QuestScriptDef) && !typeof(ThinkNode).IsAssignableFrom(type) && !typeof(QuestNode).IsAssignableFrom(type)) return;
            for (Type? current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                if (current.Assembly != typeof(DialogTreeDef).Assembly && current != typeof(DutyDef) && current != typeof(QuestScriptDef) && !typeof(ThinkNode).IsAssignableFrom(current) && !typeof(QuestNode).IsAssignableFrom(current)) break;
                foreach (FieldInfo field in current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (field.IsDefined(typeof(UnsavedAttribute), true)) continue;
                    object? child = field.GetValue(value);
                    if (Match(child))
                    {
                        if (field.IsInitOnly || !field.FieldType.IsInstanceOfType(replacement)) throw new InvalidDataException("CQF_AI_InvalidValue: immutable/incompatible definition binding " + field.Name);
                        changes.Add(() => field.SetValue(value, replacement));
                    }
                    else Visit(child);
                }
            }
        }
        private bool Match(object? value) => value is Def def && replacement != null && !ReferenceEquals(def, replacement) && def.GetType() == replacement.GetType() && def.defName == replacement.defName;
        private readonly HashSet<object> visited = new HashSet<object>();
        private readonly List<Action> changes = new List<Action>();
        private Def? replacement;
    }
}
