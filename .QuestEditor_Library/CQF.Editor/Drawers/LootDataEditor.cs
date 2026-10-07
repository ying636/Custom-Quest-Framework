using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;
using System.Xml.Linq;
using System.Xml;

namespace QuestEditor_Library
{
    public static class LootDataEditor
    {
        public static void Draw_0(QuestEditor_Library.LootData cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            float width = inRect.width - 35f - x;
            cqfReceiver.DrawHeader(ref y, x + 10f, width - 10f);
            cqfReceiver.DrawBasicSettings(ref y, x, width);
            cqfReceiver.DrawThingList(ref y, inRect, x, width);
            cqfReceiver.DrawCategoryList(ref y, inRect, x, width);
            cqfReceiver.DrawSpecialThingList(ref y, inRect, x, width);
            cqfReceiver.DrawPawnList(ref y, x, width);
            CQFEditorTools.DrawLabelAndText_Line(y, "LootChance".Translate(), ref cqfReceiver.chance, ref cqfReceiver.buffer, 16f + x);
            y += 30f;
        }

        public static void DrawHeader_1(QuestEditor_Library.LootData cqfReceiver, ref float y, float x, float width)
        {
            Widgets.DrawHighlight(new Rect(x - 4f, y + 4f, width + 8f, 32f));
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y + 7f, width - 75f, 30f), cqfReceiver.dataName.Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            Rect button = new Rect(x + width - 60f, y + 7f, 25f, 25f);
            if (CQFUIStyle.ButtonImage(button, TexButton.Rename))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE(name => cqfReceiver.dataName = name));
            }

            TooltipHandler.TipRegion(button, "Rename".Translate());
            button.x += 30f;
            if (CQFUIStyle.ButtonImage(button, TexButton.Copy))
            {
                CQFEditorTools.lootData = cqfReceiver.Copy();
            }

            TooltipHandler.TipRegion(button, "Copy".Translate());
            y += 48f;
        }

        public static void DrawBasicSettings_2(QuestEditor_Library.LootData cqfReceiver, ref float y, float x, float width)
        {
            CQFEditorTools.DrawFieldAndText(ref y, "MessageAfterOpening".Translate(), ref cqfReceiver.message, x + 8f, 400f);
            y += 40f;
        }

        public static void DrawThingList_3(QuestEditor_Library.LootData cqfReceiver, ref float y, Rect inRect, float x, float width)
        {
            cqfReceiver.DrawListHeader(ref y, x, width, "LootThings".Translate(), () => CQFThingData.OpenLootThingSelectWindow(d => cqfReceiver.things.Add(new CQFThingDefCount { thing = d })), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.things, t => cqfReceiver.things.Remove(t), t => t.thing.label + "x" + t.count));
            float initY = y;
            foreach (CQFThingDefCount thing in cqfReceiver.things)
            {
                float itemY = y;
                thing.Draw(ref y, inRect, x + 10f);
                y += 4f;
                cqfReceiver.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }

            if (!cqfReceiver.things.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 12f, width - 24f, "CQF_NoLootThings".Translate());
            }

            y += 10f;
        }

        public static void DrawCategoryList_4(QuestEditor_Library.LootData cqfReceiver, ref float y, Rect inRect, float x, float width)
        {
            cqfReceiver.DrawListHeader(ref y, x, width, "LootCategorys".Translate(), () => CQFEditorTools.DrawFloatMenu<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll(t2 => t2.defName != "Corpses" && !t2.Parents.Contains(ThingCategoryDefOf.Corpses) && t2 != ThingCategoryDefOf.Animals), t2 => cqfReceiver.categorys.Add(new CQFThingCategoryCount() { category = t2 }), t2 => t2.label), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.categorys, t => cqfReceiver.categorys.Remove(t), t => t.category.label + "x" + t.count));
            foreach (CQFThingCategoryCount cetegory in cqfReceiver.categorys)
            {
                float itemY = y;
                cetegory.Draw(ref y, inRect, x + 10f);
                y += 4f;
                cqfReceiver.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }

            if (!cqfReceiver.categorys.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 12f, width - 24f, "CQF_NoLootCategories".Translate());
            }

            y += 10f;
        }

        public static void DrawSpecialThingList_5(QuestEditor_Library.LootData cqfReceiver, ref float y, Rect inRect, float x, float width)
        {
            cqfReceiver.DrawListHeader(ref y, x, width, "SpecialThingData".Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(CQFThingData).AllSubclassesNonAbstract().FindAll(t => t != typeof(CQFThingDefCount) && t != typeof(CQFThingCategoryCount)), t2 => cqfReceiver.specialThingDatas.Add((CQFThingData)Activator.CreateInstance(t2)), t2 => t2.Name.Translate()), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.specialThingDatas, t => cqfReceiver.specialThingDatas.Remove(t), t => t.ToString()));
            foreach (CQFThingData data in cqfReceiver.specialThingDatas)
            {
                float itemY = y;
                data.Draw(ref y, inRect, x + 10f);
                y += 4f;
                cqfReceiver.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }

            if (!cqfReceiver.specialThingDatas.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 12f, width - 24f, "CQF_NoSpecialThingData".Translate());
            }

            y += 10f;
        }

        public static void DrawPawnList_6(QuestEditor_Library.LootData cqfReceiver, ref float y, float x, float width)
        {
            cqfReceiver.DrawListHeader(ref y, x, width, "LootPawn".Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(PawnSpawnData).AllSubclassesNonAbstract(), t => cqfReceiver.pawnDatas.Add((PawnSpawnData)Activator.CreateInstance(t)), t => t.Name.Translate()), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.pawnDatas, t => cqfReceiver.pawnDatas.Remove(t), t => t.dataName));
            foreach (PawnSpawnData pawnData in cqfReceiver.pawnDatas)
            {
                float itemY = y;
                Rect rectData = new Rect(x + 16f, y + 3f, width - 32f, 25f);
                if (CQFUIStyle.ButtonText(rectData, pawnData.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(pawnData));
                }

                TooltipHandler.TipRegion(rectData, "CQF_ClickToEdit".Translate());
                y += 30f;
                cqfReceiver.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }

            if (!cqfReceiver.pawnDatas.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 12f, width - 24f, "CQF_NoLootPawns".Translate());
            }

            y += 10f;
        }

        public static void DrawListHeader_7(QuestEditor_Library.LootData cqfReceiver, ref float y, float x, float width, string label, Action addAction, Action removeAction)
        {
            Widgets.DrawHighlight(new Rect(x + 4f, y - 2f, width - 8f, 32f));
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 84f, 25f), label.Colorize(CQFUIStyle.Accent));
            Rect button = new Rect(x + width - 66f, y + 2f, 25f, 25f);
            if (CQFUIStyle.ButtonImage(button, TexButton.Plus))
            {
                addAction();
            }

            TooltipHandler.TipRegion(button, "Add".Translate());
            button.x += 30f;
            if (CQFUIStyle.ButtonImage(button, TexButton.Delete))
            {
                removeAction();
            }

            TooltipHandler.TipRegion(button, "Remove".Translate());
            y += 38f;
        }

        public static void DrawEmptyState_8(QuestEditor_Library.LootData cqfReceiver, ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(CQFUIStyle.Muted));
            y += 32f;
        }
    }
}
