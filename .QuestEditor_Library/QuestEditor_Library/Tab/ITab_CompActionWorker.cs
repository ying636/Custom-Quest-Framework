using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ITab_CompActionWorker : ITab
    {
        public ITab_CompActionWorker()
        {
            this.size = new Vector2(500f, 500f);
            this.labelKey = "ITab_CompActionWorker";
            this.tutorTag = "CompActionWorker";
        }

        internal object CQFSelectedObject => this.SelObject;
        internal Vector2 CQFSize => this.size;
        public override bool IsVisible => CQFEditorBridge.IsLoaded && DebugSettings.godMode;
        protected override bool StillValid => CQFEditorBridge.IsLoaded && DebugSettings.godMode;

        protected override void FillTab()
        {
            Rect viewport = new Rect(8f, 8f, this.CQFSize.x - 16f, this.CQFSize.y - 16f);
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.height));
            using CQFUIScope scope = new CQFUIScope(content.width, viewport.height);
            Widgets.BeginScrollView(viewport, ref this.scrollPos, content);
            float y = 10f;
            if (this.CQFSelectedObject is Thing thing && thing.TryGetComp<CompActionWorker>()is CompActionWorker comp)
            {
                for (int i = 0; i < comp.comps.Count; i++)
                {
                    ActionComp c = comp.comps[i];
                    if (CQFUIStyle.ButtonText(new Rect(10f, y, content.width - 20f, 25f), c.compName, false))
                    {
                        Find.WindowStack.Add(CQFEditorBridge.Editor.CreateActionCompWindow(c, thing));
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
            this.height = y;
        }

        public Vector2 scrollPos = Vector2.zero;
        public float height = 0f;
    }
}
