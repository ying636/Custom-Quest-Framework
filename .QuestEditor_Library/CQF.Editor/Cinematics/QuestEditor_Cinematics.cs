using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class QuestEditor_Cinematics : Window
{
    public QuestEditor_Cinematics() : this(RestoreDraft())
    {
    }

    public QuestEditor_Cinematics(CQFCinematicProject project)
    {
        this.project = project;
        worldIdentity = CurrentWorldIdentity();
        layer = WindowLayer.GameUI;
        doCloseX = true;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnAccept = false;
        forcePause = true;
        drawShadow = false;
        timeline.SelectionChanged = _ => ShowInspector();
        timeline.StepAdded = StepAdded;
    }

    public override Vector2 InitialSize => new(Mathf.Max(320f, UI.screenWidth - 16f), Mathf.Min(252f, UI.screenHeight * 0.35f));
    protected override float Margin => 8f;
    public CQFCinematicProject Project => project;
    public CQFCinematicStep? Selected => timeline.Selected;

    public override void PostOpen()
    {
        base.PostOpen();
        if (Current.Game == null || Find.CurrentMap == null)
        {
            Messages.Message("CQF_Cinematic_MapRequired".Translate(), MessageTypeDefOf.CautionInput);
            Close();
            return;
        }
        try { project.RebindTargets(); }
        catch (Exception error) { Report("CQF_Cinematic_ProjectLoadFailed", error); }
        RememberDraft();
        ShowInspector();
    }

    public override void PostClose()
    {
        rolesWindow?.Close(false);
        rolesWindow = null;
        inspector?.Close(false);
        inspector = null;
        CancelPick();
        RememberDraft();
        base.PostClose();
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (Current.Game == null || Find.CurrentMap == null || CurrentWorldIdentity() != worldIdentity)
        {
            Close(false);
            return;
        }
        DrawMapSelection();
    }

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        float nameWidth = Mathf.Clamp(inRect.width * 0.25f, 80f, 280f);
        TooltipHandler.TipRegion(new Rect(0f, 0f, nameWidth, 28f), "CQF_Cinematic_ProjectName".Translate());
        project.DisplayName = Widgets.TextField(new Rect(0f, 0f, nameWidth, 28f), project.DisplayName);
        float x = nameWidth + 6f;
        float buttonWidth = Mathf.Max(20f, (inRect.width - x - 60f) / 5f);
        DrawButton(ref x, 0f, buttonWidth, "CQF_Cinematic_OpenProject", OpenProjectMenu);
        DrawButton(ref x, 0f, buttonWidth, "CQF_Cinematic_SaveProject", SaveProject);
        DrawButton(ref x, 0f, buttonWidth, "CQF_Cinematic_RecordCamera", () => RecordCamera());
        DrawButton(ref x, 0f, buttonWidth, "CQF_Cinematic_RecordMove", () => RecordPawnMove());
        DrawButton(ref x, 0f, buttonWidth, "CQF_Cinematic_RecordFacing", () => RecordPawnFacing());
        (string Key, Action Click, bool Enabled)[] controls =
        {
            ("CQF_Cinematic_AddStep", () => timeline.OpenAddMenu(project.Definition), true),
            ("CQF_Cinematic_CopyStep", () => timeline.CopySelected(project.Definition.steps), Selected != null),
            ("CQF_Cinematic_RemoveStep", () => timeline.RemoveSelected(project.Definition.steps), Selected != null),
            ("CQF_Cinematic_PreviewSelection", () => PlayPreview(true), Selected != null),
            ("CQF_Cinematic_PreviewAll", () => PlayPreview(false), project.Definition.steps.Count > 0),
            ("CQF_Cinematic_TestCopy", StartTest, project.Definition.steps.Count > 0),
            ("CQF_Cinematic_ActorBindings", () => OpenRoleBindings(), true),
            (inspector?.IsOpen == true ? "CQF_Cinematic_HideInspector" : "CQF_Cinematic_ShowInspector", ToggleInspector, true)
        };
        float width = (inRect.width - (controls.Length - 1) * 6f) / controls.Length;
        x = 0f;
        foreach (var control in controls)
            DrawButton(ref x, 34f, width, control.Key, control.Click, control.Enabled);
        timeline.Draw(new Rect(0f, 69f, inRect.width, Mathf.Max(40f, inRect.height - 69f)), project.Definition, Summary);
    }

    public void RecordCamera(CQFCinematicStep_Camera? existing = null)
    {
        Map? map = Find.CurrentMap;
        if (map == null) return;
        Vector3 position = Find.Camera.transform.position;
        position.y = 0f;
        IntVec3 cell = position.ToIntVec3();
        cell.x = Mathf.Clamp(cell.x, 0, map.Size.x - 1);
        cell.z = Mathf.Clamp(cell.z, 0, map.Size.z - 1);
        CQFCinematicStep_Camera step = existing ?? new CQFCinematicStep_Camera();
        step.targetKey = project.BindTarget(new TargetInfo(cell, map));
        step.offset = position - cell.ToVector3Shifted();
        step.size = Find.CameraDriver.RootSize;
        if (existing == null) timeline.Add(project.Definition, step);
        inspector?.RefreshValues();
    }

    public void RecordPawnMove(CQFCinematicStep_PawnMove? existing = null)
    {
        SelectPawn(pawn => PickCell(pawn, "CQF_Cinematic_MapPickDestination", target =>
        {
            CQFCinematicStep_PawnMove step = existing ?? new CQFCinematicStep_PawnMove();
            if (existing != null && project.Definition.roles.Any(role => role.key == existing.pawnKey))
                project.SetRehearsalPawn(step.pawnKey, pawn);
            else step.pawnKey = project.BindTarget(new TargetInfo(pawn));
            step.targetKey = project.BindTarget(new TargetInfo(target.Cell, pawn.Map));
            if (existing == null) timeline.Add(project.Definition, step);
        }), existing?.pawnKey);
    }

    public void RecordPawnFacing(CQFCinematicStep_PawnFace? existing = null)
    {
        SelectPawn(pawn => PickCell(pawn, "CQF_Cinematic_MapPickFacing", target =>
        {
            CQFCinematicStep_PawnFace step = existing ?? new CQFCinematicStep_PawnFace();
            if (existing != null && project.Definition.roles.Any(role => role.key == existing.pawnKey))
                project.SetRehearsalPawn(step.pawnKey, pawn);
            else step.pawnKey = project.BindTarget(new TargetInfo(pawn));
            step.targetKey = project.BindTarget(new TargetInfo(target.Cell, pawn.Map));
            if (existing == null) timeline.Add(project.Definition, step);
        }), existing?.pawnKey);
    }

    public void BindPawn(Action<string> accept)
    {
        SelectPawn(pawn => accept(project.BindPawnRole(pawn)), useSelection: false);
    }

    public void OpenRoleBindings(string? key = null)
    {
        inspector?.Close(false);
        rolesWindow?.Close(false);
        rolesWindow = new Window_CinematicRoles(this, key);
        Find.WindowStack.Add(rolesWindow);
    }

    public void PickRehearsalPawn(string key)
    {
        SelectPawn(pawn => project.SetRehearsalPawn(key, pawn), useSelection: false);
    }

    public void BindMapTarget(Action<string> accept)
    {
        Map? map = Find.CurrentMap;
        if (map == null) return;
        picking = true;
        Find.Targeter.BeginTargeting(new TargetingParameters { canTargetLocations = true, canTargetPawns = true, canTargetBuildings = true, canTargetItems = true }, target =>
        {
            picking = false;
            accept(project.BindTarget(target.HasThing ? new TargetInfo(target.Thing) : new TargetInfo(target.Cell, map)));
        }, actionWhenFinished: () => picking = false, requiresCastedSelected: false);
    }

    public string TargetLabel(string key)
    {
        if (project.Definition.roles.Any(role => role.key == key)) return project.RoleLabel(key);
        if (!project.Targets.TryGetValue(key ?? string.Empty, out TargetInfo target)) return "CQF_Cinematic_None".Translate();
        return target.HasThing ? target.Thing.LabelShortCap.ToString() : target.Cell.ToString();
    }

    public void CancelPick()
    {
        if (!picking || Current.Game == null) return;
        Find.Targeter.StopTargeting();
        picking = false;
    }

    public override void OnCancelKeyPressed()
    {
        if (picking)
        {
            CancelPick();
            Event.current.Use();
        }
        else base.OnCancelKeyPressed();
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 size = InitialSize;
        windowRect = new Rect(8f, UI.screenHeight - size.y - 40f, size.x, size.y);
    }

    private void DrawButton(ref float x, float y, float width, string key, Action click, bool enabled = true)
    {
        Rect rect = new(x, y, width, 28f);
        if (CQFUIStyle.ButtonText(rect, key.Translate(), active: enabled)) click();
        string tip = key + "_Tip";
        TooltipHandler.TipRegion(rect, tip.CanTranslate() ? tip.Translate() : key.Translate());
        x += width + 6f;
    }

    private void ShowInspector()
    {
        if (rolesWindow?.IsOpen == true) return;
        if (inspector?.IsOpen == true) return;
        inspector = new Window_CinematicInspector(this);
        Find.WindowStack.Add(inspector);
    }

    private void ToggleInspector()
    {
        if (inspector?.IsOpen == true) inspector.Close();
        else ShowInspector();
    }

    private void StepAdded(CQFCinematicStep step)
    {
        project.EnsureRoles();
        if (step is CQFCinematicStep_Subtitle subtitle) project.GetSubtitle(subtitle);
        ShowInspector();
    }

    private void OpenProjectMenu()
    {
        CancelPick();
        List<FloatMenuOption> options = new()
        {
            new FloatMenuOption("CQF_Cinematic_New".Translate(), () => ConfirmReplace(() => ReplaceProject(new CQFCinematicProject())))
        };
        string directory = Path.Combine(CQFContentPaths.Quests, "Cinematics");
        try
        {
            HashSet<string> listedDefinitions = new(StringComparer.Ordinal);
            if (Directory.Exists(directory))
                foreach (string path in Directory.GetFiles(directory, "*.xml").OrderBy(Path.GetFileName))
                {
                    string file = path;
                    try
                    {
                        using StringReader source = new(File.ReadAllText(file, Encoding.UTF8));
                        using XmlReader reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2097152 });
                        XElement root = XElement.Load(reader);
                        XElement? definition = root.Name == "Defs" ? root.Elements().SingleOrDefault() : root;
                        if (definition == null) throw new InvalidDataException("CQF_Cinematic_InvalidXml");
                        string id = definition.Element("defName")?.Value ?? Path.GetFileNameWithoutExtension(file);
                        string name = definition.Element("label")?.Value ?? id;
                        listedDefinitions.Add(id);
                        options.Add(new FloatMenuOption(name, () => ConfirmReplace(() => LoadProject(file))));
                    }
                    catch (Exception error) { Report("CQF_Cinematic_ProjectLoadFailed", new InvalidDataException(file + "\n" + error.Message, error)); }
                }
            foreach (CQFCinematicDef definition in DefDatabase<CQFCinematicDef>.AllDefsListForReading.OrderBy(item => item.LabelCap.ToString()))
            {
                CQFCinematicDef entry = definition;
                if (listedDefinitions.Contains(entry.defName)) continue;
                options.Add(new FloatMenuOption(entry.LabelCap, () => ConfirmReplace(() =>
                {
                    try { ReplaceProject(CQFCinematicProject.Load(entry)); }
                    catch (Exception error) { Report("CQF_Cinematic_ProjectLoadFailed", error); }
                })));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        catch (Exception error) { Report("CQF_Cinematic_ProjectLoadFailed", error); }
    }

    private void LoadProject(string path)
    {
        try { ReplaceProject(CQFCinematicProject.Load(path)); }
        catch (Exception error) { Report("CQF_Cinematic_ProjectLoadFailed", error); }
    }

    private void ReplaceProject(CQFCinematicProject replacement)
    {
        rolesWindow?.Close(false);
        CancelPick();
        project = replacement;
        RememberDraft();
        timeline.Reset();
        ShowInspector();
    }

    private void SaveProject()
    {
        try
        {
            string path = project.Save();
            Messages.Message("CQF_Cinematic_ProjectSaved".Translate() + ": " + path, MessageTypeDefOf.TaskCompletion);
        }
        catch (Exception error) { Report("CQF_Cinematic_ExportFailed", error); }
    }

    private void PlayPreview(bool selectedOnly)
    {
        try
        {
            if (selectedOnly && Selected == null) return;
            CQFCinematicProject previewProject = project.CloneForTest();
            previewProject.RebindTargets();
            if (selectedOnly) previewProject.Definition.steps = new List<CQFCinematicStep> { Selected!.Copy() };
            previewProject.Definition.pauseGame = false;
            previewProject.Definition.lockInput = false;
            GameComponent_Cinematics host = GameComponent_Cinematics.Instance ?? throw new InvalidOperationException("CQF_Cinematic_NoGame");
            CQFCinematicInstance instance = host.Play(previewProject.Definition, previewProject.Targets, previewProject.Quest, true);
            CancelPick();
            Close(false);
            Find.WindowStack.Add(new Window_CinematicPlayback(instance, () => Find.WindowStack.Add(this), project.DisplayName));
        }
        catch (Exception error) { Report("CQF_Cinematic_PlayFailed", error); }
    }

    private void StartTest()
    {
        try
        {
            CancelPick();
            RememberDraft();
            CQFCinematicTestSession.Start(project, restored => Find.WindowStack.Add(new QuestEditor_Cinematics(restored)));
            Close(false);
        }
        catch (Exception error) { Report("CQF_Cinematic_TestFailed", error); }
    }

    private void SelectPawn(Action<Pawn> accept, string? currentKey = null, bool useSelection = true)
    {
        Pawn? selected = useSelection ? Find.Selector.SingleSelectedThing as Pawn : null;
        if (selected == null && !currentKey.NullOrEmpty() && project.Targets.TryGetValue(currentKey!, out TargetInfo bound)) selected = bound.Thing as Pawn;
        if (selected?.Spawned == true && selected.Map == Find.CurrentMap)
        {
            accept(selected);
            return;
        }
        Messages.Message("CQF_Cinematic_MapSelectPawn".Translate(), MessageTypeDefOf.NeutralEvent);
        picking = true;
        Find.Targeter.BeginTargeting(TargetingParameters.ForPawns(), target =>
        {
            picking = false;
            if (target.Thing is Pawn pawn) accept(pawn);
        }, actionWhenFinished: () => picking = false, requiresCastedSelected: false);
    }

    private void PickCell(Pawn pawn, string prompt, Action<LocalTargetInfo> accept)
    {
        if (!pawn.Spawned || pawn.Map != Find.CurrentMap) throw new InvalidOperationException("CQF_Cinematic_SelectTarget");
        Messages.Message(prompt.Translate(), MessageTypeDefOf.NeutralEvent);
        picking = true;
        Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target =>
        {
            picking = false;
            if (target.Cell.InBounds(pawn.Map)) accept(target);
        }, target => DrawMapLine(pawn, target.Cell), target => target.Cell.InBounds(pawn.Map), actionWhenFinished: () => picking = false);
    }

    private void DrawMapSelection()
    {
        string? pawnKey = Selected switch
        {
            CQFCinematicStep_PawnMove move => move.pawnKey,
            CQFCinematicStep_PawnFace face => face.pawnKey,
            _ => null
        };
        string? targetKey = Selected switch
        {
            CQFCinematicStep_PawnMove move => move.targetKey,
            CQFCinematicStep_PawnFace face => face.targetKey,
            CQFCinematicStep_Camera camera => camera.targetKey,
            _ => null
        };
        if (targetKey.NullOrEmpty() || !project.Targets.TryGetValue(targetKey!, out TargetInfo target) || target.Map != Find.CurrentMap) return;
        if (!pawnKey.NullOrEmpty() && project.Targets.TryGetValue(pawnKey!, out TargetInfo actor) && actor.Thing is Pawn pawn)
            DrawMapLine(pawn, target.Cell);
        else if (target.Cell.InBounds(Find.CurrentMap)) GenDraw.DrawFieldEdges(new List<IntVec3> { target.Cell }, CQFUIStyle.Accent);
    }

    private void DrawMapLine(Pawn pawn, IntVec3 cell)
    {
        if (pawn.Map != Find.CurrentMap || !cell.InBounds(pawn.Map)) return;
        GenDraw.DrawLineBetween(pawn.DrawPos, cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays));
        GenDraw.DrawFieldEdges(new List<IntVec3> { cell }, CQFUIStyle.Accent);
    }

    private string Summary(CQFCinematicStep step)
    {
        return step switch
        {
            CQFCinematicStep_Subtitle subtitle => LanguageDatabase.activeLanguage.folderName.StartsWith("Chinese", StringComparison.OrdinalIgnoreCase)
                ? project.GetSubtitle(subtitle).Chinese : project.GetSubtitle(subtitle).English,
            CQFCinematicStep_PawnMove move => TargetLabel(move.pawnKey) + " → " + TargetLabel(move.targetKey),
            CQFCinematicStep_PawnFace face => TargetLabel(face.pawnKey) + " → " + TargetLabel(face.targetKey),
            CQFCinematicStep_Camera camera => TargetLabel(camera.targetKey),
            CQFCinematicStep_Image image => Path.GetFileName(image.imagePath),
            CQFCinematicStep_Sound sound => sound.sound?.LabelCap.ToString() ?? string.Empty,
            _ => string.Empty
        };
    }

    private void Report(string key, Exception error)
    {
        Log.Error("[CQF] Cinematic map editor operation failed: " + error);
        string detail = error.Message.CanTranslate() ? error.Message.Translate().ToString() : error.Message;
        Messages.Message(key == "CQF_Cinematic_TestFailed" ? key.Translate(detail) : key.Translate() + ": " + detail, MessageTypeDefOf.RejectInput);
    }

    private void ConfirmReplace(Action replace)
    {
        if (project.Definition.steps.Count == 0 && project.Subtitles.Count == 0 && project.DisplayName == "CQF_Cinematic_New".Translate().ToString())
        {
            replace();
            return;
        }
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CQF_Cinematic_ReplaceDraftConfirm".Translate(), replace));
    }

    private void RememberDraft()
    {
        if (worldIdentity.Length == 0) return;
        try
        {
            if (Current.ProgramState == ProgramState.Playing && CurrentWorldIdentity() == worldIdentity
                && project.Targets.Values.All(target => target.Map is { Disposed: false })) project.CaptureBindings();
            drafts[worldIdentity] = project.CloneForTest();
        }
        catch (Exception error) { Report("CQF_Cinematic_ExportFailed", error); }
    }

    private static CQFCinematicProject RestoreDraft()
    {
        string identity = CurrentWorldIdentity();
        if (identity.Length > 0 && drafts.TryGetValue(identity, out CQFCinematicProject? saved))
            return saved.CloneForTest();
        return new CQFCinematicProject();
    }

    private static string CurrentWorldIdentity()
    {
        if (Current.Game?.World?.info == null) return string.Empty;
        return Current.Game.World.info.persistentRandomValue.ToString(CultureInfo.InvariantCulture) + "\n" + Current.Game.World.info.seedString;
    }

    private readonly CQFCinematicTimeline timeline = new();
    private readonly string worldIdentity;
    private static readonly Dictionary<string, CQFCinematicProject> drafts = new(StringComparer.Ordinal);
    private CQFCinematicProject project;
    private Window_CinematicInspector? inspector;
    private Window_CinematicRoles? rolesWindow;
    private bool picking;
}
