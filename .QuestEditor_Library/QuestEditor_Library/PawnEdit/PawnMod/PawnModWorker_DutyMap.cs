using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class PawnModWorker_DutyMap : PawnModWorker
    {
        public override PawnModData CreateData()
        {
            return new PawnModData_DutyMap();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_DutyMap data = pawnDef.DataFor<PawnModData_DutyMap>();
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_DutyMap".Translate(this.ValueOrNone(data.dutyMap?.defName))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DutyMapDef>.AllDefsListForReading, dutyMap => data.dutyMap = dutyMap, dutyMap => dutyMap.defName);
            }
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_DutyMapStartNode".Translate(this.ValueOrNone(data.dutyMapStartNodeId))))
            {
                DutyMapDef map = data.dutyMap;
                if (map != null)
                {
                    CQFEditorTools.DrawFloatMenu(map.nodes, node => data.dutyMapStartNodeId = node.nodeId, node => node.nodeId);
                }
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            PawnModData_DutyMap data = pawnDef.DataFor<PawnModData_DutyMap>();
            data.dutyMap = DefDatabase<DutyMapDef>.GetNamedSilentFail(node["dutyMap"]?.InnerText);
            data.dutyMapStartNodeId = node["dutyMapStartNodeId"]?.InnerText;
        }

        public override void OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest)
        {
            PawnModData_DutyMap data = pawnDef.DataFor<PawnModData_DutyMap>();
            if (pawn == null || data.dutyMap == null)
            {
                return;
            }
            GameComponent_ComplexDuty.Instance.SetDutyMap(pawn, data.dutyMap, quest, true);
            if (!data.dutyMapStartNodeId.NullOrEmpty() && data.dutyMap.GetNode(data.dutyMapStartNodeId) != null)
            {
                LordJob_ComplexCustom.GetForPawn(pawn)?.ChangeNode(pawn, data.dutyMapStartNodeId, quest);
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            PawnModData_DutyMap data = pawnDef.DataFor<PawnModData_DutyMap>();
            if (preview || pawn == null || data.dutyMap == null)
            {
                return;
            }
            GameComponent_ComplexDuty.Instance.SetDutyMap(pawn, data.dutyMap, null, true);
        }
    }
}

