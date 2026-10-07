using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAITargetCatalog
    {
        public CQFAITargetCatalog(Func<IEnumerable<CQFAITarget>> discover) { this.discover = discover; }
        public XElement Query(string search, int offset)
        {
            if (search.Length > 100 || offset < 0 || offset > 100000) throw new InvalidDataException("CQF_AI_InvalidTool: target search");
            Refresh();
            CQFAITarget[] matches = targets.Where(target => (target.Name + " " + target.Label + " " + target.Type + " " + target.Kind).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            return new XElement("targets", new XAttribute("total", matches.Length), new XAttribute("offset", offset), matches.Skip(offset).Take(40).Select(target => target.Summary));
        }
        public CQFAITarget Resolve(string id)
        {
            CQFAITarget? known = targets.SingleOrDefault(target => target.Id == id);
            if (known?.IsValid() == true) return known;
            Refresh();
            return targets.SingleOrDefault(target => target.Id == id) ?? throw new InvalidDataException("CQF_AI_MissingTarget");
        }
        public static IEnumerable<CQFAITarget> Discover(CQFAIModel model, IEnumerable<CQFAIEditorContext>? contexts = null, IEnumerable<Map>? maps = null)
        {
            CQFAIEditorContext[] editors = (contexts ?? Find.WindowStack.Windows.OfType<ICQFAIEditorHost>().Select(host => host.AIContext)
                .Where(context => context != null).Cast<CQFAIEditorContext>())
                .Where(context => context != null && context.IsValid?.Invoke() != false).Cast<CQFAIEditorContext>().ToArray();
            foreach (CQFAIEditorContext context in editors)
            {
                object identity = context.Owner ?? context.Identity;
                object data = context.Read();
                string kind = data is CQFAILiveMapInfo info ? ReferenceEquals(info.backend?.Identity, Find.CurrentMap) ? "current_map" : "map" : "editor";
                string label = data is Def draft ? draft.label ?? "" : "";
                if (data is DialogTreeDef tree && !string.IsNullOrEmpty(tree.title)) label += " " + (tree.title.CanTranslate() ? tree.title.Translate().ToString() : tree.title);
                yield return new CQFAITarget(identity, context.Name, data.GetType().FullName!, kind, () => context,
                    () => context.IsValid?.Invoke() != false, label);
            }
            foreach (Map map in maps ?? Current.Game?.Maps ?? new List<Map>())
            {
                if (editors.Any(editor => ReferenceEquals(editor.Identity, map))) continue;
                yield return new CQFAITarget(map, map.ToString(), typeof(CQFAILiveMapInfo).FullName!, ReferenceEquals(Find.CurrentMap, map) ? "current_map" : "map",
                    () => CQFAILiveMapContext.Create(map), () => Current.Game?.Maps.Contains(map) == true);
            }
            HashSet<Type> editable = new HashSet<Type>(model.Types);
            foreach (Type type in GenDefDatabase.AllDefTypesWithDatabases().Where(type => editable.Contains(type)
                && (type.Assembly == typeof(DialogTreeDef).Assembly || type == typeof(Verse.AI.DutyDef) || type == typeof(RimWorld.QuestScriptDef))))
                foreach (Def definition in GenDefDatabase.GetAllDefsInDatabaseForDef(type))
                {
                    if (editors.Any(editor => editor.Read() is Def draft && draft.GetType() == type && draft.defName == definition.defName)) continue;
                    string name = definition.defName;
                    Def current = definition;
                    string label = definition.label ?? "";
                    if (definition is DialogTreeDef tree && !string.IsNullOrEmpty(tree.title)) label += " " + (tree.title.CanTranslate() ? tree.title.Translate().ToString() : tree.title);
                    yield return new CQFAITarget(definition, name, type.FullName!, "loaded_definition",
                        () => new CQFAIEditorContext(name, () => current, value => { CQFQuestDefBootstrap.HotLoadDefinition((Def)value); current = (Def)value; },
                            validate: value =>
                            {
                                if (((Def)value).defName != name) throw new InvalidDataException("CQF_AI_DefinitionNameFixed");
                                string[] errors = ((Def)value).ConfigErrors().Take(16).ToArray();
                                if (errors.Length > 0) throw new InvalidDataException("CQF_AI_InvalidValue: " + string.Join("; ", errors));
                            },
                            isValid: () => ReferenceEquals(GenDefDatabase.GetDef(type, name, false), current)),
                        () => ReferenceEquals(GenDefDatabase.GetDef(type, name, false), current), label);
                }
        }
        private void Refresh()
        {
            List<CQFAITarget> next = new List<CQFAITarget>();
            var previousByName = targets.GroupBy(target => (target.Kind, target.Name, target.Type)).ToDictionary(group => group.Key, group => group.ToArray());
            foreach (CQFAITarget candidate in discover())
            {
                if (!candidate.IsValid()) continue;
                CQFAITarget? previous = previousByName.TryGetValue((candidate.Kind, candidate.Name, candidate.Type), out CQFAITarget[] matches)
                    ? matches.FirstOrDefault(target => ReferenceEquals(target.Identity, candidate.Identity) || target.Kind == "loaded_definition" && target.IsValid()) : null;
                if (previous != null) previous.Label = candidate.Label;
                next.Add(previous ?? candidate);
            }
            targets.Clear(); targets.AddRange(next);
        }
        private readonly Func<IEnumerable<CQFAITarget>> discover;
        private readonly List<CQFAITarget> targets = new List<CQFAITarget>();
    }
}
