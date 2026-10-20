using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class TrappingChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
    private DomainDef Basic => DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
    private DomainDef Opponent => DefDatabase<DomainDef>.GetNamed(GenCommandLine.CommandLineArgPassed("cqfcontestbloodsea")
        ? "RSH_BloodSeaDomain" : "CQF_TestDomain");
    private bool FirstLoses => caseIndex % 2 == 0 || caseIndex == 5;
    private MapComponent_Domain Loser => FirstLoses ? battlefield! : battlefield!.Challenger!;
    private MapComponent_Domain Winner => FirstLoses ? battlefield!.Challenger! : battlefield!;

    public void Resume(int firstId, int secondId, int savedCase)
    {
        caseIndex = savedCase;
        battlefield = CQFDomainRuntime.Instance.GetInstances(Basic).First(side => side.Caster?.thingIDNumber == firstId);
        firstPawn = battlefield.Caster;
        secondPawn = battlefield.Challenger!.Caster;
        Check(secondPawn!.thingIDNumber == secondId, "loaded captive battlefield retains both caster identities");
        source = battlefield.SourceMap!;
        firstOrigin = battlefield.Participants.Single(entry => entry.pawn == firstPawn).originalPosition;
        secondOrigin = battlefield.Challenger.Participants.Single(entry => entry.pawn == secondPawn).originalPosition;
        casualty = battlefield.Participants.First(entry => entry.pawn != firstPawn).pawn;
        stage = 3;
    }

    private void Update()
    {
        if (finished || stage == 99 || LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
        try
        {
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (Time.realtimeSinceStartup > 270f) throw new TimeoutException("Trapping checks case " + caseIndex + ", stage " + stage);
            if (stage == 0)
            {
                if (!runtime.HasAvailable(Basic) || !runtime.HasAvailable(Opponent)) return;
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                IntVec3 focus = source.mapPawns.FreeColonistsSpawned.First().Position;
                firstPawn = MakePawn(focus);
                secondPawn = MakePawn(focus + new IntVec3(8, 0, 0));
                casualty = MakePawn(focus + new IntVec3(3, 0, 3));
                firstOrigin = firstPawn.Position;
                secondOrigin = secondPawn.Position;
                battlefield = runtime.EnterOrContest(Basic, firstPawn, new[] { secondPawn, casualty });
                runtime.EnterOrContest(Opponent, secondPawn, Array.Empty<Pawn>());
                casualty.Kill(null);
                Current.Game.CurrentMap = battlefield.map;
                frames = 0;
                stage = 1;
            }
            else if (stage == 1)
            {
                Find.CameraDriver.SetRootPosAndSize(battlefield!.map.Center.ToVector3Shifted(), 18f);
                if (++frames < 5) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Trapping_Border_" + caseIndex + ".png"));
                if (caseIndex == 4)
                {
                    battlefield.DrainHealth(battlefield.Health);
                    battlefield.Challenger!.DrainHealth(battlefield.Challenger.Health);
                    Current.Game.CurrentMap = source;
                    stage = 4;
                    return;
                }
                if (caseIndex == 5) firstPawn!.Kill(null);
                if (caseIndex < 2) Loser.RequestReturn("trapping checks manual cancellation");
                else Loser.DrainHealth(Loser.Health);
                stage = 2;
            }
            else if (stage == 2)
            {
                if (!Loser.Trapped) return;
                CheckCaptured();
                GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                saved.Record(firstPawn!.thingIDNumber, secondPawn!.thingIDNumber);
                saved.TrappingCase = caseIndex;
                GameDataSaveLoader.SaveGame("CQFTrappingChecks");
                stage = 99;
                GameDataSaveLoader.LoadGame("CQFTrappingChecks");
            }
            else if (stage == 3)
            {
                CheckCaptured();
                Check((firstPawn!.Dead ? firstPawn.Corpse!.Position : firstPawn.Position).IsValid && casualty!.Corpse?.Map == battlefield!.map,
                    "full reload keeps captives and corpses inside the active opposing domain");
                Current.Game.CurrentMap = source;
                Winner.RequestReturn("trapping checks final recall");
                stage = 4;
            }
            else if (stage == 4)
            {
                if ((firstPawn!.Dead ? firstPawn.Corpse?.Map : firstPawn.Map) != source || secondPawn!.Map != source
                    || !GameComponent_MapManager.Instance!.IsHibernating(battlefield!.map)) return;
                Check((firstPawn.Dead ? firstPawn.Corpse!.Position : firstPawn.Position) == firstOrigin && secondPawn.Position == secondOrigin,
                    "last domain releases both sides to their own original coordinates");
                Check(casualty!.Corpse?.Map == source, "last domain also returns the captive corpse");
                Check(battlefield!.Available && !battlefield.Trapped && battlefield.Challenger == null,
                    "final recall clears captive records and hibernates the battlefield");
                if (firstPawn.Dead) firstPawn.Corpse!.Destroy();
                else firstPawn.Destroy();
                secondPawn.Destroy();
                casualty.Corpse!.Destroy();
                if (++caseIndex == 6) { Finish(true); return; }
                stage = 0;
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Trapping runtime checks failed: " + error);
            Finish(false);
        }
    }

    private Pawn MakePawn(IntVec3 near)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        GenSpawn.Spawn(pawn, source!.GetComponent<MapComponent_Domain>().FindFreeCell(source, near, true), source);
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        return pawn;
    }

    private void CheckCaptured()
    {
        Check(!Loser.Active && Loser.Trapped && Winner.Active
            && (firstPawn!.Dead ? firstPawn.Corpse?.Map : firstPawn.Map) == battlefield!.map && secondPawn!.Map == battlefield.map,
            "case " + caseIndex + " cancellation or collapse leaves both sides trapped on the same battlefield");
        Check(RuntimeTransitionAbsent(), "dissolution starts no return transition or camera transfer");
        Check(battlefield!.map.AllCells.All(cell => Winner.OwnsCell(cell)
            && cell.GetTerrain(battlefield.map) == battlefield.Contest!.TerrainAt(Winner, cell)),
            "surviving field owns and renders the whole map immediately, including while paused");
        Check(Loser.Anchor == null && Loser.Caster!.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>().All(hediff => hediff.Domain != Loser),
            "dissolution removes only its own anchor and shared health protection");
        DamageInfo hit = new(DamageDefOf.Blunt, 3f);
        Check(!Loser.AbsorbDamage(Loser.Caster!, ref hit) && hit.Amount == 3f,
            "captive caster no longer absorbs normal damage through the dissolved domain");
        if (Loser.Definition.defName == "RSH_BloodSeaDomain")
            Check(new[] { "RSH_BloodSeaTraverse", "RSH_BloodSeaTentacles", "RSH_BloodSeaDomainTide" }
                .All(name => Loser.Caster!.abilities.GetAbility(DefDatabase<AbilityDef>.GetNamed(name)) == null),
                "dissolved blood sea removes its granted abilities while its caster remains captive");
        Check(!battlefield!.CanAcceptChallenger && !Winner.CanAcceptChallenger && !Loser.Available,
            "captive records cannot be reused as a new domain or accept a third entrant");
    }

    private bool RuntimeTransitionAbsent() => CQFDomainRuntime.Instance.Transition == null && Find.CurrentMap == battlefield!.map;

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private Map? source;
    private MapComponent_Domain? battlefield;
    private Pawn? firstPawn;
    private Pawn? secondPawn;
    private Pawn? casualty;
    private IntVec3 firstOrigin;
    private IntVec3 secondOrigin;
    private int caseIndex;
    private int stage;
    private int frames;
    private bool finished;
}
