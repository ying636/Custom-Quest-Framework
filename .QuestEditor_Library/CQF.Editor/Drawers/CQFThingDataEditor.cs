using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;
using System.Xml.Linq;
using System.Xml;

namespace QuestEditor_Library
{
    public static class CQFThingDataEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFThingData cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawIcon(ref y);
            Widgets.Label(new Rect(60f + x, y + 5f, 35f, 35f), "x");
            int min = cqfReceiver.count.min;
            int max = cqfReceiver.count.max;
            Widgets.TextFieldNumeric<int>(new Rect(75f + x, y, 35f, 35f), ref min, ref cqfReceiver.bufferMin);
            Widgets.Label(new Rect(113f + x, y + 5f, 35f, 35f), "~");
            Widgets.TextFieldNumeric<int>(new Rect(125f + x, y, 35f, 35f), ref max, ref cqfReceiver.bufferMax);
            cqfReceiver.count = new IntRange(min, max);
            if (cqfReceiver.CanSelectStuff)
            {
                Rect rect = new Rect(180f + x, y + 3f, 150f, 25f);
                if (Widgets.ButtonText(rect, "SelectStuff".Translate(cqfReceiver.stuff?.label), false))
                {
                    CQFEditorTools.DrawFloatMenu<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((t) => t.IsStuff), (t) => cqfReceiver.stuff = t, (t) => t.label, new List<FloatMenuOption>() { new FloatMenuOption("Null".Translate(), () => cqfReceiver.stuff = null) });
                }

                TooltipHandler.TipRegion(rect, "CQFStuffTip".Translate());
            }

            y += 35f;
        }

        public static void DrawWithSingleCount_1(QuestEditor_Library.CQFThingData cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawIcon(ref y);
            Widgets.Label(new Rect(60f + x, y + 5f, 35f, 35f), "x");
            int min = cqfReceiver.count.min;
            Widgets.TextFieldNumeric<int>(new Rect(75f + x, y, 35f, 35f), ref min, ref cqfReceiver.bufferMin);
            cqfReceiver.count = new IntRange(min, min);
            if (cqfReceiver.CanSelectStuff)
            {
                Rect rect = new Rect(180f + x, y + 3f, 150f, 25f);
                if (Widgets.ButtonText(rect, "SelectStuff".Translate(cqfReceiver.stuff?.label), false))
                {
                    CQFEditorTools.DrawFloatMenu<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((t) => t.IsStuff), (t) => cqfReceiver.stuff = t, (t) => t.label, new List<FloatMenuOption>() { new FloatMenuOption("Null".Translate(), () => cqfReceiver.stuff = null) });
                }

                TooltipHandler.TipRegion(rect, "CQFStuffTip".Translate());
            }

            y += 30f;
        }
    }
}
