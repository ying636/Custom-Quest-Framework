using Verse;

namespace CQF.HibernationChecks;

public sealed class HibernationProbe : Thing
{
    public int Ticks => ticks;

    public override void TickRare()
    {
        ticks++;
    }

    public override void TickLong()
    {
        ticks++;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticks, "CQF_probeTicks");
    }

    protected override void Tick()
    {
        ticks++;
    }

    private int ticks;
}
