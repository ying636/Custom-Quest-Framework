using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using Verse;

internal static class FrameworkSettingsChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        new Harmony("CQF.Checks.FrameworkSettings").Patch(AccessTools.Method(typeof(Log), nameof(Log.Error)),
            prefix: new HarmonyMethod(typeof(ChecksLogHandler), nameof(ChecksLogHandler.RecordError)));
        CheckPersistence();
        CheckSchedule();
        Check(ChecksLogHandler.ErrorCount == 0, "framework settings checks produce no logged errors");
    }

    private static void CheckPersistence()
    {
        CustomQuestFramework_ModSetting previous = CustomQuestFramework_ModSetting.setting;
        string path = Path.GetTempFileName();
        try
        {
            CustomQuestFramework_ModSetting settings = new();
            Check(settings.alwaysLoadedModules.Count == 0 && settings.QuestBookCheckInterval == GenDate.TicksPerDay,
                "modules default to demand loading and quest book checks default to one game day");
            settings.alwaysLoadedModules.AddRange(new[] { "Domain", "MapProjections", "CQF_Check_FutureModule" });
            settings.questBookCheckInterval = 2500;
            Scribe.saver.InitSaving(path, "settings");
            settings.ExposeData();
            Scribe.saver.FinalizeSaving();
            CustomQuestFramework_ModSetting restored = new();
            Scribe.loader.InitLoading(path);
            restored.ExposeData();
            Scribe.loader.FinalizeLoading();
            Check(restored.alwaysLoadedModules.SequenceEqual(settings.alwaysLoadedModules),
                "module preferences including a future module survive native settings save and reload");
            Check(restored.QuestBookCheckInterval == 2500, "custom quest book interval survives native settings save and reload");
            restored.questBookCheckInterval = 0;
            Check(restored.QuestBookCheckInterval == 1, "zero intervals are bounded to one tick");
            restored.questBookCheckInterval = -50;
            Check(restored.QuestBookCheckInterval == 1, "negative intervals are bounded to one tick");
        }
        finally
        {
            Scribe.ForceStop();
            CustomQuestFramework_ModSetting.setting = previous;
            File.Delete(path);
        }
    }

    private static void CheckSchedule()
    {
        Game previousGame = Current.Game;
        GameComponent_QuestBook previousComponent = GameComponent_QuestBook.Instance;
        CustomQuestFramework_ModSetting previousSettings = CustomQuestFramework_ModSetting.setting;
        try
        {
            Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            game.tickManager = new TickManager();
            game.components = new();
            Current.Game = game;
            CustomQuestFramework_ModSetting settings = new() { questBookCheckInterval = 100 };
            GameComponent_QuestBook component = new(game);
            QuestBookStep step = new() { id = "CQF_Check_IntervalStep", objectives = new() { new CheckObjective() } };
            QuestBookInstance instance = new()
            {
                state = QuestBookState.Active,
                bookDef = new QuestBookDef
                {
                    chapters = new() { new QuestBookChapter { id = "CQF_Check_IntervalChapter", steps = new() { step } } }
                },
                steps = new()
                {
                    new QuestBookStepState
                    {
                        stepId = step.id,
                        status = QuestBookStepStatus.Active,
                        objectives = new() { new QuestBookObjectiveProgress() }
                    }
                }
            };
            component.Instances.Add(instance);
            QuestBookObjectiveProgress progress = instance.GetObjectiveProgress(step.id, 0);
            game.tickManager.DebugSetTicksGame(1000);
            component.GameComponentTick();
            Check(progress.currentCount == 1, "the initial check runs with a custom interval");
            game.tickManager.DebugSetTicksGame(1099);
            component.GameComponentTick();
            Check(progress.currentCount == 1, "automatic objectives are not checked before the configured interval");
            game.tickManager.DebugSetTicksGame(1100);
            component.GameComponentTick();
            Check(progress.currentCount == 2, "automatic objectives are checked at the configured interval");
            settings.questBookCheckInterval = 10;
            game.tickManager.DebugSetTicksGame(1101);
            component.GameComponentTick();
            game.tickManager.DebugSetTicksGame(1110);
            component.GameComponentTick();
            Check(progress.currentCount == 2, "changing the interval starts a fresh countdown in the running game");
            game.tickManager.DebugSetTicksGame(1111);
            component.GameComponentTick();
            Check(progress.currentCount == 3, "the changed interval applies without restarting the game");
            Check(instance.CheckObjectives(step.id) && progress.currentCount == 4, "manual checks remain available between automatic checks");
            settings.questBookCheckInterval = int.MaxValue;
            game.tickManager.DebugSetTicksGame(1112);
            component.GameComponentTick();
            game.tickManager.DebugSetTicksGame(1113);
            component.GameComponentTick();
            Check(progress.currentCount == 4, "large intervals do not overflow and trigger checks every tick");
        }
        finally
        {
            Current.Game = previousGame;
            GameComponent_QuestBook.Instance = previousComponent;
            CustomQuestFramework_ModSetting.setting = previousSettings;
        }
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    private sealed class CheckObjective : QuestBookObjective
    {
        public override bool RequiresCheck => true;

        public override bool Check(QuestBookObjectiveProgress progress)
        {
            progress.currentCount++;
            return true;
        }

        public override bool Process(QuestBookObjectiveProgress progress, Signal signal) => false;
    }
}
