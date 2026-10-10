using Verse;

namespace CQF.HibernationChecks;

public sealed class HibernationMapProbe : MapComponent
{
    public HibernationMapProbe(Map map) : base(map)
    {
    }

    public int Ticks => ticks;
    public int Updates => updates;

    public override void MapComponentTick()
    {
        ticks++;
    }

    public override void MapComponentUpdate()
    {
        updates++;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref ticks, "CQF_mapProbeTicks");
        Scribe_Values.Look(ref updates, "CQF_mapProbeUpdates");
    }

    private int ticks;
    private int updates;
}
