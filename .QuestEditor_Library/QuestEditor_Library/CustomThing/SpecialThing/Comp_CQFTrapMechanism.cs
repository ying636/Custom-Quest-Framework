#if RIMWORLD_1_7
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Comp_CQFTrapMechanism : ThingComp
{
    private enum Phase
    {
        Idle,
        Warmup,
        Active,
        Cooldown
    }

    public CQFTrapProgramDef DefaultProgram => ((CompProperties_CQFTrapMechanism)this.props).program;

    public void Start(CQFTrapProgramDef? program = null, Pawn? trigger = null)
    {
        if (!this.enabled || this.phase != Phase.Idle || !this.parent.Spawned)
            return;

        program ??= this.DefaultProgram;
        if (program == null)
        {
            Log.WarningOnce("CQF_TrapProgramMissing".Translate(this.parent.def.defName), this.parent.thingIDNumber);
            return;
        }

        string? error = program.ConfigErrors().FirstOrDefault();
        if (error != null)
        {
            Log.WarningOnce("CQF_TrapProgramInvalid".Translate(program.defName, error), program.shortHash ^ 1943810426);
            return;
        }

        if (trigger != null && !program.repeatWhileHeld)
        {
            if (trigger == this.heldPawn)
                return;
            this.heldPawn = trigger;
            this.heldMap = trigger.Map;
            this.heldCell = trigger.Position;
        }

        this.program = program;
        this.phase = Phase.Warmup;
        this.ticksRemaining = program.warmupTicks;
        this.pulsesRemaining = program.pulses;
    }

    public void SetEnabled(bool value)
    {
        this.enabled = value;
        if (!value)
            this.ResetCycle();
    }

    public override void CompTick()
    {
        if (this.heldPawn != null && (!this.heldPawn.Spawned || this.heldPawn.Map != this.heldMap || this.heldPawn.Position != this.heldCell))
        {
            this.heldPawn = null;
            this.heldMap = null;
        }

        if (this.phase == Phase.Idle || !this.parent.Spawned)
            return;

        if (this.program == null)
        {
            Log.WarningOnce("CQF_TrapProgramMissing".Translate(this.parent.def.defName), this.parent.thingIDNumber);
            this.ResetCycle();
            return;
        }

        if (this.ticksRemaining > 0)
            this.ticksRemaining--;

        if (this.phase == Phase.Cooldown)
        {
            if (this.ticksRemaining == 0)
                this.ResetCycle();
            return;
        }

        if (this.phase == Phase.Warmup)
        {
            if (this.ticksRemaining > 0)
                return;
            this.phase = Phase.Active;
            this.ticksRemaining = this.program.pattern == CQFTrapPattern.Spikes ? this.program.activeTicks : 0;
        }

        if (this.program.pattern == CQFTrapPattern.Spikes)
        {
            this.DamageSpikeCells(this.program);
            if (this.phase == Phase.Active && this.ticksRemaining <= 1)
                this.BeginCooldown();
        }
        else if (this.ticksRemaining == 0)
        {
            this.Launch();
            if (this.phase != Phase.Active)
                return;
            this.pulsesRemaining--;
            if (this.pulsesRemaining <= 0)
                this.BeginCooldown();
            else
                this.ticksRemaining = this.program.intervalTicks;
        }
    }

    public override void PostDraw()
    {
        if (!this.parent.Spawned || this.program?.pattern != CQFTrapPattern.Spikes
            || (this.phase != Phase.Warmup && this.phase != Phase.Active))
            return;

        this.CollectSpikeCells(this.parent.Map);
        if (this.phase == Phase.Warmup)
        {
            GenDraw.DrawFieldEdges(this.spikeCells, Color.red);
            return;
        }

        Graphic graphic = this.program.activeGraphic.Graphic;
        foreach (IntVec3 cell in this.spikeCells)
        {
            Vector3 position = cell.ToVector3Shifted();
            position.y = this.parent.DrawPos.y + 0.02f;
            graphic.Draw(position, this.parent.Rotation, this.parent);
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref this.enabled, "enabled", true);
        Scribe_Values.Look(ref this.phase, "phase");
        Scribe_Values.Look(ref this.ticksRemaining, "ticksRemaining");
        Scribe_Values.Look(ref this.pulsesRemaining, "pulsesRemaining");
        Scribe_Defs.Look(ref this.program, "program");
        Scribe_References.Look(ref this.heldPawn, "heldPawn");
        Scribe_References.Look(ref this.heldMap, "heldMap");
        Scribe_Values.Look(ref this.heldCell, "heldCell");
        Scribe_Collections.Look(ref this.hitPawns, "hitPawns", LookMode.Value);
    }

    private void DamageSpikeCells(CQFTrapProgramDef activeProgram)
    {
        Map map = this.parent.Map;
        this.CollectSpikeCells(map);
        this.pendingHits.Clear();
        foreach (IntVec3 cell in this.spikeCells)
        {
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing is Pawn pawn && !pawn.Dead && !pawn.Flying
                    && (this.hitPawns ??= new HashSet<int>()).Add(pawn.thingIDNumber))
                {
                    this.pendingHits.Add(pawn);
                }
            }
        }
        foreach (Pawn pawn in this.pendingHits)
        {
            if (this.phase != Phase.Active || !this.parent.Spawned)
                break;
            if (!pawn.Dead && pawn.Spawned && pawn.Map == map && this.spikeCells.Contains(pawn.Position))
                pawn.TakeDamage(new DamageInfo(activeProgram.damage, activeProgram.damageAmount));
        }
        this.pendingHits.Clear();
    }

    private void CollectSpikeCells(Map map)
    {
        this.spikeCells.Clear();
        IntVec3 cell = this.parent.Position;
        IntVec3 step = this.parent.Rotation.FacingCell;
        for (int offset = 0; offset < this.program!.length && cell.InBounds(map); offset++, cell += step)
        {
            Building edifice = cell.GetEdifice(map);
            if (edifice is Building_Door door ? !door.Open : edifice?.def.passability == Traversability.Impassable)
                break;
            this.spikeCells.Add(cell);
        }
    }

    private void Launch()
    {
        CQFTrapMovingHazard movingHazard = (CQFTrapMovingHazard)ThingMaker.MakeThing(this.program!.movingThing);
        movingHazard.Initialize(this.program);
        GenSpawn.Spawn(movingHazard, this.parent.Position, this.parent.Map, this.parent.Rotation);
    }

    private void BeginCooldown()
    {
        if (this.program!.cooldownTicks == 0)
        {
            this.ResetCycle();
            return;
        }
        this.phase = Phase.Cooldown;
        this.ticksRemaining = this.program.cooldownTicks;
        this.hitPawns?.Clear();
    }

    private void ResetCycle()
    {
        this.phase = Phase.Idle;
        this.program = null;
        this.ticksRemaining = 0;
        this.pulsesRemaining = 0;
        this.hitPawns?.Clear();
    }

    private bool enabled = true;
    private Phase phase;
    private int ticksRemaining;
    private int pulsesRemaining;
    private CQFTrapProgramDef? program;
    private Pawn? heldPawn;
    private Map? heldMap;
    private IntVec3 heldCell;
    private HashSet<int>? hitPawns;
    private readonly List<IntVec3> spikeCells = new List<IntVec3>();
    private readonly List<Pawn> pendingHits = new List<Pawn>();
}
#endif
