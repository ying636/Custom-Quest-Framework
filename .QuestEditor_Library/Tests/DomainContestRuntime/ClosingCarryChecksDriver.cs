using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class ClosingCarryChecksDriver : MonoBehaviour
{
    private string DirectoryPath => Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE") ?? GenFilePaths.SaveDataFolderPath;
    private string ResultPath => Path.Combine(DirectoryPath, "ClosingCarryChecks.txt");

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
        try
        {
            if (stage != 0 && Time.realtimeSinceStartup - startedAt > 80f) throw new TimeoutException("Closing carry stage " + stage);
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                DomainDef definition = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                if (!runtime.HasAvailable(definition)) return;
                source = Find.CurrentMap;
                caster = source.mapPawns.FreeColonistsSpawned.First();
                caster.drafter.Drafted = true;
                caster.abilities ??= new Pawn_AbilityTracker(caster);
                caster.abilities.GainAbility(definition.openingAbility);
                Find.CameraDriver.SetRootPosAndSize(caster.Position.ToVector3Shifted(), 15f);
                Ability ability = caster.abilities.GetAbility(definition.openingAbility);
                Check(!ability.GizmoDisabled(out string reason), "ordinary basic expansion button is enabled: " + reason);
                ability.QueueCastingJob(caster, caster);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                startedAt = Time.realtimeSinceStartup;
                stage = 1;
            }
            else if (stage == 1)
            {
                if (runtime.Transition is DomainMaterialTransition transition && transition.Progress > 0f && shots < 6)
                {
                    Material material = (Material)typeof(DomainMaterialTransition).GetField("material", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(transition)!;
                    validCoverage &= material.GetFloat("_Radius") > 0f && material.GetFloat("_Height") > 0f;
                    ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "ClosingFirst_" + shots++ + ".png"));
                }
                if (caster!.Map == source || runtime.Transition != null) return;
                Check(shots == 6 && validCoverage, "the first six entry frames inherit the already sealed field coverage");
                field = caster.Map.GetComponent<MapComponent_Domain>();
                Check(field.Active && !field.ArrivalLocked && field.LastTerrainRenderedFrame > 0,
                    "the destination terrain renders before the entry lock is released");
                Current.Game.CurrentMap = source;
                field.RequestReturn("closing carry check cleanup");
                stage = 2;
            }
            else if (stage == 2)
            {
                if (caster!.Map != source || !GameComponent_MapManager.Instance!.IsHibernating(field!.map)) return;
                Check(!field.Active, "ordinary entry cleanup returns the caster and hibernates the map");
                File.AppendAllText(ResultPath, "COMPLETE\n");
                finished = true;
                Application.Quit(0);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Closing carry checks failed: " + error);
            finished = true;
            Application.Quit(1);
        }
    }

    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    private Map? source;
    private Pawn? caster;
    private MapComponent_Domain? field;
    private float startedAt;
    private int stage;
    private int shots;
    private bool validCoverage = true;
    private bool finished;
}
