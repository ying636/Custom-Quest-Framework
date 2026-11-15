using Verse;

namespace QuestEditor_Library;

public sealed class CQFPaletteSelection : IExposable
{
    public void ExposeData()
    {
        Scribe_Values.Look(ref this.thingDef, "thingDef", string.Empty);
        Scribe_Values.Look(ref this.stuffDef, "stuffDef", string.Empty);
    }

    public string thingDef = string.Empty;
    public string stuffDef = string.Empty;
}
