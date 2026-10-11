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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "CQFActions".Translate(), a => a.GetType().Name.Translate());
        }

        public virtual void DrawTab()
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect viewport = new Rect(8f, 36f, Mathf.Min(490f, CQFUIScope.ContentWidth - 16f), Mathf.Max(40f, CQFUIScope.ContentHeight - 44f));
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.height));
            Widgets.BeginScrollView(viewport, ref this.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            CQFUIStyle.DrawBox(new Rect(8f, 8f, content.width - 16f, Mathf.Max(40f, this.height - 8f)), 1, CQFUIStyle.AccentTexture);
            float y = 20f;
            Rect rectCP = new Rect(content.width - 40f, y, 25f, 25f);
            if (CQFUIStyle.ButtonImage(rectCP, TexButton.Copy))
            {
                this.CopyData();
            }

            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            rectCP.x += 32f;
            if (CQFUIStyle.ButtonImage(rectCP, TexButton.Paste))
            {
                this.PasteData();
            }

            TooltipHandler.TipRegion(rectCP, "Paste".Translate());
            y += 36f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, 15f, this.actions, content, "CQFActions".Translate().Colorize(CQFUIStyle.Accent), a => a.GetType().Name.Translate());
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
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public float height = 0f;
        public Vector2 scrollPos;
        public List<CQFAction> actions = new List<CQFAction>();
    }
}
