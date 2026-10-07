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
            Rect viewport = new Rect(8f, 8f, cqfReceiver.CQFSize.x - 16f, cqfReceiver.CQFSize.y - 16f);
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, cqfReceiver.height));
            using CQFUIScope scope = new CQFUIScope(content.width, viewport.height);
            Widgets.BeginScrollView(viewport, ref cqfReceiver.scrollPos, content);
            float y = 10f;
            if (cqfReceiver.CQFSelectedObject is Thing thing && thing.TryGetComp<CompActionWorker>()is CompActionWorker comp)
            {
                for (int i = 0; i < comp.comps.Count; i++)
                {
                    ActionComp c = comp.comps[i];
                    if (CQFUIStyle.ButtonText(new Rect(10f, y, content.width - 20f, 25f), c.compName, false))
                    {
                        Find.WindowStack.Add(new QuestEditor_EditActionComp(c, thing));
                    }

                    y += 30f;
                };
                Rect rect = new Rect(content.width - 40f, y, 28f, 28f);
                if (CQFUIStyle.ButtonImage(rect, TexButton.Paste))
                {
                    comp.PasteSingleComp();
                }

                TooltipHandler.TipRegion(rect, "Paste".Translate());
                y += 36f;
                CQFEditorTools.DrawButtonForList(ref y, comp.comps, c => c.compName);
            }

            Widgets.EndScrollView();
            cqfReceiver.height = y;
        }
    }
}
