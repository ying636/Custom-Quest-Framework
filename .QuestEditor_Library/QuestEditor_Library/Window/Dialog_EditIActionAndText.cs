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
            Widgets.BeginScrollView(new Rect(5f, 5f, 490, 590f), ref this.pos, new Rect(0f, 0f, 490f, this.height));
            float y = 10f;
            if (this.t.TryGetComp<CompCustomText>() is CompCustomText comp2)
            {
                Rect rect = new Rect(5f, y, 350f, 25f);
                Widgets.CheckboxLabeled(rect, "UseCustomName".Translate(), ref comp2.useCustomName);
                rect.y += 30f;
                if (comp2.useCustomName)
                {
                    CQFEditorTools.DrawLabelAndText_Line(rect.y, "CQF_CustomName".Translate(), ref comp2.customName, 5f, 350f);
                    rect.y += 30f;
                }
                Widgets.CheckboxLabeled(rect, "UseCustomDescription".Translate(), ref comp2.useCustomDescription);
                rect.y += 30f;
                if (comp2.useCustomDescription)
                {
                    CQFEditorTools.DrawLabelAndText_Line(rect.y, "CQF_CustomDescription".Translate(), ref comp2.customDescription, 5f, 350f);
                    rect.y += 30f;
                }
                y = rect.y;
            }
            if (t.TryGetComp<CompActionWorker>() is CompActionWorker comp)
            {
                CQFEditorTools.DrawIDrawList(ref y,5f,comp.comps,inRect, "ITab_CompActionWorker".Translate().Colorize(ColorLibrary.SkyBlue),() => comp.comps.Add(new ActionComp()),a => a.compName);
            }
            Widgets.EndScrollView();
            this.height = y;
        }

        public string buffer;
        public float height;
        public Vector2 pos = Vector2.zero;
        Thing t = null;
    }
}
