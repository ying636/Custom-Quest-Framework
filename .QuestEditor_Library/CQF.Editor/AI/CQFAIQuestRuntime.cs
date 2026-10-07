using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIQuestRuntime
    {
        public CQFAIQuestRuntime(CQFAIModel model, CQFAIRuntimeJournal journal) { this.model = model; this.journal = journal; }
        public XElement Help() => new XElement("quests",
            new XElement("queries", "quests (optional search), quest/quest_parts (quest_id), quest_part_fields (quest_id,part_index), quest_books (optional search), quest_book/quest_steps (instance_id), quest_objectives (instance_id,step_id). offset/limit supported. Parts show field counts and short previews; field pages read scalar/Def-reference fields without traversing runtime object graphs. Steps report objective counts; read objective pages separately."),
            new XElement("operation", new XAttribute("kind", "quest_accept"), "quest_id, optional pawn_id. Accepts an existing NotYetAccepted Quest; native notifications/actions may execute."),
            new XElement("operation", new XAttribute("kind", "quest_end"), "quest_id, outcome=Success|Fail|Unknown, send_letter=true. Ends an existing ongoing Quest through its native API, not state-field mutation."),
            new XElement("operation", new XAttribute("kind", "quest_book_start"), "definition=QuestBookDef, optional quest_id. Creates/starts a real QuestBook instance (auto instance without Quest). Runs onStart actions."),
            new XElement("operation", new XAttribute("kind", "quest_book_step"), "instance_id,step_id,outcome=complete|fail, optional targets. Runs actual step actions/rewards and subsequent activation; only active steps may be operated."),
            new XElement("operation", new XAttribute("kind", "quest_book_end"), "instance_id,outcome=complete|fail,end_quest=false. Runs actual book completion/failure actions."),
            new XElement("operation", new XAttribute("kind", "quest_objective"), "instance_id,step_id,index,count>=0,completed=false. Changes one objective's progress with undo. Does not run rewards or automatically complete the step; use quest_book_check explicitly."),
            new XElement("operation", new XAttribute("kind", "quest_book_check"), "instance_id, optional step_id. Re-evaluates objective progress and may complete steps/run actions. All operations except quest_objective are not fully undoable."));
        public XElement Read(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "quest_id", "part_index", "instance_id", "step_id", "search", "offset", "limit");
            string kind = CQFAIRuntimeRequest.Text(request, "kind"), search = CQFAIRuntimeRequest.Text(request, "search");
            if (search.Length > 100) throw new InvalidDataException("CQF_AI_InvalidTool: search");
            if (kind == "quests") return CQFAIRuntimeRequest.Page("quests", Find.QuestManager.QuestsListForReading.Where(quest => (quest.id + " " + quest.name).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).Select(Summary), request);
            if (kind == "quest") return Summary(CQFAIRuntimeRequest.Quest(request, true)!);
            if (kind == "quest_parts") return CQFAIRuntimeRequest.Page("parts", CQFAIRuntimeRequest.Quest(request, true)!.PartsListForReading.Select((part, index) =>
            {
                XElement[] fields = PartFields(part).ToArray();
                return new XElement("part", new XAttribute("index", index), new XAttribute("type", part.GetType().FullName!), new XAttribute("fieldCount", fields.Length), fields.Take(4).Select(field => new XElement("preview", new XAttribute("name", field.Attribute("name")!.Value), field.Value.Length > 200 ? field.Value.Substring(0, 200) : field.Value)));
            }), request);
            if (kind == "quest_part_fields")
            {
                Quest quest = CQFAIRuntimeRequest.Quest(request, true)!; int index = CQFAIRuntimeRequest.Int(request, "part_index");
                if (index < 0 || index >= quest.PartsListForReading.Count) throw new InvalidDataException("CQF_AI_MissingResource: Quest part index");
                return CQFAIRuntimeRequest.Page("fields", PartFields(quest.PartsListForReading[index]), request);
            }
            if (kind == "quest_books") return CQFAIRuntimeRequest.Page("questBooks", Instances().Where(book => (book.instanceId + " " + book.bookDef?.defName).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).Select(BookSummary), request);
            QuestBookInstance book = Book(request);
            if (kind == "quest_book") return BookSummary(book);
            if (kind == "quest_steps") return CQFAIRuntimeRequest.Page("steps", book.steps.Select(step => new XElement("step", new XAttribute("id", step.stepId), new XAttribute("chapter", step.chapterId), new XAttribute("status", step.status),
                new XAttribute("objectiveCount", step.objectives?.Count ?? 0))), request);
            if (kind == "quest_objectives")
            {
                QuestBookStepState step = book.GetStepState(CQFAIRuntimeRequest.Text(request, "step_id")) ?? throw new InvalidDataException("CQF_AI_MissingResource: QuestBook step");
                return CQFAIRuntimeRequest.Page("objectives", (step.objectives ?? new List<QuestBookObjectiveProgress>()).Select((objective, index) => new XElement("objective", new XAttribute("index", index), new XAttribute("count", objective.currentCount), new XAttribute("completed", objective.completed))), request);
            }
            throw new InvalidDataException("CQF_AI_InvalidTool: Quest query");
        }
        public XElement Operate(XElement request)
        {
            string kind = CQFAIRuntimeRequest.Text(request, "kind"), outcome = CQFAIRuntimeRequest.Text(request, "outcome");
            CQFAIRuntimeRequest.Fields(request, kind switch
            {
                "quest_accept" => new[] { "kind", "quest_id", "pawn_id" },
                "quest_end" => new[] { "kind", "quest_id", "outcome", "send_letter" },
                "quest_book_start" => new[] { "kind", "definition", "quest_id" },
                "quest_book_step" => new[] { "kind", "instance_id", "step_id", "outcome", "targets" },
                "quest_book_end" => new[] { "kind", "instance_id", "outcome", "end_quest" },
                "quest_objective" => new[] { "kind", "instance_id", "step_id", "index", "count", "completed" },
                "quest_book_check" => new[] { "kind", "instance_id", "step_id" },
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: Quest operation")
            });
            if (kind is "quest_accept" or "quest_end")
            {
                Quest quest = CQFAIRuntimeRequest.Quest(request, true)!;
                if (kind == "quest_accept")
                {
                    if (quest.State != QuestState.NotYetAccepted) throw new InvalidDataException("CQF_AI_InvalidValue: Quest already accepted/ended");
                    Pawn? pawn = request.Element("pawn_id") == null ? null : CQFAIRuntimeRequest.Pawn(request);
                    journal.MarkIrreversible(); quest.Accept(pawn);
                    if (!quest.EverAccepted) throw new InvalidDataException("CQF_AI_ApplyMismatch: Quest acceptance");
                }
                else
                {
                    if (quest.State != QuestState.Ongoing) throw new InvalidDataException("CQF_AI_InvalidValue: Quest must be ongoing");
                    if (outcome is not ("Success" or "Fail" or "Unknown") || !Enum.TryParse(outcome, out QuestEndOutcome result)) throw new InvalidDataException("CQF_AI_InvalidValue: Quest outcome");
                    bool letter = CQFAIRuntimeRequest.Bool(request, "send_letter", true);
                    journal.MarkIrreversible(); quest.End(result, letter);
                }
                return new XElement("questOperated", new XAttribute("undoSupported", false), Summary(quest));
            }
            if (kind == "quest_book_start")
            {
                QuestBookDef def = CQFAIRuntimeRequest.Def<QuestBookDef>(CQFAIRuntimeRequest.Text(request, "definition"));
                CQFAIChanges.Validate(def);
                string[] errors = def.ConfigErrors().Take(16).ToArray();
                if (errors.Length > 0) throw new InvalidDataException("CQF_AI_InvalidValue: " + string.Join("; ", errors));
                Quest? quest = CQFAIRuntimeRequest.Quest(request);
                GameComponent_QuestBook component = Current.Game?.components?.OfType<GameComponent_QuestBook>().FirstOrDefault() ?? throw new InvalidDataException("CQF_AI_MissingResource: QuestBook component");
                journal.MarkIrreversible(); QuestBookInstance started = quest == null ? component.CreateAutoInstance(def) : component.CreateInstance(def, quest);
                if (started == null) throw new InvalidDataException("CQF_AI_ApplyMismatch: QuestBook start");
                return new XElement("questBookStarted", new XAttribute("undoSupported", false), BookSummary(started));
            }
            QuestBookInstance book = Book(request);
            string stepId = CQFAIRuntimeRequest.Text(request, "step_id");
            QuestBookStepState? step = stepId.Length == 0 ? null : book.GetStepState(stepId);
            if (stepId.Length > 0 && (step == null || book.GetStepDef(stepId) == null)) throw new InvalidDataException("CQF_AI_MissingResource: QuestBook step");
            if (kind == "quest_objective")
            {
                int index = CQFAIRuntimeRequest.Int(request, "index"), count = CQFAIRuntimeRequest.Int(request, "count");
                if (step == null || index < 0 || index >= step.objectives.Count || count < 0 || count > 1000000) throw new InvalidDataException("CQF_AI_InvalidValue: objective index/count");
                QuestBookObjectiveProgress objective = step.objectives[index]; int oldCount = objective.currentCount; bool oldCompleted = objective.completed, completed = CQFAIRuntimeRequest.Bool(request, "completed");
                return journal.Edit("questBook:" + book.instanceId + ":" + stepId + ":" + index, () => { objective.currentCount = count; objective.completed = completed; },
                    () => { objective.currentCount = oldCount; objective.completed = oldCompleted; }, () => new XElement("objective", new XAttribute("count", objective.currentCount), new XAttribute("completed", objective.completed)));
            }
            if (kind == "quest_book_step")
            {
                if (book.state != QuestBookState.Active || step?.status != QuestBookStepStatus.Active || outcome is not ("complete" or "fail")) throw new InvalidDataException("CQF_AI_InvalidValue: active book/step and valid outcome required");
                Dictionary<string, TargetInfo> targets = CQFAIRuntimeRequest.Targets(request);
                journal.MarkIrreversible(); if (outcome == "complete") book.CompleteStepById(stepId, targets); else book.FailStepById(stepId, book.boundQuest);
                if (step.status != (outcome == "complete" ? QuestBookStepStatus.Completed : QuestBookStepStatus.Failed)) throw new InvalidDataException("CQF_AI_ApplyMismatch: QuestBook step");
            }
            else if (kind == "quest_book_end")
            {
                if (book.state != QuestBookState.Active || outcome is not ("complete" or "fail")) throw new InvalidDataException("CQF_AI_InvalidValue: active book and valid outcome required");
                bool end = CQFAIRuntimeRequest.Bool(request, "end_quest");
                journal.MarkIrreversible(); if (outcome == "complete") book.Complete(book.boundQuest, end); else book.Fail(book.boundQuest);
            }
            else if (kind == "quest_book_check")
            {
                if (book.state != QuestBookState.Active) throw new InvalidDataException("CQF_AI_InvalidValue: active book required");
                journal.MarkIrreversible(); if (step == null) book.CheckObjectives(); else book.CheckObjectives(stepId);
            }
            else throw new InvalidDataException("CQF_AI_InvalidTool: Quest operation");
            return new XElement("questBookOperated", new XAttribute("undoSupported", false), BookSummary(book), step == null ? null : new XElement("step", new XAttribute("id", step.stepId), new XAttribute("status", step.status)));
        }
        public static IEnumerable<QuestBookInstance> Instances() => Current.Game?.components?.OfType<GameComponent_QuestBook>().FirstOrDefault() is GameComponent_QuestBook component
            ? (IEnumerable<QuestBookInstance>?)typeof(GameComponent_QuestBook).GetField("instances", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(component) ?? Array.Empty<QuestBookInstance>() : Array.Empty<QuestBookInstance>();
        private static QuestBookInstance Book(XElement request) => Instances().FirstOrDefault(book => book.instanceId == CQFAIRuntimeRequest.Text(request, "instance_id")) ?? throw new InvalidDataException("CQF_AI_MissingResource: QuestBook instance");
        private static XElement Summary(Quest quest) => new XElement("quest", new XAttribute("id", quest.id), new XAttribute("state", quest.State), new XAttribute("accepted", quest.EverAccepted), new XAttribute("partCount", quest.PartsListForReading.Count), new XElement("name", Bounded(quest.name ?? "")));
        private static XElement BookSummary(QuestBookInstance book) => new XElement("questBook", new XAttribute("id", book.instanceId ?? ""), new XAttribute("definition", book.bookDef?.defName ?? ""), new XAttribute("questId", book.boundQuest?.id ?? -1),
            new XAttribute("state", book.state), new XAttribute("startedTick", book.startedTick), new XAttribute("completedTick", book.completedTick), new XAttribute("stepCount", book.steps?.Count ?? 0));
        private static string Bounded(string text) => text.Length <= 2000 ? text : text.Substring(0, 2000);
        private static IEnumerable<XElement> PartFields(QuestPart part) => part.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string) || typeof(Def).IsAssignableFrom(field.FieldType)).OrderBy(field => field.Name)
            .Select(field => new XElement("field", new XAttribute("name", field.Name), new XAttribute("type", field.FieldType.FullName!), field.GetValue(part) is Def def ? def.defName : Bounded(field.GetValue(part)?.ToString() ?? "")));
        private readonly CQFAIModel model;
        private readonly CQFAIRuntimeJournal journal;
    }
}
