using Verse;

internal sealed class PauseProbeThing : Thing
{
    public int Ticks => ticks;

    public override void TickRare() => ticks++;
    public override void TickLong() => ticks++;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticks, "CQF_Check_ticks");
    }
    protected override void Tick() => ticks++;

    private int ticks;
}
