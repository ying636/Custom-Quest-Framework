#if RIMWORLD_1_7
using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library;

public sealed class CompProperties_CQFTrapMechanism : CompProperties
{
    public CompProperties_CQFTrapMechanism()
    {
        this.compClass = typeof(Comp_CQFTrapMechanism);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef))
            yield return error;
        if (this.program == null)
            yield return "A CQF trap mechanism requires a default program.";
        if (parentDef.tickerType != TickerType.Normal)
            yield return "A CQF trap mechanism requires a Normal ticker.";
    }

    public CQFTrapProgramDef program = null!;
}
#endif
