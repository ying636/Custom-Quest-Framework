using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CQF.EntityBookRuntimeChecks;

public sealed class EntityBookChecksDriver : MonoBehaviour
{
    private string Root => GenFilePaths.SaveDataFolderPath;
    private string Results => Path.Combine(Root, "EntityBookChecks.txt");
    private GameComponent_EntityBook Book => Current.Game.GetComponent<GameComponent_EntityBook>();
    private GameComponent_Cinematics Host => GameComponent_Cinematics.Instance ?? throw new InvalidOperationException("Cinematic host is unavailable.");
    private float Age => Time.realtimeSinceStartup - stageStarted;

    private void Update()
    {
        if (finished) return;
        try
        {
            if (startedAt > 0f && Time.realtimeSinceStartup - startedAt > 200f) throw new TimeoutException("Entity book checks exceeded 200 seconds at " + stage);
            if (startedAt == 0f && Time.realtimeSinceStartup > 240f) throw new TimeoutException("Entity book quicktest did not reach a playable map.");
            if (LongEventHandler.AnyEventNowOrWaiting || LongEventHandler.ShouldWaitForEvent) return;
            if (Current.ProgramState != ProgramState.Playing || Current.Game == null || Find.CurrentMap == null || !Current.Game.PlayerHasControl) return;
            devtools?.CheckPendingClick();
            if (devtools?.ClickPending == true || Age < 0.35f) return;
            switch (stage)
            {
                case Stage.Boot:
                    Initialize();
                    break;
                case Stage.EnglishSelect:
                    Check(Private<CQFEntityBookEntry>(window!, "current").SourceId == templateSource, "native mouse-down/up selects the searched entity template row");
                    devtools!.Capture("CQF_EntityBook_English", () => window!.IsOpen);
                    Go(Stage.EnglishCapture);
                    break;
                case Stage.EnglishCapture:
                    if (!devtools!.CompleteCapture()) return;
                    templatePreview = Private<Pawn>(window!, "previewPawn");
                    Check(Book.GetRecords(templateSource, questA).Count == 0 && !GameComponent_Editor.Instance.pawns.ContainsKey(template!.defName),
                        "template preview creates neither a formal entity record nor the unique actor cache");
                    Check(!templatePreview.Spawned && !templatePreview.Discarded && Find.WorldPawns.AllPawnsAliveOrDead.Count == initialWorldPawns
                        && GameComponent_Editor.Instance.pawns.Count == initialUniquePawns && !Find.WorldPawns.Contains(templatePreview),
                        "template preview is an isolated unspawned sample without adding world pawns or unique actors");
                    staleSelection = WindowPoint(window!, Private<Rect>(window!, "selectEntityRect").center);
                    PrivateSet(window!, "search", "CQF_NO_MATCH_93D0_F8");
                    Go(Stage.EmptySearch);
                    break;
                case Stage.EmptySearch:
                    Check(!window!.HasVisibleSelection && window.GetType().GetField("current", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window) == null,
                        "a search without results clears the previous entity and its details");
                    Check(templatePreview!.Discarded, "changing to an empty search releases the old portrait sample");
                    devtools!.Capture("CQF_EntityBook_EmptySearch", () => window.IsOpen);
                    Go(Stage.EmptySearchCapture);
                    break;
                case Stage.EmptySearchCapture:
                    if (!devtools!.CompleteCapture()) return;
                    devtools.Click(staleSelection);
                    Go(Stage.EmptySearchRejected);
                    break;
                case Stage.EmptySearchRejected:
                    window!.GetType().GetMethod("SelectReference", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { false });
                    Check(window.IsOpen && selectedToken.Length == 0, "native clicking the former button and direct invocation cannot select a hidden entity");
                    PrivateSet(window, "search", template!.label);
                    window.SynchronizeSelection();
                    ClickFirstRow();
                    Go(Stage.EnglishReselect);
                    break;
                case Stage.EnglishReselect:
                    templatePreview = Private<Pawn>(window!, "previewPawn");
                    ClickReferenceButton();
                    Go(Stage.EnglishChosen);
                    break;
                case Stage.EnglishChosen:
                    Check(selectedToken == CQFEntityReference.Encode(templateSource) && !window!.IsOpen, "native entity selection returns the future template token and closes the entity book");
                    Check(templatePreview!.Discarded && !Find.WorldPawns.Contains(templatePreview), "closing the template entity book discards its owned portrait sample");
                    Check(project!.Definition.roles.Single(role => role.key == templateRole).source == CQFCinematicRoleSource.EntityBook, "entity selection assigns the cinematic actor through the production callback");
                    CheckAuthoring();
                    typeRecipe = new PawnSpawnData_ComplexPawn { pawnDef = template!, dataName = "CQF Common Settings", count = new IntRange(2, 5), generationChance = 0.35f, way = new ArrivingWay_DropPod() };
                    typeSource = typeRecipe.EntityBookSourceId;
                    recipeDialog = new Dialog_CQFEntityRecipe(typeRecipe);
                    Find.WindowStack.Add(recipeDialog);
                    Go(Stage.CommonSettings);
                    break;
                case Stage.CommonSettings:
                    devtools!.Capture("CQF_EntityBook_CommonSettings", () => recipeDialog!.IsOpen);
                    Go(Stage.CommonSettingsCapture);
                    break;
                case Stage.CommonSettingsCapture:
                    if (!devtools!.CompleteCapture()) return;
                    recipeDialog!.ChangeRecipeType(typeof(PawnSpawnData));
                    typeConfirmation = Find.WindowStack.Windows.OfType<Dialog_MessageBox>().Single();
                    Check(ReferenceEquals(recipeDialog.Recipe, typeRecipe), "changing a configured recipe type waits for confirmation without mutating the current data");
                    devtools.Click(WindowPoint(typeConfirmation, new Vector2(90f, typeConfirmation.windowRect.height - 36f - 18f)));
                    Go(Stage.TypeCancelled);
                    break;
                case Stage.TypeCancelled:
                    Check(!typeConfirmation!.IsOpen && ReferenceEquals(recipeDialog!.Recipe, typeRecipe), "native cancel keeps the original recipe and every setting");
                    recipeDialog!.ChangeRecipeType(typeof(PawnSpawnData));
                    typeConfirmation = Find.WindowStack.Windows.OfType<Dialog_MessageBox>().Single();
                    devtools!.Capture("CQF_EntityBook_TypeChangeConfirmation", () => typeConfirmation.IsOpen);
                    Go(Stage.TypeConfirmationCapture);
                    break;
                case Stage.TypeConfirmationCapture:
                    if (!devtools!.CompleteCapture()) return;
                    devtools.Click(WindowPoint(typeConfirmation!, new Vector2(typeConfirmation!.windowRect.width * 0.75f - 18f, typeConfirmation.windowRect.height - 36f - 18f)));
                    Go(Stage.TypeConfirmed);
                    break;
                case Stage.TypeConfirmed:
                    PawnSpawnData changed = recipeDialog!.Recipe;
                    Check(changed.GetType() == typeof(PawnSpawnData) && changed.count.min == 2 && changed.count.max == 5
                        && Math.Abs(changed.generationChance - 0.35f) < 0.0001f && changed.way is ArrivingWay_DropPod
                        && changed.EntityBookSourceId == typeSource && changed.dataName == typeRecipe!.dataName,
                        "native confirmation changes the type while preserving count, chance, arrival mode, name and source identity");
                    Check(changed.Copy().way is ArrivingWay_DropPod, "the confirmed arrival mode survives the production recipe copy path");
                    recipeDialog.Close(false);
                    placementWindow = new Window_CQFEntityPlacement(EntityBookPlacementChecks.PreviewMarker, questA);
                    Find.WindowStack.Add(placementWindow);
                    Go(Stage.MapPlacement);
                    break;
                case Stage.MapPlacement:
                    devtools!.Capture("CQF_EntityBook_MapPlacement", () => placementWindow!.IsOpen);
                    Go(Stage.MapPlacementCapture);
                    break;
                case Stage.MapPlacementCapture:
                    if (!devtools!.CompleteCapture()) return;
                    devtools.Click(WindowPoint(placementWindow!, new Vector2(130f, 170f)));
                    Go(Stage.MapActorAdded);
                    break;
                case Stage.MapActorAdded:
                    QuestEditor_Cinematics placedStudio = Find.WindowStack.Windows.OfType<QuestEditor_Cinematics>().Single();
                    string placedToken = CQFEntityReference.Encode(EntityBookPlacementChecks.PreviewMarker.pawns.Single().EntityBookSourceId, 0);
                    Check(!placementWindow!.IsOpen && placedStudio.Project.Quest == questA
                        && placedStudio.Project.Definition.roles.Single().sourceKey == placedToken,
                        "native map placement action adds the exact future source member to the cinematic with the same quest");
                    placedStudio.Close(false);
                    EntityBookPlacementChecks.VerifyAndCleanup(Check);
                    SetLanguage("ChineseSimplified (简体中文)");
                    window = new Window_CQFEntityBook(token => selectedRecipeToken = token, quest: questA);
                    Find.WindowStack.Add(window);
                    PrivateSet(window, "search", recipe!.dataName);
                    PrivateSet(window, "filter", 1);
                    ClickFirstRow();
                    Go(Stage.ChineseSelect);
                    break;
                case Stage.ChineseSelect:
                    Check(Private<CQFEntityBookEntry>(window!, "current").SourceId == recipe!.EntityBookSourceId, "native row selection respects the spawn-data search filter");
                    devtools!.Capture("CQF_EntityBook_Chinese", () => window!.IsOpen);
                    Go(Stage.ChineseCapture);
                    break;
                case Stage.ChineseCapture:
                    if (!devtools!.CompleteCapture()) return;
                    ClickReferenceButton();
                    Go(Stage.ChineseMemberRejected);
                    break;
                case Stage.ChineseMemberRejected:
                    Check(window!.IsOpen && selectedRecipeToken.Length == 0, "native selection refuses a multi-actor source until a member is explicitly chosen");
                    PrivateSet(window, "memberIndex", 1);
                    PrivateSet(window, "memberNumber", 2);
                    PrivateSet(window, "memberBuffer", "2");
                    devtools!.Capture("CQF_EntityBook_Chinese_RecipePreview", () => window.IsOpen);
                    Go(Stage.ChineseRecipePreview);
                    break;
                case Stage.ChineseRecipePreview:
                    if (!devtools!.CompleteCapture()) return;
                    recipePreview = Private<Pawn>(window!, "previewPawn");
                    Check(!recipePreview.Spawned && !recipePreview.Discarded && recipePreview.kindDef == recipe!.kind,
                        "a regular ungenerated human recipe creates a visible unspawned portrait sample");
                    Check(Book.Records.Count == 0 && Find.WorldPawns.AllPawnsAliveOrDead.Count == initialWorldPawns
                        && GameComponent_Editor.Instance.pawns.Count == initialUniquePawns && !Find.WorldPawns.Contains(recipePreview),
                        "regular recipe preview does not register formal entities or add world and unique actors");
                    ClickReferenceButton();
                    Go(Stage.ChineseMemberChosen);
                    break;
                case Stage.ChineseMemberChosen:
                    Check(selectedRecipeToken == CQFEntityReference.Encode(recipe!.EntityBookSourceId, 1) && !window!.IsOpen,
                        "native selection returns the explicit future member index for a multi-actor source");
                    Check(recipePreview!.Discarded && !Find.WorldPawns.Contains(recipePreview), "closing the regular recipe book discards its owned portrait sample");
                    Check(Find.CurrentMap.mapPawns.AllPawnsSpawned.Count == initialMapPawns && Book.Records.Count == 0,
                        "browsing both sources and their previews does not spawn or register formal actors");
                    CheckGeneration();
                    StartMovement();
                    Go(Stage.Moving);
                    break;
                case Stage.Moving:
                    if (instance!.Outcome == CQFCinematicOutcome.Failed) throw new InvalidOperationException("Formal entity-bound movement failed.");
                    if (productionActor!.CurJob?.def == JobDefOf.Goto) sawGoto = true;
                    if (instance.Outcome != CQFCinematicOutcome.Completed) return;
                    Check(sawGoto && productionActor.Position == destination, "formal cinematic actor resolution starts a real Goto and reaches the recorded destination");
                    Check(rehearsal!.Position == rehearsalPosition && productionActor != rehearsal, "formal playback uses the generated actor rather than the independent rehearsal sample");
                    SaveWorld();
                    Go(Stage.BeforeLoad);
                    break;
                case Stage.BeforeLoad:
                    Go(Stage.AfterLoad);
                    GameDataSaveLoader.LoadGame(saveName);
                    break;
                case Stage.AfterLoad:
                    if (Current.Game == oldGame) return;
                    CheckReload();
                    Finish(true);
                    break;
            }
        }
        catch (Exception error)
        {
            Log.Error("[CQF EntityBook checks] " + error);
            Directory.CreateDirectory(Root);
            File.AppendAllText(Results, "FAIL " + stage + " " + error + "\n", Encoding.UTF8);
            Finish(false);
        }
    }

    private void Initialize()
    {
        startedAt = Time.realtimeSinceStartup;
        File.WriteAllText(Results, "TRACE UI input uses native ImguiDispatch events; screenshots use RimDevTools.\n", Encoding.UTF8);
        string provider = Path.GetDirectoryName(CQFContentPaths.Quests)!;
        Check(typeof(CQFEntityReference).Assembly.Location.Contains("GameSnapshot", StringComparison.OrdinalIgnoreCase)
            && typeof(Window_CQFEntityBook).Assembly.Location.Contains("GameSnapshot", StringComparison.OrdinalIgnoreCase)
            && provider.Contains("GameSnapshot", StringComparison.OrdinalIgnoreCase), "runtime, editor and writable content are isolated from the user's real installation");
        Check(!Waylith.EngineApplication.Instance.Headless && Waylith.EngineApplication.Instance.RenderContext != null
            && !Waylith.EngineApplication.Instance.RenderContext.IsFramebufferMinimized(), "the hidden native game has a live framebuffer for DevTools screenshots");
        DebugSettings.pauseOnError = false;
        DebugSettings.godMode = true;
        CloseLogWindows();
        devtools = new EntityBookDevtools(Path.Combine(Root, "DevToolsEntityBook"));
        rehearsal = Find.CurrentMap.mapPawns.FreeColonistsSpawned.First();
        rehearsal.drafter.Drafted = true;
        rehearsal.jobs.EndCurrentJob(JobCondition.InterruptForced);
        rehearsalPosition = rehearsal.Position;
        initialMapPawns = Find.CurrentMap.mapPawns.AllPawnsSpawned.Count;
        initialWorldPawns = Find.WorldPawns.AllPawnsAliveOrDead.Count;
        initialUniquePawns = GameComponent_Editor.Instance.pawns.Count;
        EntityBookPlacementChecks.RunBefore(Check);
        Find.TickManager.Pause();
        Find.CameraDriver.SetRootPosAndSize(rehearsal.Position.ToVector3Shifted(), 22f);
        questA = MakeQuest("CQF Entity Book A");
        questB = MakeQuest("CQF Entity Book B");
        CheckNullQuestContext();
        recipe = new PawnSpawnData { kind = rehearsal.kindDef, faction = Faction.OfPlayer.def.defName, dataName = "CQF Entity Book Test Guards", count = new IntRange(2, 2) };
        CQFEntityBookCatalog.Track(recipe);
        PawnSpawnData saved = CQFEntityBookCatalog.Store.Save(recipe);
        CQFEntityBookCatalog.Reload();
        Check(saved.EntityBookSourceId == recipe.EntityBookSourceId && CQFEntityBookCatalog.Store.Load().Any(value => value.EntityBookSourceId == recipe.EntityBookSourceId),
            "collecting and loading spawn data preserves its source identity");
        PawnSpawnData copy = CQFEntityRecipeStore.Copy(saved);
        copy.extraKinds.Add(rehearsal.kindDef);
        copy.dataName = "CQF Entity Book Independent Copy";
        Check(copy.EntityBookSourceId == saved.EntityBookSourceId && saved.extraKinds.Count == 0 && saved.dataName == recipe.dataName,
            "using a recipe creates independent nested lists while retaining its binding identity");
        PawnSpawnData duplicate = CQFEntityRecipeStore.CopyAsNew(saved);
        Check(duplicate.EntityBookSourceId != saved.EntityBookSourceId, "save-as-new creates an independent source identity");
        template = new ComplexPawnDef { defName = "CQF_EntityBook_RuntimeGuard", label = "CQF Entity Book Template Guard" };
        PawnModData_Basic basic = template.DataFor<PawnModData_Basic>();
        basic.kindDef = rehearsal.kindDef;
        basic.faction = Faction.OfPlayer.def;
        basic.unique = true;
        DefDatabase<ComplexPawnDef>.Add(template);
        templateSource = "template:" + template.defName;
        IReadOnlyList<CQFEntityBookEntry> entries = CQFEntityBookCatalog.GetEntries(questA);
        Check(entries.Any(entry => entry.SourceId == recipe.EntityBookSourceId && entry.Recipe != null)
            && entries.Any(entry => entry.SourceId == templateSource && entry.Template == template), "the catalog combines spawn-data recipes and entity-editor templates");
        SetLanguage("English");
        project = new CQFCinematicProject { Quest = questA };
        window = new Window_CQFEntityBook(token =>
        {
            selectedToken = token;
            templateRole = project.BindEntityRole(token);
        }, quest: questA);
        Find.WindowStack.Add(window);
        PrivateSet(window, "search", template.label);
        PrivateSet(window, "filter", 2);
        ClickFirstRow();
        Go(Stage.EnglishSelect);
    }

    private void CheckAuthoring()
    {
        Check(project!.Targets.Count == 0, "choosing a future actor does not require an existing rehearsal pawn");
        string recipeToken = CQFEntityReference.Encode(recipe!.EntityBookSourceId, 1);
        string recipeRole = project.BindEntityRole(recipeToken);
        Check(project.BindEntityRole(recipeToken) == recipeRole, "repeated selection of one source member reuses its actor slot");
        destination = ReachableCell(rehearsal!, 5f);
        project.Definition.steps.Add(new CQFCinematicStep_PawnMove { pawnKey = templateRole, targetKey = project.BindTarget(new TargetInfo(destination, rehearsal!.Map)) });
        project.Definition.steps.Add(new CQFCinematicStep_PawnWait { pawnKey = recipeRole, duration = 0.1f });
        project.SetRehearsalPawn(templateRole, rehearsal);
        project.CaptureBindings();
        Check(project.Definition.bindings.All(binding => binding.key != templateRole) && project.RehearsalBindings.Any(binding => binding.key == templateRole),
            "the rehearsal pawn is kept outside the formal entity binding");
        CQFCinematicProject clone = project.CloneForTest();
        Check(clone.Definition.roles.Single(role => role.key == templateRole).sourceKey == selectedToken
            && clone.Definition.roles.Single(role => role.key == recipeRole).sourceKey == recipeToken, "checkpoint clones preserve future entity references and stable member selections");
        projectPath = project.Save();
        XElement definition = XElement.Load(projectPath).Elements().Single();
        Check(definition.Element("roles")!.Elements().Any(role => role.Element("source")?.Value == nameof(CQFCinematicRoleSource.EntityBook)
            && role.Element("sourceKey")?.Value == selectedToken), "saving an ungenerated entity actor writes its formal source and reference");
        CQFCinematicProject loaded = CQFCinematicProject.Load(projectPath);
        Check(loaded.Definition.roles.Single(role => role.key == templateRole).source == CQFCinematicRoleSource.EntityBook
            && loaded.RehearsalBindings.Any(binding => binding.key == templateRole), "scene reload preserves separate production references and rehearsal samples");
        ExpectFailure(() => CQFEntityReference.ResolveSingle(selectedToken, questA), "an ungenerated production reference fails clearly without falling back to rehearsal");
    }

    private void CheckGeneration()
    {
        Map map = Find.CurrentMap;
        IntVec3 position = ReachableCell(rehearsal!, 3f);
        Dictionary<string, TargetInfo>? spawned = recipe!.Spawn(position, map, "CQF_EntityBook_A", questA!);
        Check(spawned?.Count == 2, "real PawnSpawnData.Spawn creates the configured pair");
        IReadOnlyList<CQFEntityRecord> first = Book.GetRecords(recipe.EntityBookSourceId, questA);
        Check(first.Count == 2 && first.Select(record => record.MemberIndex).SequenceEqual(new[] { 0, 1 })
            && first.All(record => record.IsAvailable && record.Pawn!.Spawned), "the real spawn path automatically registers stable entity members");
        recipeMember = CQFEntityReference.Encode(recipe.EntityBookSourceId, 1);
        survivor = CQFEntityReference.ResolveSingle(recipeMember, questA);
        Check(survivor == first[1].Pawn, "specific member references resolve the matching generated pawn");
        ExpectFailure(() => CQFEntityReference.ResolveSingle(CQFEntityReference.Encode(recipe.EntityBookSourceId), questA), "a multi-actor source requires an explicit member");
        ExpectFailure(() => CQFEntityReference.ResolveSingle(recipeMember, questB), "a source generated for one quest cannot be resolved by another quest");
        Pawn deceased = first[0].Pawn!;
        deceased.Kill(null);
        Check(!Book.GetRecords(recipe.EntityBookSourceId, questA)[0].IsAvailable && CQFEntityReference.ResolveSingle(recipeMember, questA) == survivor,
            "a dead first member remains unavailable without shifting the second member's identity");
        ExpectFailure(() => CQFEntityReference.ResolveSingle(CQFEntityReference.Encode(recipe.EntityBookSourceId, 0), questA), "selecting a dead member reports unavailability");
        recipe.Spawn(position, map, "CQF_EntityBook_B", questB!);
        IReadOnlyList<CQFEntityRecord> second = Book.GetRecords(recipe.EntityBookSourceId, questB);
        Check(second.Count == 2 && second[0].MemberIndex == 0 && second[1].MemberIndex == 1
            && CQFEntityReference.ResolveSingle(recipeMember, questB) != survivor, "each quest owns an independent member sequence for the same source");
        CQFAction_AddQuestTag action = new() { tag = "CQF_EntityBook_Selected", targetsText = new() { recipeMember } };
        action.Work(new Dictionary<string, TargetInfo>(), questA!);
        string tag = "Quest" + questA!.id + ".CQF_EntityBook_Selected";
        Check(survivor!.questTags.Contains(tag) && second.All(record => !record.Pawn!.questTags.Contains(tag)) && rehearsal!.questTags?.Contains(tag) != true,
            "a real CQF action applies its entity target only to the selected member in the current quest");
        PawnSpawnData_ComplexPawn complex = new() { pawnDef = template!, dataName = "CQF Entity Book Template Spawn" };
        Dictionary<string, TargetInfo>? templateResult = complex.Spawn(position, map, "CQF_EntityBook_Template", questA);
        productionActor = CQFEntityReference.ResolveSingle(selectedToken, questA);
        Check(templateResult?.Values.Any(target => target.Thing == productionActor) == true
            && Book.GetRecords(complex.EntityBookSourceId, questA).Single().Pawn == productionActor, "template spawning registers both the template source and the chosen generation recipe");
        productionActor.drafter.Drafted = true;
        productionActor.jobs.EndCurrentJob(JobCondition.InterruptForced);
        survivor.drafter.Drafted = true;
        Check(CQFEntityBookBridge.DisplayReference(recipeMember).Contains(recipe.dataName, StringComparison.Ordinal)
            && !CQFEntityBookBridge.DisplayReference(recipeMember).StartsWith("cqf-entity:", StringComparison.Ordinal), "normal actor names use readable source labels instead of internal reference tokens");
        PawnSpawnData consumed = new() { kind = rehearsal!.kindDef, faction = Faction.OfPlayer.def.defName, dataName = "CQF Entity Book Consumed Source" };
        consumed.Spawn(position, map, "CQF_EntityBook_Consumed", questA!);
        consumedSource = consumed.EntityBookSourceId;
        CQFEntityBookEntry? runtimeEntry = CQFEntityBookCatalog.GetEntries(questA).SingleOrDefault(entry => entry.SourceId == consumedSource);
        Check(runtimeEntry != null && runtimeEntry.Recipe == null && runtimeEntry.Template == null && runtimeEntry.Label == consumed.dataName,
            "a generated source remains visible in the catalog when its original recipe was never collected or retained");
        Check(!CQFEntityBookCatalog.GetEntries(questB).Any(entry => entry.SourceId == consumedSource), "a consumed runtime-only source appears only in its own quest catalog");
        Check(CQFEntityBookBridge.DisplayReference(CQFEntityReference.Encode(consumedSource)) == consumed.dataName,
            "runtime-only references display their recorded source label without exposing internal identifiers");
    }

    private void StartMovement()
    {
        CQFCinematicProject playback = project!.CloneForTest();
        destination = ReachableCell(productionActor!, 5f);
        playback.Definition.steps = new() { new CQFCinematicStep_PawnMove { pawnKey = templateRole, targetKey = playback.BindTarget(new TargetInfo(destination, productionActor!.Map)), timeout = 20f, urgency = LocomotionUrgency.Jog } };
        playback.Definition.lockInput = false;
        playback.Definition.maxDuration = 30f;
        playback.CaptureBindings();
        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
        instance = Host.Play(playback.Definition, new Dictionary<string, TargetInfo>(), questA);
        Check(instance.Targets[templateRole].Thing == productionActor, "formal playback resolves the entity actor without passing any rehearsal overrides");
    }

    private void SaveWorld()
    {
        Find.TickManager.Pause();
        oldGame = Current.Game;
        oldProduction = productionActor;
        productionId = productionActor!.GetUniqueLoadID();
        survivorId = survivor!.GetUniqueLoadID();
        questAId = questA!.id;
        questBId = questB!.id;
        saveName = "CQF_EntityBook_" + Guid.NewGuid().ToString("N");
        string savePath = Path.GetFullPath(GenFilePaths.FilePathForSavedGame(saveName));
        if (Path.GetDirectoryName(savePath) != Path.GetFullPath(GenFilePaths.SavedGamesFolderPath)) throw new IOException("The fixture save would leave the isolated Saves folder.");
        GameDataSaveLoader.SaveGame(saveName);
        Check(File.Exists(savePath) && new FileInfo(savePath).Length > 0, "a full native world save includes the entity registry and its actor references");
    }

    private void CheckReload()
    {
        questA = Find.QuestManager.QuestsListForReading.Single(quest => quest.id == questAId);
        questB = Find.QuestManager.QuestsListForReading.Single(quest => quest.id == questBId);
        Pawn loadedActor = CQFEntityReference.ResolveSingle(selectedToken, questA);
        Pawn loadedSurvivor = CQFEntityReference.ResolveSingle(recipeMember, questA);
        Check(!ReferenceEquals(Current.Game, oldGame) && !ReferenceEquals(loadedActor, oldProduction) && loadedActor.GetUniqueLoadID() == productionId
            && loadedSurvivor.GetUniqueLoadID() == survivorId, "native load reconstructs new pawn objects while preserving entity references");
        IReadOnlyList<CQFEntityRecord> restored = Book.GetRecords(recipe!.EntityBookSourceId, questA);
        Check(restored.Count == 2 && restored.Select(record => record.MemberIndex).SequenceEqual(new[] { 0, 1 }) && !restored[0].IsAvailable,
            "dead member slots and the surviving member index remain stable through native world save/load");
        Check(CQFEntityReference.ResolveSingle(recipeMember, questB) != loadedSurvivor, "quest isolation survives full world reload");
        Check(CQFEntityBookCatalog.GetEntries(questA).Any(entry => entry.SourceId == consumedSource && entry.Label == "CQF Entity Book Consumed Source")
            && CQFEntityBookBridge.DisplayReference(CQFEntityReference.Encode(consumedSource)) == "CQF Entity Book Consumed Source",
            "runtime-only catalog entries and their readable labels are rebuilt from the loaded registry");
        CQFCinematicProject reloaded = CQFCinematicProject.Load(projectPath);
        Check(reloaded.Quest == questA && reloaded.Definition.ResolveBindings(Find.CurrentMap, quest: questA)[templateRole].Thing == loadedActor,
            "the authoring scene rebinds its formal entity actor and task to newly loaded objects");
        recipe.Spawn(loadedActor.Position, Find.CurrentMap, "CQF_EntityBook_AfterLoad", questA);
        IReadOnlyList<CQFEntityRecord> appended = Book.GetRecords(recipe.EntityBookSourceId, questA);
        Check(appended.Count == 4 && appended.Select(record => record.MemberIndex).SequenceEqual(new[] { 0, 1, 2, 3 })
            && CQFEntityReference.ResolveSingle(recipeMember, questA) == loadedSurvivor, "later generation appends members after reload without changing earlier references");
        Check(devtools!.MouseDowns >= 10 && devtools.MouseDowns == devtools.MouseUps && devtools.Captures.Count == 7,
            "DevTools captured both languages and the engine delivered matched native selection events");
    }

    private Quest MakeQuest(string name)
    {
        Quest quest = Quest.MakeRaw();
        quest.root = DefDatabase<QuestScriptDef>.AllDefsListForReading.First();
        quest.name = name;
        Find.QuestManager.Add(quest);
        return quest;
    }

    private void CheckNullQuestContext()
    {
        Map map = Find.CurrentMap;
        var previous = map.info.parent;
        try
        {
            map.info.parent = new MapParent_Custom { quest = questA! };
            Check(GameTools.GetQuestFromMap(map) == questA, "the context probe map is explicitly associated with a task");
            Window_CQFEntityBook global = new(quest: null);
            object? actual = typeof(Window_CQFEntityBook).GetField("quest", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(global);
            Check(actual == null, "explicit null task context remains global even on a task-associated map");
        }
        finally { map.info.parent = previous; }
    }

    private IntVec3 ReachableCell(Pawn pawn, float radius)
        => GenRadial.RadialCellsAround(pawn.Position, radius, false).First(cell => cell.InBounds(pawn.Map) && cell.Standable(pawn.Map)
            && cell.DistanceToSquared(pawn.Position) >= 4f && pawn.Map.reachability.CanReach(pawn.Position, cell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)));

    private void ClickFirstRow()
    {
        float margin = (float)typeof(Window).GetProperty("Margin", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        devtools!.Click(window!.windowRect.position + new Vector2(margin + 100f, margin + 150f + 6f + 32f));
    }

    private void ClickReferenceButton()
    {
        float margin = (float)typeof(Window).GetProperty("Margin", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        Rect select = Private<Rect>(window!, "selectEntityRect");
        if (select.width < 1f || select.height < 1f) throw new InvalidOperationException("The native entity selection button was not drawn.");
        devtools!.Click(window!.windowRect.position + new Vector2(margin, margin) + select.center);
    }

    private static Vector2 WindowPoint(Window owner, Vector2 point)
    {
        float margin = (float)typeof(Window).GetProperty("Margin", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
        return owner.windowRect.position + new Vector2(margin, margin) + point;
    }

    private void SetLanguage(string folder)
    {
        LoadedLanguage language = LanguageDatabase.AllLoadedLanguages.Single(candidate => candidate.folderName == folder);
        LanguageDatabase.activeLanguage = language;
        language.LoadMetadata();
        language.LoadData();
        Check("CQF_EntityBook_Title".Translate().ToString() != "CQF_EntityBook_Title", "entity book UI labels resolve in " + folder);
    }

    private static T Private<T>(object owner, string name)
        => (T)(owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner) ?? throw new MissingFieldException(name));

    private static void PrivateSet(object owner, string name, object value)
        => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);

    private void ExpectFailure(Action work, string description)
    {
        string? failure = null;
        try { work(); }
        catch (InvalidOperationException error) { failure = error.Message; }
        Check(!string.IsNullOrWhiteSpace(failure), description);
        File.AppendAllText(Results, "EXPECTED " + failure + "\n", Encoding.UTF8);
    }

    private void CloseLogWindows()
    {
        foreach (Window log in Find.WindowStack.Windows.Where(candidate => candidate.GetType().Name == "EditWindow_Log").ToArray()) log.Close(false);
    }

    private void Check(bool passed, string description)
    {
        if (!passed) throw new InvalidOperationException(description);
        File.AppendAllText(Results, "PASS " + ++checks + " " + description + "\n", Encoding.UTF8);
    }

    private void Go(Stage next)
    {
        stage = next;
        stageStarted = Time.realtimeSinceStartup;
        File.AppendAllText(Results, "TRACE stage=" + stage + "\n", Encoding.UTF8);
    }

    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        devtools?.Dispose();
        File.WriteAllText(Path.Combine(Root, "EntityBookEvidence.json"), JsonSerializer.Serialize(new
        {
            tool = "RimDevTools native screenshot service and ImguiDispatch engine events", success, checks, stage = stage.ToString(),
            mouseDowns = devtools?.MouseDowns, mouseUps = devtools?.MouseUps, captures = devtools?.Captures,
            inputScope = "Native window selection uses engine GUI events. Search filters are fixture values. No OS mouse input or computer-use tool is used."
        }, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        File.AppendAllText(Results, success ? "COMPLETE\n" : "FAILED\n", Encoding.UTF8);
        if (Current.Game?.Info != null) Current.Game.Info.permadeathMode = false;
        Application.Quit(success ? 0 : 1);
    }

    private enum Stage { Boot, EnglishSelect, EnglishCapture, EmptySearch, EmptySearchCapture, EmptySearchRejected, EnglishReselect, EnglishChosen, CommonSettings, CommonSettingsCapture, TypeCancelled, TypeConfirmationCapture, TypeConfirmed, MapPlacement, MapPlacementCapture, MapActorAdded, ChineseSelect, ChineseCapture, ChineseMemberRejected, ChineseRecipePreview, ChineseMemberChosen, Moving, BeforeLoad, AfterLoad }
    private EntityBookDevtools? devtools;
    private Window_CQFEntityBook? window;
    private CQFCinematicProject? project;
    private Dialog_CQFEntityRecipe? recipeDialog;
    private Dialog_MessageBox? typeConfirmation;
    private Window_CQFEntityPlacement? placementWindow;
    private PawnSpawnData? typeRecipe;
    private string typeSource = "";
    private Vector2 staleSelection;
    private PawnSpawnData? recipe;
    private ComplexPawnDef? template;
    private Quest? questA;
    private Quest? questB;
    private Pawn? rehearsal;
    private Pawn? productionActor;
    private Pawn? survivor;
    private Pawn? oldProduction;
    private Pawn? templatePreview;
    private Pawn? recipePreview;
    private Game? oldGame;
    private CQFCinematicInstance? instance;
    private IntVec3 destination;
    private IntVec3 rehearsalPosition;
    private string templateSource = "";
    private string selectedToken = "";
    private string selectedRecipeToken = "";
    private string recipeMember = "";
    private string consumedSource = "";
    private string templateRole = "";
    private string projectPath = "";
    private string saveName = "";
    private string productionId = "";
    private string survivorId = "";
    private int questAId;
    private int questBId;
    private int initialMapPawns;
    private int initialWorldPawns;
    private int initialUniquePawns;
    private int checks;
    private float startedAt;
    private float stageStarted;
    private Stage stage;
    private bool sawGoto;
    private bool finished;
}
