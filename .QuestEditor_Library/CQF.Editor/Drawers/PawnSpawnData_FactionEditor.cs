using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class PawnSpawnData_FactionEditor
    {
        public static void DrawKind_0(QuestEditor_Library.PawnSpawnData_Faction cqfReceiver, float x, ref float y)
        {
            Rect rect = PawnSpawnDataEditor.DrawField(ref y, x, "CQF_PawnGroupMaker".Translate(string.Empty));
            FactionDef? factionDef = cqfReceiver.faction.NullOrEmpty() ? null : DefDatabase<FactionDef>.GetNamedSilentFail(cqfReceiver.faction);
            if (CQFUIStyle.ButtonText(rect, cqfReceiver.kindDef?.defName ?? "Select".Translate().ToString(), active: factionDef != null && !factionDef.pawnGroupMakers.NullOrEmpty()) && factionDef != null)
            {
                CQFEditorTools.DrawFloatMenu(factionDef.pawnGroupMakers, (k) => cqfReceiver.kindDef = k.kindDef, (k) =>
                {
                    return k.kindDef.defName + ":" + k.commonality;
                });
            }

            TooltipHandler.TipRegion(rect, "CQF_PawnGroupMaker_Tip".Translate());
            Rect points = PawnSpawnDataEditor.DrawField(ref y, x, "SpawmPoint".Translate(), 190f);
            float part = (points.width - 18f) / 2f;
            int min = cqfReceiver.point.min;
            int max = cqfReceiver.point.max;
            Widgets.TextFieldNumeric(new Rect(points.x, points.y, part, points.height), ref min, ref cqfReceiver.buffer1);
            Widgets.Label(new Rect(points.x + part + 3f, points.y + 4f, 12f, 25f), "~");
            Widgets.TextFieldNumeric(new Rect(points.x + part + 18f, points.y, part, points.height), ref max, ref cqfReceiver.buffer2);
            cqfReceiver.point = new IntRange(min, max);
        }
    }
}
