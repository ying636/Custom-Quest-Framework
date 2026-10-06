using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using RimWorld.Planet;
using Verse.Grammar;
using UnityEngine;
using System.Xml;
using System.IO;
using System.Reflection;

namespace QuestEditor_Library
{
    public static class QuestNode_Root_CustomMapEditor
    {
        public static void Draw_0(QuestEditor_Library.QuestNode_Root_CustomMap cqfReceiver, ref float y, Rect inRect, float x)
        {
            y += 10f;
            CQFEditorTools.DrawLabelAndText_SlateRef_Line(y, "tile".Translate(), ref cqfReceiver.tile, x + 7f, 110f);
            TooltipHandler.TipRegion(new Rect(x + 7f, y, 150f, 25f), "tile_Tip".Translate());
            y += 30f;
            CQFEditorTools.DrawSelectableText(y, "MapFaction".Translate(), ref cqfReceiver.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => cqfReceiver.faction = f.defName, (f) => f.label, new List<FloatMenuOption>() { new FloatMenuOption("RandomHostile".Translate(), () => cqfReceiver.faction = "RandomHostile"), new FloatMenuOption("RandomAlly".Translate(), () => cqfReceiver.faction = "RandomAlly"), new FloatMenuOption("RandomNeutral".Translate(), () => cqfReceiver.faction = "RandomNeutral"), }), 7f + x, 120f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "SiteIconPath".Translate(), ref cqfReceiver.siteIconPath, x + 7f, 150f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExpandingIconPath".Translate(), ref cqfReceiver.expandingIconPath, x + 7f, 150f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 7f, y, 300f, 25f), "DisdestroyBecauseOfNoColonist".Translate(), ref cqfReceiver.disdestroyBecauseOfNoColonist);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 7f, y, 300f, 25f), "Reenterable".Translate(), ref cqfReceiver.reenterable);
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
            Rect worldObjectDefRect = new Rect(x + 7f, y, 800f, 25f);
            CQFEditorTools.DrawSelectButton(x + 7f, ref y, "WorldObjectDefOfSite".Translate(cqfReceiver.worldObjectDef == null ? "Default".Translate().ToString() : cqfReceiver.worldObjectDef.label), DefDatabase<WorldObjectDef>.AllDefsListForReading, d => cqfReceiver.worldObjectDef = d, d => d.label, new List<FloatMenuOption>() { new FloatMenuOption("Default".Translate(), () => cqfReceiver.worldObjectDef = null) });
            TooltipHandler.TipRegion(worldObjectDefRect, "WorldObjectDefOfSite_Tip".Translate());
            List<PlanetLayerDef> PlanetLayerDef = DefDatabase<PlanetLayerDef>.AllDefsListForReading;
            CQFEditorTools.DrawSelectableField(x + 7f, ref y, "planetLayer".Translate(cqfReceiver.planetLayer == null ? null : cqfReceiver.planetLayer.ToString()), PlanetLayerDef, d => cqfReceiver.planetLayer = d, d => d.label, new Vector2(120f, 25f));
            cqfReceiver.DrawBiomeFilter(ref y, inRect, x + 7f);
            cqfReceiver.DrawWorldConditions(ref y, inRect, x + 7f);
        }

        public static void DrawBiomeFilter_1(QuestEditor_Library.QuestNode_Root_CustomMap cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawSectionHeader(ref y, inRect, x, cqfReceiver.enableBlack ? "BiomesBlackList".Translate() : "BiomesWhiteList".Translate(), () => CQFEditorTools.DrawFloatMenu<BiomeDef>(DefDatabase<BiomeDef>.AllDefs.ToList().FindAll(b => !cqfReceiver.blacklist.Contains(b)), b => cqfReceiver.blacklist.Add(b), b => b.label), () => CQFEditorTools.DrawFloatMenu<BiomeDef>(cqfReceiver.blacklist, b => cqfReceiver.blacklist.Remove(b), b => b.label));
            if (cqfReceiver.blacklist.NullOrEmpty())
            {
                Widgets.Label(new Rect(x, y, 600f, 25f), "NoBiomeFilters".Translate().Colorize(Color.gray));
                y += 30f;
                return;
            }

            foreach (BiomeDef biome in cqfReceiver.blacklist)
            {
                Widgets.Label(new Rect(x, y, 600f, 25f), (biome.label ?? biome.defName).CapitalizeFirst());
                y += 25f;
            }

            y += 5f;
        }

        public static void DrawWorldConditions_2(QuestEditor_Library.QuestNode_Root_CustomMap cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawSectionHeader(ref y, inRect, x, "WorldConditions".Translate(), () =>
            {
                List<Type> types = typeof(WorldCondition).AllSubclassesNonAbstract().ToList();
                CQFEditorTools.DrawFloatMenu(types, t => cqfReceiver.worldConditions.Add((WorldCondition)Activator.CreateInstance(t)), t => t.Name.Translate());
            }, () => CQFEditorTools.DrawFloatMenu<WorldCondition>(cqfReceiver.worldConditions, d => cqfReceiver.worldConditions.Remove(d), cqfReceiver.WorldConditionLabel));
            if (cqfReceiver.worldConditions.NullOrEmpty())
            {
                Widgets.Label(new Rect(x, y, 600f, 25f), "NoWorldConditions".Translate().Colorize(Color.gray));
                y += 30f;
                return;
            }

            foreach (WorldCondition condition in cqfReceiver.worldConditions)
            {
                if (Widgets.ButtonText(new Rect(x, y, 600f, 25f), cqfReceiver.WorldConditionLabel(condition), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(condition));
                }

                y += 30f;
            }

            y += 5f;
        }

        public static void DrawSectionHeader_3(QuestEditor_Library.QuestNode_Root_CustomMap cqfReceiver, ref float y, Rect inRect, float x, string title, Action addAction, Action removeAction)
        {
            Widgets.Label(new Rect(x, y, 400f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            Rect button = new Rect(x + 420f, y, 30f, 30f);
            if (Widgets.ButtonImage(button, TexButton.Plus))
            {
                addAction();
            }

            button.x += 40f;
            if (Widgets.ButtonImage(button, TexButton.Delete))
            {
                removeAction();
            }

            y += 30f;
        }
    }
}
