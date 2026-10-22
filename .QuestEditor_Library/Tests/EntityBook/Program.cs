using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using Verse;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            ConfigureRuntime();
            if (args.Contains("--benchmark-records", StringComparer.Ordinal))
            {
                EntityBookRecordPerformance.Run(NewPawn, NewQuest);
                return 0;
            }
            CheckReferenceEncoding();
            CheckRecipeIdentity();
            CheckBindingsAndPersistence();
            CheckPreviewMode();
            CheckActionTargets();
            Check(EntityBookTestLog.Errors.Count == 0, "native XML and Scribe have no hidden errors: " + string.Join("\n", EntityBookTestLog.Errors));
            Console.WriteLine("Entity book checks passed: " + checks);
            return 0;
        }
        catch (Exception error) { Console.WriteLine(error); return 1; }
    }

    private static void ConfigureRuntime()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        new Harmony("CQF.EntityBookChecks.Log").Patch(AccessTools.Method(typeof(Log), nameof(Log.Error)),
            prefix: new HarmonyMethod(typeof(EntityBookTestLog), nameof(EntityBookTestLog.Error)));
        ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        content.assemblies = new ModAssemblyHandler(content);
        content.assemblies.loadedAssemblies.Add(typeof(CQFEntityReference).Assembly);
        content.assemblies.loadedAssemblies.Add(typeof(Program).Assembly);
        LoadedModManager.RunningModsListForReading.Add(content);
        GenTypes.ClearCache();
        LoadedLanguage language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
        language.folderName = "English";
        language.info = new LanguageInfo { languageWorkerClass = typeof(LanguageWorker_English) };
        language.keyedReplacements = new();
        typeof(LoadedLanguage).GetField("dataIsLoaded", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(language, true);
        LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
    }

    private static void CheckReferenceEncoding()
    {
        foreach (string source in new[] { "spawn:abc123", "template:CQF_Merchant", "template:CQF_Name:%/特殊" })
        {
            foreach ((int? member, bool group) in new[] { ((int?)null, false), (null, true), (0, false), (41, false) })
            {
                string token = CQFEntityReference.Encode(source, member, group);
                Check(CQFEntityReference.IsReference(token) && CQFEntityReference.TryParse(token, out CQFEntityReference? parsed)
                    && parsed!.SourceId == source && parsed.MemberIndex == member && parsed.Group == group, "reference roundtrip " + token);
            }
        }
        Check(!CQFEntityReference.IsReference("Trigger") && !CQFEntityReference.TryParse("Trigger", out _), "existing target names stay outside entity references");
        foreach (string token in new[] { "cqf-entity:", "cqf-entity::single", "cqf-entity:spawn%3Aabc:-1", "cqf-entity:spawn%3Aabc:other" })
            Check(!CQFEntityReference.TryParse(token, out _), "malformed reference rejected " + token);
        Throws<ArgumentException>(() => CQFEntityReference.Encode("spawn:abc", -1), "negative member rejected");
        Throws<ArgumentException>(() => CQFEntityReference.Encode("spawn:abc", 0, true), "group and member selectors cannot conflict");
    }

    private static void CheckRecipeIdentity()
    {
        PawnSpawnData[] recipes = { new(), new PawnSpawnData_Faction(), new PawnSpawnData_Group(), new PawnSpawnData_Random(),
            new PawnSpawnData_ComplexPawn(), new MainPawnSpawnData() };
        foreach (PawnSpawnData recipe in recipes)
        {
            string id = recipe.EntityBookSourceId;
            recipe.dataName = "CQF_TestRecipe";
            PawnSpawnData copy = recipe.Copy();
            Check(copy.EntityBookSourceId == id && copy.GetType() == recipe.GetType(), "XML copy preserves source identity and type " + recipe.GetType().Name);
            recipe.dataName = "CQF_RenamedRecipe";
            Check(recipe.EntityBookSourceId == id, "renaming does not change source identity " + recipe.GetType().Name);
            Check(RoundTrip(recipe).EntityBookSourceId == id, "native Scribe preserves source identity " + recipe.GetType().Name);
        }
        PawnSpawnData fresh = new();
        Check(fresh.EntityBookSourceId != recipes[0].EntityBookSourceId, "new recipes receive distinct source identities");
        MainPawnSpawnData nested = new() { spawnData = new PawnSpawnData_Random { datas = new() { new PawnSpawnData() } } };
        MainPawnSpawnData copied = (MainPawnSpawnData)nested.Copy();
        Check(copied.EntityBookSourceId == nested.EntityBookSourceId && copied.spawnData.EntityBookSourceId == nested.spawnData.EntityBookSourceId
            && ((PawnSpawnData_Random)copied.spawnData).datas[0].EntityBookSourceId == ((PawnSpawnData_Random)nested.spawnData).datas[0].EntityBookSourceId,
            "nested generation recipes preserve each independent source identity");
        PawnSpawnData dropPod = new() { way = new ArrivingWay_DropPod() };
        Check(dropPod.Copy().way is ArrivingWay_DropPod, "recipe XML copying preserves the selected arrival mode");
        PawnSpawnData extension = new() { way = new EntityBookArrivingWay { delay = 47 } };
        Check(extension.Copy().way is EntityBookArrivingWay { delay: 47 }, "recipe XML copying preserves an extension arrival mode and its settings");
    }

    private static void CheckBindingsAndPersistence()
    {
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        game.components = new();
        typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, new List<Map>());
        Current.Game = game;
        EntityBookState state = new();
        GameComponent_EntityBook book = state.Book;
        game.components.Add(book);
        Quest firstQuest = NewQuest(101);
        Quest secondQuest = NewQuest(102);
        Pawn first = NewPawn(401);
        Pawn second = NewPawn(402);
        Pawn third = NewPawn(403);
        string source = "spawn:CQF_CheckRecipe";
        string unique = CQFEntityReference.Encode(source);
        book.RegisterBatch(source, new[] { first }, firstQuest, sourceLabel: "merchant");
        book.RegisterBatch(source, new[] { second }, secondQuest);
        book.RegisterBatch(source, new[] { third });
        Check(CQFEntityReference.ResolveSingle(unique, firstQuest) == first, "first quest resolves its own source record");
        Check(CQFEntityReference.ResolveSingle(unique, secondQuest) == second, "second quest cannot borrow another quest's actor");
        Check(CQFEntityReference.ResolveSingle(unique) == third, "no-quest scope resolves only no-quest records");
        Throws<InvalidOperationException>(() => CQFEntityReference.ResolveSingle(unique, NewQuest(103)), "missing quest scope cannot fall back to no-quest records");
        book.RegisterBatch(source, new[] { first, first }, firstQuest);
        Check(book.GetRecords(source, firstQuest).Count == 1, "re-registering the same pawn is idempotent");
        book.RegisterBatch(source, new[] { second }, firstQuest);
        Throws<InvalidOperationException>(() => CQFEntityReference.ResolveSingle(unique, firstQuest), "ambiguous sources require an explicit member");
        string firstToken = CQFEntityReference.Encode(source, 0);
        string secondToken = CQFEntityReference.Encode(source, 1);
        string groupToken = CQFEntityReference.Encode(source, group: true);
        Check(CQFEntityReference.ResolveSingle(firstToken, firstQuest) == first && CQFEntityReference.ResolveSingle(secondToken, firstQuest) == second,
            "explicit members use stable generation order");
        Check(GameTools.GetTarget(new(), firstQuest, secondToken).Thing == second, "single target resolver accepts entity references");
        Dictionary<string, TargetInfo> groupTargets = GameTools.GetTargets(new(), firstQuest, new() { groupToken });
        Check(groupTargets.Count == 2 && groupTargets[firstToken].Thing == first && groupTargets[secondToken].Thing == second,
            "group resolver expands members under their stable explicit references");
        Dictionary<string, TargetInfo> singleTargets = GameTools.GetTargets(new(), firstQuest, new() { secondToken });
        Check(singleTargets.Count == 1 && singleTargets[secondToken].Thing == second, "single entity selection is never expanded as a legacy database group");
        Throws<InvalidOperationException>(() => GameTools.GetTarget(new(), firstQuest, groupToken), "single-target use rejects a multi-member group");
        Check(GameTools.GetTarget(new() { [unique] = new TargetInfo(third) }, firstQuest, unique).Thing == third,
            "explicit target context retains priority over entity resolution");
        Check(GameTools.GetTarget(new Dictionary<string, TargetInfo>(StringComparer.OrdinalIgnoreCase)
            { [secondToken.ToUpperInvariant()] = new TargetInfo(third) }, firstQuest, secondToken).Thing == second,
            "custom context dictionary comparers do not change exact target-key matching");
        Check(GameTools.GetTargets(new() { [groupToken] = new TargetInfo(third) }, firstQuest, new() { groupToken }).Single().Value.Thing == third,
            "explicit group-token context retains its selected target");
        CQFCinematicRole role = new() { key = "CQF_Lead", source = CQFCinematicRoleSource.EntityBook, sourceKey = secondToken };
        Check(role.Resolve(null!, Array.Empty<CQFCinematicBinding>(), quest: firstQuest).Thing == second, "cinematic actor uses the same entity reference resolver");
        Check(role.Resolve(null!, Array.Empty<CQFCinematicBinding>(), new Dictionary<string, TargetInfo> { [role.key] = new TargetInfo(third) }, firstQuest).Thing == third,
            "explicit rehearsal actor overrides formal entity-book source");
        typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(first.health, PawnHealthState.Dead);
        Throws<InvalidOperationException>(() => CQFEntityReference.ResolveSingle(firstToken, firstQuest), "dead member fails explicitly");
        Check(CQFEntityReference.ResolveSingle(secondToken, firstQuest) == second, "death never shifts another member into a removed member slot");
        Throws<InvalidOperationException>(() => CQFEntityReference.ResolveSingle(unique, firstQuest), "one surviving member does not remove historical ambiguity");
        groupTargets = GameTools.GetTargets(new(), firstQuest, new() { groupToken });
        Check(groupTargets.Count == 1 && groupTargets[secondToken].Thing == second && !groupTargets.ContainsKey(firstToken),
            "live group filtering retains original member identity");
        book.RegisterBatch(source, new[] { third }, firstQuest);
        Check(book.GetRecords(source, firstQuest).Select(record => record.MemberIndex).SequenceEqual(new[] { 0, 1, 2 }), "new generations append without recycling dead member indexes");
        Check(ReferenceEquals(book.Records, book.Records), "all-records read-only view is reused");
        IReadOnlyList<CQFEntityRecord> indexed = book.GetRecords(source, firstQuest);
        Check(ReferenceEquals(indexed, book.GetRecords(source, firstQuest)), "source record reads reuse the indexed read-only view");
        Throws<NotSupportedException>(() => ((ICollection<CQFEntityRecord>)indexed).Clear(), "source records cannot be changed through the returned view");
        Throws<NotSupportedException>(() => ((ICollection<CQFEntityRecord>)book.Records).Clear(), "all records cannot be changed through the returned view");
        Check(book.GetRecords("spawn:CQF_Unknown").Count == 0, "an absent indexed source remains empty");
        state = RoundTrip(state, first, second, third);
        game.components.Clear();
        game.components.Add(state.Book);
        Check(state.Book.GetRecords(source, firstQuest).Count == 3 && CQFEntityReference.ResolveSingle(secondToken, firstQuest) == second,
            "native Scribe restores references and stable indexes");
        Check(CQFEntityReference.ResolveSingle(unique, secondQuest) == second && CQFEntityReference.ResolveSingle(unique) == third,
            "native Scribe retains quest and no-quest scope separation");
        state.Book.RegisterBatch(source, new[] { second }, firstQuest);
        Check(state.Book.GetRecords(source, firstQuest).Count == 3, "loaded bindings remain idempotent");
        Pawn next = NewPawn(404);
        state.Book.RegisterBatch(source, new[] { next }, firstQuest);
        Check(CQFEntityReference.ResolveSingle(CQFEntityReference.Encode(source, 3), firstQuest) == next, "next member identity continues correctly after loading");
        Game otherGame = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        otherGame.components = new() { new GameComponent_EntityBook(otherGame) };
        Current.Game = otherGame;
        Throws<InvalidOperationException>(() => CQFEntityReference.ResolveSingle(secondToken, firstQuest), "another game cannot resolve stale world records");
        Current.Game = game;
    }

    private static void CheckPreviewMode()
    {
        PawnKindDef kind = (PawnKindDef)RuntimeHelpers.GetUninitializedObject(typeof(PawnKindDef));
        kind.defName = "CQF_EntityBookPreviewKind";
        kind.race = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        kind.race.race = new RaceProperties { lifeStageAges = new() };
        ComplexPawnDef template = new() { defName = "CQF_EntityBookPreviewTemplate",
            modDatas = new() { new PawnModData_Basic { kindDef = kind } } };
        PawnModDef mod = new() { defName = "CQF_EntityBookPreviewProbe", workerClass = typeof(EntityBookPreviewWorker) };
        DefDatabase<PawnModDef>.Add(mod);
        EntityBookPawnGenerator.Pawn = NewPawn(451);
        Harmony harmony = new("CQF.EntityBookChecks.Generation");
        MethodInfo generation = AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) });
        harmony.Patch(generation, prefix: new HarmonyMethod(typeof(EntityBookPawnGenerator), nameof(EntityBookPawnGenerator.Generate)));
        try
        {
            GameComponent_EntityBook book = Current.Game.GetComponent<GameComponent_EntityBook>();
            int before = book.Records.Count;
            Check(template.CreatePreviewPawn() == EntityBookPawnGenerator.Pawn && EntityBookPreviewWorker.Modes.SequenceEqual(new[] { true }),
                "preview creation passes preview=true to entity modules");
            Check(EntityBookPawnGenerator.Requests[0].ForceGenerateNewPawn && !EntityBookPawnGenerator.Requests[0].CanGeneratePawnRelations,
                "preview generation always creates a new pawn without world pawn relations");
            Check(book.Records.Count == before && EntityBookPreviewWorker.SpawnNotifications == 0,
                "preview creation does not register actors or notify formal spawn callbacks");
            Check(template.Spawn() == EntityBookPawnGenerator.Pawn && EntityBookPreviewWorker.Modes.SequenceEqual(new[] { true, false }),
                "formal generation passes preview=false to entity modules");
            Check(!EntityBookPawnGenerator.Requests[1].ForceGenerateNewPawn && EntityBookPawnGenerator.Requests[1].CanGeneratePawnRelations,
                "formal generation keeps the existing world pawn and relation request defaults");
        }
        finally
        {
            harmony.Unpatch(generation, HarmonyPatchType.All, harmony.Id);
            AccessTools.Method(typeof(DefDatabase<PawnModDef>), "Remove").Invoke(null, new object[] { mod });
        }
    }

    private static void CheckActionTargets()
    {
        GameComponent_EntityBook book = Current.Game.GetComponent<GameComponent_EntityBook>();
        Quest quest = NewQuest(201);
        Pawn first = NewPawn(501);
        Pawn second = NewPawn(502);
        Pawn replacement = NewPawn(503);
        first.relations = (Pawn_RelationsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RelationsTracker));
        replacement.relations = (Pawn_RelationsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RelationsTracker));
        string firstToken = CQFEntityReference.Encode("spawn:CQF_ActionFirst");
        string secondToken = CQFEntityReference.Encode("spawn:CQF_ActionSecond");
        string missing = CQFEntityReference.Encode("spawn:CQF_ActionMissing");
        book.RegisterBatch("spawn:CQF_ActionFirst", new[] { first }, quest);
        book.RegisterBatch("spawn:CQF_ActionSecond", new[] { second }, quest);
        PawnRelationDef relation = new() { defName = "CQF_EntityBookCheckRelation" };
        CQFAction_SetRelation action = new() { targetA = firstToken, targetB = secondToken, relation = relation };
        Harmony harmony = new("CQF.EntityBookChecks.Relations");
        MethodInfo adding = AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation));
        harmony.Patch(adding, prefix: new HarmonyMethod(typeof(EntityBookRelationProbe), nameof(EntityBookRelationProbe.Add)));
        try
        {
            action.Work(new(), quest);
            Check(EntityBookRelationProbe.Calls == 1 && EntityBookRelationProbe.Tracker == first.relations && EntityBookRelationProbe.Other == second
                && EntityBookRelationProbe.Relation == relation, "relation actions resolve both entity-book actor references");
            action.Work(new() { [firstToken] = new TargetInfo(replacement) }, quest);
            Check(EntityBookRelationProbe.Calls == 2 && EntityBookRelationProbe.Tracker == replacement.relations,
                "relation actions retain explicit target context priority for entity references");
            action.targetA = "CQF_LegacyActor";
            action.targetB = "CQF_LegacyOther";
            action.Work(new() { [action.targetA] = new TargetInfo(first), [action.targetB] = new TargetInfo(second) }, quest);
            Check(EntityBookRelationProbe.Calls == 3 && EntityBookRelationProbe.Tracker == first.relations && EntityBookRelationProbe.Other == second,
                "ordinary action target keys retain their existing direct-context behavior");
        }
        finally { harmony.Unpatch(adding, HarmonyPatchType.All, harmony.Id); }
        Throws<InvalidOperationException>(() => new CQFAction_StartDialog { interviewerText = missing, intervieeText = secondToken }.Work(new(), quest),
            "dialog action rejects a missing entity-book interviewer explicitly");
        Throws<InvalidOperationException>(() => new CQFAction_SetRelation { targetA = missing, targetB = secondToken }.Work(new(), quest),
            "relation action rejects a missing entity-book actor explicitly");
        Throws<InvalidOperationException>(() => new CQFAction_Skip { skipedTargetText = missing, targetLocationText = secondToken }.Work(new(), quest),
            "teleport action rejects a missing entity-book actor explicitly");
        Throws<InvalidOperationException>(() => new CQFAction_Skip { skipedTargetText = firstToken, targetLocationText = missing }.Work(new(), quest),
            "teleport destination can use an entity reference and rejects a missing one explicitly");
        Throws<InvalidOperationException>(() => new CQFAction_SkipToPlayerMap { skipedTargetText = missing }.Work(new(), quest),
            "return-home action rejects a missing entity-book actor explicitly");
        Throws<InvalidOperationException>(() => new CQFAction_StartMentalState { stateTargetText = missing }.RealWork(new(), quest),
            "mental-state action rejects a missing entity-book opponent explicitly");
    }

    private static Quest NewQuest(int id)
    {
        Quest quest = (Quest)RuntimeHelpers.GetUninitializedObject(typeof(Quest));
        quest.id = id;
        return quest;
    }

    private static Pawn NewPawn(int id)
    {
        Pawn pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "CQF_EntityBookCheckPawn";
        pawn.thingIDNumber = id;
        pawn.health = (Pawn_HealthTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_HealthTracker));
        typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pawn.health, PawnHealthState.Mobile);
        typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pawn, (sbyte)-1);
        return pawn;
    }

    private static T RoundTrip<T>(T source, params ILoadReferenceable[] references) where T : class, IExposable
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_EntityBook_" + Guid.NewGuid().ToString("N") + ".xml");
        T? value = source;
        try
        {
            Scribe.saver.InitSaving(path, "checks");
            Scribe_Deep.Look(ref value, "state");
            Scribe.saver.FinalizeSaving();
            value = null;
            Scribe.loader.InitLoading(path);
            LoadedObjectDirectory directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Scribe.loader.crossRefs)!;
            foreach (ILoadReferenceable reference in references) directory.RegisterLoaded(reference);
            Scribe_Deep.Look(ref value, "state");
            Scribe.loader.FinalizeLoading();
            return value ?? throw new InvalidDataException("Native Scribe lost entity-book data.");
        }
        finally { Scribe.ForceStop(); if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        checks++;
    }

    private static void Throws<T>(Action action, string name) where T : Exception
    {
        try { action(); }
        catch (T) { Check(true, name); return; }
        throw new InvalidOperationException(name);
    }

    private static int checks;
}
