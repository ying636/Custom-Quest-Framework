using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomHediffEditor
    {
        public static void PasteSingleComp_0(QuestEditor_Library.CustomHediff cqfReceiver)
        {
            if (CQFEditorTools.actionComp != null)
            {
                cqfReceiver.comps.Add(CQFEditorTools.actionComp.Copy());
            }
        }
    }
}
