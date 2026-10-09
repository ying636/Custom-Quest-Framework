using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnSpawnDataEditor
    {
        public static void Draw_0(PawnSpawnData cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width);
            cqfReceiver.DrawName(ref y, x, new Rect(x, y, inRect.width - x, 36f));
            DrawSection(ref y, x, "CQF_PawnData_GenerationSettings");
            cqfReceiver.DrawKind(x, ref y);
            Rect faction = DrawField(ref y, x, "PawnDataFaction".Translate());
            cqfReceiver.faction = Widgets.TextField(new Rect(faction.x, faction.y, faction.width - 34f, faction.height), cqfReceiver.faction ?? string.Empty);
            if (CQFUIStyle.ButtonImage(new Rect(faction.xMax - 28f, faction.y + 2f, 28f, 28f), TexButton.ReorderDown, tooltip: "Select".Translate()))
                CQFEditorTools.DrawFactionFloatMenu(value => cqfReceiver.faction = value);
            TooltipHandler.TipRegion(new Rect(faction.x, faction.y, faction.width - 34f, faction.height), "PawnDataFactionTip".Translate());
            Rect spawnType = DrawField(ref y, x, "SpawnType".Translate(string.Empty));
            if (CQFUIStyle.ButtonText(spawnType, cqfReceiver.spawnType.ToString().Translate()))
                CQFEditorTools.DrawFloatMenu(new List<SpawnType> { SpawnType.BuildingDamaged, SpawnType.BuildingTick, SpawnType.MapGeneration, SpawnType.BuildingDestroyed }, value => cqfReceiver.spawnType = value, value => value.ToString().Translate());
            if (cqfReceiver.spawnType == SpawnType.BuildingTick)
            {
                TooltipHandler.TipRegion(spawnType, "SpawnTypeTip_BuildingTick".Translate());
                Widgets.TextFieldNumeric(DrawField(ref y, x, "TimeToSpawn".Translate()), ref cqfReceiver.timeToSpawn, ref cqfReceiver.buffer_time);
            }
            string sourceMessage = cqfReceiver.spawnMessage.CanTranslate() ? cqfReceiver.spawnMessage.Translate().ToString() : cqfReceiver.spawnMessage ?? string.Empty;
            string editedMessage = Widgets.TextField(DrawField(ref y, x, "SpawnMessage".Translate()), sourceMessage);
            if (editedMessage != sourceMessage) cqfReceiver.spawnMessage = editedMessage;
            Rect count = DrawField(ref y, x, "QE_Count".Translate(), 190f);
            float part = (count.width - 18f) / 2f;
            int min = cqfReceiver.count.min;
            int max = cqfReceiver.count.max;
            Widgets.TextFieldNumeric(new Rect(count.x, count.y, part, count.height), ref min, ref cqfReceiver.buffer);
            Widgets.Label(new Rect(count.x + part + 3f, count.y + 4f, 12f, 25f), "~");
            Widgets.TextFieldNumeric(new Rect(count.x + part + 18f, count.y, part, count.height), ref max, ref cqfReceiver.bufferMax);
            cqfReceiver.count = new IntRange(min, max);
            if (max < 1) DrawWarning(ref y, x, "CQF_PawnData_InvalidCount");
            DrawSection(ref y, x, "CQF_PawnData_LordAndDialog");
            Rect enable = DrawField(ref y, x, "EnableLord".Translate());
            Widgets.Checkbox(new Vector2(enable.x, enable.y + 4f), ref cqfReceiver.enableLord, 24f);
            TooltipHandler.TipRegion(new Rect(enable.x, enable.y, 28f, enable.height), "LordAndFactionTip".Translate());
            if (cqfReceiver.enableLord)
            {
                Rect duty = DrawField(ref y, x, "DutyType".Translate(string.Empty));
                if (CQFUIStyle.ButtonText(duty, CQFEditorTools.DutyLabel(cqfReceiver.duty) ?? "Select".Translate().ToString()))
                    CQFEditorTools.OpenDutySelect(value => cqfReceiver.duty = value);
                if (cqfReceiver.duty != null && !cqfReceiver.duty.description.NullOrEmpty()) TooltipHandler.TipRegion(duty, cqfReceiver.duty.description);
                if (cqfReceiver.duty == QEDefOf.QE_Duty_Guard)
                {
                    Rect route = DrawField(ref y, x, "CurRoute".Translate(string.Empty));
                    if (CQFUIStyle.ButtonText(route, cqfReceiver.routeName ?? "Select".Translate().ToString()) && Find.CurrentMap != null)
                        CQFEditorTools.DrawFloatMenu(Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Keys.ToList(), value => cqfReceiver.routeName = value, value => value);
                }
                if (cqfReceiver.duty == QEDefOf.QE_Duty_Waiter)
                {
                    Rect rotation = DrawField(ref y, x, "PawnRotation".Translate(string.Empty));
                    if (CQFUIStyle.ButtonText(rotation, cqfReceiver.rotation.ToStringHuman()))
                        CQFEditorTools.DrawFloatMenu(new List<Rot4> { Rot4.East, Rot4.West, Rot4.North, Rot4.South }, value => cqfReceiver.rotation = value, value => value.ToStringHuman());
                }
                Rect lord = DrawField(ref y, x, "LordNameWithTarget".Translate());
                cqfReceiver.lordDataName = Widgets.TextField(new Rect(lord.x, lord.y, lord.width - 34f, lord.height), cqfReceiver.lordDataName ?? string.Empty);
                if (CQFUIStyle.ButtonImage(new Rect(lord.xMax - 28f, lord.y + 2f, 28f, 28f), TexButton.ReorderDown, tooltip: "Select".Translate()) && Find.CurrentMap != null)
                    CQFEditorTools.DrawFloatMenu(Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().Lords, value => cqfReceiver.lordDataName = value.data.name, value => value.data.name);
                TooltipHandler.TipRegion(new Rect(lord.x, lord.y, lord.width - 34f, lord.height), "CustomLordNameTip".Translate());
            }
            Rect dialog = DrawField(ref y, x, "DialogTree".Translate(string.Empty));
            if (CQFUIStyle.ButtonText(dialog, cqfReceiver.dialogManager?.defName ?? "Select".Translate().ToString()))
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, value => cqfReceiver.dialogManager = value, value => value.defName);
            Rect misc = DrawField(ref y, x, string.Empty);
            if (CQFUIStyle.ButtonText(misc, "Misc".Translate())) Find.WindowStack.Add(new QuestEditor_PawnDataMisc(cqfReceiver));
            DrawSection(ref y, x, "CQF_PawnData_Inventory");
            cqfReceiver.DrawInventory(ref y, x);
            if (cqfReceiver.GetType() != typeof(PawnSpawnData)) cqfReceiver.DrawCanSaveWarning(ref y, x, inRect);
        }

        public static void DrawCanSaveWarning_1(PawnSpawnData cqfReceiver, ref float y, float x, Rect inRect)
        {
            if (cqfReceiver.CanSaveToMap()) return;
            DrawWarning(ref y, x, "PawnDataCannotSaveToMapWarning");
        }

        public static void DrawName_2(PawnSpawnData cqfReceiver, ref float y, float x, Rect nameRect)
        {
            if (ReferenceEquals(CQFEditorContext.FixedPawnHeader, cqfReceiver)) return;
            using CQFUIScope scope = new CQFUIScope();
            float right = CQFUIScope.ContentWidth - 12f;
            float renameWidth = Mathf.Min(100f, (right - x) * 0.25f);
            Rect title = new Rect(x + 12f, y, Mathf.Max(40f, right - x - renameWidth - 96f), 32f);
            Text.Font = GameFont.Medium;
            Widgets.Label(title, (cqfReceiver.dataName ?? string.Empty).Truncate(title.width).Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(title, "PawnDataNameTip".Translate());
            if (CQFUIStyle.ButtonText(new Rect(right - renameWidth - 76f, y, renameWidth, 32f), "Rename".Translate()))
                Find.WindowStack.Add(new Dialog_RenameForQE(name => cqfReceiver.dataName = name));
            if (CQFUIStyle.ButtonImage(new Rect(right - 68f, y + 2f, 28f, 28f), TexButton.Copy, tooltip: "Copy".Translate())) CQFEditorTools.data = cqfReceiver.Copy();
            string tipKey = cqfReceiver.GetType().Name + "_Tip";
            CQFUIStyle.ButtonImage(new Rect(right - 28f, y + 2f, 28f, 28f), CQFEditorTools.TipIcon, tooltip: tipKey.CanTranslate() ? tipKey.Translate().ToString() : null);
            y += 44f;
        }

        public static void DrawKind_3(PawnSpawnData cqfReceiver, float x, ref float y)
        {
            Rect kind = DrawField(ref y, x, "QE_PawnKind".Translate(string.Empty));
            if (CQFUIStyle.ButtonText(kind, cqfReceiver.kind?.label ?? "Select".Translate().ToString()))
                Find.WindowStack.Add(new Dialog_Select<PawnKindDef>(new TextSelectDrawer<PawnKindDef>(DefDatabase<PawnKindDef>.AllDefsListForReading, value => value.label, value => cqfReceiver.kind = value), "SelectPawnKind".Translate()));
            if (cqfReceiver.kind == null) DrawWarning(ref y, x, "CQF_PawnData_MissingKind");
        }

        public static void DrawInventory_4(PawnSpawnData cqfReceiver, ref float y, float x = 0f)
        {
            float right = CQFUIScope.ContentWidth - 12f;
            Widgets.Label(new Rect(x + 12f, y + 4f, Mathf.Max(40f, right - x - 92f), 28f), "InventoryThing".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, cqfReceiver.inventoryThings, value => value.thing?.label ?? value.GetType().Name, () => Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll(value => value.category == ThingCategory.Item && !value.IsCorpse), value => value.uiIcon, value => value.label, value => cqfReceiver.inventoryThings.Add(new CQFThingDefCount { thing = value })), "Select".Translate())), right - 66f, 28f, 38f);
            y += 36f;
            foreach (CQFThingDefCount item in cqfReceiver.inventoryThings) DrawInventoryItem(ref y, x, item, item.thing?.label, item.thing?.uiIcon);
            y += 10f;
            Widgets.Label(new Rect(x + 12f, y + 4f, Mathf.Max(40f, right - x - 92f), 28f), "InventoryThingCategorys".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, cqfReceiver.inventoryCategorys, value => value.category?.label ?? value.GetType().Name, () => Find.WindowStack.Add(new Dialog_Select<ThingCategoryDef>(new TextureSelectDrawer<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll(value => value.defName != "Corpses" && !value.Parents.Contains(ThingCategoryDefOf.Corpses) && value != ThingCategoryDefOf.Animals), value => value.icon, value => value.label, value => cqfReceiver.inventoryCategorys.Add(new CQFThingCategoryCount { category = value })), "Select".Translate())), right - 66f, 28f, 38f);
            y += 36f;
            foreach (CQFThingCategoryCount item in cqfReceiver.inventoryCategorys) DrawInventoryItem(ref y, x, item, item.category?.label, item.category?.icon);
            y += 10f;
        }

        internal static Rect DrawField(ref float y, float x, string label, float requestedWidth = 320f)
        {
            float available = Mathf.Max(80f, CQFUIScope.ContentWidth - x - 24f);
            float labelWidth = Mathf.Min(160f, available * 0.38f);
            Rect labelRect = new Rect(x + 12f, y, labelWidth, 32f);
            using (new CQFUIScope())
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                Widgets.Label(labelRect, label.Truncate(labelWidth));
            }
            if (Text.CalcSize(label).x > labelWidth) TooltipHandler.TipRegion(labelRect, label);
            Rect field = new Rect(labelRect.xMax + 8f, y, Mathf.Min(requestedWidth, available - labelWidth - 8f), 32f);
            y += 40f;
            return field;
        }

        private static void DrawSection(ref float y, float x, string key)
        {
            Rect header = new Rect(x + 8f, y + 4f, Mathf.Max(80f, CQFUIScope.ContentWidth - x - 20f), 32f);
            Widgets.DrawBoxSolid(header, CQFUIStyle.Header);
            CQFUIStyle.DrawBox(header);
            Widgets.Label(header.ContractedBy(8f, 3f), key.Translate().Colorize(CQFUIStyle.TextColor));
            y += 48f;
        }

        private static void DrawWarning(ref float y, float x, string key)
        {
            string text = key.Translate();
            float width = Mathf.Max(80f, CQFUIScope.ContentWidth - x - 24f);
            float height = Text.CalcHeight(text, width);
            Widgets.Label(new Rect(x + 12f, y, width, height), text.Colorize(Color.red));
            y += height + 8f;
        }

        private static void DrawInventoryItem(ref float y, float x, CQFThingData item, string label, Texture2D icon)
        {
            Rect row = new Rect(x + 12f, y, Mathf.Max(80f, CQFUIScope.ContentWidth - x - 24f), 32f);
            Widgets.DrawBoxSolid(row, CQFUIStyle.Card);
            if (icon != null) Widgets.DrawTextureFitted(new Rect(row.x + 4f, y + 4f, 24f, 24f), icon, 1f);
            Rect name = new Rect(row.x + 36f, y, Mathf.Max(20f, row.width - 170f), 32f);
            if (CQFUIStyle.ButtonText(name, label ?? "Select".Translate().ToString(), false)) Find.WindowStack.Add(new Dialog_EditIDrawable(item));
            TooltipHandler.TipRegion(name, "CQF_ClickToEdit".Translate());
            int min = item.count.min;
            int max = item.count.max;
            Widgets.TextFieldNumeric(new Rect(row.xMax - 124f, y + 2f, 50f, 28f), ref min, ref item.bufferMin);
            Widgets.Label(new Rect(row.xMax - 70f, y + 4f, 12f, 25f), "~");
            Widgets.TextFieldNumeric(new Rect(row.xMax - 52f, y + 2f, 50f, 28f), ref max, ref item.bufferMax);
            item.count = new IntRange(min, max);
            y += 38f;
        }
    }
}
