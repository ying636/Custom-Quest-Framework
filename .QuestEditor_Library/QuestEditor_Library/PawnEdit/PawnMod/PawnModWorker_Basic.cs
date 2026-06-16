using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Basic : PawnModWorker
    {
        public override PawnModData CreateData()
        {
            return new PawnModData_Basic();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Basic data = pawnDef.DataFor<PawnModData_Basic>();
            Rect row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_DefName".Translate(), 170f);
            pawnDef.defName = Widgets.TextField(new Rect(row.x, row.y, Mathf.Min(360f, row.width), 30f), pawnDef.defName);
            this.EndRow(ref y);
            row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_Label".Translate(), 170f);
            pawnDef.label = Widgets.TextField(new Rect(row.x, row.y, Mathf.Min(360f, row.width), 30f), pawnDef.label);
            this.EndRow(ref y);
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_PawnKind".Translate(this.ValueOrNone(data.kindDef?.label))))
            {
                Find.WindowStack.Add(new Dialog_Select<PawnKindDef>(DefDatabase<PawnKindDef>.AllDefsListForReading, null, kind => kind.label, "CQF_PawnEditor_SelectPawnKind".Translate(), kind => data.kindDef = kind));
            }
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Faction".Translate() + this.ValueOrNone(data.faction?.label)))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<FactionDef>.AllDefsListForReading, faction => data.faction = faction, faction => faction.label);
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            PawnModData_Basic data = pawnDef.DataFor<PawnModData_Basic>();
            data.unique = ParseHelper.FromString<bool>(node["unique"]?.InnerText ?? "false");
            data.kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(node["kindDef"]?.InnerText);
            data.faction = DefDatabase<FactionDef>.GetNamedSilentFail(node["faction"]?.InnerText);
        }
    }
}
