using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class QuestNode_DoCQFActionsEditor
    {
        public static void Draw_0(QuestEditor_Library.QuestNode_DoCQFActions cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFSignalEditor.DrawBookField(y, "inSignal".Translate(), cqfReceiver.inSignal.ToString(), value => cqfReceiver.inSignal = new SlateRef<string>(value), x, 100f, inRect.width - x - 12f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, cqfReceiver.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
        }
    }
}
