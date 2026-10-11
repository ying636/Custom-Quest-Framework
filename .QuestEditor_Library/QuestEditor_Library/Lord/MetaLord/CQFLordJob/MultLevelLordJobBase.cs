using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public abstract class MultLevelLordJobBase : LordJob_ComplexCustom
    {
        public override bool ShouldExistWithoutPawns => this.metaLord != null && !this.metaLord.Disposed;
        public override bool AddFleeToil => false;
        public override Quest Quest => this.metaLord?.quest ?? base.Quest;

        public virtual float GetPawnAcceptScore(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != this.lord.faction || pawn.Dead || pawn.IsPrisoner ||
                (this.capacity > 0 && !this.lord.ownedPawns.Contains(pawn) && this.lord.ownedPawns.Count >= this.capacity))
                return -1f;
            return this.lord.ownedPawns.Contains(pawn) ? 101f :
                (this.groupName == "CQF_Transit" ? 0f : 100f / (this.lord.ownedPawns.Count + 1));
        }

        public override void Notify_AddedToLord()
        {
            base.Notify_AddedToLord();
            if (this.metaLord == null)
                throw new InvalidOperationException("[CQF] Multi-level Lord has no MetaLord.");
            this.metaLord.RegisterGroup(this.lord);
        }

        public override void Notify_PawnAdded(Pawn pawn)
        {
            base.Notify_PawnAdded(pawn);
            this.metaLord.NotifyMemberAdded(pawn);
            if (this.metaLord.Disposed || pawn.GetLord() != this.lord ||
                this.Map.GetComponent<MapComponent_CQFLord>().Manager.GetForPawn(pawn) != this.metaLord) return;
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance?.TryGetRuntime(pawn);
            if (runtime?.dutyMap == null) this.ApplyDefaultDutyMap(pawn);
            this.ApplyDuty(pawn);
        }

        public override bool ShouldRemovePawn(Pawn pawn, PawnLostCondition reason)
        {
            if (reason == PawnLostCondition.Incapped) return false;
            if (reason == PawnLostCondition.ExitedMap && pawn.Map == this.Map && pawn.Spawned) return false;
            return base.ShouldRemovePawn(pawn, reason);
        }

        public override void Notify_PawnLost(Pawn pawn, PawnLostCondition condition)
        {
            base.Notify_PawnLost(pawn, condition);
            if (condition == PawnLostCondition.ExitedMap && pawn.Map != null &&
                LevelPather.GetRoot(pawn.Map) == this.metaLord.rootMap)
                return;
            if (condition != PawnLostCondition.InMentalState && condition != PawnLostCondition.Drafted)
                this.metaLord.RemoveMember(pawn, condition.ToString());
        }

        public override void LordJobTick()
        {
            if (!this.announced)
            {
                this.announced = true;
                this.metaLord.Trigger(new LordEventSignal(LordEventSignalType.GroupAdded)
                { group = this.lord, targetMap = this.Map });
            }
            base.LordJobTick();
        }

        public override void ApplyDuty(Pawn pawn, Quest quest = null)
        {
            if (this.metaLord?.GetMove(pawn) != null)
            {
                pawn.mindState.duty = new Verse.AI.PawnDuty(QEDefOf.QE_Duty_MoveLevel);
                return;
            }
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance?.TryGetRuntime(pawn);
            if (runtime?.CurrentNode != null) base.ApplyDuty(pawn, quest);
            else if (pawn.Spawned)
                pawn.mindState.duty = new Verse.AI.PawnDuty(this.fallbackDuty ?? DutyDefOf.Defend, pawn.Position, 12f);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            this.metaLord?.UnregisterGroup(this.lord);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref this.metaLord, "metaLord");
            Scribe_Values.Look(ref this.groupName, "groupName");
            Scribe_Values.Look(ref this.capacity, "capacity");
            Scribe_Values.Look(ref this.announced, "announced");
            Scribe_Defs.Look(ref this.fallbackDuty, "fallbackDuty");
            Scribe_Collections.Look(ref this.groupTags, "groupTags", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                this.groupTags ??= new List<string>();
        }

        public MetaLord metaLord;
        [NoTranslate] public string groupName;
        public int capacity;
        public DutyDef fallbackDuty;
        public List<string> groupTags = new List<string>();
        private bool announced;
    }
}
