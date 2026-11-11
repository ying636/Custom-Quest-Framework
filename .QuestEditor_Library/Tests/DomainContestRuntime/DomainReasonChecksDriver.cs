using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class DomainReasonChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE")!, "DomainReasons.txt");

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
        try
        {
            if (Time.realtimeSinceStartup > 180f) throw new TimeoutException("Domain reason checks timed out.");
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                DomainDef blood = DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain");
                if (!runtime.HasAvailable(basic) || !runtime.HasAvailable(blood)) return;
                Find.TickManager.Pause();
                using System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                File.AppendAllText(ResultPath, "PROCESS " + process.Id + " " + process.MainModule!.FileName + " " + Environment.CommandLine + "\n");
                source = Find.CurrentMap;
                first = MakePawn(source.Center);
                second = MakePawn(source.Center + new IntVec3(6, 0, 0));
                third = MakePawn(source.Center + new IntVec3(50, 0, 0));
                CheckButton(first, null);
                DomainOpening_Basic opening = DomainOpening_Basic.Start(basic, first);
                CheckButton(first, "CQF_DomainAlreadyOpening");
                DomainMaterialTransition transition = new(basic.visuals!, first.Position, false);
                runtime.SetTransition(transition);
                transition.BindOpening(opening);
                CheckButton(second, null);
                CheckButton(third, "CQF_DomainOpeningOutOfRange", transition.PresentationLabel, Math.Sqrt(opening.Position.DistanceToSquared(third.Position)).ToString("0.0"), (opening.Radius + basic.OpeningRadius).ToString("0.0"));
                Map spare = runtime.Acquire(basic, third);
                IntVec3 origin = third.Position;
                third.DeSpawn(); GenSpawn.Spawn(third, spare.Center, spare);
                CheckButton(third, "CQF_DomainTransitionOtherMap", transition.PresentationLabel);
                third.DeSpawn(); GenSpawn.Spawn(third, origin, source);
                GameComponent_MapManager.Instance!.Hibernate(spare);
                runtime.SetTransition(null);
                runtime.SetTransition(new DomainMaterialTransition(basic.visuals!, first.Position, true));
                runtime.Transition!.BindOpening(opening);
                CheckButton(second, "CQF_DomainReturnInProgress", runtime.Transition!.PresentationLabel);
                runtime.SetTransition(null);
                runtime.SetTransition(new DomainMaterialTransition(basic.visuals!, first.Position, false));
                runtime.Transition!.BindOpening(opening);
                CheckButton(second, "CQF_DomainTransitionInProgress", runtime.Transition!.PresentationLabel);
                runtime.SetTransition(null);
                opening = DomainOpening_Basic.Start(basic, first);
                DomainOpening_Basic other = DomainOpening_Basic.Start(basic, second);
                DomainOpening_Fusion fusion = DomainOpening_Fusion.Create(opening, other);
                transition = new DomainMaterialTransition(basic.visuals!, first.Position, false);
                runtime.SetTransition(transition); transition.BindOpening(fusion);
                CheckButton(third, "CQF_DomainTransitionInProgress", transition.PresentationLabel);
                runtime.SetTransition(null);
                foreach (DomainOpening remaining in source.listerThings.AllThings.OfType<DomainOpening>().ToList()) remaining.Destroy();
                third.DeSpawn(); CheckButton(third, "CQF_DomainCasterNotSpawned");
                GenSpawn.Spawn(third, origin, source);
                Hediff anesthetic = second.health.AddHediff(DefDatabase<HediffDef>.GetNamed("Anesthetic"));
                CheckButton(second, "CQF_DomainCasterDowned");
                second.health.RemoveHediff(anesthetic);
                Pawn dead = MakePawn(source.Center + new IntVec3(3, 0, 3));
                dead.Kill(null); CheckButton(dead, "CQF_DomainCasterDead");
                dead.Corpse?.Destroy();
                MapComponent_Domain field = runtime.EnterOrContest(blood, first, new[] { second, third });
                Current.Game.CurrentMap = field.map;
                CheckButton(first, "CQF_DomainAnotherActive", field.Instances.First(side => side.Caster == first).DisplayLabel);
                CheckButton(second, null);
                field.BeginArrivalTransition(); CheckButton(second, "CQF_DomainArrivalLocked", field.DisplayLabel); field.EndArrivalTransition();
                FieldInfo closing = typeof(MapComponent_Domain).GetField("closing", BindingFlags.NonPublic | BindingFlags.Instance)!;
                closing.SetValue(field, true); CheckButton(second, "CQF_DomainEnding", field.DisplayLabel); closing.SetValue(field, false);
                FieldInfo worker = typeof(MapComponent_Domain).GetField("worker", BindingFlags.NonPublic | BindingFlags.Instance)!;
                DomainWorker originalWorker = field.Worker!;
                worker.SetValue(field, new DomainWorker());
                CheckButton(second, "CQF_DomainContestsUnsupported", field.DisplayLabel);
                worker.SetValue(field, originalWorker);
                field = runtime.EnterOrContest(basic, second, Array.Empty<Pawn>()).Battlefield;
                Current.Game.CurrentMap = field.map;
                CheckButton(third, null);
                CheckButton(second, null);
                CheckButton(first, "CQF_DomainAnotherActive", field.Instances.First(side => side.Caster == first).DisplayLabel);
                field.RequestReturn("domain reason checks"); field.Challenger!.RequestReturn("domain reason checks");
                stage = 1;
            }
            else if (runtime.Transition == null && first!.Map == source && second!.Map == source && third!.Map == source)
            {
                first.Destroy(); second.Destroy(); third.Destroy();
                File.AppendAllText(ResultPath, "COMPLETE\n"); finished = true; Application.Quit(0);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Domain reason checks failed: " + error);
            finished = true; Application.Quit(1);
        }
    }

    private Pawn MakePawn(IntVec3 cell)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true));
        GenSpawn.Spawn(pawn, source!.GetComponent<MapComponent_Domain>().FindFreeCell(source, cell, true), source);
        pawn.drafter.Drafted = true;
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        pawn.abilities.GainAbility(DefDatabase<AbilityDef>.GetNamed("CQF_TestDomainExpansion"));
        return pawn;
    }

    private void CheckButton(Pawn pawn, string? key, params object[] arguments)
    {
        Ability ability = pawn.abilities.GetAbility(DefDatabase<AbilityDef>.GetNamed("CQF_TestDomainExpansion"));
        bool disabled = ability.GizmoDisabled(out string reason);
        string expected = key == null ? string.Empty : arguments.Length == 0 ? key.Translate().ToString() : key.Translate(arguments).ToString();
        if (disabled != (key != null) || key != null && (reason != expected || reason == key || reason.Contains("或")))
            throw new InvalidOperationException("Unexpected ability state for " + key + ": disabled=" + disabled + ", actual=" + reason + ", expected=" + expected);
        File.AppendAllText(ResultPath, "PASS " + (key ?? "Enabled") + ": " + reason + "\n");
    }

    private Map? source;
    private Pawn? first;
    private Pawn? second;
    private Pawn? third;
    private int stage;
    private bool finished;
}
