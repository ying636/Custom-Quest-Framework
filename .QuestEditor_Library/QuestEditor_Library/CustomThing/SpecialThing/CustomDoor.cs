using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CustomDoor : Building_Door, IDrawTabable, ICustomThing
    {
        public override bool PawnCanOpen(Pawn p)
        {
            return base.PawnCanOpen(p)
                && !this.openingConditions.Exists(c =>
                !c.Satisfied(new Dictionary<string, TargetInfo>() { ["Trigger"] = p , ["CustomThing"] = this},out string r,GameTools.GetQuestFromThing(this)));
        }
        protected override void DoorOpen(int ticksToClose = 110)
        {
            base.DoorOpen(ticksToClose);
            this.openingActions.ForEach(a => a.Work(new Dictionary<string, TargetInfo>()
            { ["CustomThing"] = this}, GameTools.GetQuestFromThing(this)));
        }
        public void DrawTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomDoor.DrawTab()", this, arguments);
        }
        public CustomThingData GetData(IntVec3 pos)
        {
            return new CustomThingData_CustomDoor(this, pos);
        }   
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.openingConditions, "openingConditions", LookMode.Deep);
            Scribe_Collections.Look(ref this.openingActions, "openingActions", LookMode.Deep);
        }
        internal void DrawActionList(ref float y, float x, float width, Rect inRect)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                inRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomDoor.DrawActionList(Ref:float,None:float,None:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawSectionHeader(ref float y, float x, float width, string label, string tip, Action addAction, Action removeAction, Func<bool> canRemove)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                label,
                tip,
                addAction,
                removeAction,
                canRemove
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomDoor.DrawSectionHeader(Ref:float,None:float,None:float,None:string,None:string,None:System.Action,None:System.Action,None:System.Func<bool>)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Rect rect = new Rect(x + 6f, startY - 2f, width - 12f, Mathf.Max(34f, endY - startY + 4f));
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DrawLine(new Vector2(rect.x + 6f, rect.yMax), new Vector2(rect.xMax - 6f, rect.yMax), ColorLibrary.SkyBlue, 1f);
        }
        internal void DrawEmptyState(ref float y, float x, float width, string label)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                label
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomDoor.DrawEmptyState(Ref:float,None:float,None:float,None:string)", this, arguments);
            y = (float)arguments[0];
        }
        public float height;
        public Vector2 pos = Vector2.zero;
        public List<CQFAction> openingActions = new List<CQFAction>();
        public List<DialogCondition> openingConditions = new List<DialogCondition>();
    }
}
