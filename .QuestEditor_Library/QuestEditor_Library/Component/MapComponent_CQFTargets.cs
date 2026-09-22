using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class MapComponent_CQFTargets : MapComponent
    {
        public MapComponent_CQFTargets(Map map) : base(map)
        {
        }

        public IReadOnlyList<TargetWithKey> Entries => this.entries.Where(entry => entry != null &&
            entry.target.HasThing && !entry.target.Thing.Destroyed && entry.target.Thing.Map == this.map).ToList();

        public TargetInfo GetTarget(string key)
        {
            return this.entries.FirstOrDefault(entry => entry != null && entry.key == key && entry.target.HasThing &&
                !entry.target.Thing.Destroyed && entry.target.Thing.Map == this.map)?.target ?? TargetInfo.Invalid;
        }

        public List<string> GetKeys(Thing thing)
        {
            return this.Entries.Where(entry => entry.target.Thing == thing).Select(entry => entry.key).ToList();
        }

        public bool TryRegister(string key, Thing thing, bool reportErrors = true)
        {
            if (thing == null || !thing.Spawned || thing.Map != this.map || key.NullOrEmpty() ||
                key != key.Trim() || CQFEditorTools.TargetTexts.Contains(key))
            {
                if (reportErrors)
                {
                    this.ReportError("CQF_TargetKeyInvalid", key ?? string.Empty);
                }
                return false;
            }
            TargetInfo existing = this.GetTarget(key);
            if (existing.IsValid && existing.Thing != thing)
            {
                if (reportErrors)
                {
                    this.ReportError("CQF_TargetKeyConflict", key);
                }
                return false;
            }
            this.entries.RemoveAll(entry => entry == null || entry.key == key);
            this.entries.Add(new TargetWithKey(key, thing));
            return true;
        }

        public bool CanRegisterDefinition(CustomMapDataDef definition)
        {
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            IEnumerable<IEnumerable<string>> allKeys = definition.thingDatas.Select(data => data.targetKeys)
                .Concat(definition.customThings.Where(data => !(data is CustomThingData_ZoneCore)).Select(data => data.targetKeys))
                .Concat(definition.zoneCores.Select(data => data.targetKeys));
            foreach (IEnumerable<string> collection in allKeys)
            {
                if (collection == null)
                {
                    continue;
                }
                foreach (string key in collection)
                {
                    if (key.NullOrEmpty() || key != key.Trim() || CQFEditorTools.TargetTexts.Contains(key))
                    {
                        this.ReportError("CQF_TargetKeyInvalid", key ?? string.Empty);
                        return false;
                    }
                    if (!keys.Add(key) || this.GetTarget(key).IsValid)
                    {
                        this.ReportError("CQF_TargetKeyConflict", key);
                        return false;
                    }
                }
            }
            return true;
        }

        public List<Action> BeginTargetRegistration()
        {
            List<Action> previous = this.generationActions;
            this.generationActions = new List<Action>();
            return previous;
        }

        public void FinishTargetRegistration(List<Action> previous, bool execute)
        {
            List<Action> pending = this.generationActions;
            this.generationActions = previous;
            if (execute)
            {
                if (previous != null)
                {
                    previous.AddRange(pending);
                    return;
                }
                foreach (Action action in pending)
                {
                    action();
                }
            }
        }

        public void AfterTargetRegistration(Action action)
        {
            if (this.generationActions != null)
            {
                this.generationActions.Add(action);
            }
            else
            {
                action();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.entries, "CQF_MapTargets", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.entries ??= new List<TargetWithKey>();
                this.entries.RemoveAll(entry => entry == null || !entry.target.HasThing || entry.target.Thing.Destroyed);
            }
        }

        public static TargetInfo Resolve(Dictionary<string, TargetInfo> context, Quest quest, string key)
        {
            if (key.NullOrEmpty())
            {
                return TargetInfo.Invalid;
            }
            List<Map> maps = context?.Values.Select(target => target.Map).Where(map => map != null).Distinct().ToList()
                ?? new List<Map>();
            if (maps.Count == 0 && quest != null && Current.Game != null)
            {
                maps.AddRange(Find.Maps.Where(map => GameTools.GetQuestFromMap(map) == quest));
            }
            TargetInfo result = TargetInfo.Invalid;
            foreach (Map map in maps)
            {
                TargetInfo candidate = map.GetComponent<MapComponent_CQFTargets>().GetTarget(key);
                if (!candidate.IsValid)
                {
                    continue;
                }
                if (result.IsValid && result != candidate)
                {
                    Log.Error($"[CQF] Ambiguous map target key: {key}.");
                    return TargetInfo.Invalid;
                }
                result = candidate;
            }
            return result;
        }

        private void ReportError(string key, string targetKey)
        {
            Log.Error($"[CQF] {key}: {targetKey}; map={this.map.uniqueID}.");
            if (Current.ProgramState == ProgramState.Playing)
            {
                Messages.Message(key.Translate() + ": " + targetKey, MessageTypeDefOf.RejectInput, false);
            }
        }

        private List<TargetWithKey> entries = new List<TargetWithKey>();
        private List<Action> generationActions;
    }
}
