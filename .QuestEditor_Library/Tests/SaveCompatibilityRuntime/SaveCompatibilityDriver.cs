using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.SaveCompatibilityRuntimeChecks;

public sealed class SaveCompatibilityDriver : MonoBehaviour
{
    private string Root => GenFilePaths.SaveDataFolderPath;
    private string Results => Path.Combine(Root, Producer ? "LegacyProducerChecks.txt" : "SaveCompatibilityChecks.txt");
    private GameComponent_Editor Editor => Current.Game.GetComponent<GameComponent_Editor>();
    private GameComponent_Cinematics Host => GameComponent_Cinematics.Instance!;
    private Pawn Actor => Editor.pawns["CQF_CompatibilityUnique"];
    private Quest Quest => Find.QuestManager.QuestsListForReading.Single(value => value.name == "CQF Compatibility Quest");
    private PawnSpawnData Recipe => Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().pawnSpawnDatas_Tick
        .Single(entry => entry.data.dataName == "CQF Compatibility Deferred").data;

    private void Update()
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup > 240f) throw new TimeoutException("Save compatibility fixture timed out at " + stage);
            if (LongEventHandler.AnyEventNowOrWaiting || LongEventHandler.ShouldWaitForEvent) return;
            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Current.Game.PlayerHasControl) return;
            Find.TickManager.Pause();
            switch (stage)
            {
                case 0:
                    File.WriteAllText(Results, "TRACE Actual published 4.4 assembly writes the producer save.\n", Encoding.UTF8);
                    Check(typeof(CQFCinematicDef).Assembly.Location.Contains("GameSnapshot", StringComparison.OrdinalIgnoreCase), "only the isolated snapshot runtime is loaded");
                    if (Producer) Produce();
                    else
                    {
                        previousGame = Current.Game;
                        stage = 10;
                        GameDataSaveLoader.LoadGame(LegacySave);
                    }
                    break;
                case 1:
                    if (Host.Active?.StepIndex != 1 || Host.Active.Elapsed < 0.3f) return;
                    Check(Editor.GetQuestData(Quest).GetGroup("CQF_CompatibilityAction").Count == 1, "the legacy cinematic action executes once before its waiting step");
                    Save(LegacySave);
                    XElement oldXml = XElement.Load(GenFilePaths.FilePathForSavedGame(LegacySave));
                    Check(!oldXml.Descendants().Any(element => element.Name.LocalName == "entityBookId")
                        && !oldXml.Descendants().Attributes("Class").Any(attribute => attribute.Value == "QuestEditor_Library.GameComponent_EntityBook"),
                        "the published assembly creates an authentic save without entity book fields or component");
                    File.WriteAllText(Path.Combine(Root, "LegacyActor.txt"), Actor.GetUniqueLoadID(), Encoding.UTF8);
                    Finish(true);
                    break;
#if !CQF_LEGACY_PRODUCER
                case 10:
                    if (ReferenceEquals(previousGame, Current.Game) || Host.Active?.StepIndex != 1) return;
                    ValidateUpgrade();
                    stageAt = Time.realtimeSinceStartup;
                    stage = 11;
                    break;
                case 11:
                    if (Time.realtimeSinceStartup - stageAt < 0.5f) return;
                    Check(Editor.GetQuestData(Quest).GetGroup("CQF_CompatibilityAction").Count == 1,
                        "resuming the old cinematic does not repeat an already executed action");
                    Check(Host.Active?.StepIndex == 1 && Host.Active.Elapsed > oldElapsed, "the old running cinematic advances its saved waiting state");
                    Save("CQF_CompatibilityIdentity");
                    Check(!string.IsNullOrWhiteSpace(Recipe.entityBookId), "saving an untouched old recipe assigns its missing entity identity");
                    sourceId = Recipe.EntityBookSourceId;
                    XElement upgradedXml = XElement.Load(GenFilePaths.FilePathForSavedGame("CQF_CompatibilityIdentity"));
                    Check(upgradedXml.Descendants("entityBookId").Any(element => element.Value == Recipe.entityBookId), "the first upgraded save persists the newly assigned recipe identity");
                    Recipe.Spawn(Actor.Position, Find.CurrentMap, "CQF_CompatibilityDeferred", Quest);
                    Check(Current.Game.GetComponent<GameComponent_EntityBook>().GetRecords(sourceId, Quest).Count == 1,
                        "the loaded legacy generation recipe still spawns and registers its new actor");
                    Editor.pawns["CQF_CompatibilityLaterCache"] = Actor;
                    Save(UpgradedSave);
                    previousGame = Current.Game;
                    stage = 12;
                    GameDataSaveLoader.LoadGame(UpgradedSave);
                    break;
                case 12:
                    if (ReferenceEquals(previousGame, Current.Game) || Host.Active?.StepIndex != 1) return;
                    ValidateSecondLoad();
                    Finish(true);
                    break;
#endif
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(Results, "FAIL " + error + "\n", Encoding.UTF8);
            Log.Error("[CQF save compatibility] " + error);
            Finish(false);
        }
    }

    private void Produce()
    {
        DebugSettings.pauseOnError = false;
        Pawn actor = Find.CurrentMap.mapPawns.FreeColonistsSpawned.First();
        Editor.pawns["CQF_CompatibilityUnique"] = actor;
        Quest quest = RimWorld.Quest.MakeRaw();
        quest.root = DefDatabase<QuestScriptDef>.AllDefsListForReading.First();
        quest.name = "CQF Compatibility Quest";
        Find.QuestManager.Add(quest);
        Editor.GetQuestData(quest).RecordTarget("CQF_LegacyActor", new TargetInfo(actor));
        Editor.GlobalDatabase.RecordTarget("CQF_LegacyGlobalActor", new TargetInfo(actor));
        Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().pawnSpawnDatas_Tick.Add(new PawnDataWithPosAndTime
        {
            time = 0, position = actor.Position,
            data = new PawnSpawnData { kind = actor.kindDef, faction = Faction.OfPlayer.def.defName, dataName = "CQF Compatibility Deferred",
                count = new IntRange(1, 1), generationChance = 1f, way = new ArrivingWay(), spawnType = SpawnType.BuildingTick, timeToSpawn = int.MaxValue }
        });
        CQFCinematicDef definition = new()
        {
            defName = "CQF_CompatibilityRunning", lockInput = false, restoreCamera = false, maxDuration = 1000f,
            roles = new() { new CQFCinematicRole { key = "CQF_Actor", label = "legacy actor", source = CQFCinematicRoleSource.QuestDatabase, sourceKey = "CQF_LegacyActor" } },
            steps = new()
            {
                new CQFCinematicStep_Actions { actions = new() { new CQFAction_RecordToGroup { recordKey = "CQF_CompatibilityAction", targetsText = new() { "CQF_Actor" } } } },
                new CQFCinematicStep_Wait { duration = 600f }
            }
        };
        Host.Play(definition, new Dictionary<string, TargetInfo>(), quest);
        stage = 1;
    }

#if !CQF_LEGACY_PRODUCER
    private void ValidateUpgrade()
    {
        GameComponent_EntityBook? loadedBook = Current.Game.GetComponent<GameComponent_EntityBook>();
        Check(loadedBook != null, "native loading adds the entity book component absent from a 4.4 save");
        GameComponent_EntityBook book = loadedBook ?? throw new InvalidOperationException("The upgraded game has no entity book component.");
        string actorId = File.ReadAllText(Path.Combine(Root, "LegacyActor.txt"), Encoding.UTF8).Trim();
        Check(Actor.GetUniqueLoadID() == actorId && Actor.Spawned && !Actor.Destroyed, "the old unique cache keeps the same real pawn identity");
        IReadOnlyList<CQFEntityRecord> records = book.GetRecords("template:CQF_CompatibilityUnique");
        Check(records.Count == 1 && records[0].Pawn == Actor && records[0].MemberIndex == 0,
            "the proven legacy unique template cache is imported as one stable unscoped member");
        Check(book.GetRecords("template:CQF_CompatibilityUnique", Quest).Count == 0, "legacy import never invents quest ownership");
        Check(CQFEntityReference.ResolveSingle(CQFEntityReference.Encode("template:CQF_CompatibilityUnique")) == Actor,
            "the newly available entity reference resolves the original pawn");
        Check(Editor.GetQuestData(Quest).GetTarget("CQF_LegacyActor").Thing == Actor
            && Editor.GlobalDatabase.GetTarget("CQF_LegacyGlobalActor").Thing == Actor, "both legacy quest and global database references survive");
        CQFCinematicRole role = new() { key = "CQF_Actor", label = "legacy actor", source = CQFCinematicRoleSource.QuestDatabase, sourceKey = "CQF_LegacyActor" };
        Check(role.Resolve(Find.CurrentMap, Array.Empty<CQFCinematicBinding>(), quest: Quest).Thing == Actor,
            "the old database actor source still resolves without conversion to entity references");
        Check(Recipe.entityBookId == "" && Recipe.count.min == 1 && Recipe.count.max == 1 && Recipe.generationChance == 1f
            && Recipe.way != null && Recipe.spawnType == SpawnType.BuildingTick, "old deferred recipe fields and defaults load without new identity data");
        Check(Host.Active!.Targets["CQF_Actor"].Thing == Actor, "the old running cinematic keeps its saved actor reference");
        oldElapsed = Host.Active.Elapsed;
    }

    private void ValidateSecondLoad()
    {
        GameComponent_EntityBook book = Current.Game.GetComponent<GameComponent_EntityBook>();
        Check(book.GetRecords("template:CQF_CompatibilityUnique").Count == 1
            && book.GetRecords("template:CQF_CompatibilityUnique")[0].Pawn == Actor, "a second load preserves the imported record without duplicate migration");
        Check(book.GetRecords("template:CQF_CompatibilityLaterCache").Count == 0,
            "a current-format save does not retroactively import a later cache entry");
        Check(Recipe.EntityBookSourceId == sourceId && book.GetRecords(sourceId, Quest).Count == 1,
            "recipe identity and its newly generated actor survive the upgraded save reload");
        Check(Editor.GetQuestData(Quest).GetGroup("CQF_CompatibilityAction").Count == 1,
            "the old cinematic action remains exactly once after the second native reload");
        Check(book.GetRecords("template:CQF_CompatibilityUnique", Quest).Count == 0,
            "quest isolation remains unchanged after migration and resave");
    }
#endif

    private void Save(string name)
    {
        string path = Path.GetFullPath(GenFilePaths.FilePathForSavedGame(name));
        if (Path.GetDirectoryName(path) != Path.GetFullPath(GenFilePaths.SavedGamesFolderPath)) throw new IOException("Unexpected fixture save location.");
        GameDataSaveLoader.SaveGame(name);
        Check(File.Exists(path) && new FileInfo(path).Length > 0, "a complete native save was written: " + name);
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(Results, "PASS " + ++checks + " " + message + "\n", Encoding.UTF8);
    }

    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        File.AppendAllText(Results, success ? "COMPLETE\n" : "FAILED\n", Encoding.UTF8);
        File.WriteAllText(Path.Combine(Root, Producer ? "LegacyProducerEvidence.json" : "SaveCompatibilityEvidence.json"),
            JsonSerializer.Serialize(new { success, checks, producer = Producer, stage }, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        if (Current.Game?.Info != null) Current.Game.Info.permadeathMode = false;
        Application.Quit(success ? 0 : 1);
    }

    private const string LegacySave = "CQF_CompatibilityLegacy44";
    private const string UpgradedSave = "CQF_CompatibilityUpgraded";
#if CQF_LEGACY_PRODUCER
    private static readonly bool Producer = true;
#else
    private static readonly bool Producer = false;
#endif
    private int stage;
    private int checks;
    private bool finished;
#if !CQF_LEGACY_PRODUCER
    private float stageAt;
    private float oldElapsed;
    private string sourceId = "";
#endif
    private Game? previousGame;
}
