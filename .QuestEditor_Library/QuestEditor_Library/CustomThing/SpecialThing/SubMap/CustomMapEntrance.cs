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
        public void Swtich(bool value) 
        {
            this.opended = value;
            this.Map.mapDrawer.MapMeshDirty(this.Position,MapMeshFlagDefOf.Things);
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
            string result = base.GetInspectString();
            if (this.mapDef != null)
            {
                result +="EntranceToSubMap".Translate(this.MapName);
            } else if (this.CustomMap != null && this.CustomMap.Parent is MapParent_Custom custom && this.CustomMap.Parent.Spawned && !this.CustomMap.Parent.Destroyed) 
            {
                result += "EntranceToSubMap".Translate(custom.MapName);
            }
            return result.Trim();
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
            Widgets.BeginScrollView(new Rect(7f, 25f, 475f, 590f), ref this.scrollPos, new Rect(7f, 10f, 475f, this.height));
            Widgets.DrawBox(new Rect(8f, 10f, 470f, this.height), 1, QuestEditor_Dialog.blueTex);
            float y = 20f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, y, 900f, 38f), "CustomMap".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            y += 40f;
            Rect rect = new Rect(15f, y, 430f, 30f);
            if (Widgets.ButtonText(rect,"CurCustomMap".Translate(this.MapName),false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, (x) => this.mapDef = x, (x) => x.label);
            }
            y += 35f;
            Widgets.CheckboxLabeled(new Rect(15f,y,350f,25f), "DefaultOpened".Translate(), ref this.opended);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y,"ExitName".Translate(),ref this.exitName,15f,150f);
            y += 30f;
            this.height = y + 5f;
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
        private CompCustomText textComp = null;

        public bool thereIsPawnIsEntering = false;
    }
}
