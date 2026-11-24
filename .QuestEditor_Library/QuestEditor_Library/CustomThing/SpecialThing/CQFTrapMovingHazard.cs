#if RIMWORLD_1_7
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFTrapMovingHazard : ThingWithComps
{
    public override Vector3 DrawPos
    {
        get
        {
            Vector3 drawPos = base.DrawPos;
            if (this.program != null)
            {
                int ticksFromCenter = this.ticksInCell;
                if (this.cellsRemaining < this.program.length)
                    ticksFromCenter -= this.program.ticksPerCell / 2;
                float progress = ticksFromCenter / (float)this.program.ticksPerCell;
                drawPos += this.Rotation.FacingCell.ToVector3() * progress;
            }
            return drawPos;
        }
    }

    public void Initialize(CQFTrapProgramDef program)
    {
        this.program = program;
        this.cellsRemaining = program.length;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref this.program, "program");
        Scribe_Values.Look(ref this.cellsRemaining, "cellsRemaining");
        Scribe_Values.Look(ref this.ticksInCell, "ticksInCell");
        Scribe_Collections.Look(ref this.hitPawns, "hitPawns", LookMode.Value);
    }

    protected override void Tick()
    {
        base.Tick();
        if (!this.Spawned)
            return;
        if (this.program == null)
        {
            Log.WarningOnce("CQF_TrapProgramMissing".Translate(this.def.defName), this.thingIDNumber);
            this.Destroy();
            return;
        }

        Map map = this.Map;
        bool atOrigin = this.cellsRemaining == this.program.length;
        int ticksToCross = atOrigin ? this.program.ticksPerCell - this.program.ticksPerCell / 2 : this.program.ticksPerCell;
        this.ticksInCell++;
        if (this.ticksInCell < ticksToCross)
        {
            if (!atOrigin)
                this.CheckCollision(this.Position, map);
            return;
        }

        this.ticksInCell = 0;
        IntVec3 next = this.Position + this.Rotation.FacingCell;
        if (!next.InBounds(map) || this.CheckCollision(next, map))
        {
            if (!this.Destroyed)
                this.Destroy();
            return;
        }

        if (!this.Spawned)
            return;
        this.Position = next;
        this.cellsRemaining--;
        if (this.cellsRemaining == 0)
            this.Destroy();
    }

    private bool CheckCollision(IntVec3 cell, Map map)
    {
        Building edifice = cell.GetEdifice(map);
        if (edifice is Building_Door door ? !door.Open : edifice?.def.passability == Traversability.Impassable)
        {
            this.Destroy();
            return true;
        }

        List<Thing> things = cell.GetThingList(map);
        if (this.program!.pattern == CQFTrapPattern.Arrow)
        {
            foreach (Thing thing in things)
            {
                if (thing is not Pawn pawn || pawn.Dead)
                    continue;
                pawn.TakeDamage(new DamageInfo(this.program.damage, this.program.damageAmount));
                if (!this.Destroyed)
                    this.Destroy();
                return true;
            }
            return false;
        }

        this.pendingHits.Clear();
        foreach (Thing thing in things)
        {
            if (thing is Pawn pawn && !pawn.Dead && (this.hitPawns ??= new HashSet<int>()).Add(pawn.thingIDNumber))
                this.pendingHits.Add(pawn);
        }
        foreach (Pawn pawn in this.pendingHits)
        {
            if (!pawn.Dead && pawn.Spawned && pawn.Map == map && pawn.Position == cell)
                pawn.TakeDamage(new DamageInfo(this.program.damage, this.program.damageAmount));
        }
        this.pendingHits.Clear();
        return false;
    }

    private CQFTrapProgramDef? program;
    private int cellsRemaining;
    private int ticksInCell;
    private HashSet<int>? hitPawns;
    private readonly List<Pawn> pendingHits = new List<Pawn>();
}
#endif
