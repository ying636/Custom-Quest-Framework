using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Backstory : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.KindDef == null || pawnDef.KindDef.race.race.Humanlike;
        }

        public override PawnModData CreateData()
        {
            return new PawnModData_Backstory();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Backstory.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
        }
        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            PawnModData_Backstory data = pawnDef.DataFor<PawnModData_Backstory>();
            if (pawn.story == null)
            {
                return;
            }
            if (data.childhood != null)
            {
                pawn.story.Childhood = data.childhood;
            }
            if (data.adulthood != null)
            {
                pawn.story.Adulthood = data.adulthood;
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            PawnModData_Backstory data = pawnDef.DataFor<PawnModData_Backstory>();
            data.childhood = DefDatabase<BackstoryDef>.GetNamedSilentFail(node["childhood"]?.InnerText);
            data.adulthood = DefDatabase<BackstoryDef>.GetNamedSilentFail(node["adulthood"]?.InnerText);
        }
        internal void DrawBackstoryButton(ref float y, Rect inRect, float x, string label, Action<BackstoryDef> action)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                label,
                action
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Backstory.DrawBackstoryButton(Ref:float,None:UnityEngine.Rect,None:float,None:string,None:System.Action<RimWorld.BackstoryDef>)", this, arguments);
            y = (float)arguments[0];
        }
}
}
