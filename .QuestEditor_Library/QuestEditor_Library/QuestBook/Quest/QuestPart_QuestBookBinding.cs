using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class QuestPart_QuestBookBinding : QuestPart
    {
        public string instanceId;
        public QuestBookDef bookDef;

        public override void PostQuestAdded()
        {
            base.PostQuestAdded();
            if (GameComponent_QuestBook.Instance == null)
            {
                Log.Error("CQF task book binding could not find GameComponent_QuestBook.");
                return;
            }
            QuestBookInstance instance = GameComponent_QuestBook.Instance.CreateInstance(bookDef, quest);
            if (instance != null)
            {
                instanceId = instance.instanceId;
            }
        }

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            GameComponent_QuestBook.Instance?.FindById(instanceId)?.ReceiveSignal(signal, GetTargets(signal));
        }

        public override void Notify_PreCleanup()
        {
            base.Notify_PreCleanup();
            QuestBookInstance instance = GameComponent_QuestBook.Instance?.FindById(instanceId);
            if (instance == null || instance.state != QuestBookState.Active)
            {
                return;
            }
            if (quest.State == QuestState.EndedSuccess)
            {
                instance.Complete(quest, false);
            }
            else if (quest.State == QuestState.EndedFailed || quest.State == QuestState.EndedOfferExpired)
            {
                instance.Fail(quest);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref instanceId, "instanceId");
            Scribe_Defs.Look(ref bookDef, "bookDef");
        }

        private static Dictionary<string, TargetInfo> GetTargets(Signal signal)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            foreach (NamedArgument argument in signal.args.Args)
            {
                if (argument.arg is TargetInfo target)
                {
                    targets[argument.label] = target;
                }
                else if (argument.arg is Thing thing)
                {
                    targets[argument.label] = thing;
                }
            }
            return targets;
        }
    }
}
