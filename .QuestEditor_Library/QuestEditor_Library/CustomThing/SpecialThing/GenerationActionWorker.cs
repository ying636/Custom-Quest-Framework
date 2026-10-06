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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.GenerationActionWorker.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual void DrawTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.GenerationActionWorker.DrawTab()", this, arguments);
        }
        public void PasteData()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.GenerationActionWorker.PasteData()", this, arguments);
        }
        public void CopyData()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.GenerationActionWorker.CopyData()", this, arguments);
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
