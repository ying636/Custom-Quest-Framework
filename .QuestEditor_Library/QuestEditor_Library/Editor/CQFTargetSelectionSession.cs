using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFTargetSelectionSession
    {
        public CQFTargetSelectionSession(Map map, Thing source, Action<string> assign)
        {
            this.map = map;
            this.source = source;
            this.assign = assign;
        }

        public static void DrawField(ref float y, Rect inRect, float x, string value, Action<string> assign)
        {
            float width = Mathf.Max(210f, inRect.width - x - 12f);
            Widgets.Label(new Rect(x, y, 110f, 25f), "DialogueTarget".Translate());
            string edited = Widgets.TextField(new Rect(x + 114f, y, width - 114f, 25f), value ?? string.Empty);
            if (edited != value)
            {
                assign(edited);
            }
            y += 29f;
            Map map = CQFEditorContext.Map ?? Find.CurrentMap;
            Thing source = CQFEditorContext.SourceThing;
            float buttonWidth = (width - 8f) / 3f;
            if (Widgets.ButtonText(new Rect(x, y, buttonWidth, 25f), "Select".Translate()))
            {
                Find.WindowStack.Add(new Window_CQFTargetPicker(map, source, assign));
            }
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && map != null && Current.ProgramState == ProgramState.Playing;
            if (Widgets.ButtonText(new Rect(x + buttonWidth + 4f, y, buttonWidth, 25f), "CQF_TargetPickOnMap".Translate()))
            {
                new CQFTargetSelectionSession(map, source, assign).Begin();
            }
            GUI.enabled = oldEnabled && map != null && !edited.NullOrEmpty();
            if (Widgets.ButtonText(new Rect(x + (buttonWidth + 4f) * 2f, y, buttonWidth, 25f), "CQF_TargetLocate".Translate()))
            {
                Locate(ResolveEditorTarget(map, source, edited));
            }
            GUI.enabled = oldEnabled;
            y += 34f;
        }

        public static List<TargetWithKey> GetAvailableTargets(Map map)
        {
            List<TargetWithKey> result = new List<TargetWithKey>();
            if (map == null)
            {
                return result;
            }
            result.AddRange(map.GetComponent<MapComponent_CQFTargets>().Entries);
            GameComponent_Editor editor = GameComponent_Editor.Instance;
            Quest quest = GameTools.GetQuestFromMap(map);
            IEnumerable<QuestData> databases = new[] { editor.GetQuestData(quest), editor.TemporaryDatabase, editor.GlobalDatabase };
            foreach (QuestData database in databases)
            {
                if (database == null)
                {
                    continue;
                }
                foreach (TargetWithKey entry in database.TargetDatas)
                {
                    if (entry != null && !entry.key.NullOrEmpty() && entry.target.IsValid && entry.target.Map == map &&
                        !entry.target.ThingDestroyed && !result.Any(existing => existing.key == entry.key))
                    {
                        result.Add(entry);
                    }
                }
            }
            foreach (Thing thing in map.listerThings.AllThings)
            {
                CompActionWorker worker = thing.TryGetComp<CompActionWorker>();
                if (worker?.comps == null)
                {
                    continue;
                }
                foreach (ActionComp component in worker.comps.Where(component => component.mode == ActionTriggerMode.MapGeneration))
                {
                    foreach (CQFAction_RecordToDatabase action in component.actions.OfType<CQFAction_RecordToDatabase>())
                    {
                        if (action.GetType() == typeof(CQFAction_RecordToDatabase) && !action.recordKey.NullOrEmpty() &&
                            action.targetsText?.Count == 1 && action.targetsText[0] == "CustomThing" &&
                            (action.recordToQuestBase || action.recordToTemporaryBase || action.recordToGlobalBase) &&
                            !result.Any(existing => existing.key == action.recordKey))
                        {
                            result.Add(new TargetWithKey(action.recordKey, thing));
                        }
                    }
                }
            }
            return result.OrderBy(entry => entry.key, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static TargetInfo ResolveEditorTarget(Map map, Thing source, string key)
        {
            if (key == "CustomThing" && source != null)
            {
                return source;
            }
            return GetAvailableTargets(map).FirstOrDefault(entry => entry.key == key)?.target ?? TargetInfo.Invalid;
        }

        public static void Locate(TargetInfo target)
        {
            if (!target.IsValid || target.Map == null)
            {
                Messages.Message("CQF_TargetNotFound".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            CameraJumper.TryJump(target.Cell, target.Map);
            if (target.HasThing)
            {
                Find.Selector.Select(target.Thing, false, false);
            }
            Find.WindowStack.Add(new Window_CQFTargetHighlight(target));
        }

        public static bool CanPersistThing(Thing thing)
        {
            return thing != null && thing.Spawned && !(thing is Pawn) && !(thing is Gas) && !(thing is Corpse) &&
                !(thing is Spawner) && !(thing is GenerationActionWorker) && !(thing is CustomMapEnterSpot) && !(thing is ZoneCore);
        }

        public void Begin()
        {
            if (this.map == null || Current.ProgramState != ProgramState.Playing)
            {
                Messages.Message("CQF_TargetMapUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.TickManager.Pause();
            foreach (Window window in Find.WindowStack.Windows.ToList())
            {
                if (window.GetType().Namespace == typeof(CQFTargetSelectionSession).Namespace && window.layer == WindowLayer.Dialog)
                {
                    this.windows.Add(new KeyValuePair<Window, Rect>(window, window.windowRect));
                    Find.WindowStack.TryRemove(window, false);
                }
            }
            Current.Game.CurrentMap = this.map;
            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetBuildings = true,
                canTargetItems = true,
                canTargetPlants = true,
                canTargetPawns = false,
                canTargetLocations = false,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = target => CanPersistThing(target.Thing) && target.Map == this.map
            }, target => this.selected = target.Thing, actionWhenFinished: this.Finish, requiresCastedSelected: false);
        }

        private void Finish()
        {
            if (this.source?.Spawned == true)
            {
                Find.Selector.Select(this.source, false, false);
            }
            foreach (KeyValuePair<Window, Rect> entry in this.windows)
            {
                Find.WindowStack.Add(entry.Key);
                entry.Key.windowRect = entry.Value;
            }
            this.windows.Clear();
            if (this.selected == null)
            {
                return;
            }
            List<string> keys = GetAvailableTargets(this.map).Where(entry => entry.target.Thing == this.selected)
                .Select(entry => entry.key).Distinct().ToList();
            if (keys.Count == 1)
            {
                this.UseKey(keys[0]);
            }
            else if (keys.Count > 1)
            {
                Find.WindowStack.Add(new FloatMenu(keys.Select(key => new FloatMenuOption(key, () => this.UseKey(key))).ToList()));
            }
            else
            {
                Find.WindowStack.Add(new Dialog_CQFTargetKey(this.selected, this.assign));
            }
        }

        private void UseKey(string key)
        {
            if (this.map.GetComponent<MapComponent_CQFTargets>().TryRegister(key, this.selected))
            {
                this.assign(key);
            }
        }

        private readonly Map map;
        private readonly Thing source;
        private readonly Action<string> assign;
        private readonly List<KeyValuePair<Window, Rect>> windows = new List<KeyValuePair<Window, Rect>>();
        private Thing selected;
    }
}
