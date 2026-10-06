using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Noise;

namespace QuestEditor_Library
{
    public static class ReplaceDataEditor
    {
        public static void Draw_0(QuestEditor_Library.ReplaceData cqfReceiver, ref float y, Rect inRect, float x)
        {
            float width = inRect.width - x - 12f;
            Rect nameSection = new Rect(x, y, width, 78f);
            Widgets.DrawMenuSection(nameSection);
            Widgets.Label(new Rect(x + 12f, y + 8f, width - 24f, 25f), "DataName".Translate().Colorize(ColorLibrary.PaleBlue));
            cqfReceiver.dataName = Widgets.TextField(new Rect(x + 12f, y + 39f, width - 24f, 27f), cqfReceiver.dataName);
            y = nameSection.yMax + 10f;
            cqfReceiver.DrawThingReplacementSection(ref y, x, width, "ThingReplacement".Translate(), cqfReceiver.replaceThings, cqfReceiver.OpenThingReplacementSelector);
            cqfReceiver.DrawThingReplacementSection(ref y, x, width, "StuffReplacement".Translate(), cqfReceiver.replaceStuffs, cqfReceiver.OpenStuffReplacementSelector);
            cqfReceiver.DrawTerrainReplacementSection(ref y, x, width);
        }

        public static void DrawThingReplacementSection_1(QuestEditor_Library.ReplaceData cqfReceiver, ref float y, float x, float width, string title, Dictionary<string, string> replacements, Action addAction)
        {
            float sectionHeight = 50f + Math.Max(1, replacements.Count) * 42f;
            Rect sectionRect = new Rect(x, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            cqfReceiver.DrawReplacementHeader(y, x, width, title, addAction, () => CQFEditorTools.DrawFloatMenu(replacements.ToList(), pair => replacements.Remove(pair.Key), cqfReceiver.GetThingReplacementLabel));
            float rowY = y + 42f;
            if (!replacements.Any())
            {
                Widgets.Label(new Rect(x + 14f, rowY + 5f, width - 28f, 25f), "-");
            }

            foreach (KeyValuePair<string, string> pair in replacements)
            {
                ThingDef source = DefDatabase<ThingDef>.GetNamedSilentFail(pair.Key);
                ThingDef target = DefDatabase<ThingDef>.GetNamedSilentFail(pair.Value);
                cqfReceiver.DrawReplacementRow(new Rect(x + 10f, rowY, width - 20f, 36f), source, source?.label ?? pair.Key, target, target?.label ?? pair.Value);
                rowY += 42f;
            }

            y = sectionRect.yMax + 10f;
        }

        public static void DrawTerrainReplacementSection_2(QuestEditor_Library.ReplaceData cqfReceiver, ref float y, float x, float width)
        {
            float sectionHeight = 50f + Math.Max(1, cqfReceiver.replaceTerrains.Count) * 42f;
            Rect sectionRect = new Rect(x, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            cqfReceiver.DrawReplacementHeader(y, x, width, "TerrainReplacement".Translate(), cqfReceiver.OpenTerrainReplacementSelector, () => CQFEditorTools.DrawFloatMenu(cqfReceiver.replaceTerrains.ToList(), pair => cqfReceiver.replaceTerrains.Remove(pair.Key), cqfReceiver.GetTerrainReplacementLabel));
            float rowY = y + 42f;
            if (!cqfReceiver.replaceTerrains.Any())
            {
                Widgets.Label(new Rect(x + 14f, rowY + 5f, width - 28f, 25f), "-");
            }

            foreach (KeyValuePair<string, string> pair in cqfReceiver.replaceTerrains)
            {
                TerrainDef source = DefDatabase<TerrainDef>.GetNamedSilentFail(pair.Key);
                TerrainDef target = DefDatabase<TerrainDef>.GetNamedSilentFail(pair.Value);
                cqfReceiver.DrawReplacementRow(new Rect(x + 10f, rowY, width - 20f, 36f), source, source?.label ?? pair.Key, target, target?.label ?? pair.Value);
                rowY += 42f;
            }

            y = sectionRect.yMax + 10f;
        }

        public static void DrawReplacementRow_3(QuestEditor_Library.ReplaceData cqfReceiver, Rect rect, Def source, string sourceLabel, Def target, string targetLabel)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            float sideWidth = (rect.width - 58f) / 2f;
            Rect sourceIconRect = new Rect(rect.x + 4f, rect.y + 3f, 30f, 30f);
            if (source != null)
            {
                Widgets.DefIcon(sourceIconRect, source);
            }

            Widgets.Label(new Rect(sourceIconRect.xMax + 6f, rect.y + 6f, sideWidth - 40f, 25f), sourceLabel);
            Rect arrowRect = new Rect(rect.center.x - 14f, rect.y + 4f, 28f, 28f);
            Widgets.DrawTextureFitted(arrowRect, QuestEditor_SaveMapToFile.arrowIcon, 1f);
            Rect targetIconRect = new Rect(rect.center.x + 20f, rect.y + 3f, 30f, 30f);
            if (target != null)
            {
                Widgets.DefIcon(targetIconRect, target);
            }

            Widgets.Label(new Rect(targetIconRect.xMax + 6f, rect.y + 6f, sideWidth - 40f, 25f), targetLabel);
            TooltipHandler.TipRegion(rect, sourceLabel + " -> " + targetLabel);
        }
    }
}
