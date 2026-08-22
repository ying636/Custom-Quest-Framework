using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class GameComponent_QuestBook : GameComponent
    {
        public GameComponent_QuestBook(Game game)
        {
            Instance = this;
        }

        public static GameComponent_QuestBook Instance;
        public List<QuestBookInstance> Instances => instances ??= new List<QuestBookInstance>();

        public override void StartedNewGame()
        {
            EnsureAutoStartedBooks();
        }

        public override void LoadedGame()
        {
            EnsureAutoStartedBooks();
        }

        public QuestBookInstance CreateInstance(QuestBookDef bookDef, Quest quest)
        {
            if (bookDef == null || quest == null)
            {
                Log.Error("CQF task book cannot be created without a definition and quest.");
                return null;
            }

            QuestBookInstance existing = Instances.FirstOrDefault(x => x.boundQuest == quest);
            if (existing != null)
            {
                return existing;
            }

            QuestBookInstance instance = new QuestBookInstance
            {
                instanceId = "QuestBook_" + quest.id,
                bookDef = bookDef,
                boundQuest = quest,
                state = QuestBookState.Active
            };
            instance.Initialize();
            Instances.Add(instance);
            ApplyQuestVisibility(instance);
            return instance;
        }

        public QuestBookInstance CreateAutoInstance(QuestBookDef bookDef)
        {
            if (bookDef == null || bookDef.defName.NullOrEmpty())
            {
                Log.Error("CQF task book auto activation requires a valid QuestBookDef.");
                return null;
            }
            QuestBookInstance existing = Instances.FirstOrDefault(x => x?.bookDef?.defName == bookDef.defName);
            if (existing != null)
            {
                return existing;
            }
            QuestBookInstance instance = new QuestBookInstance
            {
                instanceId = "QuestBook_Auto_" + bookDef.defName,
                bookDef = bookDef,
                state = QuestBookState.Active
            };
            instance.Initialize();
            Instances.Add(instance);
            return instance;
        }

        public QuestBookInstance FindById(string instanceId)
        {
            if (instanceId.NullOrEmpty())
            {
                return null;
            }
            return Instances.FirstOrDefault(x => x.instanceId == instanceId);
        }

        public QuestBookInstance FindByQuest(Quest quest)
        {
            return quest == null ? null : Instances.FirstOrDefault(x => x.boundQuest == quest);
        }

        public void RefreshDefinition(QuestBookDef bookDef)
        {
            if (bookDef == null || bookDef.defName.NullOrEmpty())
            {
                Log.Error("CQF task book hot reload received an invalid definition.");
                return;
            }
            foreach (QuestBookInstance instance in Instances.Where(x => x?.bookDef?.defName == bookDef.defName).ToList())
            {
                instance.RefreshDefinition(bookDef);
                ApplyQuestVisibility(instance);
            }
        }

        public override void GameComponentTick()
        {
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick < nextObjectiveCheckTick)
            {
                return;
            }
            nextObjectiveCheckTick = currentTick + GenDate.TicksPerDay;
            foreach (QuestBookInstance instance in Instances.Where(x => x?.state == QuestBookState.Active).ToList())
            {
                instance.CheckObjectives();
            }
        }

        public void OpenBook(QuestBookInstance instance)
        {
            if (instance == null)
            {
                Log.Error("Tried to open a null CQF task book instance.");
                return;
            }
            Find.WindowStack.Add(new MainTabWindow_QuestBook(instance));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref instances, "CQF_QuestBook_instances", LookMode.Deep);
            Scribe_Values.Look(ref nextObjectiveCheckTick, "CQF_QuestBook_nextObjectiveCheckTick");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                instances ??= new List<QuestBookInstance>();
                foreach (QuestBookInstance instance in instances)
                {
                    instance?.PostLoadInit();
                    if (instance?.boundQuest != null)
                    {
                        ApplyQuestVisibility(instance);
                    }
                }
            }
        }

        internal void ApplyQuestVisibility(QuestBookInstance instance)
        {
            if (instance?.boundQuest == null || instance.bookDef == null)
            {
                return;
            }
            if (instance.bookDef.questVisibility == QuestBookQuestVisibility.BookOnly || instance.bookDef.questVisibility == QuestBookQuestVisibility.Internal)
            {
                instance.boundQuest.hidden = true;
                instance.boundQuest.hiddenInUI = true;
            }
        }

        internal void CompleteFromQuest(QuestBookInstance instance, bool success)
        {
            if (instance == null || instance.state == QuestBookState.Completed || instance.state == QuestBookState.Failed)
            {
                return;
            }
            if (success)
            {
                instance.Complete(instance.boundQuest, true);
            }
            else
            {
                instance.Fail(instance.boundQuest);
            }
        }

        private void EnsureAutoStartedBooks()
        {
            foreach (QuestBookDef bookDef in DefDatabase<QuestBookDef>.AllDefsListForReading.Where(def => def.autoStart && !def.defName.NullOrEmpty()))
            {
                CreateAutoInstance(bookDef);
            }
        }

        private List<QuestBookInstance> instances = new List<QuestBookInstance>();
        private int nextObjectiveCheckTick;
    }

    public class QuestBookInstance : IExposable
    {
        public string instanceId;
        public QuestBookDef bookDef;
        public Quest boundQuest;
        public QuestBookState state;
        public List<QuestBookChapterState> chapters = new List<QuestBookChapterState>();
        public List<QuestBookStepState> steps = new List<QuestBookStepState>();
        public int startedTick;
        public int completedTick;

        public void Initialize()
        {
            startedTick = Find.TickManager.TicksGame;
            chapters.Clear();
            steps.Clear();
            foreach (QuestBookChapter chapter in bookDef?.chapters ?? Enumerable.Empty<QuestBookChapter>())
            {
                QuestBookChapterState chapterState = new QuestBookChapterState { chapterId = chapter.id, status = QuestBookStepStatus.Locked };
                chapters.Add(chapterState);
                foreach (QuestBookStep step in chapter.steps)
                {
                    QuestBookStepState stepState = new QuestBookStepState { chapterId = chapter.id, stepId = step.id, status = QuestBookStepStatus.Locked };
                    stepState.objectives = step.objectives.Select(objective => new QuestBookObjectiveProgress()).ToList();
                    steps.Add(stepState);
                }
            }
            ActivateFirstStep();
        }

        public void PostLoadInit()
        {
            chapters ??= new List<QuestBookChapterState>();
            steps ??= new List<QuestBookStepState>();
            foreach (QuestBookStepState step in steps)
            {
                step.objectives ??= new List<QuestBookObjectiveProgress>();
            }
        }

        public QuestBookStepState GetStepState(string stepId)
        {
            return steps.FirstOrDefault(x => x.stepId == stepId);
        }

        public QuestBookObjectiveProgress GetObjectiveProgress(string stepId, int objectiveIndex)
        {
            return GetStepState(stepId)?.objectives?.ElementAtOrDefault(objectiveIndex);
        }

        public QuestBookStep GetStepDef(string stepId)
        {
            return bookDef?.chapters.SelectMany(x => x.steps).FirstOrDefault(x => x.id == stepId);
        }

        public void RefreshDefinition(QuestBookDef newDefinition)
        {
            if (newDefinition == null || newDefinition.defName.NullOrEmpty())
            {
                Log.Error("CQF task book instance hot reload received an invalid definition.");
                return;
            }
            bookDef = newDefinition;
            HashSet<string> chapterIds = new HashSet<string>();
            HashSet<string> stepIds = new HashSet<string>();
            foreach (QuestBookChapter chapter in newDefinition.chapters)
            {
                chapterIds.Add(chapter.id);
                QuestBookChapterState chapterState = chapters.FirstOrDefault(state => state.chapterId == chapter.id);
                if (chapterState == null)
                {
                    chapters.Add(new QuestBookChapterState { chapterId = chapter.id, status = QuestBookStepStatus.Locked });
                }
                foreach (QuestBookStep step in chapter.steps)
                {
                    stepIds.Add(step.id);
                    QuestBookStepState stepState = GetStepState(step.id);
                    if (stepState == null)
                    {
                        stepState = new QuestBookStepState
                        {
                            chapterId = chapter.id,
                            stepId = step.id,
                            status = QuestBookStepStatus.Locked,
                            objectives = step.objectives.Select(objective => new QuestBookObjectiveProgress()).ToList()
                        };
                        steps.Add(stepState);
                        continue;
                    }
                    stepState.chapterId = chapter.id;
                    stepState.objectives ??= new List<QuestBookObjectiveProgress>();
                    while (stepState.objectives.Count < step.objectives.Count)
                    {
                        stepState.objectives.Add(new QuestBookObjectiveProgress());
                    }
                    if (stepState.objectives.Count > step.objectives.Count)
                    {
                        stepState.objectives.RemoveRange(step.objectives.Count, stepState.objectives.Count - step.objectives.Count);
                    }
                }
            }
            chapters.RemoveAll(chapter => !chapterIds.Contains(chapter.chapterId));
            steps.RemoveAll(step => !stepIds.Contains(step.stepId));
            if (state == QuestBookState.Active && !steps.Any(step => step.status == QuestBookStepStatus.Active))
            {
                ActivateFirstStep();
            }
        }

        public void ReceiveSignal(Signal signal, Dictionary<string, TargetInfo> targets)
        {
            if (state != QuestBookState.Active || signal.tag.NullOrEmpty())
            {
                return;
            }
            foreach (QuestBookStepState stepState in steps.Where(x => x.status == QuestBookStepStatus.Active).ToList())
            {
                QuestBookStep stepDef = GetStepDef(stepState.stepId);
                if (stepDef == null)
                {
                    Log.Error("CQF task book step is missing: " + stepState.stepId);
                    continue;
                }
                for (int objectiveIndex = 0; objectiveIndex < stepDef.objectives.Count; objectiveIndex++)
                {
                    QuestBookObjective objectiveDef = stepDef.objectives[objectiveIndex];
                    QuestBookObjectiveProgress objectiveState = GetObjectiveProgress(stepState.stepId, objectiveIndex);
                    if (objectiveState == null || objectiveState.completed)
                    {
                        continue;
                    }
                    objectiveDef.Worker?.Process(objectiveDef, objectiveState, signal);
                }
                TryCompleteStep(stepState, stepDef, targets);
            }
        }

        public void CompleteStepById(string stepId, Dictionary<string, TargetInfo> targets = null)
        {
            QuestBookStepState stepState = GetStepState(stepId);
            QuestBookStep stepDef = GetStepDef(stepId);
            if (stepState == null || stepDef == null)
            {
                Log.Error("CQF task book step could not be completed: " + stepId);
                return;
            }
            TryCompleteStep(stepState, stepDef, targets ?? new Dictionary<string, TargetInfo>(), true);
        }

        public void FailStepById(string stepId, Quest quest)
        {
            QuestBookStepState stepState = GetStepState(stepId);
            QuestBookStep stepDef = GetStepDef(stepId);
            if (stepState == null || stepDef == null)
            {
                Log.Error("CQF task book step could not be failed: " + stepId);
                return;
            }
            stepState.status = QuestBookStepStatus.Failed;
            RunActions(stepDef.onFailActions, quest);
        }

        public void Complete(Quest quest, bool endQuest)
        {
            if (state == QuestBookState.Completed)
            {
                return;
            }
            state = QuestBookState.Completed;
            completedTick = Find.TickManager.TicksGame;
            RunActions(bookDef?.onCompleteActions, quest);
            if (endQuest && quest != null && !quest.Historical && (bookDef.completionAuthority == QuestBookCompletionAuthority.QuestBook || bookDef.completionAuthority == QuestBookCompletionAuthority.Either))
            {
                quest.End(QuestEndOutcome.Success, sendLetter: false, playSound: false);
            }
        }

        public void Fail(Quest quest)
        {
            if (state == QuestBookState.Failed)
            {
                return;
            }
            state = QuestBookState.Failed;
            RunActions(bookDef?.onFailActions, quest);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref instanceId, "instanceId");
            Scribe_Defs.Look(ref bookDef, "bookDef");
            Scribe_References.Look(ref boundQuest, "boundQuest");
            Scribe_Values.Look(ref state, "state", QuestBookState.Locked);
            Scribe_Collections.Look(ref chapters, "chapters", LookMode.Deep);
            Scribe_Collections.Look(ref steps, "steps", LookMode.Deep);
            Scribe_Values.Look(ref startedTick, "startedTick");
            Scribe_Values.Look(ref completedTick, "completedTick");
        }

        private void ActivateFirstStep()
        {
            QuestBookStepState firstStep = steps.FirstOrDefault();
            if (firstStep != null)
            {
                ActivateStep(firstStep, boundQuest);
            }
        }

        private void ActivateStep(QuestBookStepState stepState, Quest quest)
        {
            if (stepState == null || stepState.status == QuestBookStepStatus.Active || stepState.status == QuestBookStepStatus.Completed)
            {
                return;
            }
            stepState.status = QuestBookStepStatus.Active;
            QuestBookStep stepDef = GetStepDef(stepState.stepId);
            RunActions(stepDef?.onActivateActions, quest);
        }

        private void TryCompleteStep(QuestBookStepState stepState, QuestBookStep stepDef, Dictionary<string, TargetInfo> targets, bool force = false)
        {
            if (stepState.status == QuestBookStepStatus.Completed)
            {
                return;
            }
            IEnumerable<int> required = Enumerable.Range(0, stepDef.objectives.Count).Where(index => !stepDef.objectives[index].optional);
            bool completed = stepDef.completionMode == QuestBookCompletionMode.Any
                ? required.Any(index => GetObjectiveProgress(stepState.stepId, index)?.completed == true)
                : required.All(index => GetObjectiveProgress(stepState.stepId, index)?.completed == true);
            if (!force && (stepDef.completionMode == QuestBookCompletionMode.Manual || !completed))
            {
                return;
            }
            stepState.status = QuestBookStepStatus.Completed;
            if (!stepDef.rewards.NullOrEmpty())
            {
                CQFRewardDelivery.TryDrop(stepDef.rewards, boundQuest);
            }
            RunActions(stepDef.onCompleteActions, boundQuest, targets);
            foreach (string nextStepId in stepDef.nextStepIds)
            {
                QuestBookStepState nextStep = GetStepState(nextStepId);
                if (nextStep != null)
                {
                    ActivateStep(nextStep, boundQuest);
                }
            }
            if (!stepDef.nextStepIds.Any() && steps.All(x => x.status == QuestBookStepStatus.Completed))
            {
                Complete(boundQuest, true);
            }
        }

        public void CheckObjectives()
        {
            if (state != QuestBookState.Active)
            {
                return;
            }
            foreach (QuestBookStepState stepState in steps.Where(x => x.status == QuestBookStepStatus.Active).ToList())
            {
                CheckObjectives(stepState.stepId);
            }
        }

        public bool CheckObjectives(string stepId)
        {
            if (state != QuestBookState.Active)
            {
                return false;
            }
            QuestBookStepState stepState = GetStepState(stepId);
            if (stepState == null)
            {
                Log.Error("CQF task book step state is missing: " + stepId);
                return false;
            }
            if (stepState.status != QuestBookStepStatus.Active)
            {
                return false;
            }
            QuestBookStep stepDef = GetStepDef(stepState.stepId);
            if (stepDef == null)
            {
                Log.Error("CQF task book step is missing: " + stepState.stepId);
                return false;
            }
            for (int objectiveIndex = 0; objectiveIndex < stepDef.objectives.Count; objectiveIndex++)
            {
                QuestBookObjective objective = stepDef.objectives[objectiveIndex];
                QuestBookObjectiveProgress progress = GetObjectiveProgress(stepState.stepId, objectiveIndex);
                if (progress == null || progress.completed || objective.Worker == null)
                {
                    continue;
                }
                objective.Worker.Check(objective, progress);
            }
            TryCompleteStep(stepState, stepDef, new Dictionary<string, TargetInfo>());
            return true;
        }

        private void RunActions(List<CQFAction> actions, Quest quest, Dictionary<string, TargetInfo> targets = null)
        {
            if (actions.NullOrEmpty())
            {
                return;
            }
            Dictionary<string, TargetInfo> safeTargets = targets ?? new Dictionary<string, TargetInfo>();
            foreach (CQFAction action in actions)
            {
                try
                {
                    action?.Work(safeTargets, quest);
                }
                catch (Exception ex)
                {
                    Log.Error("CQF task book action failed: " + ex);
                }
            }
        }

    }

}
