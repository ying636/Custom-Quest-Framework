using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ITab_CompActionWorker : ITab
    {
        public ITab_CompActionWorker()
        {
            this.size = new Vector2(500f,500f);
            this.labelKey = "ITab_CompActionWorker";
            this.tutorTag = "CompActionWorker";
        }
        public override bool IsVisible => DebugSettings.godMode;
        protected override bool StillValid => DebugSettings.godMode;
        protected override void FillTab()
        {
            Widgets.BeginScrollView(new Rect(5f, 5f,490,590f), ref this.scrollPos, new Rect(0f, 0f,490f, this.height));
            float y = 10f;
            if (this.SelObject is Thing thing && thing.TryGetComp<CompActionWorker>() is CompActionWorker comp) 
            {
                for (int i = 0; i < comp.comps.Count; i++)
                {
                    ActionComp c = comp.comps[i];
                    if (Widgets.ButtonText(new Rect(10f, y, 150f, 25f), c.compName, false))
                    {
                        Find.WindowStack.Add(new QuestEditor_EditActionComp(c));
                    }
                    y += 30f;
                };
                Rect rect = new Rect(332.5f, y, 25f, 25f);
                if (Widgets.ButtonImage(rect, TexButton.Paste))
                {
                    comp.PasteSingleComp();
                }
                TooltipHandler.TipRegion(rect, "Paste".Translate());
                CQFEditorTools.DrawButtonForList(ref y,comp.comps,c => c.compName);
            }
            Widgets.EndScrollView();
            this.height = y;
        }

        public Vector2 scrollPos = Vector2.zero;
        public float height =0f;
    }
}
