using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Dialog : PawnModWorker
    {
        public override PawnModData CreateData()
        {
            return new PawnModData_Dialog();
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
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Dialog.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
        }
        public override void OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest)
        {
            DialogManagerDef dialogManager = pawnDef.DataFor<PawnModData_Dialog>().dialogManager;
            if (dialogManager != null && pawn != null)
            {
                GameComponent_Editor.Instance?.AddDialog(pawn, dialogManager);
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            pawnDef.DataFor<PawnModData_Dialog>().dialogManager = DefDatabase<DialogManagerDef>.GetNamedSilentFail(node["dialogManager"]?.InnerText);
        }
    }
}
