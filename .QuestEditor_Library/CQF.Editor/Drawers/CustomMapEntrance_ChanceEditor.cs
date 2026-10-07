using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomMapEntrance_ChanceEditor
    {
        public static void DrawTab_0(QuestEditor_Library.CustomMapEntrance_Chance cqfReceiver)
        {
            Rect outRect = new Rect(0f, 36f, 540f, 554f);
            float width = outRect.width - 40f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, cqfReceiver.height + 10f));
            Widgets.BeginScrollView(outRect, ref cqfReceiver.scrollPos, viewRect);
            float x = 10f;
            float y = 10f;
            cqfReceiver.DrawCopyPasteHeader(ref y, x, width);
            cqfReceiver.DrawMapPool(ref y, x, width);
            cqfReceiver.DrawTagPool(ref y, x, width);
            cqfReceiver.DrawSectionHeader(ref y, x, width, "CQF_PortalSettingsSection".Translate(), "CQF_PortalSettingsSectionTip".Translate());
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "DefaultOpened".Translate(), ref cqfReceiver.opended);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "ExitName".Translate(), ref cqfReceiver.exitName, x + 8f, 150f);
            y += 30f;
            cqfReceiver.DrawActionSection(ref y, x, width, cqfReceiver.enterActions);
            cqfReceiver.height = y + 10f;
            Widgets.EndScrollView();
        }

        public static void DrawCopyPasteHeader_1(QuestEditor_Library.CustomMapEntrance_Chance cqfReceiver, ref float y, float x, float width)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 84f, 25f), "CustomMapEntrance_Chance".Translate().Colorize(ColorLibrary.SkyBlue));
            Rect buttonRect = new Rect(x + width - 66f, y + 2f, 25f, 25f);
            if (Widgets.ButtonImage(buttonRect, TexButton.Copy))
            {
                CQFEditorTools.exitName = cqfReceiver.exitName;
                CQFEditorTools.tagWithChance = cqfReceiver.tagWithChance.ListFullCopy();
                CQFEditorTools.mapDefWithChance = cqfReceiver.mapDefWithChance.ListFullCopy();
            }

            TooltipHandler.TipRegion(buttonRect, "CQF_ChanceCopySettingsTip".Translate());
            buttonRect.x += 30f;
            if (Widgets.ButtonImage(buttonRect, TexButton.Paste))
            {
                cqfReceiver.exitName = CQFEditorTools.exitName;
                cqfReceiver.tagWithChance = CQFEditorTools.tagWithChance.ListFullCopy();
                cqfReceiver.mapDefWithChance = CQFEditorTools.mapDefWithChance.ListFullCopy();
            }

            TooltipHandler.TipRegion(buttonRect, "CQF_ChancePasteSettingsTip".Translate());
            y += 38f;
        }

        public static void DrawMapPool_2(QuestEditor_Library.CustomMapEntrance_Chance cqfReceiver, ref float y, float x, float width)
        {
            cqfReceiver.DrawSectionHeader(ref y, x, width, "CQF_ChanceMapPoolSection".Translate(), "MapDefWithChance_Tip".Translate(), () => cqfReceiver.mapDefWithChance.Add(new MapDefWithChance()), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.mapDefWithChance, item => cqfReceiver.mapDefWithChance.Remove(item), item => item.def?.label ?? "Null".Translate()), cqfReceiver.mapDefWithChance.Any());
            if (!cqfReceiver.mapDefWithChance.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_ChanceNoMapDefs".Translate());
                y += 8f;
                return;
            }

            foreach (MapDefWithChance item in cqfReceiver.mapDefWithChance)
            {
                Rect rowRect = new Rect(x + 8f, y, width - 16f, 30f);
                Widgets.DrawHighlightIfMouseover(rowRect);
                string mapLabel = item.def == null ? "Null".Translate().ToString() : item.def.label;
                Rect mapButtonRect = new Rect(rowRect.x + 4f, rowRect.y + 2f, 278f, 25f);
                if (Widgets.ButtonText(mapButtonRect, "CustomMapDef".Translate(mapLabel), false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, def => item.def = def, def => def.label);
                }

                Widgets.Label(new Rect(rowRect.x + 288f, rowRect.y + 2f, 70f, 25f), "Chance".Translate());
                Widgets.TextFieldPercent(new Rect(rowRect.x + 360f, rowRect.y + 2f, 110f, 25f), ref item.chance, ref item.buffer);
                y += 34f;
            }

            y += 8f;
        }

        public static void DrawTagPool_3(QuestEditor_Library.CustomMapEntrance_Chance cqfReceiver, ref float y, float x, float width)
        {
            cqfReceiver.DrawSectionHeader(ref y, x, width, "CQF_ChanceTagPoolSection".Translate(), "TagWithChance_Tip".Translate(), () => cqfReceiver.tagWithChance.Add(new TagWithChance()), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.tagWithChance, item => cqfReceiver.tagWithChance.Remove(item), item => item.tag), cqfReceiver.tagWithChance.Any());
            if (!cqfReceiver.tagWithChance.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_ChanceNoTags".Translate());
                y += 8f;
                return;
            }

            foreach (TagWithChance item in cqfReceiver.tagWithChance)
            {
                Rect rowRect = new Rect(x + 8f, y, width - 16f, 30f);
                Widgets.DrawHighlightIfMouseover(rowRect);
                item.tag = Widgets.TextField(new Rect(rowRect.x + 4f, rowRect.y + 2f, 278f, 25f), item.tag);
                Widgets.Label(new Rect(rowRect.x + 288f, rowRect.y + 2f, 70f, 25f), "Chance".Translate());
                Widgets.TextFieldPercent(new Rect(rowRect.x + 360f, rowRect.y + 2f, 110f, 25f), ref item.chance, ref item.buffer);
                y += 34f;
            }

            y += 8f;
        }
    }
}
