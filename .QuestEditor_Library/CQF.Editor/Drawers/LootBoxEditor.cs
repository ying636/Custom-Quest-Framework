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
    public static class LootBoxEditor
    {
        public static void DrawTab_0(QuestEditor_Library.LootBox cqfReceiver)
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect outRect = new Rect(8f, 18f, Mathf.Max(80f, Mathf.Min(536f, CQFUIScope.ContentWidth - 16f)), Mathf.Max(40f, Mathf.Min(584f, CQFUIScope.ContentHeight - 26f)));
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, cqfReceiver.height));
            Widgets.BeginScrollView(outRect, ref cqfReceiver.scrollPos, viewRect);
            using CQFUIScope contentScope = new CQFUIScope(viewRect.width);
            float y = 8f;
            cqfReceiver.DrawSectionHeader(ref y, viewRect.width, "LootBox".Translate());
            CQFEditorTools.DrawLabelAndText_Line(y, "LootBoxName".Translate(), ref cqfReceiver.lootBoxName, 16f, Mathf.Max(40f, viewRect.width - 262f));
            Rect rectCP = new Rect(viewRect.width - 70f, y, 25f, 25f);
            if (CQFUIStyle.ButtonImage(rectCP, TexButton.Copy))
            {
                cqfReceiver.CopyData();
            }

            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            rectCP.x += 30f;
            if (CQFUIStyle.ButtonImage(rectCP, TexButton.Paste))
            {
                cqfReceiver.PasteData();
            }

            TooltipHandler.TipRegion(rectCP, "Paste".Translate());
            y += 38f;
            Rect saveRect = cqfReceiver.DrawSectionHeader(ref y, viewRect.width, cqfReceiver.useLootDef ? "CQF_UseLootDataDef".Translate() : "CQF_CustomLootData".Translate(), !cqfReceiver.useLootDef, !cqfReceiver.useLootDef);
            if (cqfReceiver.useLootDef)
            {
                if (CQFUIStyle.ButtonText(new Rect(16f, y, viewRect.width - 32f, 28f), "LootDef".Translate(cqfReceiver.lootDef?.defName), false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<LootDataDef>.AllDefsListForReading, d => cqfReceiver.lootDef = d, d => d.defName);
                }

                y += 38f;
            }
            else
            {
                if (CQFUIStyle.ButtonImage(saveRect, CQFEditorTools.icon_Save))
                {
                    LongEventHandler.QueueLongEvent(() =>
                    {
                        LootDataDef def = new LootDataDef();
                        def.defName = cqfReceiver.lootBoxName;
                        def.loots = cqfReceiver.loots;
                        DefDatabase<LootDataDef>.Add(def);
                        string path = Path.Combine(CQFContentPaths.Quests, "Data", cqfReceiver.lootBoxName + ".xml");
                        XElement defs = new XElement("Defs");
                        XElement defXml = new XElement("QuestEditor_Library.LootDataDef");
                        XElement lootsXml = new XElement("loots");
                        cqfReceiver.loots.ForEach(l => lootsXml.Add(l.SaveToXElement("li")));
                        defXml.Add(new XElement("defName", cqfReceiver.lootBoxName));
                        defXml.Add(lootsXml);
                        defs.Add(defXml);
                        defs.Save(path);
                        Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                    }, "SavingAsDef".Translate(), true, e => Log.Message(e.Message));
                }

                TooltipHandler.TipRegion(saveRect, "SaveAsDef".Translate());
                float initY = y;
                Rect rectData = new Rect(20f, y + 3f, viewRect.width - 40f, 28f);
                foreach (LootData data in cqfReceiver.loots)
                {
                    if (CQFUIStyle.ButtonText(rectData, data.dataName + "  " + data.chance * 100f + "%", false))
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(data));
                    }

                    TooltipHandler.TipRegion(rectData, "CQF_ClickToEdit".Translate());
                    y += 32f;
                    rectData.y += 32f;
                }

                if (!cqfReceiver.loots.Any())
                {
                    Widgets.Label(new Rect(20f, y + 4f, viewRect.width - 40f, 25f), "CQF_NoLootData".Translate().Colorize(CQFUIStyle.Muted));
                    y += 32f;
                }

                CQFUIStyle.DrawBox(new Rect(10f, initY, viewRect.width - 20f, y - initY), 1, QuestEditor_Dialog.blueTex);
                y += 10f;
                float buttonWidth = Mathf.Min(132f, (viewRect.width - 36f) / 3f);
                if (CQFUIStyle.ButtonText(new Rect(10f, y, buttonWidth, 32f), "AddNewLootData".Translate()))
                {
                    cqfReceiver.loots.Add(new LootData());
                }

                if (CQFUIStyle.ButtonText(new Rect(18f + buttonWidth, y, buttonWidth, 32f), "Paste".Translate()) && CQFEditorTools.lootData != null)
                {
                    cqfReceiver.loots.Add(CQFEditorTools.lootData.Copy());
                }

                if (CQFUIStyle.ButtonText(new Rect(26f + buttonWidth * 2f, y, buttonWidth, 32f), "DeleteLootData".Translate()) && cqfReceiver.loots.Any())
                {
                    CQFEditorTools.DrawFloatMenu(cqfReceiver.loots, (x) => cqfReceiver.loots.Remove(x), (x) => x.dataName);
                }

                y += 44f;
            }

            cqfReceiver.DrawSectionHeader(ref y, viewRect.width, "CQF_LootSettings".Translate());
            CQFEditorTools.DrawLabelAndText_Line(y, "JobReport".Translate(), ref cqfReceiver.openReport, 16f, 220f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "TickToOpenLoot".Translate(), ref cqfReceiver.tickToOpen, ref cqfReceiver.buffer, 16f, 220f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f, y, viewRect.width - 32f, 25f), "DestroyAfterOpening".Translate(), ref cqfReceiver.destroyAfterOpening);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f, y, viewRect.width - 32f, 25f), "OpenWhenDestroyed".Translate(), ref cqfReceiver.openWhenDestroyed);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f, y, viewRect.width - 32f, 25f), "UseLootDef".Translate(), ref cqfReceiver.useLootDef);
            y += 30f;
            cqfReceiver.height = y + 15f;
            Widgets.EndScrollView();
        }

        public static void PasteData_1(QuestEditor_Library.LootBox cqfReceiver)
        {
            cqfReceiver.lootBoxName = CQFEditorTools.lootBoxName;
            cqfReceiver.tickToOpen = CQFEditorTools.tickToOpen;
            cqfReceiver.destroyAfterOpening = CQFEditorTools.destroyAfterOpening;
            cqfReceiver.openReport = CQFEditorTools.openReport;
            cqfReceiver.loots = new List<LootData>();
            CQFEditorTools.loots.ListFullCopy().ForEach(l => cqfReceiver.loots.Add(l.Copy()));
            cqfReceiver.buffer = CQFEditorTools.buffer;
            cqfReceiver.useLootDef = CQFEditorTools.useLootDef;
            cqfReceiver.lootDef = CQFEditorTools.lootDef;
            cqfReceiver.openWhenDestroyed = CQFEditorTools.openWhenDestroyed;
        }

        public static void CopyData_2(QuestEditor_Library.LootBox cqfReceiver)
        {
            CQFEditorTools.lootBoxName = cqfReceiver.lootBoxName;
            CQFEditorTools.tickToOpen = cqfReceiver.tickToOpen;
            CQFEditorTools.destroyAfterOpening = cqfReceiver.destroyAfterOpening;
            CQFEditorTools.openReport = cqfReceiver.openReport;
            CQFEditorTools.buffer = cqfReceiver.buffer;
            CQFEditorTools.loots = cqfReceiver.loots.ListFullCopy();
            CQFEditorTools.useLootDef = cqfReceiver.useLootDef;
            CQFEditorTools.lootDef = cqfReceiver.lootDef;
            CQFEditorTools.openWhenDestroyed = cqfReceiver.openWhenDestroyed;
        }

        public static Rect DrawSectionHeader_3(QuestEditor_Library.LootBox cqfReceiver, ref float y, float width, string label, bool drawSaveButton = false, bool skipLine = false)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(10f, y, width - (drawSaveButton ? 64f : 20f), 30f), label.Colorize(CQFUIStyle.Accent));
            Rect saveRect = Rect.zero;
            if (drawSaveButton)
            {
                saveRect = new Rect(width - 44f, y + 2f, 25f, 25f);
            }

            Text.Font = GameFont.Small;
            y += 32f;
            if (!skipLine)
            {
                Widgets.DrawLine(new Vector2(10f, y), new Vector2(width - 20f, y), CQFUIStyle.Accent, 1f);
                y += 10f;
            }
            else
            {
                y += 3f;
            }

            return saveRect;
        }
    }
}
