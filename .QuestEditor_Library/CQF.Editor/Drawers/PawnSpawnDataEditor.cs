using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class PawnSpawnDataEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnSpawnData cqfReceiver, ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(16f + x, y + 10f, 500f, 45f);
            cqfReceiver.DrawName(ref y, x, rect);
            cqfReceiver.DrawKind(x, ref y);
            CQFEditorTools.DrawSelectableText(y, "PawnDataFaction".Translate(), ref cqfReceiver.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => cqfReceiver.faction = f.defName, (f) => f.label, new List<FloatMenuOption>() { new FloatMenuOption("RandomHostile".Translate(), () => cqfReceiver.faction = "RandomHostile"), new FloatMenuOption("RandomAlly".Translate(), () => cqfReceiver.faction = "RandomAlly"), new FloatMenuOption("RandomNeutral".Translate(), () => cqfReceiver.faction = "RandomNeutral"), new FloatMenuOption("PawnDataMapFaction".Translate(), () => cqfReceiver.faction = "MapFaction") }), 20f + x, 120f);
            TooltipHandler.TipRegion(new Rect(x + 20f, y, 340f, 25f), "PawnDataFactionTip".Translate());
            y += 30f;
            TooltipHandler.TipRegion(rect, "Copy".Translate());
            Rect spawmType = new Rect(20f + x, y, 420f, 25f);
            if (Widgets.ButtonText(spawmType, "SpawnType".Translate(cqfReceiver.spawnType.ToString().Translate()), false))
            {
                CQFEditorTools.DrawFloatMenu<SpawnType>(new List<SpawnType>() { SpawnType.BuildingDamaged, SpawnType.BuildingTick, SpawnType.MapGeneration, SpawnType.BuildingDestroyed }, (t) => cqfReceiver.spawnType = t, (t) => t.ToString().Translate());
            }

            if (cqfReceiver.spawnType == SpawnType.BuildingTick)
            {
                TooltipHandler.TipRegion(spawmType, "SpawnTypeTip_BuildingTick".Translate());
                y += 30f;
                string text_Time = "TimeToSpawn".Translate();
                Widgets.Label(new Rect(20f + x, y, 150f, 25f), text_Time);
                Widgets.TextFieldNumeric<int>(new Rect(Text.CalcSize(text_Time).x + x + 25f, y, 150f, 25f), ref cqfReceiver.timeToSpawn, ref cqfReceiver.buffer_time);
            }

            y += 30f;
            string text_Spawn = "SpawnMessage".Translate();
            Widgets.Label(new Rect(20f + x, y, 150f, 25f), text_Spawn);
            cqfReceiver.spawnMessage = Widgets.TextField(new Rect(Text.CalcSize(text_Spawn).x + x + 25f, y, 300f, 25f), cqfReceiver.spawnMessage);
            y += 30f;
            CQFEditorTools.DrawIntRange(ref y, "QE_Count".Translate(), ref cqfReceiver.count, ref cqfReceiver.buffer, ref cqfReceiver.bufferMax, x + 20f);
            Rect enable = new Rect(20f + x, y, 150f, 25f);
            Widgets.CheckboxLabeled(enable, "EnableLord".Translate(), ref cqfReceiver.enableLord);
            TooltipHandler.TipRegion(enable, new TipSignal("LordAndFactionTip".Translate()));
            y += 30f;
            if (cqfReceiver.enableLord)
            {
                Rect rectDuty = new Rect(20f + x, y, 250f, 25f);
                if (Widgets.ButtonText(rectDuty, "DutyType".Translate(CQFEditorTools.DutyLabel(cqfReceiver.duty)), false))
                {
                    CQFEditorTools.OpenDutySelect(d => cqfReceiver.duty = d);
                }

                if (cqfReceiver.duty?.description != null && cqfReceiver.duty.description != "")
                {
                    TooltipHandler.TipRegion(rectDuty, cqfReceiver.duty.description);
                }

                if (cqfReceiver.duty == QEDefOf.QE_Duty_Guard && Widgets.ButtonText(new Rect(180f + x, y, 200f, 25f), "CurRoute".Translate(cqfReceiver.routeName), false) && Find.CurrentMap != null && Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Any())
                {
                    CQFEditorTools.DrawFloatMenu<string>(Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Keys.ToList(), (r) => cqfReceiver.routeName = r, (r) => r);
                }

                y += 30f;
                if (cqfReceiver.duty == QEDefOf.QE_Duty_Waiter)
                {
                    CQFEditorTools.DrawButtonAndText(ref y, "PawnRotation".Translate(cqfReceiver.rotation.ToStringHuman()), "SelectRotation".Translate(), () => CQFEditorTools.DrawFloatMenu<Rot4>(new List<Rot4>() { Rot4.East, Rot4.West, Rot4.North, Rot4.South }, (r) => cqfReceiver.rotation = r, (r) => r.ToStringHuman()), 20f + x);
                }

                Rect rect2 = new Rect(20f + x, y, 150f, 25f);
                CQFEditorTools.DrawSelectableText(y, "LordNameWithTarget".Translate(), ref cqfReceiver.lordDataName, () => CQFEditorTools.DrawFloatMenu(Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().Lords, l => cqfReceiver.lordDataName = l.data.name, l => l.data.name), x + 20f, 150f);
                TooltipHandler.TipRegion(rect2, "CustomLordNameTip".Translate());
                y += 30f;
            }

            CQFEditorTools.DrawButtonAndText(ref y, "DialogTree".Translate(cqfReceiver.dialogManager?.defName), "Select".Translate(), () => CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, (t) => cqfReceiver.dialogManager = t, (t) => t.defName), 20f + x);
            y += 5f;
            if (Widgets.ButtonText(new Rect(20f + x, y, 300f, 30f), "Misc".Translate(), false))
            {
                Find.WindowStack.Add(new QuestEditor_PawnDataMisc(cqfReceiver));
            }

            y += 35f;
            cqfReceiver.DrawInventory(ref y, x);
            y += 5f;
            cqfReceiver.DrawCanSaveWarning(ref y, x, inRect);
        }

        public static void DrawCanSaveWarning_1(QuestEditor_Library.PawnSpawnData cqfReceiver, ref float y, float x, Rect inRect)
        {
            if (cqfReceiver.CanSaveToMap())
            {
                return;
            }

            Rect rect = new Rect(20f + x, y, Mathf.Max(360f, inRect.width - x - 60f), 44f);
            Widgets.Label(rect, "PawnDataCannotSaveToMapWarning".Translate().Colorize(Color.red));
            TooltipHandler.TipRegion(rect, "PawnDataCannotSaveToMapWarning".Translate());
            y += 50f;
        }

        public static void DrawName_2(QuestEditor_Library.PawnSpawnData cqfReceiver, ref float y, float x, Rect nameRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(nameRect, cqfReceiver.dataName.Colorize(ColorLibrary.SkyBlue));
            nameRect.width -= 300f;
            TooltipHandler.TipRegion(nameRect, "PawnDataNameTip".Translate());
            Text.Font = GameFont.Small;
            Rect rect = new Rect(370f + x, y, 30f, 30f);
            if (Widgets.ButtonImage(rect, TexButton.Copy))
            {
                CQFEditorTools.data = cqfReceiver.Copy();
            }

            TooltipHandler.TipRegion(rect, "Copy".Translate());
            Rect tipRect = new Rect(rect.xMax + 5f, y + 2.5f, 25f, 25f);
            Widgets.ButtonImage(tipRect, CQFEditorTools.TipIcon);
            string tipKey = cqfReceiver.GetType().Name + "_Tip";
            if (tipKey.CanTranslate())
            {
                TooltipHandler.TipRegion(tipRect, tipKey.Translate());
            }

            y += 50f;
            if (Widgets.ButtonText(new Rect(16f + x, y, 150f, 25f), "Rename".Translate()))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE((name) => cqfReceiver.dataName = name));
            }

            y += 40f;
        }

        public static void DrawKind_3(QuestEditor_Library.PawnSpawnData cqfReceiver, float x, ref float y)
        {
            if (Widgets.ButtonText(new Rect(20f + x, y, 250f, 25f), "QE_PawnKind".Translate(cqfReceiver.kind?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<PawnKindDef>(new TextSelectDrawer<PawnKindDef>(DefDatabase<PawnKindDef>.AllDefs.ToList(), k => k.label, (k) => cqfReceiver.kind = k, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
        }

        public static void DrawInventory_4(QuestEditor_Library.PawnSpawnData cqfReceiver, ref float y, float x = 0f)
        {
            Widgets.Label(new Rect(16f + x, y, 150f, 25f), "InventoryThing".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, cqfReceiver.inventoryThings, t2 => t2.thing.label + "x" + t2.count, () => Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((c) => c.category == ThingCategory.Item && !c.IsCorpse), c => c.uiIcon, c => c.label, (d) => cqfReceiver.inventoryThings.Add(new CQFThingDefCount() { thing = d }), null, null, null, null, null, null, null), "Select".Translate())), 340f, 25f, 40f);
            y += 30f;
            Widgets.DrawLine(new Vector2(16f + x, y), new Vector2(465f + x, y), ColorLibrary.SkyBlue, 2.5f);
            foreach (CQFThingDefCount thing in cqfReceiver.inventoryThings)
            {
                y += 5f;
                thing.Draw(ref y, new Rect(), x);
                y += 5f;
            }

            y += 20f;
            y += 45f;
            Widgets.Label(new Rect(16f + x, y, 150f, 25f), "InventoryThingCategorys".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, cqfReceiver.inventoryCategorys, t2 => t2.category.label + "x" + t2.count, () => Find.WindowStack.Add(new Dialog_Select<ThingCategoryDef>(new TextureSelectDrawer<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll((c) => c.defName != "Corpses" && !c.Parents.Contains(ThingCategoryDefOf.Corpses) && c != ThingCategoryDefOf.Animals), c => c.icon, c => c.label, (d) => cqfReceiver.inventoryCategorys.Add(new CQFThingCategoryCount() { category = d }), null, null, null, null, null, null, null), "Select".Translate())), 340f, 25f, 40f);
            y += 30f;
            Widgets.DrawLine(new Vector2(16f + x, y), new Vector2(465f + x, y), ColorLibrary.SkyBlue, 2f);
            foreach (CQFThingCategoryCount cetegory in cqfReceiver.inventoryCategorys)
            {
                y += 5f;
                cetegory.Draw(ref y, new Rect(), x);
                y += 5f;
            }

            y += 45f;
        }
    }
}
