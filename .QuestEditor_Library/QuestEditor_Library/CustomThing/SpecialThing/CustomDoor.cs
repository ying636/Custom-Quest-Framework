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
            Rect inRect = new Rect(0f, 0f, 500f, 500f);
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref this.pos, new Rect(0f, 0f, inRect.width - 20f, this.height + 10f));
            float x = 10f;
            float y = 15f; 
            CQFEditorTools.DrawActionList(ref y, x, this.openingActions, inRect, "OpeningActions".Translate().Colorize(ColorLibrary.SkyBlue), true);
            Widgets.Label(new Rect(x, y, 150f, 25f), "OpeningConditions".Translate().Colorize(ColorLibrary.PaleBlue));
            CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), c => c.Name.Translate(), c =>
    this.openingConditions.Add((DialogCondition)Activator.CreateInstance(c)), null, null, null, null, null, null), "Select".Translate())), () => CQFEditorTools.DrawFloatMenu(this.openingConditions, c => this.openingConditions.Remove(c), c => c.GetType().Name.Translate()), inRect.width - 150f, 30);
            y += 30f;
            foreach (DialogCondition c in this.openingConditions)
            {
                c.Draw(ref y, inRect, x);
            }
            this.height = y;
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


        public float height;
        public Vector2 pos = Vector2.zero;
        public List<CQFAction> openingActions = new List<CQFAction>();
        public List<DialogCondition> openingConditions = new List<DialogCondition>();
    }
}
