using Verse;

namespace QuestEditor_Library;

public class Comp_DomainProtection : ThingComp
{
    public Comp_DomainProtection(Hediff_DomainProtection owner)
    {
        protection = owner;
    }

    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = protection.Domain?.AbsorbDamage((Pawn)parent, ref dinfo) == true;
    }

    private readonly Hediff_DomainProtection protection;
}
