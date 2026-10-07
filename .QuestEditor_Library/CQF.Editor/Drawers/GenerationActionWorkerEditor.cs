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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, cqfReceiver.actions, inRect, "CQFActions".Translate(), a => a.GetType().Name.Translate());
        }

        public static void DrawTab_1(QuestEditor_Library.GenerationActionWorker cqfReceiver)
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect viewport = new Rect(8f, 36f, Mathf.Min(490f, CQFUIScope.ContentWidth - 16f), Mathf.Max(40f, CQFUIScope.ContentHeight - 44f));
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, cqfReceiver.height));
            Widgets.BeginScrollView(viewport, ref cqfReceiver.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            CQFUIStyle.DrawBox(new Rect(8f, 8f, content.width - 16f, Mathf.Max(40f, cqfReceiver.height - 8f)), 1, QuestEditor_Dialog.blueTex);
            float y = 20f;
            Rect rectCP = new Rect(content.width - 40f, y, 25f, 25f);
            if (CQFUIStyle.ButtonImage(rectCP, TexButton.Copy))
            {
                cqfReceiver.CopyData();
            }

            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            rectCP.x += 32f;
            if (CQFUIStyle.ButtonImage(rectCP, TexButton.Paste))
            {
                cqfReceiver.PasteData();
            }

            TooltipHandler.TipRegion(rectCP, "Paste".Translate());
            y += 36f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, 15f, cqfReceiver.actions, content, "CQFActions".Translate().Colorize(CQFUIStyle.Accent), a => a.GetType().Name.Translate());
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
