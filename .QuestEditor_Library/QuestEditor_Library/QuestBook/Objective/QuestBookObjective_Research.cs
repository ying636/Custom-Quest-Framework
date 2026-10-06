using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjective_Research : QuestBookObjective
    {
        public override bool RequiresCheck => true;

        public ResearchProjectDef targetResearch;

        public override bool UsesResearchTarget => true;

        public override ResearchProjectDef TargetResearch
        {
            get => targetResearch;
            set => targetResearch = value;
        }

        public override void DrawSpecial(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective_Research.DrawSpecial(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
    }
}
