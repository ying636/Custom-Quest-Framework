using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class AbilityCompProperties_DomainProtection : AbilityCompProperties
{
    public AbilityCompProperties_DomainProtection()
    {
        compClass = typeof(AbilityComp_DomainProtection);
    }

    public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (openingShield < 0f || float.IsNaN(openingShield) || float.IsInfinity(openingShield))
            yield return "Domain opening shield must be finite and nonnegative.";
    }

    public float openingShield = 1000f;
}
