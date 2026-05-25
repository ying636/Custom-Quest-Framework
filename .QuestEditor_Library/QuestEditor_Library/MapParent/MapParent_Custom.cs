using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace QuestEditor_Library
{
    public class MapParent_Custom : PocketMapParent
    {
        public CustomMapExit Exit => this.exit;
        public virtual string MapName => (!this.customName.NullOrEmpty() ? this.customName 
            : this.mapDataDef?.label ?? this.def?.label);
        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            bool result = (!this.permanent && (this.sourceMap == null || !this.sourceMap.Parent.Spawned 
                || this.sourceMap.Parent.Destroyed)) || this.forceRemoveWorldObjectWhenMapRemoved;
            alsoRemoveWorldObject = result;
            if (!result && Prefs.DevMode)
            {
                Log.Message("CQF Map Destroy:No player pawn");
            }
            return result;
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            yield break;
        }
        public override IEnumerable<FloatMenuOption> GetTransportersFloatMenuOptions(IEnumerable<IThingHolder> pods, Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            yield break;
        }
        public override IEnumerable<FloatMenuOption> GetShuttleFloatMenuOptions(IEnumerable<IThingHolder> pods, Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            yield break;
        }
        public override void PostMapGenerate()
        {
            base.PostMapGenerate();
            CellIndices cellIndices = this.Map.cellIndices;
            GameTools.FogMap(this.Map);
            if (Current.ProgramState == ProgramState.Playing)
            {
                this.Map.roofGrid.Drawer.SetDirty();
            }
            foreach (IntVec3 loc in this.Map.AllCells)
            {
                this.Map.mapDrawer.MapMeshDirty(loc, MapMeshFlagDefOf.FogOfWar);
            }
        }

        public override void Notify_MyMapRemoved(Map map)
        {
            base.Notify_MyMapRemoved(map);
            if (Prefs.DevMode)
            {
                Log.Message("CQF Map Remove:" + this.MapName);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.mapDataDef, "CQF_MapParent_mapDataDef");
            Scribe_Values.Look(ref this.level, "CQF_MapParent_level");
            Scribe_Values.Look(ref this.permanent, "permanent");
            Scribe_Values.Look(ref this.customName, "CQF_MapParent_customName");
            Scribe_Values.Look(ref this.enterSpot, "CQF_MapParent_EnterSpot");
            Scribe_References.Look(ref this.entrance, "CQF_MapParent_entrance");
            Scribe_References.Look(ref this.exit, "CQF_MapParent_Exit");
            Scribe_References.Look(ref this.rootSite, "CQF_MapParent_rootSite");
            Scribe_References.Look(ref this.quest, "quest");   
            Scribe_Collections.Look(ref this.tags, "CQF_MapParent_tags",LookMode.Value);
        }

        public string customName = null;
        public int level = 0;
        public IntVec3 enterSpot;
        public CustomMapEntrance entrance;
        public CustomMapExit exit;
        public CustomMapDataDef mapDataDef;

        public CustomSite rootSite;
        public bool permanent;

        public List<string> tags = new List<string>();
        public Quest quest;
    }
}
