using HarmonyLib;
using LudeonTK;
using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.ProjectionModuleChecks;

public sealed class ModuleChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        try
        {
            if (waiting > 0)
            {
                if (--waiting == 0) { File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "COMPLETE\n"); finished = true; Application.Quit(0); }
                return;
            }
            bool domain = GenCommandLine.TryGetCommandLineArg("cqfprojectionmodules", out string modules) && modules == "Domain";
            Check(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.MapProjections"), "unrequested projection DLL remains unloaded in the actual game");
            Check(!typeof(GameComponent_MapManager).Assembly.GetReferencedAssemblies().Any(assembly => assembly.Name == "CQF.MapProjections"), "core has no optional projection assembly dependency");
            Check(!CQFModuleLoader.IsLoaded("MapProjections") && CQFModuleLoader.DomainLoaded == domain, "domain-only and no-module requests remain independent from projections");
            Check(!Current.Game.components.Any(component => component.GetType().Name == "GameComponent_CQFProjectionUI"), "unrequested projection UI component is not created");
            Check(!Find.CurrentMap.components.Any(component => component.GetType().Name == "MapComponent_CQFMapProjections"), "unrequested projection map component is not created");
            Check((GameComponent_MapManager.Instance!.Module != null) == domain, "domain map runtime remains available only when explicitly requested");
            Check(!Harmony.GetAllPatchedMethods().Any(method => Harmony.GetPatchInfo(method)?.Owners.Contains("CQF.MapProjections") == true), "unrequested projection module installs no Harmony patches");
            Check(!LanguageDatabase.activeLanguage.TryGetTextFromKey("CQF_MP_Title", out _), "unrequested projection localization stays outside active content");
            Dialog_Debug.ResetStaticData();
            Dialog_Debug.TrySetupNodeGraph();
            List<DebugActionNode> nodes = new() { Dialog_Debug.rootNode };
            bool found = false;
            while (nodes.Count > 0)
            {
                DebugActionNode node = nodes[^1];
                nodes.RemoveAt(nodes.Count - 1);
                found |= node.sourceAttribute?.name == "CQF_MP_Title";
                nodes.AddRange(node.children);
            }
            Check(!found, "unrequested projection developer action is absent");
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "01-ModuleDisabled.png"));
            waiting = 5;
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "FAIL " + error + "\n");
            Log.Error("[CQF.ProjectionModuleChecks] " + error);
            finished = true;
            Application.Quit(1);
        }
    }

    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException(label);
        File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "PASS " + label + "\n");
    }

    private bool finished;
    private int waiting;
}
