using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.PathFinderJob;

namespace QuestEditor_Library
{
    public static class ZoneCondition_FactionMemeEditor
    {
        public static void Draw_0(QuestEditor_Library.ZoneCondition_FactionMeme cqfReceiver, ref float y, Rect inRect, float x)
        {
            ZoneConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "PawnDataFaction".Translate(), ref cqfReceiver.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => cqfReceiver.faction = f.defName, (f) => f.label, new List<FloatMenuOption>() { new FloatMenuOption("RandomHostile".Translate(), () => cqfReceiver.faction = "RandomHostile"), new FloatMenuOption("RandomAlly".Translate(), () => cqfReceiver.faction = "RandomAlly"), new FloatMenuOption("RandomNeutral".Translate(), () => cqfReceiver.faction = "RandomNeutral"), new FloatMenuOption("PawnDataMapFaction".Translate(), () => cqfReceiver.faction = "MapFaction") }), 20f + x, 120f);
            y += 30f;
            if (Widgets.ButtonText(new Rect(x, y, 350f, 25f), "RequiredMeme".Translate(cqfReceiver.meme?.label), false))
            {
                CQFEditorTools.DrawFloatMenu<MemeDef>(DefDatabase<MemeDef>.AllDefs.ToList(), (f) => cqfReceiver.meme = f, (f) => f.label);
            }

            y += 30f;
        }
    }
}
