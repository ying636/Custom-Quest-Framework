using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditIActionAndText : Window
    {
        public Dialog_EditIActionAndText(Thing t)
        {
            this.t = t;
            this.forcePause = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect outRect = new Rect(5f, 5f, inRect.width - 10f, inRect.height - 10f);
            float viewWidth = outRect.width - 18f;
            Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(this.height, outRect.height));
            Widgets.BeginScrollView(outRect, ref this.pos, viewRect);
            float y = 10f;
            if (this.t.TryGetComp<CompCustomText>() is CompCustomText comp2)
            {
                this.DrawTextSection(ref y, viewWidth - 10f, "CQF_CustomName".Translate(), ref comp2.useCustomName, ref comp2.customName, false);
                this.DrawTextSection(ref y, viewWidth - 10f, "CQF_CustomDescription".Translate(), ref comp2.useCustomDescription, ref comp2.customDescription, true);
                this.DrawTextSection(ref y, viewWidth - 10f, "CQF_CustomInspectText".Translate(), ref comp2.useCustomInspectText, ref comp2.customInspectText, true);
            }
            if (t.TryGetComp<CompActionWorker>() is CompActionWorker comp)
            {
                CQFEditorTools.DrawIDrawList(ref y, 5f, comp.comps, viewRect, "ITab_CompActionWorker".Translate().Colorize(ColorLibrary.SkyBlue), () => comp.comps.Add(new ActionComp()), a => a.compName);
            }
            Widgets.EndScrollView();
            this.height = y + 10f;
        }

        private void DrawTextSection(ref float y, float width, string label, ref bool enabled, ref string text, bool multiline)
        {
            float editorHeight = multiline ? 58f : 30f;
            float sectionHeight = 38f + (enabled ? editorHeight + 10f : 0f);
            Rect sectionRect = new Rect(5f, y, width, sectionHeight);
            Rect headerRect = new Rect(sectionRect.x, sectionRect.y, sectionRect.width, 32f);
            Widgets.DrawBoxSolid(sectionRect, new Color(0.07f, 0.08f, 0.09f, 0.72f));
            Widgets.DrawBoxSolid(headerRect, new Color(0.14f, 0.17f, 0.2f, 0.9f));
            Widgets.Label(new Rect(headerRect.x + 10f, headerRect.y + 4f, headerRect.width - 50f, 25f), label);
            Widgets.Checkbox(new Vector2(headerRect.xMax - 30f, headerRect.y + 4f), ref enabled, 24f);

            if (enabled)
            {
                Rect editorRect = new Rect(sectionRect.x + 8f, headerRect.yMax + 6f, sectionRect.width - 16f, editorHeight);
                text = multiline ? Widgets.TextArea(editorRect, text ?? string.Empty) : Widgets.TextField(editorRect, text ?? string.Empty);
            }

            Widgets.DrawBox(sectionRect, 1);
            y += sectionHeight + 8f;
        }

        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
        Thing t = null;
    }
}
