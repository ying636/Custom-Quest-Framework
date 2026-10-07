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
    public static class LordDataEditor
    {
        public static void Draw_0(QuestEditor_Library.LordData cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawLabelAndText_Line(y, "LordName".Translate(), ref cqfReceiver.name, x, Mathf.Max(80f, inRect.width - x - 180f));
            y += 30f;
            cqfReceiver.Data.Draw(ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "MapDataFaction".Translate(), ref cqfReceiver.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => cqfReceiver.faction = f.defName, (f) => f.label, new List<FloatMenuOption>() { new FloatMenuOption("RandomHostile".Translate(), () => cqfReceiver.faction = "RandomHostile"), new FloatMenuOption("RandomAlly".Translate(), () => cqfReceiver.faction = "RandomAlly"), new FloatMenuOption("RandomNeutral".Translate(), () => cqfReceiver.faction = "RandomNeutral"), new FloatMenuOption("PawnDataMapFaction".Translate(), () => cqfReceiver.faction = "MapFaction") }), x, Mathf.Max(80f, inRect.width - x - 180f));
            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow_UseIcon(ref y, x, cqfReceiver.actions, inRect, "ActionsAfterGeneration".Translate(), a => a.GetType().Name.Translate());
        }
    }
}
