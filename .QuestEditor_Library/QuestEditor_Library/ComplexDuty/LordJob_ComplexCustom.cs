using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class LordJob_ComplexCustom : LordJob
    {
        public override bool KeepExistingWhileHasAnyBuilding => true;
        public override bool AlwaysShowWeapon => true;

        public Quest Quest => GameTools.GetQuestFromMap(this.Map);

        public override StateGraph CreateGraph()
        {
            StateGraph result = new StateGraph();
            result.AddToil(new LordToil_ComplexCustom());
            return result;
        }

        public override void LordJobTick()
        {
            base.LordJobTick();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.defaultDutyMap, "defaultDutyMap");
            Scribe_Values.Look(ref this.defaultStartNodeId, "defaultStartNodeId");
        }

        public void ApplyDefaultDutyMap(Pawn pawn, Quest quest = null)
        {
            if (pawn == null || this.defaultDutyMap == null)
            {
                return;
            }
            GameComponent_ComplexDuty.Instance.SetDutyMap(pawn, this.defaultDutyMap, quest ?? this.Quest, true);
            if (!this.defaultStartNodeId.NullOrEmpty() && this.defaultDutyMap.GetNode(this.defaultStartNodeId) != null)
            {
                this.ChangeNode(pawn, this.defaultStartNodeId, quest ?? this.Quest);
            }
        }

        public void ApplyDuty(Pawn pawn, Quest quest = null)
        {
            DutyMapRuntime runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
            DutyMapNode node = runtime?.CurrentNode;
            if (node != null)
            {
                pawn.mindState.duty = node.MakeDuty(pawn, quest ?? this.Quest);
            }
        }

        public void ChangeNode(Pawn pawn, string nodeId, Quest quest = null)
        {
            DutyMapRuntime runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
            if (runtime?.dutyMap == null || nodeId.NullOrEmpty())
            {
                return;
            }
            DutyMapNode oldNode = runtime.CurrentNode;
            runtime.currentNodeId = nodeId;
            runtime.lastTransitionTick = Find.TickManager.TicksGame;
            DutyMapNode newNode = runtime.CurrentNode;
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo> { ["Target"] = new TargetInfo(pawn) };
            Quest contextQuest = quest ?? this.Quest;
            oldNode?.exitActions?.ForEach(action => action.Work(targets, contextQuest));
            newNode?.enterActions?.ForEach(action => action.Work(targets, contextQuest));
            this.ApplyDuty(pawn, contextQuest);
        }

        public bool TryChangeByTransition(Pawn pawn, string fromNodeId, string toNodeId, Quest quest = null)
        {
            return this.TryChangeByTransition(pawn, fromNodeId, toNodeId, quest, new Dictionary<string, TargetInfo>());
        }

        public bool TryChangeByTransition(Pawn pawn, string fromNodeId, string toNodeId, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            DutyMapRuntime runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
            if (runtime?.dutyMap == null || fromNodeId.NullOrEmpty() || toNodeId.NullOrEmpty())
            {
                return false;
            }
            string current = runtime.currentNodeId.NullOrEmpty() ? runtime.dutyMap.StartNode?.nodeId : runtime.currentNodeId;
            if (current != fromNodeId)
            {
                return false;
            }
            DutyMapTransition transition = runtime.dutyMap.TransitionsFrom(fromNodeId).FirstOrDefault(t => t.toNodeId == toNodeId);
            Quest contextQuest = quest ?? this.Quest;
            if (transition == null || !transition.CanTransition(pawn, runtime, contextQuest, targets ?? new Dictionary<string, TargetInfo>()))
            {
                return false;
            }
            this.ChangeNode(pawn, toNodeId, contextQuest);
            return true;
        }

        public static LordJob_ComplexCustom EnsureForPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
            {
                return null;
            }
            if (pawn.GetLord() is Lord lord)
            {
                if (lord.LordJob is LordJob_ComplexCustom complexJob)
                {
                    return complexJob;
                }
                complexJob = new LordJob_ComplexCustom();
                lord.SetJob(complexJob);
                lord.GotoToil(lord.Graph.StartingToil);
                return complexJob;
            }
            Lord newLord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_ComplexCustom(), pawn.Map);
            newLord.AddPawn(pawn);
            newLord.GotoToil(newLord.Graph.StartingToil);
            return newLord.LordJob as LordJob_ComplexCustom;
        }

        public static LordJob_ComplexCustom GetForPawn(Pawn pawn)
        {
            if (pawn?.GetLord()?.LordJob is LordJob_ComplexCustom complexJob)
            {
                return complexJob;
            }
            return null;
        }

        public DutyMapDef defaultDutyMap;
        public string defaultStartNodeId;
    }
}

