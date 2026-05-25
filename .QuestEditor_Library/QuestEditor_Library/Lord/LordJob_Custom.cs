using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class LordJob_Custom : LordJob
    {
        public override bool KeepExistingWhileHasAnyBuilding => true;
        public override StateGraph CreateGraph()
        {
            StateGraph result = new StateGraph();
            result.AddToil(new LordToil_Custom());
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving) 
            {
                this.pawnRouteDatas.RemoveAll(d => d.Key == null || d.Key.Dead);
                this.pawnDutyDatas.RemoveAll(d => d.Key == null || d.Key.Dead);
                this.defendDatas.RemoveAll(d => d.Key == null || d.Key.Dead);
            }
            Scribe_Collections.Look(ref this.pawnRouteDatas, "QE_LordJob_DefendAndPatrol_pawnRouteDatas", LookMode.Reference, LookMode.Deep, ref this.tmpPawns, ref this.tmpRoutes);
            Scribe_Collections.Look(ref this.pawnDutyDatas, "QE_LordJob_DefendAndPatrol_pawnDutyDatas", LookMode.Reference, LookMode.Def, ref this.tmpPawns_Duty, ref this.tmpDuties);
            Scribe_Collections.Look(ref this.defendDatas, "defendDatas",
                LookMode.Reference, LookMode.Value, ref this.tmpPawns_Defend, ref this.tmpPoss);

        }

        public Dictionary<Pawn, DutyDef> pawnDutyDatas = new Dictionary<Pawn, DutyDef>();
        public List<Pawn> tmpPawns_Duty = new List<Pawn>();
        public List<DutyDef> tmpDuties = new List<DutyDef>();
        public Dictionary<Pawn, RouteData> pawnRouteDatas = new Dictionary<Pawn, RouteData>();
        public List<Pawn> tmpPawns = new List<Pawn>();
        public List<RouteData> tmpRoutes = new List<RouteData>();
        public Dictionary<Pawn, IntVec3> defendDatas = new Dictionary<Pawn, IntVec3>();
        public List<Pawn> tmpPawns_Defend = new List<Pawn>();
        public List<IntVec3> tmpPoss = new List<IntVec3>();
    }
    public class RouteData : IExposable
    {
        public RouteData() { }
        public RouteData(List<IntVec3> routue) 
        {
            this.routue = routue;
            this.cur = routue.First();
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.cur, "QE_RouteData_cur");
            Scribe_Collections.Look(ref this.routue, "QE_RouteData_routue", LookMode.Value);
        }

        public IntVec3 cur;
        public List<IntVec3> routue;
    }
    public class PawnDataWithPosAndTime : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.time, "QE_PawnDataWithPosAndTime_time");
            Scribe_Values.Look(ref this.position, "QE_PawnDataWithPosAndTime_position");
            Scribe_Deep.Look(ref this.data, "QE_PawnDataWithPosAndTime_data");
        }

        public int time = 0;
        public IntVec3 position;
        public PawnSpawnData data;
    }
}
