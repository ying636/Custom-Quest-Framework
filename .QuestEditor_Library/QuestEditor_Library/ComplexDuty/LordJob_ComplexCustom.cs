using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
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
            if (this.lord?.ownedPawns == null || this.lord.ownedPawns.Count == 0)
            {
                return;
            }
            int tick = Find.TickManager.TicksGame;
            foreach (Pawn pawn in this.lord.ownedPawns.ListFullCopy())
            {
                CustomDutyMap runtime = GameComponent_ComplexDuty.Instance?.GetRuntime(pawn);
                if (runtime?.dutyMap == null)
                {
                    continue;
                }
                if (runtime.nextTickTransitionTick > tick)
                {
                    continue;
                }
                this.TryRunTickTransition(pawn, this.Quest);
            }
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
            else
            {
                this.RefreshTickTransition(pawn, quest ?? this.Quest);
            }
        }

        public void ApplyDuty(Pawn pawn, Quest quest = null)
        {
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
            DutyMapNode node = runtime?.CurrentNode;
            if (node != null)
            {
                pawn.mindState.duty = node.MakeDuty(pawn, quest ?? this.Quest);
            }
        }

        public void ChangeNode(Pawn pawn, string nodeId, Quest quest = null)
        {
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
            if (runtime?.dutyMap == null || nodeId.NullOrEmpty())
            {
                return;
            }
            DutyMapNode oldNode = runtime.CurrentNode;
            runtime.currentNodeId = nodeId;
            runtime.lastTransitionTick = Find.TickManager.TicksGame;
            DutyMapNode newNode = runtime.CurrentNode;
            Quest contextQuest = quest ?? this.Quest;
            Dictionary<string, TargetInfo> targets = this.MakeTargets(pawn);
            oldNode?.exitActions?.ForEach(action => action.Work(targets, contextQuest));
            newNode?.enterActions?.ForEach(action => action.Work(targets, contextQuest));
            this.ApplyDuty(pawn, contextQuest);
            this.RefreshTickTransition(pawn, contextQuest);
        }

        public bool TryChangeByTransition(Pawn pawn, string fromNodeId, string toNodeId, Quest quest = null)
        {
            return this.TryChangeByTransition(pawn, fromNodeId, toNodeId, quest, this.MakeTargets(pawn));
        }

        public bool TryChangeByTransition(Pawn pawn, string fromNodeId, string toNodeId, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
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
            Dictionary<string, TargetInfo> contextTargets = this.MakeTargets(pawn, targets);
            if (transition == null || !transition.CanTransition(pawn, runtime, contextQuest, contextTargets))
            {
                return false;
            }
            this.ChangeNode(pawn, toNodeId, contextQuest);
            return true;
        }

        public bool TryRunTriggeredTransition(Pawn pawn, Quest quest = null)
        {
            return this.TryRunTriggeredTransition(pawn, quest, this.MakeTargets(pawn), null);
        }

        public bool TryRunTriggeredTransition(Pawn pawn, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            return this.TryRunTriggeredTransition(pawn, quest, targets, null);
        }

        public bool TryRunTriggeredTransition(Pawn pawn, Quest quest, Dictionary<string, TargetInfo> targets, System.Type triggerType)
        {
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
            if (runtime?.dutyMap == null)
            {
                return false;
            }
            string current = runtime.currentNodeId.NullOrEmpty() ? runtime.dutyMap.StartNode?.nodeId : runtime.currentNodeId;
            if (current.NullOrEmpty())
            {
                return false;
            }
            Quest contextQuest = quest ?? this.Quest;
            Dictionary<string, TargetInfo> contextTargets = this.MakeTargets(pawn, targets);
            foreach (DutyMapTransition transition in runtime.dutyMap.TransitionsFrom(current))
            {
                if (triggerType != null && transition.triggers?.Any(trigger => trigger.GetType() == triggerType) != true)
                {
                    continue;
                }
                if (transition.CanTransition(pawn, runtime, contextQuest, contextTargets))
                {
                    this.ChangeNode(pawn, transition.toNodeId, contextQuest);
                    return true;
                }
            }
            return false;
        }

        public bool TryRunTickTransition(Pawn pawn, Quest quest = null)
        {
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance.GetRuntime(pawn);
            if (runtime?.dutyMap == null)
            {
                return false;
            }
            string current = runtime.currentNodeId.NullOrEmpty() ? runtime.dutyMap.StartNode?.nodeId : runtime.currentNodeId;
            if (current.NullOrEmpty())
            {
                return false;
            }
            Quest contextQuest = quest ?? this.Quest;
            Dictionary<string, TargetInfo> targets = this.MakeTargets(pawn);
            bool tried = false;
            foreach (DutyMapTransition transition in runtime.dutyMap.TransitionsFrom(current))
            {
                if (transition.triggers?.Any(trigger => trigger is CustomDutyTrigger_TickInterval) != true)
                {
                    continue;
                }
                tried = true;
                if (transition.CanTransition(pawn, runtime, contextQuest, targets))
                {
                    this.ChangeNode(pawn, transition.toNodeId, contextQuest);
                    return true;
                }
            }
            if (tried)
            {
                this.RefreshTickTransition(pawn, contextQuest);
            }
            return false;
        }

        public void RemovePawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance?.GetRuntime(pawn);
            if (runtime != null)
            {
                runtime.nextTickTransitionTick = -1;
            }
        }

        private Dictionary<string, TargetInfo> MakeTargets(Pawn pawn, Dictionary<string, TargetInfo> targets = null)
        {
            Dictionary<string, TargetInfo> result = targets == null
                ? new Dictionary<string, TargetInfo>()
                : new Dictionary<string, TargetInfo>(targets);
            if (pawn != null && !result.ContainsKey("Target"))
            {
                result["Target"] = new TargetInfo(pawn);
            }
            return result;
        }

        private void RefreshTickTransition(Pawn pawn, Quest quest)
        {
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance?.GetRuntime(pawn);
            if (runtime?.dutyMap == null)
            {
                return;
            }
            string current = runtime.currentNodeId.NullOrEmpty() ? runtime.dutyMap.StartNode?.nodeId : runtime.currentNodeId;
            if (current.NullOrEmpty())
            {
                runtime.nextTickTransitionTick = -1;
                return;
            }
            int interval = -1;
            foreach (DutyMapTransition transition in runtime.dutyMap.TransitionsFrom(current))
            {
                if (transition?.triggers == null)
                {
                    continue;
                }
                foreach (CustomDutyTrigger_TickInterval trigger in transition.triggers.OfType<CustomDutyTrigger_TickInterval>())
                {
                    if (trigger.intervalTicks > 0)
                    {
                        interval = interval < 0 ? trigger.intervalTicks : Mathf.Min(interval, trigger.intervalTicks);
                    }
                }
            }
            runtime.nextTickTransitionTick = interval > 0 ? Find.TickManager.TicksGame + interval : -1;
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
