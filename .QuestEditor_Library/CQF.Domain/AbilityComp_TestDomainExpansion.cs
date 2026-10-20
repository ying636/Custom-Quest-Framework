using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class AbilityComp_TestDomainExpansion : CompAbilityEffect
{
    public override string ExtraTooltipPart()
    {
        MapComponent_Domain? domain = parent.pawn.MapHeld?.GetComponent<MapComponent_Domain>().FindDomainFor(parent.pawn);
        return domain?.Active == true && domain.Caster == parent.pawn
            ? domain.Health.ToString("N0") + " / " + domain.MaximumHealth.ToString("N0") : string.Empty;
    }

    public override bool GizmoDisabled(out string reason)
    {
        Pawn pawn = parent.pawn;
        if (!pawn.Spawned || pawn.Dead || pawn.Downed)
        {
            reason = "CQF_TestDomainCasterUnavailable".Translate();
            return true;
        }
        MapComponent_Domain domain = pawn.Map.GetComponent<MapComponent_Domain>();
        MapComponent_Domain? ownDomain = domain.FindDomainFor(pawn);
        if (!CQFDomainRuntime.Instance.CanBeginOpening(pawn) || domain.HasActiveDomain
            && (ownDomain?.Active == true ? ownDomain.Definition.defName != "CQF_TestDomain" || !ownDomain.Ready : !domain.CanAcceptChallenger)
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
        MapComponent_Domain? domain = pawn.Map.GetComponent<MapComponent_Domain>().FindDomainFor(pawn);
        if (domain?.Active == true)
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
