using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorkerEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
        }

        public static Rect DrawRowLabel_1(QuestEditor_Library.PawnModWorker cqfReceiver, ref float y, Rect inRect, float x, string label, float labelWidth = 150f, float height = 30f)
        {
            Rect labelRect = new Rect(x, y + 3f, labelWidth, 25f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.PaleBlue));
            return new Rect(x + labelWidth + 8f, y, Mathf.Max(120f, inRect.width - x - labelWidth - 24f), height);
        }

        public static bool DrawSelectRow_2(QuestEditor_Library.PawnModWorker cqfReceiver, ref float y, Rect inRect, float x, string label, float height = 30f)
        {
            Rect rect = new Rect(x, y, inRect.width - x - 20f, height);
            bool result = cqfReceiver.DrawTextButton(rect, label);
            cqfReceiver.EndRow(ref y, height);
            return result;
        }

        public static void DrawColorRow_3(QuestEditor_Library.PawnModWorker cqfReceiver, ref float y, Rect inRect, float x, string label, Color color, Action<Color> apply)
        {
            cqfReceiver.DrawColorRow(ref y, inRect, x, label, color, apply, null);
        }

        public static void DrawColorRow_4(QuestEditor_Library.PawnModWorker cqfReceiver, ref float y, Rect inRect, float x, string label, Color? color, Action<Color> apply, Action clear)
        {
            Rect rect = new Rect(x, y, inRect.width - x - 20f, 30f);
            if (cqfReceiver.DrawTextButton(rect, label))
            {
                cqfReceiver.OpenColorDialog(label, color ?? Color.white, apply, clear);
            }

            if (color != null)
            {
                cqfReceiver.DrawColorSwatch(new Rect(rect.xMax - 32f, rect.y + 3f, 24f, 24f), color.Value);
            }

            cqfReceiver.EndRow(ref y);
        }

        public static void OpenColorDialog_5(QuestEditor_Library.PawnModWorker cqfReceiver, string label, Color color, Action<Color> apply, Action clear = null)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_PawnEditor_ColorLibrary".Translate(), () => Find.WindowStack.Add(new Dialog_ChooseColor(label, color, DefDatabase<ColorDef>.AllDefsListForReading.Select(def => def.color).ToList(), apply))),
                new FloatMenuOption("CQF_PawnEditor_HexColor".Translate(), () => Find.WindowStack.Add(new Dialog_RGB(color, apply)))
            };
            if (clear != null)
            {
                options.Add(new FloatMenuOption("CQF_PawnEditor_UseDefaultSkinColor".Translate(), clear));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
