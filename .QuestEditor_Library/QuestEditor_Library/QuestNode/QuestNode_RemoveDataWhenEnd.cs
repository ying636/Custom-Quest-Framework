using RimWorld;
using RimWorld.QuestGen;

namespace QuestEditor_Library;

public class QuestNode_RemoveDataWhenEnd : QuestNode
{
    protected override void RunInt()
    {
        QuestGen.quest.AddPart<QuestPart_RemoveDataWhenEnd>();
    }

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }
}

public class QuestPart_RemoveDataWhenEnd : QuestPart
{
    public override void Notify_PreCleanup()
    {
        GameComponent_Editor.Instance.RemoveQuestData(this.quest);
    }
    
}
