using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjective_Research : QuestBookObjective
    {
        public override bool RequiresCheck => true;
        public override bool UsesResearchTarget => true;
        public override ResearchProjectDef TargetResearch { get => targetResearch; set => targetResearch = value; }

        public override void DrawSpecial(ref float y, Rect inRect, float x)
        {
            this.DrawDetectionSection(ref y, inRect, (Rect card, ref float rowY) =>
            {
                this.DrawRowLabel(card, rowY, "CQF_QuestBook_TargetResearch");
                string label = this.TargetResearch == null ? "CQF_QuestBook_None".Translate().ToString() : this.TargetResearch.LabelCap;
                Rect button = new Rect(card.x + 184f, rowY, card.width - 198f, 28f);
                if (CQFUIStyle.ButtonText(button, label, false, true))
                {
                    List<ResearchProjectDef> projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading.OrderBy(def => def.label).ToList();
                    Find.WindowStack.Add(new Dialog_Select<ResearchProjectDef>(new TextSelectDrawer<ResearchProjectDef>(projects, def => def.LabelCap, def => this.TargetResearch = def, null, def => def.description, null, def => def.defName, null, null), "CQF_QuestBook_TargetResearch".Translate()));
                }

                rowY += 36f;
            });
        }

        public override bool Process(QuestBookObjectiveProgress progress, Signal signal)
        {
            return false;
        }

        public override bool Check(QuestBookObjectiveProgress progress)
        {
            if (targetResearch == null || progress == null)
            {
                return false;
            }

            progress.currentCount = targetResearch.IsFinished ? 1 : 0;
            progress.completed = targetResearch.IsFinished;
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref targetResearch, "targetResearch");
        }

        public ResearchProjectDef targetResearch;
    }
}
