using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace QuestEditor_Library
{
    public static class QuestNode_Root_MainMapEditor
    {
        public static void Draw_0(QuestEditor_Library.QuestNode_Root_MainMap cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            y += 10f;
            CQFEditorTools.DrawSelectButton(x + 7f, ref y, "MainMapDef".Translate(cqfReceiver.mainMapDef.GetValue(QuestGen.slate)?.defName), DefDatabase<MainMapDef>.AllDefsListForReading, def => cqfReceiver.mainMapDef = def, def => def.defName);
            CQFEditorTools.DrawLabelAndText_SlateRef_Line(y, "tile".Translate(), ref cqfReceiver.tile, x + 7f, 110f);
            TooltipHandler.TipRegion(new Rect(x + 7f, y, 150f, 25f), "tile_Tip".Translate());
            y += 30f;
            CQFEditorTools.DrawSelectableText(y, "MapFaction".Translate(), ref cqfReceiver.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll(f => !f.isPlayer), f => cqfReceiver.faction = f.defName, f => f.label, new List<FloatMenuOption>() { new FloatMenuOption("RandomHostile".Translate(), () => cqfReceiver.faction = "RandomHostile"), new FloatMenuOption("RandomAlly".Translate(), () => cqfReceiver.faction = "RandomAlly"), new FloatMenuOption("RandomNeutral".Translate(), () => cqfReceiver.faction = "RandomNeutral"), }), 7f + x, 120f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "SiteIconPath".Translate(), ref cqfReceiver.siteIconPath, x + 7f, 150f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExpandingIconPath".Translate(), ref cqfReceiver.expandingIconPath, x + 7f, 150f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 7f, y, 300f, 25f), "DisdestroyBecauseOfNoColonist".Translate(), ref cqfReceiver.disdestroyBecauseOfNoColonist);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 7f, y, 300f, 25f), "BeUnreenterableWhenAllEnemiesDefeated".Translate(), ref cqfReceiver.beUnreenterableWhenAllEnemiesDefeated);
            y += 30f;
            Rect r = new Rect(x + 7f, y, 250f, 25f);
            Widgets.CheckboxLabeled(r, "replaceMapGeneration".Translate(), ref cqfReceiver.replaceMapGeneration);
            TooltipHandler.TipRegion(r, "replaceMapGeneration_Tip".Translate());
            y += 30f;
            CQFEditorTools.DrawLabelAndText_SlateRef_Line(y, "StoreAsText".Translate(), ref cqfReceiver.storeAs, x + 7f, 100f);
            y += 30f;
            CQFEditorTools.DrawIntRange(ref y, "MapDistance".Translate(), ref cqfReceiver.distance, ref cqfReceiver.buffer, ref cqfReceiver.bufferMin, x + 7f);
            List<PlanetLayerDef> planetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefsListForReading;
            CQFEditorTools.DrawSelectableField(x + 7f, ref y, "planetLayer".Translate(cqfReceiver.planetLayer == null ? null : cqfReceiver.planetLayer.ToString()), planetLayerDefs, d => cqfReceiver.planetLayer = d, d => d.label, new Vector2(120f, 25f));
            string listText = "";
            cqfReceiver.blacklist.ForEach(b => listText = b.label + "," + listText);
            Widgets.Label(new Rect(x + 7f, y, 300f, 60f), (cqfReceiver.enableBlack ? "BiomesBlackList".Translate() : "BiomesWhiteList".Translate()) + listText);
            y += 70f;
            if (CQFUIStyle.ButtonText(new Rect(x + 7f, y, 70f, 25f), "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<BiomeDef>(DefDatabase<BiomeDef>.AllDefs.ToList().FindAll(b => !cqfReceiver.blacklist.Contains(b)), b => cqfReceiver.blacklist.Add(b), b => b.label);
            }

            if (CQFUIStyle.ButtonText(new Rect(x + 70f, y, 70f, 25f), "Delete".Translate()))
            {
                CQFEditorTools.DrawFloatMenu<BiomeDef>(cqfReceiver.blacklist, b => cqfReceiver.blacklist.Remove(b), b => b.label);
            }

            y += 30f;
        }
    }
}
