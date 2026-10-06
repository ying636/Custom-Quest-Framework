using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class QuestBookObjective_ThingTargetEditor
    {
        public static void DrawSpecial_0(QuestEditor_Library.QuestBookObjective_ThingTarget cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawDetectionSection(ref y, inRect, (Rect card, ref float rowY) =>
            {
                cqfReceiver.DrawRowLabel(card, rowY, "CQF_QuestBook_TargetThing");
                string label = cqfReceiver.TargetThingDef == null ? "CQF_QuestBook_None".Translate().ToString() : cqfReceiver.TargetThingDef.LabelCap;
                Rect button = new Rect(card.x + 184f, rowY, card.width - 198f, 28f);
                if (Widgets.ButtonText(button, label, false, true))
                {
                    List<ThingDef> selectableDefs = cqfReceiver.GetThingTargets().Where(def => def != null && QuestBookTextureEntry.GetThingTexturePath(def) != null).OrderBy(def => def.label).ToList();
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(selectableDefs, def => ContentFinder<Texture2D>.Get(QuestBookTextureEntry.GetThingTexturePath(def), false), def => def.LabelCap, def =>
                    {
                        cqfReceiver.TargetThingDef = def;
                        if (!cqfReceiver.iconManuallySelected)
                        {
                            cqfReceiver.iconPath = QuestBookTextureEntry.GetThingTexturePath(def);
                        }
                    }), "CQF_QuestBook_TargetThing".Translate()));
                }

                rowY += 36f;
                cqfReceiver.DrawTargetCountField(card, ref rowY);
            });
        }
    }
}
