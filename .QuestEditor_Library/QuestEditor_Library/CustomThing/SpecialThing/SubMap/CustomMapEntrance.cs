using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class CustomMapEntrance : CQFMapPortal, IDrawTabable , ICustomThing
    {
        public override string Label => this.TextComp == null || !this.textComp.useCustomName ? base.Label : this.textComp.customName;
        public override string DescriptionFlavor => this.TextComp == null 
                                                    || 
                                                    !this.textComp.useCustomDescription ? 
            (this.Desc ?? base.DescriptionFlavor) : this.textComp.customDescription;
        
        public string Desc
        {
            get
            { 
                if (this.opended && this.def.GetModExtension<ModExtension_CustomThing>() is {} ex
                    && !ex.openedDesc.NullOrEmpty()) 
                {
                    return ex.openedDesc;
                }
                return null;
            }
        }
        public override Graphic Graphic
        {
            get
            {
                Graphic result = base.Graphic;
                if (this.opended && this.def.GetModExtension<ModExtension_CustomThing>() is ModExtension_CustomThing extension && extension.openedGraphicdata is GraphicData data && data.GraphicColoredFor(this) is Graphic g) 
                {
                    return g;
                }
                return result;
            }
        }
        public CompCustomText TextComp 
        {
            get 
            {
                if (this.textComp == null) 
                {
                    this.textComp = this.TryGetComp<CompCustomText>();
                }
                return this.textComp;
            }
        }
        public virtual string MapName 
        {
            get 
            {  
                if (this.CustomMap != null && this.CustomMap.Parent is MapParent_Custom custom)
                {
                  return custom.MapName;
                }
                else if (this.mapDef != null)
                {
                    return this.mapDef.label;
                }
                return "submap";
            }
        }
        public virtual string GetEnterText => "EnterNextMap".Translate(this.MapName);
        public virtual bool GenerateMapWhenSpawn => false;
        public virtual bool DestroyMapWhenDestroy => true;
        public virtual Map CustomMap
        {
            get
            {
                return this.customMap;
            }
            set
            {
                this.customMap = value;
            }
        }
        public override CQFMapPortal Exit => this.exit;
        public virtual CustomMapDataDef MapDef => this.mapDef;
        public virtual bool DestroyMapWhenDeSpawn => true;
        public virtual void SetMapDef(CustomMapDataDef mapDef) 
        {
            this.mapDef = mapDef;
        }
        public virtual void TryEnter(Thing thing)
        {
            if (this.CustomMap == null)
            {
                if (this.exit != null)
                {
                    this.customMap = this.exit.Map;
                }
                else
                {
                    if (this.MapDef != null)
                    {
                        this.GenerateCustomMap(this.Map, thing);
                        return;
                    }
                    else
                    {
                        Messages.Message("EntranceIsBlocked".Translate(), MessageTypeDefOf.RejectInput);
                        return;
                    }
                }
            }
            Enter(thing);
        }
        public override void OnEntered(Pawn pawn)
        {
            base.OnEntered(pawn);
            this.TriggerEnterActions(pawn);
        }
        public void Swtich(bool value) 
        {
            this.opended = value;
            this.Map.mapDrawer.MapMeshDirty(this.Position,MapMeshFlagDefOf.Things);
        }
        public CustomThingData GetData(IntVec3 pos)
        {
            return new CustomThingData_CustomMapEntrance(this,pos);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (this.GenerateMapWhenSpawn && !this.init) 
            {
                this.init = true;
                this.GenerateCustomMap(map,null);
            }
        }
        public override string GetInspectString()
        {
            StringBuilder result = new StringBuilder(base.GetInspectString());
            if (this.mapDef != null)
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }
                result.Append("EntranceToSubMap".Translate(this.MapName));
            } else if (this.CustomMap != null && this.CustomMap.Parent is MapParent_Custom custom && this.CustomMap.Parent.Spawned && !this.CustomMap.Parent.Destroyed) 
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }
                result.Append("EntranceToSubMap".Translate(custom.MapName));
            }
            return result.ToString().Trim();
        }
        public override bool IsEnterable(out string reason)
        {
            if (!this.opended) 
            {
                reason = "EntranceIsBlocked".Translate();
                return false;
            }
            return base.IsEnterable(out reason);
        }
        public virtual void DrawTab()
        {
            Rect outRect = new Rect(0f, 0f, 540f, 590f);
            float width = outRect.width - 40f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.height + 10f));
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            float x = 10f;
            float y = 10f;

            this.DrawSectionHeader(ref y, x, width, "CQF_PortalMapSection".Translate(), "CQF_PortalMapSectionTip".Translate());
            string mapLabel = this.mapDef == null ? "Null".Translate().ToString() : this.mapDef.label;
            Rect rect = new Rect(x + 8f, y, width - 16f, 30f);
            if (Widgets.ButtonText(rect, "CurCustomMap".Translate(mapLabel), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, (x) => this.mapDef = x, (x) => x.label);
            }
            y += 35f;

            this.DrawSectionHeader(ref y, x, width, "CQF_PortalSettingsSection".Translate(), "CQF_PortalSettingsSectionTip".Translate());
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "DefaultOpened".Translate(), ref this.opended);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExitName".Translate(), ref this.exitName, x + 8f, 150f);
            y += 30f;

            this.DrawActionSection(ref y, x, width, this.enterActions);
            this.height = y + 10f;
            Widgets.EndScrollView();
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            if (this.DestroyMapWhenDeSpawn && this.CustomMap != null) 
            {
                PocketMapUtility.DestroyPocketMap(this.CustomMap);
                this.CustomMap = null;
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            if (this.DestroyMapWhenDestroy&& this.CustomMap != null) 
            {
                PocketMapUtility.DestroyPocketMap(this.CustomMap);
                this.CustomMap = null;
            }
        }
        //public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        //{
        //    if ((this.mapDef == null && this.CustomMap == null) || !this.opended)
        //    {
        //        yield return new FloatMenuOption("EntranceIsBlocked".Translate(), null);
        //    }
        //    else
        //    {
        //        yield return new FloatMenuOption(this.GetEnterText, delegate
        //        {
        //            selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(QEDefOf.QE_EnterOrExitSubMap, this));
        //        });
        //    }
        //    yield break;
        //}
        //public override IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(List<Pawn> selPawns)
        //{
        //    if ((this.mapDef == null && this.CustomMap == null) || !this.opended)
        //    {
        //        yield return new FloatMenuOption("EntranceIsBlocked".Translate(), null);
        //    }
        //    else
        //    {
        //        List<Pawn> pawns = selPawns.FindAll(p => p.CanReach(this, Verse.AI.PathEndMode.Touch, Danger.Deadly));
        //        if (pawns.Any())
        //        {
        //            yield return new FloatMenuOption(this.GetEnterText, delegate
        //            {
        //                pawns.ForEach(p => p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(QEDefOf.QE_EnterOrExitSubMap, this)));
        //            });
        //        }
        //    }
        //    yield break;
        //}
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "SetMapDef",
                    action = () =>
                    {
                        Find.WindowStack.Add(new Dialog_Select<CustomMapDataDef>(new TextSelectDrawer<CustomMapDataDef>(DefDatabase<CustomMapDataDef>.AllDefsListForReading, d => d.label, d => this.mapDef = d, null, null, null, null, null, null), "Select".Translate()));
                    }
                };
                yield return new Command_Action()
                {
                    defaultLabel = "GenerateCustomMap".Translate(),
                    action = () =>
                    {
                        this.GenerateCustomMap(this.Map, null);
                    }
                };
            }
            yield break;
        }

        public virtual void GenerateCustomMap(Map map, Thing thing)
        {
            if (this.CustomMap != null) 
            {
                return;
            }
            MapGenerator.PlayerStartSpot = this.Position;
            MapParent_Custom custom 
                = (MapParent_Custom)WorldObjectMaker.MakeWorldObject(map.Tile.Layer.Def.isSpace
                ? QEDefOf.QE_CustomMap_SpaceSubMap
                : QEDefOf.QE_CustomMap_SubMap);
            custom.mapDataDef = this.MapDef;
            custom.quest = Find.QuestManager.QuestsListForReading.Find(q => q.id.ToString() == this.questID);
            custom.level = 1;
            if (map.Parent is MapParent_Custom parent)
            {
                custom.level += parent.level;
                custom.rootSite = parent.rootSite;
            }
            custom.SetFaction(Find.FactionManager.OfPlayer);
            custom.entrance = this;
            if (map.Parent is CustomSite site)
            {
                custom.rootSite = site;
            }
            if (custom.rootSite != null)
            {
                custom.rootSite.allSubMaps.Add(custom);
            }
            map.GetComponent<MapComponent_CustomMapData>().AddSubMap(custom); 
            string seed = Find.World.info.seedString;
            Find.World.info.seedString = Find.TickManager.TicksGame.ToString();
            LongEventHandler.SetCurrentEventText("GenerateSubMap".Translate());
            DeepProfiler.Start("Generate map");
            this.customMap = GameTools.GenerateSubMap(this.MapDef.size, custom,
                this.MapDef.generator ?? custom.def.mapGenerator, this.GetSteps(), map);
            QuestUtility.AddQuestTag(ref this.customMap.Parent.questTags, "Quest" + this.questID + "." + this.MapDef.defName);
            QuestUtility.AddQuestTag(ref this.customMap.Parent.questTags, "Quest" + this.questID + "." + custom.level);
            if (thing is Pawn pawn)
            {
                pawn.jobs.StopAll();
                pawn.jobs.StartJob(JobMaker.MakeJob(QEDefOf.QE_EnterOrExitSubMap, this));
                Current.Game.CurrentMap = this.CustomMap;
                Current.CameraDriver.JumpToCurrentMapLoc(this.exit.Position);
            }
            if (this.exit != null)
            {
                this.customMap.fogGrid.FloodUnfogAdjacent(this.exit.Position,false);
            }
            Find.World.info.seedString = seed;
            DeepProfiler.End();
        }
        public IEnumerable<GenStepWithParams> GetSteps()
        {
            yield return new GenStepWithParams(QEDefOf.QE_CustomSite_GenStep, new GenStepParams()
            {
                sitePart = new SitePart(null, QEDefOf.QE_CustomSite, new CustomSitePartParams
                {
                    mapData = this.MapDef,
                    quest = this.questID != null ? Find.QuestManager.QuestsListForReading.Find(q => q.id.ToString() == this.questID) : null,
                    spot = this.Position,
                    isSubMap = true
                })
            });
           // yield return new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("Fog"), new GenStepParams());
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.init, "init");
            Scribe_Values.Look(ref this.thereIsPawnIsEntering, "thereIsPawnIsEntering");
            Scribe_Values.Look(ref this.exitName, "CQF_CustomMapEntrance_exitName");
            Scribe_Values.Look(ref this.questID, "CQF_CustomMapEntrance_questID");
            Scribe_Values.Look(ref this.opended, "CQF_CustomMapEntrance_opended");
            Scribe_Defs.Look(ref this.mapDef, "CQF_CustomMapEntrance_mapDef");
            Scribe_References.Look(ref this.exit, "CQF_CustomMapEntrance_exit");
            Scribe_References.Look(ref this.customMap, "CQF_CustomMapEntrance_customMap");
            Scribe_Collections.Look(ref this.enterActions, "enterActions", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.enterActions == null)
            {
                this.enterActions = new List<CQFAction>();
            }
        }

        public override Map GetOtherMap()
        {
            if (this.customMap == null) 
            {
                this.GenerateCustomMap(this.Map,null);
            }
            return this.customMap;
        }

        public override IntVec3 GetDestinationLocation()
        {
            return this.exit == null ? IntVec3.Invalid : this.exit.Position;
        }

        protected void DrawActionSection(ref float y, float x, float width, List<CQFAction> actions)
        {
            this.DrawSectionHeader(ref y, x, width, "CQF_PortalEnterActions".Translate(), "CQF_PortalEnterActionsTip".Translate(),
                () => CQFEditorTools.OpenCQFActionSelect(type => actions.Add((CQFAction)Activator.CreateInstance(type))),
                () => CQFEditorTools.DrawFloatMenu(actions, action => actions.Remove(action), action => action.GetType().Name.Translate()),
                actions.Any());
            if (actions.Any())
            {
                foreach (CQFAction action in actions)
                {
                    Rect rowRect = new Rect(x + 8f, y, width - 16f, 28f);
                    Widgets.DrawHighlightIfMouseover(rowRect);
                    if (Widgets.ButtonText(rowRect, action.GetType().Name.Translate(), false))
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(action));
                    }
                    y += 32f;
                }
            }
            else
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_PortalNoActions".Translate());
            }
            y += 8f;
        }

        protected void DrawSectionHeader(ref float y, float x, float width, string label, string tip = null,
            Action addAction = null, Action removeAction = null, bool canRemove = false)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Rect labelRect = new Rect(x + 8f, y + 4f, width - 84f, 25f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.SkyBlue));
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(labelRect, tip);
            }
            if (addAction != null)
            {
                Rect buttonRect = new Rect(x + width - 66f, y + 2f, 25f, 25f);
                if (Widgets.ButtonImage(buttonRect, TexButton.Plus))
                {
                    addAction();
                }
                TooltipHandler.TipRegion(buttonRect, "Add".Translate());
                buttonRect.x += 30f;
                if (Widgets.ButtonImage(buttonRect, TexButton.Delete) && canRemove)
                {
                    removeAction?.Invoke();
                }
                TooltipHandler.TipRegion(buttonRect, "Remove".Translate());
            }
            y += 38f;
        }

        protected void DrawEmptyState(ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(Color.gray));
            y += 32f;
        }

        private void Enter(Thing thing)
        {
            if (thing == null || this.exit == null ||
                this.exit.Position == null || this.exit.Map == null)
            {
                return;
            }
            this.thereIsPawnIsEntering = true;
            if (thing.Spawned)
            {
                thing.DeSpawn();
            }
            GenSpawn.Spawn(thing, this.exit.Position, this.exit.Map);
            if (thing is Pawn pawn)
            {
                this.OnEntered(pawn);
            }
            this.thereIsPawnIsEntering = false;
            if (this.exit.Position.Fogged(this.exit.Map))
            {
                FloodFillerFog.FloodUnfog(this.exit.Position, this.exit.Map);
            }
            if (!(thing is Pawn))
            {
                this.TriggerEnterActions(thing);
            }
        }

        private void TriggerEnterActions(Thing thing)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>
            {
                ["Trigger"] = thing,
                ["CustomThing"] = this
            };
            Quest quest = GameTools.GetQuestFromThing(this);
            foreach (CQFAction action in this.enterActions)
            {
                if (action == null)
                {
                    Log.Error("CQF custom map entrance contains a null enter action: " + this.ThingID);
                    continue;
                }
                action.Work(targets, quest);
            }
        }

        public bool opended = true;
        public float height = 0f;
        public Vector2 scrollPos;
        public bool init;
        [NoTranslate]
        public string exitName;
        public CustomMapExit exit;    
        protected Map customMap;
        protected CustomMapDataDef mapDef;
        public string questID = null;
        public List<CQFAction> enterActions = new List<CQFAction>();
        public bool thereIsPawnIsEntering = false;
        private CompCustomText textComp = null;
    }
}
