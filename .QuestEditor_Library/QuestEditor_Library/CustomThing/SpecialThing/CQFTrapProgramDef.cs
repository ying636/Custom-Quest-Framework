#if RIMWORLD_1_7
using System;
using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library;

public enum CQFTrapPattern
{
    Arrow,
    RollingStone,
    Spikes
}

public sealed class CQFTrapProgramDef : Def
{
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
            yield return error;

        if (!Enum.IsDefined(this.pattern))
            yield return "Unknown trap pattern.";
        if (this.damage == null)
            yield return "A trap program requires a DamageDef.";
        if (!float.IsFinite(this.damageAmount) || this.damageAmount <= 0f)
            yield return "Trap damage must be a positive finite number.";
        if (this.length is < 1 or > 64)
            yield return "Trap length must be between 1 and 64 cells.";
        if (this.warmupTicks < 0)
            yield return "Trap warmup cannot be negative.";
        if (this.cooldownTicks < 0)
            yield return "Trap cooldown cannot be negative.";

        if (this.pattern == CQFTrapPattern.Spikes)
        {
            if (this.activeGraphic == null)
                yield return "Spikes require an activeGraphic.";
            if (this.activeTicks < 1)
                yield return "Spikes must remain active for at least one tick.";
            yield break;
        }

        if (this.movingThing?.thingClass != typeof(CQFTrapMovingHazard)
            || this.movingThing.category != ThingCategory.Ethereal
            || this.movingThing.tickerType != TickerType.Normal)
        {
            yield return "A moving trap requires an ethereal CQFTrapMovingHazard with a Normal ticker.";
        }

        if (this.pulses is < 1 or > 32)
            yield return "A moving trap must fire between 1 and 32 pulses.";
        if (this.intervalTicks < 1)
            yield return "The interval between pulses must be positive.";
        if (this.ticksPerCell < 1)
            yield return "A moving trap must take at least one tick to cross a cell.";
    }

    public CQFTrapPattern pattern;
    public ThingDef movingThing = null!;
    public GraphicData activeGraphic = null!;
    public DamageDef damage = null!;
    public float damageAmount = 10f;
    public int length = 12;
    public int warmupTicks = 24;
    public int cooldownTicks = 100;
    public bool repeatWhileHeld;
    public int pulses = 1;
    public int intervalTicks = 12;
    public int ticksPerCell = 4;
    public int activeTicks = 48;
}
#endif
