using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class ITab_CustomTextEditor
    {
        public static void DrawTextSection_0(QuestEditor_Library.ITab_CustomText cqfReceiver, ref float y, float width, string label, ref bool enabled, ref string text, bool multiline)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(width);
            float editorHeight = multiline ? 58f : 30f;
            float sectionHeight = 38f + (enabled ? editorHeight + 10f : 0f);
            Rect sectionRect = new Rect(8f, y, width, sectionHeight);
            Rect headerRect = new Rect(sectionRect.x, sectionRect.y, sectionRect.width, 32f);
            Widgets.DrawBoxSolid(sectionRect, CQFUIStyle.Card);
            Widgets.DrawBoxSolid(headerRect, CQFUIStyle.Header);
            Widgets.Label(new Rect(headerRect.x + 10f, headerRect.y + 4f, headerRect.width - 50f, 25f), label);
            Widgets.Checkbox(new Vector2(headerRect.xMax - 30f, headerRect.y + 4f), ref enabled, 24f);
            if (enabled)
            {
                Rect editorRect = new Rect(sectionRect.x + 8f, headerRect.yMax + 6f, sectionRect.width - 16f, editorHeight);
                text = multiline ? Widgets.TextArea(editorRect, text ?? string.Empty) : Widgets.TextField(editorRect, text ?? string.Empty);
            }

            CQFUIStyle.DrawBox(sectionRect, 1);
            y += sectionHeight + 8f;
        }

        public static void FillTab_1(QuestEditor_Library.ITab_CustomText cqfReceiver)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(cqfReceiver.CQFSize.x, cqfReceiver.CQFSize.y);
            if (cqfReceiver.Comp == null)
            {
                return;
            }

            float y = 38f;
            float width = cqfReceiver.CQFSize.x - 16f;
            cqfReceiver.DrawTextSection(ref y, width, "CQF_CustomName".Translate(), ref cqfReceiver.Comp.useCustomName, ref cqfReceiver.Comp.customName, false);
            cqfReceiver.DrawTextSection(ref y, width, "CQF_CustomDescription".Translate(), ref cqfReceiver.Comp.useCustomDescription, ref cqfReceiver.Comp.customDescription, true);
            cqfReceiver.DrawTextSection(ref y, width, "CQF_CustomInspectText".Translate(), ref cqfReceiver.Comp.useCustomInspectText, ref cqfReceiver.Comp.customInspectText, true);
        }
    }
}
