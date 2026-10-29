using System.Text;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DialogPresentationChecks;

public sealed class PresentationChecksDriver : MonoBehaviour
{
    private string Root => GenFilePaths.SaveDataFolderPath;
    private string Results => Path.Combine(Root, "DialogPresentationChecks.txt");
    private float Age => Time.realtimeSinceStartup - since;

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || LongEventHandler.ShouldWaitForEvent) return;
        try
        {
            if (Time.realtimeSinceStartup > 240f) throw new TimeoutException("Dialog runtime checks timed out at " + stage);
            if (afterScreenshot != null)
            {
                if (!File.Exists(screenshotPath) || new FileInfo(screenshotPath).Length < 512)
                {
                    if (Age > 10f) throw new TimeoutException("Native screenshot was not written: " + screenshotPath);
                    return;
                }
                Action next = afterScreenshot;
                afterScreenshot = null;
                next();
                return;
            }
            if (stage == 0)
            {
                if (Current.ProgramState != ProgramState.Playing || Current.Game?.PlayerHasControl != true || Find.CurrentMap == null) return;
                Directory.CreateDirectory(Path.Combine(Root, "Screenshots"));
                pawns = Find.CurrentMap.mapPawns.FreeColonists.Take(2).ToArray();
                Check(pawns.Length == 2, "two live participants are available");
                tree = CreateTree();
                Open();
                Go(1);
            }
            else if (Age < 0.6f) return;
            else if (stage == 1)
            {
                Check(window!.InitialSize == new Vector2(635f, 635f) && window.doWindowBackground, "classic window retains its size and background");
                Check(window.CurrentNodeIndex == 0 && window.options.Count == 1 && window.elements.Count == 2, "classic text, image and option are available");
                Capture("01_Classic", () =>
                {
                window.options[0].action();
                Check(PresentationProbeAction.Executions == 1 && changed == 1 && window.CurrentNodeIndex == 1, "classic option executes once and raises one node event");
                Check(window.elements.Count == 4, "classic history retains previous content and selected option");
                Go(2);
                });
            }
            else if (stage == 2)
            {
                window!.options[0].action();
                Check(window.IsClosed && closed == 1, "classic end option closes with one notification");
                Galgame();
                Open();
                Go(3);
            }
            else if (stage == 3)
            {
                Check(!window!.doWindowBackground && window.InitialSize == new Vector2(UI.screenWidth, UI.screenHeight), "Galgame uses a full-screen transparent host");
                Check(window.options.Count == 1 && window.CurrentElements.Count == 2, "Galgame receives the same runtime content and options");
                Capture("02_Galgame_Pawns_Topic", () =>
                {
                window.options[0].action();
                Check(PresentationProbeAction.Executions == 2 && changed == 1 && window.CurrentNodeIndex == 1, "Galgame reuses existing option actions and node notifications");
                Check(window.CurrentElements.Count == 1, "Galgame exposes current-node content separately from history");
                Go(4);
                });
            }
            else if (stage == 4)
            {
                Capture("03_Galgame_Background_CustomPortrait", () =>
                {
                window!.windowRect = new Rect(20f, 20f, 720f, 600f);
                Go(5);
                });
            }
            else if (stage == 5)
            {
                Capture("04_Galgame_Narrow", () =>
                {
                window!.options[0].action();
                Check(window.IsClosed && closed == 1, "Galgame end option raises one close notification");
                preview = new CQFDialogPreviewWindow(tree!, 0);
                Find.WindowStack.Add(preview);
                Go(6);
                });
            }
            else if (stage == 6)
            {
                Check(PresentationProbeAction.Executions == 2, "drawing the Galgame preview executes no game actions");
                Capture("05_Galgame_Preview", () =>
                {
                preview!.Close(false);
                editor = new QuestEditor_Dialog { CurTree = tree! };
                Find.WindowStack.Add(editor);
                editor.SelectNode(tree!.nodeMoulds[1]);
                Go(7);
                });
            }
            else if (stage == 7)
            {
                Capture("06_Galgame_NodeSettings", () =>
                {
                editor!.Close(false);
                settings = new QuestEditor_DialogTreeMisc(tree!);
                Find.WindowStack.Add(settings);
                Go(8);
                });
            }
            else if (stage == 8)
            {
                Capture("07_Galgame_TreeSettings", () =>
                {
                settings!.Close(false);
                tree!.presentations.Add(new PresentationProbeStyle());
                tree.dialogStyle = "CQF_Test_ProbeStyle";
                preview = new CQFDialogPreviewWindow(tree, 0);
                Find.WindowStack.Add(preview);
                Go(9);
                });
            }
            else if (stage == 9)
            {
                CQFDialogFrame frame = PresentationProbeRenderer.LastFrame ?? throw new InvalidOperationException("External renderer did not receive the preview frame.");
                Check(frame.Node.index == 0 && frame.Options.Count == 1 && frame.IsPreview, "an external style receives preview content and simulated choices");
                frame.Options[0].action();
                Go(10);
            }
            else if (stage == 10)
            {
                Check(PresentationProbeRenderer.LastFrame!.Node.index == 1 && PresentationProbeAction.Executions == 2, "preview navigation changes nodes without executing result actions");
                preview!.Close(false);
                Check(PresentationProbeRenderer.Disposals == 1, "closing a preview releases its renderer once");
                Open();
                Go(11);
            }
            else if (stage == 11)
            {
                CQFDialogFrame frame = PresentationProbeRenderer.LastFrame!;
                Check(!frame.IsPreview && frame.Interviewer == pawns[0] && frame.Interviewee == pawns[1] && window!.InitialSize == new Vector2(800f, 600f), "external runtime renderer receives participants and controls window dimensions");
                frame.Options[0].action();
                Check(PresentationProbeAction.Executions == 3 && changed == 1, "external runtime style preserves action and notification semantics");
                window!.Close(false);
                Check(PresentationProbeRenderer.Disposals == 2, "closing a runtime dialog releases its renderer once");
                tree!.dialogStyle = "CQF_Galgame";
                tree.nodeMoulds[2] = new DialogNode(2) { text = "CQF_Test_Terminal" };
                tree.nodeMoulds[1].options[0].results[0].nextIndex = 2;
                Open(1);
                Go(12);
            }
            else if (stage == 12)
            {
                window!.options[0].action();
                Go(13);
            }
            else if (stage == 13)
            {
                Check(window!.CurrentNodeIndex == 2 && window.options.Count == 0 && window.nextOptions.Count == 0, "a terminal Galgame node clears stale choices");
                Capture("08_Galgame_Terminal", () =>
                {
                window.Close(false);
                Check(Directory.GetFiles(Path.Combine(Root, "Screenshots"), "*.png").Length == 8, "eight native screenshots were captured");
                Go(14);
                });
            }
            else if (stage == 14)
            {
                foreach (string path in Directory.GetFiles(Path.Combine(Root, "Screenshots"), "*.png"))
                    Check(new FileInfo(path).Length > 512, "rendered screenshot is nonempty: " + Path.GetFileName(path));
                Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(Results, error + "\n", Encoding.UTF8);
            Log.Error(error.ToString());
            Finish(false);
        }
    }

    private void Open(int index = 0)
    {
        changed = 0;
        closed = 0;
        window = new CQFDialogTreeWindow(tree!.title, pawns[0], pawns[1], null!, tree, index);
        window.NodeChanged += _ => changed++;
        window.Closed += () => closed++;
        Find.WindowStack.Add(window);
    }

    private void Galgame()
    {
        CQFDialogPresentation_Galgame gal = new();
        tree!.presentations.Add(gal);
        tree.dialogStyle = gal.Id;
        CQFDialogNodePresentation_Galgame root = (CQFDialogNodePresentation_Galgame)gal.CreateNodePresentation(tree.nodeMoulds[0]);
        tree.nodeMoulds[0].presentations.Add(root);
        CQFDialogNodePresentation_Galgame next = new() { speaker = CQFDialogSpeaker.Right, overrideLeft = true };
        next.left.source = CQFDialogPortraitSource.Image;
        next.left.imagePath = "UI/CQF/DialogImage/Cage";
        next.images.Add(new CQFDialogSceneImage { imagePath = "UI/CQF/MapBackgroup/StarrySky", placement = CQFDialogImagePlacement.Background });
        next.images.Add(new CQFDialogSceneImage { imagePath = "UI/QuestEditor/DialogManager", scale = 0.6f });
        tree.nodeMoulds[1].presentations.Add(next);
    }

    private DialogTreeDef CreateTree()
    {
        DialogTreeDef result = new() { defName = "CQF_Test_RuntimePresentation", title = "CQF_Test_Title" };
        result.nodeMoulds[0].text = "CQF_Test_Node_0";
        result.nodeMoulds[0].images.Add(new DialogImage { imagePath = "UI/CQF/DialogImage/Cage", scale = 0.4f });
        result.nodeMoulds[0].options.Add(new DialogOption { text = "CQF_Test_Next", results = new() { new DialogResult { nextIndex = 1, actions = new() { new PresentationProbeAction() } } } });
        result.nodeMoulds[1] = new DialogNode(1) { text = "CQF_Test_Node_1", options = new() { new DialogOption { text = "CQF_Test_Close", results = new() { new DialogResult() } } } };
        result.Update();
        return result;
    }

    private void Capture(string name, Action next)
    {
        foreach (Window log in Find.WindowStack.Windows.Where(item => item.GetType().Name == "EditWindow_Log").ToArray()) log.Close(false);
        string path = Path.Combine(Root, "Screenshots", name + ".png");
        if (File.Exists(path)) throw new IOException("Screenshot already exists: " + path);
        screenshotPath = path;
        afterScreenshot = next;
        since = Time.realtimeSinceStartup;
        ScreenCapture.CaptureScreenshot(path);
    }

    private void Go(int next) { stage = next; since = Time.realtimeSinceStartup; }
    private void Check(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); File.AppendAllText(Results, "PASS " + (++checks) + " " + message + "\n", Encoding.UTF8); }
    private void Finish(bool success) { finished = true; File.AppendAllText(Results, success ? "COMPLETE\n" : "FAILED\n", Encoding.UTF8); Application.Quit(success ? 0 : 1); }

    private int stage;
    private float since;
    private bool finished;
    private int checks;
    private int changed;
    private int closed;
    private Pawn[] pawns = Array.Empty<Pawn>();
    private DialogTreeDef? tree;
    private CQFDialogTreeWindow? window;
    private CQFDialogPreviewWindow? preview;
    private QuestEditor_Dialog? editor;
    private QuestEditor_DialogTreeMisc? settings;
    private string screenshotPath = string.Empty;
    private Action? afterScreenshot;
}
