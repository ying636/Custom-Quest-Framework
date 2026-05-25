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
        CQFEditorTools.DrawLabelAndText_SlateRef_Line(y,"inSignal".Translate(),ref inSignal,x,100f);
        y += 30f;
        CQFEditorTools.DrawActionList_UseWindow(ref y,x,this.actions,inRect,"TriggerActions".Translate(),
            a => a.GetType().Name.Translate());
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
            foreach (var cqfAction in this.actions)
            {
                cqfAction.Work([],this.quest);
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