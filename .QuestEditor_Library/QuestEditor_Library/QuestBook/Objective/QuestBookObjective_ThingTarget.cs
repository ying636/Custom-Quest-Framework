using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class QuestBookObjective_ThingTarget : QuestBookObjective_TargetCount
    {
        public ThingDef targetThingDef;

        public override bool UsesThingTarget => true;

        public override ThingDef TargetThingDef
        {
            get => targetThingDef;
            set => targetThingDef = value;
        }

        public override IEnumerable<ThingDef> GetThingTargets()
        {
            yield break;
        }

        public override void DrawSpecial(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective_ThingTarget.DrawSpecial(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref targetThingDef, "targetThingDef");
        }
    }
}
