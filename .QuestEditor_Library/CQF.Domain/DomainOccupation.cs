using Verse;

namespace QuestEditor_Library;

public sealed class DomainOccupation : IExposable
{
    public int Ticks => ticks;

    public bool Advance(int tick, bool eligible)
    {
        if (!eligible) { ticks = 0; lastTick = tick; return false; }
        if (lastTick == tick) return false;
        if (lastTick >= 0 && tick != lastTick + 1) ticks = 0;
        lastTick = tick;
        if (++ticks < 180) return false;
        ticks = 0;
        return true;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref ticks, "ticks");
        Scribe_Values.Look(ref lastTick, "lastTick", -1);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (ticks < 0 || ticks >= 180 || lastTick < -1))
            throw new InvalidOperationException("[CQF] Invalid saved domain occupation timer.");
    }

    private int ticks;
    private int lastTick = -1;
}
