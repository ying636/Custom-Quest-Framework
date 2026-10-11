using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAction_MetaLordEditor
    {
        public static void Draw_0(CQFAction_MetaLord data, ref float y, Rect inRect, float x)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(data, ref y, inRect, x);
            if (CQFUIStyle.ButtonText(new Rect(x, y, inRect.width - x - 12f, 30f),
                ("CQF_MetaLordOperation_" + data.operation).Translate()))
                CQFEditorTools.DrawFloatMenu(Enum.GetValues(typeof(MetaLordOperation)).Cast<MetaLordOperation>().ToList(),
                    value => data.operation = value, value => ("CQF_MetaLordOperation_" + value).Translate());
            y += 36f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordName".Translate(), ref data.organizationName, x, 180f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordMapKey".Translate(), ref data.mapKey, x, 180f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordGroup".Translate(), ref data.groupName, x, 180f);
            y += 30f;
            if (data.operation == MetaLordOperation.Move)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordTimeout".Translate(), ref data.timeoutTicks, ref data.timeoutBuffer, x, 100f);
                y += 30f;
            }
            if (data.operation == MetaLordOperation.Signal)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordSignal".Translate(), ref data.signalTag, x, 180f);
                y += 30f;
            }
            if (data.operation != MetaLordOperation.CreateGroup) return;
            CQFEditorTools.DrawFactionSelectableText(y, "PawnDataFaction".Translate(), ref data.faction, value => data.faction = value, x, 180f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordCapacity".Translate(), ref data.capacity, ref data.capacityBuffer, x, 100f);
            y += 30f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, inRect.width - x - 12f, 30f),
                "CQF_MetaLordEvents".Translate() + ": " + (data.eventDef?.defName ?? "Null")))
                CQFEditorTools.DrawFloatMenu(DefDatabase<MetaLordEventDef>.AllDefsListForReading,
                    value => data.eventDef = value, value => value.defName);
            y += 36f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, inRect.width - x - 12f, 30f),
                "CQF_LordData_DutyMap".Translate(data.dutyMap?.defName ?? "Null")))
                CQFEditorTools.DrawFloatMenu(DefDatabase<DutyMapDef>.AllDefsListForReading,
                    value => data.dutyMap = value, value => value.defName);
            y += 36f;
            CQFEditorTools.DrawEditableStringList(data.groupTags, ref y, "CQF_MetaLordTags".Translate(), x: x, width: 180f);
        }
    }
}
