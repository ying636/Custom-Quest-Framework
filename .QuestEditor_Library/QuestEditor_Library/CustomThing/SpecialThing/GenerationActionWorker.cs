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
            CQFActionListEditor.Draw(ref y, x, inRect.width - x - 12f, inRect, "CQFActions".Translate(), this.actions,
                copyList: this.CopyData, pasteList: this.PasteData);
        }

        public virtual void DrawTab()
        {
            using CQFUIScope scope = new CQFUIScope();
            GUI.color = CQFUIStyle.TextColor;
            Rect viewport = new Rect(8f, 36f, CQFUIScope.ContentWidth - 16f, Mathf.Max(40f, CQFUIScope.ContentHeight - 44f));
            Rect content = new Rect(0f, 0f, viewport.width - 20f, Mathf.Max(viewport.height, this.height));
            Widgets.BeginScrollView(viewport, ref this.scrollPos, content);
            using CQFUIScope contentScope = new CQFUIScope(content.width);
            float y = 10f;
            this.Draw(ref y, content, 8f);
            this.height = y + 8f;
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
                    defaultLabel = "CQF_RunActions".Translate(),
                    icon = TexButton.Play,
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
