using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class AbilityComp_TestDomainExpansion : CompAbilityEffect
{
    public override bool GizmoDisabled(out string reason)
    {
        Pawn pawn = parent.pawn;
        if (!pawn.Spawned || pawn.Dead || pawn.Downed)
        {
            reason = "CQF_TestDomainCasterUnavailable".Translate();
            return true;
        }
        MapComponent_Domain domain = pawn.Map.GetComponent<MapComponent_Domain>();
        if (CQFDomainRuntime.Instance.Transition != null || domain.Active
            && (domain.Definition.defName != "CQF_TestDomain" || domain.Caster != pawn || !domain.Ready)
            || pawn.Map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("CQF_DomainOpening"))
                .OfType<DomainOpening_Basic>().Any(opening => opening.Active && opening.Caster == pawn))
        {
            reason = "CQF_TestDomainBusy".Translate();
            return true;
        }
        reason = string.Empty;
        return false;
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        bool valid = !GizmoDisabled(out string reason);
        if (!valid && throwMessages) Messages.Message(reason, parent.pawn, MessageTypeDefOf.RejectInput, false);
        return valid;
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        if (!Valid(target, true)) return;
        base.Apply(target, dest);
        Pawn pawn = parent.pawn;
        MapComponent_Domain domain = pawn.Map.GetComponent<MapComponent_Domain>();
        if (domain.Active)
        {
            domain.RequestReturn();
            return;
        }
        DomainDef definition = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
        CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
        if (runtime.HasAvailable(definition))
        {
            DomainOpening_Basic.Start(definition, pawn, Array.Empty<Pawn>());
            return;
        }
        Game game = Current.Game;
        LongEventHandler.QueueLongEvent(() =>
        {
            if (Current.Game != game || !pawn.Spawned || pawn.Dead || pawn.Downed) return;
            Map realm = runtime.Acquire(definition, pawn);
            game.GetComponent<GameComponent_MapManager>().Hibernate(realm);
            DomainOpening_Basic.Start(definition, pawn, Array.Empty<Pawn>());
        }, "GeneratingMap", false, null);
    }
}
