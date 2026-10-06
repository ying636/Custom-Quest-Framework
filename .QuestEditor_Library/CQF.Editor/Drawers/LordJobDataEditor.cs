using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class LordJobDataEditor
    {
        public static void Draw_0(QuestEditor_Library.LordJobData cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawName(ref y, inRect, x);
            y += 30f;
            if (cqfReceiver.JobSelectable)
            {
                if (Widgets.ButtonText(new Rect(x, y, 350f, 25f), "CQF_LordJob".Translate(cqfReceiver.lordJob.Name.CanTranslate() ? cqfReceiver.lordJob.Name.Translate().ToString() : cqfReceiver.lordJob.Name), false))
                {
                    Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(LordJob).AllSubclassesNonAbstract(), t => t.Name.CanTranslate() ? t.Name.Translate().ToString() : t.Name, t => cqfReceiver.lordJob = t, null, t => (t.Name + "_Tip").CanTranslate() ? (t.Name + "_Tip").Translate().ToString() : ""), "Select".Translate()));
                }
            }
            else
            {
                Widgets.Label(new Rect(x, y, 350f, 25f), "CQF_LordJob".Translate(cqfReceiver.LordJob.Name.CanTranslate() ? cqfReceiver.LordJob.Name.Translate().ToString() : cqfReceiver.LordJob.Name));
            }

            y += 30f;
            if (cqfReceiver.lordJob == typeof(LordJob_ComplexCustom))
            {
                cqfReceiver.DrawComplexDutyMap(ref y, x);
            }
        }

        public static void DrawName_1(QuestEditor_Library.LordJobData cqfReceiver, ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(x, y, 250f, 25f);
            if (Widgets.ButtonText(rect, cqfReceiver.GetType().Name.Translate(), false))
            {
                List<Type> types = typeof(LordJobData).AllSubclassesNonAbstract().ListFullCopy();
                types.Add(typeof(LordJobData));
                Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(types, t => t.Name.CanTranslate() ? t.Name.Translate().ToString() : t.Name, t =>
                {
                    cqfReceiver.lordData.lordJobData = (LordJobData)Activator.CreateInstance(t);
                    cqfReceiver.lordData.lordJobData.lordData = cqfReceiver.lordData;
                }, null, t => (t.Name + "_Tip").CanTranslate() ? (t.Name + "_Tip").Translate().ToString() : ""), "Select".Translate()));
            }

            if ((cqfReceiver.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (cqfReceiver.GetType().Name + "_Tip").Translate());
            }
        }

        public static void DrawComplexDutyMap_2(QuestEditor_Library.LordJobData cqfReceiver, ref float y, float x)
        {
            if (Widgets.ButtonText(new Rect(x, y, 350f, 25f), "CQF_LordData_DutyMap".Translate(cqfReceiver.dutyMap?.defName ?? "Null"), false))
            {
                Find.WindowStack.Add(new Dialog_Select<DutyMapDef>(new TextSelectDrawer<DutyMapDef>(DefDatabase<DutyMapDef>.AllDefsListForReading, d => d.defName, d => cqfReceiver.dutyMap = d, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
            if (cqfReceiver.dutyMap != null && cqfReceiver.dutyMap.nodes.Any())
            {
                CQFEditorTools.DrawSelectableText(y, "CQF_LordData_DutyMapStartNode".Translate(), ref cqfReceiver.dutyMapStartNodeId, () => CQFEditorTools.DrawFloatMenu(cqfReceiver.dutyMap.nodes, node => cqfReceiver.dutyMapStartNodeId = node.nodeId, node => node.nodeId), x, 180f);
                y += 30f;
            }
        }
    }
}
