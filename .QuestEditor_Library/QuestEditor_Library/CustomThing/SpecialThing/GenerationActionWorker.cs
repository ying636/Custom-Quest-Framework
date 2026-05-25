using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class GenerationActionWorker : ThingWithComps, IDrawTabable, IPastableData
    {
        public void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "CQFActions".Translate(), a => a.GetType().Name.Translate());
        }
        public virtual void DrawTab()
        {
            Widgets.BeginScrollView(new Rect(7f, 25f, 475f, 590f), ref this.scrollPos, new Rect(7f, 10f, 475f, this.height));
            Widgets.DrawBox(new Rect(8f, 10f, 470f, this.height), 1, QuestEditor_Dialog.blueTex);
            float y = 20f;
            Rect rectCP = new Rect(380f, y, 25f, 25f);
            if (Widgets.ButtonImage(rectCP, TexButton.Copy))
            {
                this.CopyData();
            }
            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            rectCP.x += 30f;
            if (Widgets.ButtonImage(rectCP, TexButton.Paste))
            {
                PasteData();
            }
            TooltipHandler.TipRegion(rectCP, "Paste".Translate());
            CQFEditorTools.DrawActionList_UseWindow(ref y, 15f, this.actions, new Rect(0f, 0f, 475f, this.height), "CQFActions".Translate().Colorize(ColorLibrary.SkyBlue), a => a.GetType().Name.Translate());
            this.height = y + 5f;
            Widgets.EndScrollView();
        }
        public void PasteData()
        {
            this.actions.Clear();
            CQFEditorTools.actions.ForEach(a => this.actions.Add(a.Copy()));
        }
        public void CopyData()
        {
            CQFEditorTools.actions.Clear();
            this.actions.ForEach(a => CQFEditorTools.actions.Add(a.Copy()));
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "DEV:Do actions",
                    action = () =>
                    {
                        this.actions.ForEach(a2 => a2.Work(new Dictionary<string, TargetInfo>() { ["Position"] = new TargetInfo(this.Position, this.Map) }, null));
                    }
                };
            }
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.actions, "actions",LookMode.Deep);
        }

        public float height = 0f;
        public Vector2 scrollPos;
        public List<CQFAction> actions = new List<CQFAction>();
    }
}
