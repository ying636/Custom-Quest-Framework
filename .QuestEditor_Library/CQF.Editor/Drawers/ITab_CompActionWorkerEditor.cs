using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class ITab_CompActionWorkerEditor
    {
        public static void FillTab_0(QuestEditor_Library.ITab_CompActionWorker cqfReceiver)
        {
            Widgets.BeginScrollView(new Rect(5f, 5f, 490, 590f), ref cqfReceiver.scrollPos, new Rect(0f, 0f, 490f, cqfReceiver.height));
            float y = 10f;
            if (cqfReceiver.CQFSelectedObject is Thing thing && thing.TryGetComp<CompActionWorker>()is CompActionWorker comp)
            {
                for (int i = 0; i < comp.comps.Count; i++)
                {
                    ActionComp c = comp.comps[i];
                    if (Widgets.ButtonText(new Rect(10f, y, 150f, 25f), c.compName, false))
                    {
                        Find.WindowStack.Add(new QuestEditor_EditActionComp(c, thing));
                    }

                    y += 30f;
                };
                Rect rect = new Rect(332.5f, y, 25f, 25f);
                if (Widgets.ButtonImage(rect, TexButton.Paste))
                {
                    comp.PasteSingleComp();
                }

                TooltipHandler.TipRegion(rect, "Paste".Translate());
                CQFEditorTools.DrawButtonForList(ref y, comp.comps, c => c.compName);
            }

            Widgets.EndScrollView();
            cqfReceiver.height = y;
        }
    }
}
