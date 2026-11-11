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
        if (Current.ProgramState != ProgramState.Playing || GameComponent_MapManager.Instance?.Module is not CQFDomainRuntime runtime)
        {
            reason = "CQF_DomainLoading".Translate();
            return true;
        }
        Pawn pawn = parent.pawn;
        if (pawn.Dead || pawn.Downed || !pawn.Spawned)
        {
            reason = (pawn.Dead ? "CQF_DomainCasterDead" : pawn.Downed ? "CQF_DomainCasterDowned" : "CQF_DomainCasterNotSpawned").Translate();
            return true;
        }
        MapComponent_Domain domain = pawn.Map.GetComponent<MapComponent_Domain>();
        MapComponent_Domain? ownDomain = domain.FindDomainFor(pawn);
        if (ownDomain?.Active == true && ownDomain.Definition.defName != "CQF_TestDomain")
        {
            reason = "CQF_DomainAnotherActive".Translate(ownDomain.DisplayLabel);
            return true;
        }
        if (ownDomain?.Active == true && !ownDomain.Ready)
        {
            reason = "CQF_DomainEnding".Translate(ownDomain.DisplayLabel);
            return true;
        }
        if (pawn.Map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("CQF_DomainOpening"))
            .OfType<DomainOpening_Basic>().Any(opening => opening.Active && opening.Caster == pawn))
        {
            reason = "CQF_DomainAlreadyOpening".Translate();
            return true;
        }
        if (domain.HasActiveDomain && ownDomain?.Active != true && !domain.CanAcceptChallenger)
        {
            if (domain.InstanceCount >= MapComponent_Domain.MaximumDomains) reason = "CQF_DomainContestFull".Translate();
            else
            {
                MapComponent_Domain blocked = domain.Instances.First(side => side.Active && (!side.Ready || side.ArrivalLocked || side.Worker?.SupportsContests != true));
                reason = (!blocked.Ready ? "CQF_DomainEnding" : blocked.ArrivalLocked ? "CQF_DomainArrivalLocked" : "CQF_DomainContestsUnsupported").Translate(blocked.DisplayLabel);
            }
            return true;
        }
        if (!runtime.CanBeginOpening(pawn, DefDatabase<DomainDef>.GetNamed("CQF_TestDomain").OpeningRadius, out string reasonKey))
        {
            DomainTransition? current = runtime.Transition;
            if (reasonKey == "CQF_DomainOpeningOutOfRange" && current?.OpeningOwner is { } opening)
                reason = reasonKey.Translate(current.PresentationLabel, Math.Sqrt(opening.Position.DistanceToSquared(pawn.Position)).ToString("0.0"),
                    (opening.Radius + DefDatabase<DomainDef>.GetNamed("CQF_TestDomain").OpeningRadius).ToString("0.0"));
            else reason = reasonKey.Translate(current?.PresentationLabel ?? string.Empty);
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
        if (pawn.Map.GetComponent<MapComponent_Domain>().HasActiveDomain ? runtime.HasTerrainTemplate(definition) : runtime.HasAvailable(definition))
        {
            DomainOpening_Basic.Start(definition, pawn);
            return;
        }
        Game game = Current.Game;
        LongEventHandler.QueueLongEvent(() =>
        {
            if (Current.Game != game || !pawn.Spawned || pawn.Dead || pawn.Downed) return;
            Map realm = runtime.Acquire(definition, pawn);
            game.GetComponent<GameComponent_MapManager>().Hibernate(realm);
            DomainOpening_Basic.Start(definition, pawn);
        }, "GeneratingMap", false, null);
    }
}
