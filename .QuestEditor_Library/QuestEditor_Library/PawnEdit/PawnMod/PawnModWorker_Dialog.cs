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
            PawnModData_Dialog data = pawnDef.DataFor<PawnModData_Dialog>();
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_DialogManager".Translate(this.ValueOrNone(data.dialogManager?.defName))))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("CQF_PawnEditor_None".Translate(), () => data.dialogManager = null)
                };
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, manager => data.dialogManager = manager, manager => manager.defName, options);
            }
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

