using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

    public class QuestNode_DoCQFActions : QuestNode,IDrawable
{
    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var part = QuestGen.quest.AddPart<QuestPart_DoCQFActions>();
        part.inSignal = (QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ??
                         QuestGen.slate.Get<string>("inSignal", null, false));
        part.actions = this.actions;
    }
    public void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestNode_DoCQFActions.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        protected override bool TestRunInt(Slate slate)
    {
        return true;
    }
    
    [NoTranslate]
    public SlateRef<string> inSignal;
    public List<CQFAction> actions = new List<CQFAction>();
}

    public class QuestPart_DoCQFActions : QuestPart
{
    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == this.inSignal)
        {
            Dictionary<string, TargetInfo> receivedTargets = new Dictionary<string, TargetInfo>();
            foreach (NamedArgument receivedArg in signal.args.Args)
            {
                if (receivedArg.label.NullOrEmpty())
                {
                    continue;
                }
                if (receivedArg.arg is TargetInfo target)
                {
                    receivedTargets[receivedArg.label] = target;
                }
                else if (receivedArg.arg is Thing thing)
                {
                    receivedTargets[receivedArg.label] = thing;
                }
            }

            foreach (CQFAction cqfAction in this.actions)
            {
                cqfAction.Work(receivedTargets, this.quest);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
        Scribe_Collections.Look(ref actions,"actions",LookMode.Deep);
    }

    public string inSignal;
    public List<CQFAction> actions = new List<CQFAction>();
}
