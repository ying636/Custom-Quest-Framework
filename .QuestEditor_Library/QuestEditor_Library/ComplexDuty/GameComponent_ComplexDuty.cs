using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class GameComponent_ComplexDuty : GameComponent
    {
        public GameComponent_ComplexDuty(Game game)
        {
            Instance = this;
        }

        public static GameComponent_ComplexDuty Component => Instance;

        private Dictionary<Pawn, DutyMapRuntime> Runtimes
        {
            get
            {
                if (this.runtimes == null)
                {
                    this.runtimes = new Dictionary<Pawn, DutyMapRuntime>();
                }
                return this.runtimes;
            }
        }

        public DutyMapRuntime GetRuntime(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }
            if (!this.Runtimes.TryGetValue(pawn, out DutyMapRuntime runtime))
            {
                runtime = new DutyMapRuntime();
                this.Runtimes.Add(pawn, runtime);
            }
            return runtime;
        }

        public void SetDutyMap(Pawn pawn, DutyMapDef dutyMap, Quest quest, bool useStartNode = true)
        {
            if (pawn == null || dutyMap == null)
            {
                return;
            }
            LordJob_ComplexCustom job = LordJob_ComplexCustom.EnsureForPawn(pawn);
            DutyMapRuntime runtime = this.GetRuntime(pawn);
            runtime.dutyMap = dutyMap;
            if (useStartNode || runtime.currentNodeId.NullOrEmpty() || dutyMap.GetNode(runtime.currentNodeId) == null)
            {
                runtime.currentNodeId = dutyMap.StartNode?.nodeId;
            }
            runtime.lastTransitionTick = Find.TickManager.TicksGame;
            job?.ApplyDuty(pawn, quest);
        }

        public void SetNode(Pawn pawn, string nodeId, Quest quest)
        {
            if (pawn == null || nodeId.NullOrEmpty())
            {
                return;
            }
            LordJob_ComplexCustom job = LordJob_ComplexCustom.EnsureForPawn(pawn);
            DutyMapRuntime runtime = this.GetRuntime(pawn);
            if (runtime?.dutyMap == null)
            {
                return;
            }
            job?.ChangeNode(pawn, nodeId, quest);
        }

        public void Remove(Pawn pawn)
        {
            if (pawn != null)
            {
                this.Runtimes.Remove(pawn);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.Runtimes.RemoveAll(pair => pair.Key == null || pair.Key.Destroyed || pair.Key.Dead || pair.Value?.dutyMap == null);
            }
            Scribe_Collections.Look(ref this.runtimes, "CQF_ComplexDuty_runtimes", LookMode.Reference, LookMode.Deep, ref this.tmpPawns, ref this.tmpRuntimes);
        }

        public static GameComponent_ComplexDuty Instance;

        private Dictionary<Pawn, DutyMapRuntime> runtimes = new Dictionary<Pawn, DutyMapRuntime>();
        private List<Pawn> tmpPawns;
        private List<DutyMapRuntime> tmpRuntimes;
    }

    public class DutyMapRuntime : IExposable
    {
        public DutyMapNode CurrentNode => this.dutyMap?.GetNode(this.currentNodeId) ?? this.dutyMap?.StartNode;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.dutyMap, "dutyMap");
            Scribe_Values.Look(ref this.currentNodeId, "currentNodeId");
            Scribe_Values.Look(ref this.lastTransitionTick, "lastTransitionTick");
        }

        public DutyMapDef dutyMap;
        public string currentNodeId;
        public int lastTransitionTick;
    }
}
