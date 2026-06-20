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

        private Dictionary<Pawn, CustomDutyMap> Runtimes
        {
            get
            {
                if (this.runtimes == null)
                {
                    this.runtimes = new Dictionary<Pawn, CustomDutyMap>();
                }
                return this.runtimes;
            }
        }

        public CustomDutyMap GetRuntime(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }
            if (!this.Runtimes.TryGetValue(pawn, out CustomDutyMap runtime))
            {
                runtime = new CustomDutyMap();
                this.Runtimes.Add(pawn, runtime);
            }
            runtime.SetPawn(pawn);
            return runtime;
        }

        public void SetDutyMap(Pawn pawn, DutyMapDef dutyMap, Quest quest, bool useStartNode = true)
        {
            if (pawn == null || dutyMap == null)
            {
                return;
            }
            LordJob_ComplexCustom job = LordJob_ComplexCustom.EnsureForPawn(pawn);
            CustomDutyMap runtime = this.GetRuntime(pawn);
            runtime.dutyMap = dutyMap;
            runtime.RegisterSignalReceiver();
            if (useStartNode || runtime.currentNodeId.NullOrEmpty() || dutyMap.GetNode(runtime.currentNodeId) == null)
            {
                runtime.currentNodeId = dutyMap.StartNode?.nodeId;
            }
            runtime.lastTransitionTick = Find.TickManager.TicksGame;
            job?.ApplyDuty(pawn, quest);
            job?.TryRunTickTransition(pawn, quest);
        }

        public void SetNode(Pawn pawn, string nodeId, Quest quest)
        {
            if (pawn == null || nodeId.NullOrEmpty())
            {
                return;
            }
            LordJob_ComplexCustom job = LordJob_ComplexCustom.EnsureForPawn(pawn);
            CustomDutyMap runtime = this.GetRuntime(pawn);
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
                if (this.Runtimes.TryGetValue(pawn, out CustomDutyMap runtime))
                {
                    runtime.DeregisterSignalReceiver();
                }
                this.Runtimes.Remove(pawn);
            }
        }

        public void NotifyPawnDamaged(Pawn pawn, DamageInfo dinfo)
        {
            CustomDutyMap runtime = this.GetRuntime(pawn);
            if (runtime?.dutyMap == null)
            {
                return;
            }
            runtime.lastDamageTick = Find.TickManager.TicksGame;
            LordJob_ComplexCustom.GetForPawn(pawn)?.TryRunTriggeredTransition(pawn, null, null, typeof(CustomDutyTrigger_Damaged));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.Runtimes.RemoveAll(pair => pair.Key == null || pair.Key.Destroyed || pair.Key.Dead || pair.Value?.dutyMap == null);
            }
            Scribe_Collections.Look(ref this.runtimes, "CQF_ComplexDuty_runtimes", LookMode.Reference, LookMode.Deep, ref this.tmpPawns, ref this.tmpRuntimes);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.runtimes != null)
            {
                foreach (KeyValuePair<Pawn, CustomDutyMap> pair in this.runtimes)
                {
                    pair.Value?.SetPawn(pair.Key);
                    pair.Value?.RegisterSignalReceiver();
                }
            }
        }

        public static GameComponent_ComplexDuty Instance;

        private Dictionary<Pawn, CustomDutyMap> runtimes = new Dictionary<Pawn, CustomDutyMap>();
        private List<Pawn> tmpPawns;
        private List<CustomDutyMap> tmpRuntimes;
    }

}

