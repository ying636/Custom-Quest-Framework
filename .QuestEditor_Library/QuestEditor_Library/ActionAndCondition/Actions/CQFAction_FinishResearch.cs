using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAction_FinishResearch : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Misc;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            string label = "CQF_QuestBook_TargetResearch".Translate() + ": " + (this.researchProject == null ? "CQF_QuestBook_None".Translate().ToString() : this.researchProject.LabelCap.ToString());
            if (CQFUIStyle.ButtonText(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 12f), 28f), label, false, true, true, TextAnchor.MiddleLeft))
            {
                List<ResearchProjectDef> projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading.OrderBy(def => def.label).ToList();
                Find.WindowStack.Add(new Dialog_Select<ResearchProjectDef>(new TextSelectDrawer<ResearchProjectDef>(projects, def => def.LabelCap, def => this.researchProject = def, getTip: def => def.description, getRawText: def => def.defName), "CQF_QuestBook_TargetResearch".Translate()));
            }

            y += 36f;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (researchProject == null)
            {
                throw new InvalidOperationException("[CQF] CQFAction_FinishResearch requires a research project.");
            }

            if (!researchProject.IsFinished)
            {
                Find.ResearchManager.FinishProject(researchProject);
            }
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (researchProject != null)
            {
                result.Add(new XElement("researchProject", researchProject.defName));
            }

            return result;
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref researchProject, "researchProject");
        }

        public ResearchProjectDef? researchProject;
    }
}
