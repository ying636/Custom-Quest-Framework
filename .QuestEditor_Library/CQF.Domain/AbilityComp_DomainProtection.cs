using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class AbilityComp_DomainProtection : AbilityComp
{
    public override void Initialize(AbilityCompProperties props)
    {
        base.Initialize(props);
        Comp_DomainProtection.Ensure(parent.pawn);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        if (Scribe.mode == LoadSaveMode.PostLoadInit) Comp_DomainProtection.Ensure(parent.pawn);
    }
}
