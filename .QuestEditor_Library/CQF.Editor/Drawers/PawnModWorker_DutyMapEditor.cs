using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class PawnModWorker_DutyMapEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_DutyMap cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_DutyMap data = pawnDef.DataFor<PawnModData_DutyMap>();
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_DutyMap".Translate(cqfReceiver.ValueOrNone(data.dutyMap?.defName))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DutyMapDef>.AllDefsListForReading, dutyMap => data.dutyMap = dutyMap, dutyMap => dutyMap.defName);
            }

            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_DutyMapStartNode".Translate(cqfReceiver.ValueOrNone(data.dutyMapStartNodeId))))
            {
                DutyMapDef map = data.dutyMap;
                if (map != null)
                {
                    CQFEditorTools.DrawFloatMenu(map.nodes, node => data.dutyMapStartNodeId = node.nodeId, node => node.nodeId);
                }
            }
        }
    }
}
