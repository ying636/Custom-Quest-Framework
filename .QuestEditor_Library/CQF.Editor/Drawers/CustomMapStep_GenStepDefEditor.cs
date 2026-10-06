using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomMapStep_GenStepDefEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomMapStep_GenStepDef cqfReceiver, ref float y, Rect inRect, float x)
        {
            float width = inRect.width - x - 12f;
            Widgets.Label(new Rect(x, y, width, 30f), "CustomMapStep_GenStepDef".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 35f;
            if (Widgets.ButtonText(new Rect(x, y, width, 30f), cqfReceiver.step?.label ?? cqfReceiver.step?.defName ?? "CQF_NotSelected".Translate(), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<GenStepDef>.AllDefsListForReading, g => cqfReceiver.step = g, g => g.label ?? g.defName);
            }

            y += 35f;
        }
    }
}
