using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomMapStep_BackgroundEffectsEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomMapStep_BackgroundEffects cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            cqfReceiver.backgroundEffects ??= new List<CustomMapBackgroundEffectDef>();
            float width = inRect.width - x - 12f;
            Widgets.Label(new Rect(x, y, width - 72f, 30f), "CustomMapStep_BackgroundEffects".Translate().Colorize(CQFUIStyle.Accent));
            Rect addRect = new Rect(x + width - 64f, y, 28f, 28f);
            if (CQFUIStyle.ButtonImage(addRect, TexButton.Plus))
            {
                Find.WindowStack.Add(new Dialog_Select<CustomMapBackgroundEffectDef>(new TextSelectDrawer<CustomMapBackgroundEffectDef>(DefDatabase<CustomMapBackgroundEffectDef>.AllDefsListForReading, def => def.LabelCap, def => cqfReceiver.backgroundEffects.Add(def), null, def => def.description, null, null, null, null), "CQF_MapBackgroundSelectDynamicEffect".Translate()));
            }

            TooltipHandler.TipRegion(addRect, "Add".Translate());
            Rect removeRect = new Rect(x + width - 28f, y, 28f, 28f);
            if (CQFUIStyle.ButtonImage(removeRect, TexButton.Delete))
            {
                CQFEditorTools.DrawFloatMenu(cqfReceiver.backgroundEffects.Where(effect => effect != null).ToList(), effect => cqfReceiver.backgroundEffects.Remove(effect), effect => effect.LabelCap);
            }

            TooltipHandler.TipRegion(removeRect, "Remove".Translate());
            y += 35f;
            if (!cqfReceiver.backgroundEffects.Any(effect => effect != null))
            {
                Widgets.Label(new Rect(x + 8f, y, width - 16f, 25f), "CQF_None".Translate());
                y += 30f;
            }

            foreach (CustomMapBackgroundEffectDef effect in cqfReceiver.backgroundEffects.Where(effect => effect != null))
            {
                Rect rowRect = new Rect(x, y, width, 30f);
                Widgets.DrawHighlightIfMouseover(rowRect);
                Widgets.Label(rowRect.ContractedBy(8f, 3f), effect.LabelCap);
                TooltipHandler.TipRegion(rowRect, effect.description);
                y += 32f;
            }
        }
    }
}
