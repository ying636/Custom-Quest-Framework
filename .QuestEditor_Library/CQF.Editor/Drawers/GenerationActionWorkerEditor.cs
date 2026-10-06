using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class GenerationActionWorkerEditor
    {
        public static void Draw_0(QuestEditor_Library.GenerationActionWorker cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, cqfReceiver.actions, inRect, "CQFActions".Translate(), a => a.GetType().Name.Translate());
        }

        public static void DrawTab_1(QuestEditor_Library.GenerationActionWorker cqfReceiver)
        {
            Widgets.BeginScrollView(new Rect(7f, 25f, 475f, 590f), ref cqfReceiver.scrollPos, new Rect(7f, 10f, 475f, cqfReceiver.height));
            Widgets.DrawBox(new Rect(8f, 10f, 470f, cqfReceiver.height), 1, QuestEditor_Dialog.blueTex);
            float y = 20f;
            Rect rectCP = new Rect(380f, y, 25f, 25f);
            if (Widgets.ButtonImage(rectCP, TexButton.Copy))
            {
                cqfReceiver.CopyData();
            }

            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            rectCP.x += 30f;
            if (Widgets.ButtonImage(rectCP, TexButton.Paste))
            {
                cqfReceiver.PasteData();
            }

            TooltipHandler.TipRegion(rectCP, "Paste".Translate());
            CQFEditorTools.DrawActionList_UseWindow(ref y, 15f, cqfReceiver.actions, new Rect(0f, 0f, 475f, cqfReceiver.height), "CQFActions".Translate().Colorize(ColorLibrary.SkyBlue), a => a.GetType().Name.Translate());
            cqfReceiver.height = y + 5f;
            Widgets.EndScrollView();
        }

        public static void PasteData_2(QuestEditor_Library.GenerationActionWorker cqfReceiver)
        {
            cqfReceiver.actions.Clear();
            CQFEditorTools.actions.ForEach(a => cqfReceiver.actions.Add(a.Copy()));
        }

        public static void CopyData_3(QuestEditor_Library.GenerationActionWorker cqfReceiver)
        {
            CQFEditorTools.actions.Clear();
            cqfReceiver.actions.ForEach(a => CQFEditorTools.actions.Add(a.Copy()));
        }
    }
}
