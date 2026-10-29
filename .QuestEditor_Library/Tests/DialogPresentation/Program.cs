using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using HarmonyLib;
using QuestEditor_Library;
using Verse;

internal static class Program
{
    private static int Main()
    {
        try
        {
            FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
            prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
            new Harmony("CQF.DialogPresentationChecks").Patch(AccessTools.Method(typeof(Log), nameof(Log.Error)), prefix: new HarmonyMethod(typeof(Program), nameof(NativeError)));
            ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
            content.assemblies = new ModAssemblyHandler(content);
            content.assemblies.loadedAssemblies.Add(typeof(DialogTreeDef).Assembly);
            content.assemblies.loadedAssemblies.Add(typeof(CQFDialogEditSession).Assembly);
            content.assemblies.loadedAssemblies.Add(typeof(Program).Assembly);
            LoadedModManager.RunningModsListForReading.Add(content);
            GenTypes.ClearCache();
            CheckDefaults();
            CheckRoundTrip();
            CheckEdits();
            CheckValidation();
            Check(errors.Count == 0, "native XML loading has no errors: " + string.Join("\n", errors));
            Console.WriteLine("Dialog presentation checks passed: " + checks);
            return 0;
        }
        catch (Exception error) { Console.WriteLine(error); return 1; }
    }

    private static void CheckDefaults()
    {
        DialogTreeDef tree = Tree();
        XElement original = tree.SaveToXElement("tree");
        Check(tree.ActivePresentation == null && !tree.PresentationErrors().Any(), "existing dialogs use the classic path by default");
        Check(!original.Descendants("presentations").Any() && original.Element("dialogStyle") == null, "classic XML does not gain presentation fields");
        DialogTreeDef loaded = Load(original);
        Check(loaded.ActivePresentation == null && loaded.nodeMoulds[0].text == tree.nodeMoulds[0].text, "existing XML loads with unchanged content and style");
        Check(ReferenceEquals(loaded.nodeMoulds[0].options[0], loaded.nodeMoulds[1].options[0]), "shared option identity survives loading existing XML");
        Check(original.ToString() == loaded.SaveToXElement("tree").ToString(), "classic XML round trip is identical");
        CQFDialogPresentation_Galgame gal = new();
        CQFDialogNodePresentation_Galgame node = (CQFDialogNodePresentation_Galgame)gal.CreateNodePresentation(tree.nodeMoulds[0]);
        Check(node.images.Count == 1 && node.images[0].placement == CQFDialogImagePlacement.Center, "existing node images seed topic images in the new style");
        node.images[0].imagePath = "CQF_Test_Changed";
        Check(tree.nodeMoulds[0].images[0].imagePath == "UI/CQF/DialogImage/Cage", "new style images never mutate classic images");
    }

    private static void CheckRoundTrip()
    {
        DialogTreeDef tree = GalgameTree();
        DialogTreeDef loaded = Load(tree.SaveToXElement("tree"));
        CQFDialogPresentation_Galgame gal = (CQFDialogPresentation_Galgame)loaded.ActivePresentation!;
        CQFDialogNodePresentation_Galgame node = (CQFDialogNodePresentation_Galgame)loaded.nodeMoulds[0].presentations.Single();
        Check(gal.left.source == CQFDialogPortraitSource.Image && gal.left.imagePath == "UI/CQF/DialogImage/Cage", "custom portrait source and path survive native XML");
        Check(gal.right.participant == CQFDialogParticipant.Interviewer && gal.right.mirror, "participant binding and mirroring survive native XML");
        Check(node.speaker == CQFDialogSpeaker.Right && node.overrideLeft && node.left.source == CQFDialogPortraitSource.Hidden, "node speaker and portrait overrides survive native XML");
        Check(node.images.Count == 2 && node.images[1].placement == CQFDialogImagePlacement.Background && node.images[1].offsetX == 0.2f, "topic images and background layout survive native XML");
        Check(tree.SaveToXElement("tree").ToString() == loaded.SaveToXElement("tree").ToString(), "complete style XML round trip is identical");
        loaded.dialogStyle = string.Empty;
        DialogTreeDef classic = Load(loaded.SaveToXElement("tree"));
        Check(classic.ActivePresentation == null && classic.presentations.Count == 1 && classic.nodeMoulds[0].presentations.Count == 1, "switching to classic retains inactive style configuration");
        classic.dialogStyle = "CQF_Galgame";
        Check(((CQFDialogPresentation_Galgame)classic.ActivePresentation!).left.imagePath == gal.left.imagePath, "switching back restores portrait settings");
    }

    private static void CheckEdits()
    {
        DialogTreeDef tree = GalgameTree();
        CQFDialogEditSession session = new();
        session.Reset(tree);
        DialogTreeDef copy = session.Copy(tree);
        ((CQFDialogPresentation_Galgame)copy.ActivePresentation!).left.imagePath = "CQF_Test_Copy";
        ((CQFDialogNodePresentation_Galgame)copy.nodeMoulds[0].presentations[0]).images.Clear();
        Check(((CQFDialogPresentation_Galgame)tree.ActivePresentation!).left.imagePath != "CQF_Test_Copy", "tree portrait editing uses independent deep copies");
        Check(((CQFDialogNodePresentation_Galgame)tree.nodeMoulds[0].presentations[0]).images.Count == 2, "node image editing uses independent deep copies");
        tree.dialogStyle = string.Empty;
        session.Observe(tree);
        DialogTreeDef undo = session.Undo(tree);
        Check(undo.dialogStyle == "CQF_Galgame", "undo restores the active style");
        DialogTreeDef redo = session.Redo(undo);
        Check(redo.dialogStyle == string.Empty && redo.presentations.Count == 1, "redo preserves inactive style settings");
        Check(ReferenceEquals(redo.nodeMoulds[0].options[0], redo.nodeMoulds[1].options[0]), "undo and redo preserve shared option identity");
    }

    private static void CheckValidation()
    {
        DialogTreeDef tree = GalgameTree();
        Check(!tree.PresentationErrors().Any(), "valid presentation settings are accepted");
        tree.dialogStyle = "CQF_Test_Unknown";
        Check(tree.PresentationErrors().Any(), "unknown active styles are diagnosed");
        tree.dialogStyle = "CQF_Galgame";
        tree.presentations.Add(new CQFDialogPresentation_Galgame());
        Check(tree.PresentationErrors().Any(error => error.Contains("duplicate")), "duplicate style configurations are diagnosed");
        tree.presentations.RemoveAt(1);
        CQFDialogNodePresentation_Galgame node = (CQFDialogNodePresentation_Galgame)tree.nodeMoulds[0].presentations[0];
        node.images[0].scale = float.NaN;
        Check(tree.PresentationErrors().Any(), "non-finite image scales are diagnosed");
        node.images[0].scale = 1f;
        node.images[0].placement = (CQFDialogImagePlacement)99;
        Check(tree.PresentationErrors().Any(), "invalid image placement is diagnosed");
        node.images[0].placement = CQFDialogImagePlacement.Center;
        node.images[0].offsetX = 2f;
        Check(tree.PresentationErrors().Any(), "out-of-range offsets are diagnosed");
        tree.dialogStyle = string.Empty;
        Check(!tree.PresentationErrors().Any(), "inactive style parameters do not block classic dialogs");
    }

    private static DialogTreeDef Tree()
    {
        DialogTreeDef tree = new() { defName = "CQF_Test_Presentation", title = "CQF_Test_Title" };
        tree.nodeMoulds[0].images.Add(new DialogImage { imagePath = "UI/CQF/DialogImage/Cage", scale = 0.5f });
        DialogOption shared = new() { text = "CQF_Test_Option", results = new() { new DialogResult { nextIndex = 1 } } };
        tree.nodeMoulds[0].options.Add(shared);
        tree.nodeMoulds[1] = new DialogNode(1) { text = "CQF_Test_Node_1", options = new() { shared } };
        tree.Update();
        return tree;
    }

    private static DialogTreeDef GalgameTree()
    {
        DialogTreeDef tree = Tree();
        CQFDialogPresentation_Galgame gal = new();
        gal.left.source = CQFDialogPortraitSource.Image;
        gal.left.imagePath = "UI/CQF/DialogImage/Cage";
        tree.dialogStyle = gal.Id;
        tree.presentations.Add(gal);
        CQFDialogNodePresentation_Galgame node = (CQFDialogNodePresentation_Galgame)gal.CreateNodePresentation(tree.nodeMoulds[0]);
        node.speaker = CQFDialogSpeaker.Right;
        node.overrideLeft = true;
        node.left.source = CQFDialogPortraitSource.Hidden;
        node.images.Add(new CQFDialogSceneImage { imagePath = "UI/CQF/MapBackgroup/StarrySky", placement = CQFDialogImagePlacement.Background, offsetX = 0.2f });
        tree.nodeMoulds[0].presentations.Add(node);
        return tree;
    }

    private static DialogTreeDef Load(XElement source)
    {
        XmlDocument xml = new();
        xml.LoadXml(source.ToString());
        DialogTreeDef tree = DirectXmlToObject.ObjectFromXml<DialogTreeDef>(xml.DocumentElement, true);
        tree.Update();
        return tree;
    }

    private static bool NativeError(string text) { errors.Add(text); Console.WriteLine("NATIVE ERROR " + text); return false; }
    private static void Check(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); checks++; Console.WriteLine("PASS " + message); }

    private static int checks;
    private static readonly List<string> errors = new();
}
