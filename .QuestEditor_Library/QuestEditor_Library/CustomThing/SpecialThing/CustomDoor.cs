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
            Rect inRect = new Rect(0f, 0f, 540f, 590f);
            float width = inRect.width - 20f;
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, width, Mathf.Max(inRect.height, this.height + 10f)));
            float x = 10f;
            float y = 10f;
            this.DrawSectionHeader(ref y, x, width, "OpeningActions".Translate(), "CustomDoorOpeningActionsTip".Translate(),
                () => CQFEditorTools.OpenCQFActionSelect(t => this.openingActions.Add((CQFAction)Activator.CreateInstance(t))),
                () => CQFEditorTools.DrawFloatMenu(this.openingActions, a => this.openingActions.Remove(a), a => a.GetType().Name.Translate()),
                () => this.openingActions.Any());
            this.DrawActionList(ref y, x, width, inRect);

            this.DrawSectionHeader(ref y, x, width, "OpeningConditions".Translate(), "CustomDoorOpeningConditionsTip".Translate(),
                () => Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), c => c.Name.Translate(), c =>
                this.openingConditions.Add((DialogCondition)Activator.CreateInstance(c)), null, null, null, null, null, null), "Select".Translate())),
                () => CQFEditorTools.DrawFloatMenu(this.openingConditions, c => this.openingConditions.Remove(c), c => c.GetType().Name.Translate()),
                () => this.openingConditions.Any());
            foreach (DialogCondition c in this.openingConditions)
            {
                float itemY = y;
                c.Draw(ref y, inRect, x + 8f);
                this.DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }
            if (!this.openingConditions.Any())
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CustomDoorNoConditions".Translate());
            }
            this.height = y + 10f;
            Widgets.EndScrollView();
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

        private void DrawActionList(ref float y, float x, float width, Rect inRect)
        {
            foreach (CQFAction action in this.openingActions)
            {
                float itemY = y;
                action.Draw(ref y, inRect, x + 8f);
                this.DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }
            if (!this.openingActions.Any())
            {
                this.DrawEmptyState(ref y, x + 8f, width - 16f, "CustomDoorNoActions".Translate());
            }
            y += 10f;
        }

        private void DrawSectionHeader(ref float y, float x, float width, string label, string tip, Action addAction, Action removeAction, Func<bool> canRemove)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Rect labelRect = new Rect(x + 8f, y + 4f, width - 84f, 25f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.SkyBlue));
            TooltipHandler.TipRegion(labelRect, tip);

            Rect button = new Rect(x + width - 66f, y + 2f, 25f, 25f);
            if (Widgets.ButtonImage(button, TexButton.Plus))
            {
                addAction();
            }
            TooltipHandler.TipRegion(button, "Add".Translate());

            button.x += 30f;
            if (Widgets.ButtonImage(button, TexButton.Delete) && canRemove())
            {
                removeAction();
            }
            TooltipHandler.TipRegion(button, "Remove".Translate());
            y += 38f;
        }

        private void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Rect rect = new Rect(x + 6f, startY - 2f, width - 12f, Mathf.Max(34f, endY - startY + 4f));
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DrawLine(new Vector2(rect.x + 6f, rect.yMax), new Vector2(rect.xMax - 6f, rect.yMax), ColorLibrary.SkyBlue, 1f);
        }

        private void DrawEmptyState(ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(Color.gray));
            y += 32f;
        }

        public float height;
        public Vector2 pos = Vector2.zero;
        public List<CQFAction> openingActions = new List<CQFAction>();
        public List<DialogCondition> openingConditions = new List<DialogCondition>();
    }
}
