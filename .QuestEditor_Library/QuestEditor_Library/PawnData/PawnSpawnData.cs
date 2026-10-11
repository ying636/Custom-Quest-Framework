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
    public class PawnSpawnData : IExposable, ISaveable, IDrawable
    {
        public virtual PawnSpawnData Copy()
        {
            XElement x = this.SaveToXElement("PawnSpawnData");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            PawnSpawnData result = DirectXmlToObject.ObjectFromXml<PawnSpawnData>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }

        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width);
            this.DrawName(ref y, x, new Rect(x, y, inRect.width - x, 36f));
            DrawSection(ref y, x, "CQF_PawnData_GenerationSettings");
            this.DrawKind(x, ref y);
            Rect faction = DrawField(ref y, x, "PawnDataFaction".Translate());
            this.faction = Widgets.TextField(new Rect(faction.x, faction.y, faction.width - 34f, faction.height), this.faction ?? string.Empty);
            if (CQFUIStyle.ButtonImage(new Rect(faction.xMax - 28f, faction.y + 2f, 28f, 28f), TexButton.ReorderDown, tooltip: "Select".Translate()))
                CQFEditorTools.DrawFactionFloatMenu(value => this.faction = value);
            TooltipHandler.TipRegion(new Rect(faction.x, faction.y, faction.width - 34f, faction.height), "PawnDataFactionTip".Translate());
            Rect spawnType = DrawField(ref y, x, "SpawnType".Translate(string.Empty));
            if (CQFUIStyle.ButtonText(spawnType, this.spawnType.ToString().Translate()))
                CQFEditorTools.DrawFloatMenu(new List<SpawnType> { SpawnType.BuildingDamaged, SpawnType.BuildingTick, SpawnType.MapGeneration, SpawnType.BuildingDestroyed }, value => this.spawnType = value, value => value.ToString().Translate());
            if (this.spawnType == SpawnType.BuildingTick)
            {
                TooltipHandler.TipRegion(spawnType, "SpawnTypeTip_BuildingTick".Translate());
                Widgets.TextFieldNumeric(DrawField(ref y, x, "TimeToSpawn".Translate()), ref this.timeToSpawn, ref this.buffer_time);
            }

            string sourceMessage = this.spawnMessage.CanTranslate() ? this.spawnMessage.Translate().ToString() : this.spawnMessage ?? string.Empty;
            string editedMessage = Widgets.TextField(DrawField(ref y, x, "SpawnMessage".Translate()), sourceMessage);
            if (editedMessage != sourceMessage)
                this.spawnMessage = editedMessage;
            Rect count = DrawField(ref y, x, "QE_Count".Translate(), 190f);
            float part = (count.width - 18f) / 2f;
            int min = this.count.min;
            int max = this.count.max;
            Widgets.TextFieldNumeric(new Rect(count.x, count.y, part, count.height), ref min, ref this.buffer);
            Widgets.Label(new Rect(count.x + part + 3f, count.y + 4f, 12f, 25f), "~");
            Widgets.TextFieldNumeric(new Rect(count.x + part + 18f, count.y, part, count.height), ref max, ref this.bufferMax);
            this.count = new IntRange(min, max);
            if (max < 1)
                DrawWarning(ref y, x, "CQF_PawnData_InvalidCount");
            DrawSection(ref y, x, "CQF_PawnData_LordAndDialog");
            Rect enable = DrawField(ref y, x, "EnableLord".Translate());
            Widgets.Checkbox(new Vector2(enable.x, enable.y + 4f), ref this.enableLord, 24f);
            TooltipHandler.TipRegion(new Rect(enable.x, enable.y, 28f, enable.height), "LordAndFactionTip".Translate());
            if (this.enableLord)
            {
                Rect duty = DrawField(ref y, x, "DutyType".Translate(string.Empty));
                if (CQFUIStyle.ButtonText(duty, CQFEditorTools.DutyLabel(this.duty) ?? "Select".Translate().ToString()))
                    CQFEditorTools.OpenDutySelect(value => this.duty = value);
                if (this.duty != null && !this.duty.description.NullOrEmpty())
                    TooltipHandler.TipRegion(duty, this.duty.description);
                if (this.duty == QEDefOf.QE_Duty_Guard)
                {
                    Rect route = DrawField(ref y, x, "CurRoute".Translate(string.Empty));
                    if (CQFUIStyle.ButtonText(route, this.routeName ?? "Select".Translate().ToString()) && Find.CurrentMap != null)
                        CQFEditorTools.DrawFloatMenu(Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Keys.ToList(), value => this.routeName = value, value => value);
                }

                if (this.duty == QEDefOf.QE_Duty_Waiter)
                {
                    Rect rotation = DrawField(ref y, x, "PawnRotation".Translate(string.Empty));
                    if (CQFUIStyle.ButtonText(rotation, this.rotation.ToStringHuman()))
                        CQFEditorTools.DrawFloatMenu(new List<Rot4> { Rot4.East, Rot4.West, Rot4.North, Rot4.South }, value => this.rotation = value, value => value.ToStringHuman());
                }

                Rect lord = DrawField(ref y, x, "LordNameWithTarget".Translate());
                this.lordDataName = Widgets.TextField(new Rect(lord.x, lord.y, lord.width - 34f, lord.height), this.lordDataName ?? string.Empty);
                if (CQFUIStyle.ButtonImage(new Rect(lord.xMax - 28f, lord.y + 2f, 28f, 28f), TexButton.ReorderDown, tooltip: "Select".Translate()) && Find.CurrentMap != null)
                    CQFEditorTools.DrawFloatMenu(Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().Lords, value => this.lordDataName = value.data.name, value => value.data.name);
                TooltipHandler.TipRegion(new Rect(lord.x, lord.y, lord.width - 34f, lord.height), "CustomLordNameTip".Translate());
            }

            Rect dialog = DrawField(ref y, x, "DialogTree".Translate(string.Empty));
            if (CQFUIStyle.ButtonText(dialog, this.dialogManager?.defName ?? "Select".Translate().ToString()))
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, value => this.dialogManager = value, value => value.defName);
            Rect misc = DrawField(ref y, x, string.Empty);
            if (CQFUIStyle.ButtonText(misc, "Misc".Translate()))
                Find.WindowStack.Add(CQFEditorBridge.Editor.CreatePawnMiscWindow(this));
            DrawSection(ref y, x, "CQF_PawnData_Inventory");
            this.DrawInventory(ref y, x);
            if (this.GetType() != typeof(PawnSpawnData))
                this.DrawCanSaveWarning(ref y, x, inRect);
        }

        public virtual bool CanSaveToMap()
        {
            return this.kind != null && this.count.max >= 1;
        }

        protected internal void DrawCanSaveWarning(ref float y, float x, Rect inRect)
        {
            if (this.CanSaveToMap())
                return;
            DrawWarning(ref y, x, "PawnDataCannotSaveToMapWarning");
        }

        public void DrawName(ref float y, float x, Rect nameRect)
        {
            if (ReferenceEquals(CQFEditorContext.FixedPawnHeader, this))
                return;
            using CQFUIScope scope = new CQFUIScope();
            float right = CQFUIScope.ContentWidth - 12f;
            float renameWidth = Mathf.Min(100f, (right - x) * 0.25f);
            Rect title = new Rect(x + 12f, y, Mathf.Max(40f, right - x - renameWidth - 96f), 32f);
            Text.Font = GameFont.Medium;
            Widgets.Label(title, (this.dataName ?? string.Empty).Truncate(title.width).Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(title, "PawnDataNameTip".Translate());
            if (CQFUIStyle.ButtonText(new Rect(right - renameWidth - 76f, y, renameWidth, 32f), "Rename".Translate()))
                Find.WindowStack.Add(new Dialog_RenameForQE(name => this.dataName = name));
            if (CQFUIStyle.ButtonImage(new Rect(right - 68f, y + 2f, 28f, 28f), TexButton.Copy, tooltip: "Copy".Translate()))
                CQFEditorTools.data = this.Copy();
            string tipKey = this.GetType().Name + "_Tip";
            CQFUIStyle.ButtonImage(new Rect(right - 28f, y + 2f, 28f, 28f), CQFEditorTools.TipIcon, tooltip: tipKey.CanTranslate() ? tipKey.Translate().ToString() : null);
            y += 44f;
        }

        public virtual void DrawKind(float x, ref float y)
        {
            Rect kind = DrawField(ref y, x, "QE_PawnKind".Translate(string.Empty));
            if (CQFUIStyle.ButtonText(kind, this.kind?.label ?? "Select".Translate().ToString()))
                Find.WindowStack.Add(new Dialog_Select<PawnKindDef>(new TextSelectDrawer<PawnKindDef>(DefDatabase<PawnKindDef>.AllDefsListForReading, value => value.label, value => this.kind = value), "SelectPawnKind".Translate()));
            if (this.kind == null)
                DrawWarning(ref y, x, "CQF_PawnData_MissingKind");
        }

        public void DrawInventory(ref float y, float x = 0f)
        {
            float right = CQFUIScope.ContentWidth - 12f;
            Widgets.Label(new Rect(x + 12f, y + 4f, Mathf.Max(40f, right - x - 92f), 28f), "InventoryThing".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, this.inventoryThings, value => value.thing?.label ?? value.GetType().Name, () => Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll(value => value.category == ThingCategory.Item && !value.IsCorpse), value => value.uiIcon, value => value.label, value => this.inventoryThings.Add(new CQFThingDefCount { thing = value })), "Select".Translate())), right - 66f, 28f, 38f);
            y += 36f;
            foreach (CQFThingDefCount item in this.inventoryThings)
                DrawInventoryItem(ref y, x, item, item.thing?.label, item.thing?.uiIcon);
            y += 10f;
            Widgets.Label(new Rect(x + 12f, y + 4f, Mathf.Max(40f, right - x - 92f), 28f), "InventoryThingCategorys".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, this.inventoryCategorys, value => value.category?.label ?? value.GetType().Name, () => Find.WindowStack.Add(new Dialog_Select<ThingCategoryDef>(new TextureSelectDrawer<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll(value => value.defName != "Corpses" && !value.Parents.Contains(ThingCategoryDefOf.Corpses) && value != ThingCategoryDefOf.Animals), value => value.icon, value => value.label, value => this.inventoryCategorys.Add(new CQFThingCategoryCount { category = value })), "Select".Translate())), right - 66f, 28f, 38f);
            y += 36f;
            foreach (CQFThingCategoryCount item in this.inventoryCategorys)
                DrawInventoryItem(ref y, x, item, item.category?.label, item.category?.icon);
            y += 10f;
        }

        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            if (this.GetType() != typeof(PawnSpawnData))
            {
                result.SetAttributeValue("Class", this.GetType().FullName);
            }

            if (this.dataName != "undefined")
            {
                result.Add(new XElement("dataName", this.dataName));
            }

            if (this.kind != null)
            {
                result.Add(new XElement("kind", this.kind.defName));
            }

            if (this.enableLord)
            {
                result.Add(new XElement("enableLord", this.enableLord));
                if (!this.lordDataName.NullOrEmpty())
                {
                    result.Add(new XElement("lordDataName", this.lordDataName));
                }
            }

            if (!this.roundTrip)
            {
                result.Add(new XElement("roundTrip", this.roundTrip));
            }

            result.Add(new XElement("count", this.count));
            if (this.spawnType == SpawnType.BuildingTick)
            {
                result.Add(new XElement("timeToSpawn", this.timeToSpawn));
            }

            if (this.faction != null)
            {
                result.Add(new XElement("faction", this.faction));
            }

            if (this.routeName != null)
            {
                result.Add(new XElement("routeName", this.routeName));
            }

            if (this.generationChance != 1f)
            {
                result.Add(new XElement("generationChance", this.generationChance));
            }

            if (this.dialogManager != null)
            {
                result.Add(new XElement("dialogManager", this.dialogManager.defName));
            }

            if (this.enableLord)
            {
                result.Add(new XElement("duty", this.duty?.defName));
            }

            result.Add(new XElement("spawnType", this.spawnType));
            if (this.rotation != Rot4.South)
            {
                result.Add(new XElement("rotation", this.rotation.ToStringWord()));
            }

            if (this.spawnMessage != null && this.spawnMessage != "")
            {
                result.Add(new XElement("spawnMessage", this.spawnMessage));
            }

            if (this.extraKinds != null && this.extraKinds.Any())
            {
                XElement extra = new XElement("extraKinds");
                this.extraKinds.ForEach(x => extra.Add(new XElement("li", x.defName)));
                result.Add(extra);
            }

            if (this.actions.Any())
            {
                XElement actions = new XElement("actions");
                this.actions.ForEach(x => actions.Add(x.SaveToXElement("li")));
                result.Add(actions);
            }

            if (this.hediffs.Any())
            {
                XElement hediffs = new XElement("hediffs");
                this.hediffs.ForEach(x => hediffs.Add(x.SaveToXElement("li")));
                result.Add(hediffs);
            }

            if (this.inventoryThings.Any())
            {
                XElement thingData = new XElement("inventoryThings");
                this.inventoryThings.ForEach((x) => thingData.Add(x.SaveToXElement("li")));
                result.Add(thingData);
            }

            if (this.inventoryCategorys.Any())
            {
                XElement categoryData = new XElement("inventoryCategorys");
                this.inventoryCategorys.ForEach((x) => categoryData.Add(x.SaveToXElement("li")));
                result.Add(categoryData);
            }

            return result;
        }

        public virtual Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null, bool setLord = true)
        {
            try
            {
                if (!Rand.Chance(this.generationChance) || !position.InBounds(map))
                {
                    return null;
                }

                Faction faction = GameTools.GetFaction(this.faction, map);
                Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
                if (!position.Fogged(map) && this.spawnMessage != null && !this.spawnMessage.NullOrEmpty())
                {
                    Messages.Message((this.spawnMessage.CanTranslate() ? this.spawnMessage.Translate().ToString() : this.spawnMessage), new LookTargets(position, map), MessageTypeDefOf.NeutralEvent);
                }

                if (setLord && faction != null && lord == null && this.enableLord)
                {
                    lord = map.lordManager.lords.Find(l => l.LordJob is LordJob_Custom && l.faction == faction);
                    if (lord == null)
                    {
                        lord = LordMaker.MakeNewLord(faction, new LordJob_Custom(), map);
                    }
                }

                List<PawnKindDef> kinds = new List<PawnKindDef>();
                kinds.AddRange(this.extraKinds);
                kinds.Add(this.kind);
                List<Pawn> pawns = new List<Pawn>();
                MakePawns(position, questTag, quest, lord, faction, result, kinds, pawns);
                this.SpawnPnaw(pawns, position, map);
                if (this.dataName != "undefined")
                {
                    List<Pawn> ps = new List<Pawn>();
                    result.Values.ToList().ForEach(t => ps.Add(t.Thing as Pawn));
                    GameComponent_Editor.Instance.GetQuestData(quest)?.AddGroup(this.dataName, ps);
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error("CQF Error:" + ex);
                return null;
            }
        }

        public virtual void MakePawns(IntVec3 position, string questTag, Quest quest, Lord lord, Faction faction, Dictionary<string, TargetInfo> result, List<PawnKindDef> kinds, List<Pawn> pawns)
        {
            foreach (PawnKindDef kind in kinds)
            {
                try
                {
                    int count = this.count.RandomInRange;
                    for (int i = 0; i < count; i++)
                    {
                        Pawn pawn = (Pawn)PawnGenerator.GeneratePawn(kind, faction);
                        if (pawn == null)
                        {
                            continue;
                        }

                        pawns.Add(pawn);
                        this.ActionAfterGeneration(pawn, quest, i, questTag);
                        if (lord != null)
                        {
                            if (this.duty == DutyDefOf.Defend && this.routeName.NullOrEmpty() && lord.LordJob is LordJob_Custom lordJob)
                            {
                                lordJob.defendDatas.SetOrAdd(pawn, position);
                            }

                            lord.AddPawn(pawn);
                            PawnDuty duty = new PawnDuty(this.duty);
                            duty.overrideFacing = this.rotation;
                            duty.focus = new LocalTargetInfo(position);
                            pawn.mindState.duty = duty;
                            if (lord.LordJob is LordJob_Custom job)
                            {
                                job.pawnDutyDatas.Add(pawn, this.duty);
                            }

                            if (lord.LordJob is LordJob_ComplexCustom complexJob)
                            {
                                complexJob.ApplyDefaultDutyMap(pawn, quest);
                            }
                        }

                        if (pawn.kindDef == this.kind)
                        {
                            result.SetOrAdd(this.dataName + "." + i, pawn);
                        }
                        else
                        {
                            result.SetOrAdd(this.dataName + "_" + pawn.kindDef.defName + "." + i, pawn);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Spawn pawn fail:" + e);
                }
            }
        }

        public virtual void SpawnPnaw(List<Pawn> pawns, IntVec3 position, Map map)
        {
            this.way.SpawnPnaw(pawns, position, map);
        }

        public void ActionAfterGeneration(Pawn pawn, Quest quest, int i, string questTag)
        {
            this.actions.ForEach(x => x.Work(new Dictionary<string, TargetInfo>() { [this.dataName + "." + i] = pawn }, quest));
            foreach (HediffInformation hediff in this.hediffs)
            {
                BodyPartRecord record = null;
                if (hediff.part != null)
                {
                    List<BodyPartRecord> records = pawn.RaceProps.body.GetPartsWithDef(hediff.part);
                    record = hediff.partLabel == null || hediff.partLabel == "" ? records.First() : records.Find(x => x.customLabel == hediff.partLabel);
                }

                pawn.health.AddHediff(hediff.hediff, record).Severity = hediff.severity;
            }

            if (this.dialogManager != null)
            {
                GameComponent_Editor.Instance.AddDialog(pawn, this.dialogManager);
            }

            pawn.questTags = new List<string>()
            {
                string.Concat(new object[] { questTag, ".", this.dataName, ".", i }),
                string.Concat(new object[] { questTag, ".", this.dataName }),
                questTag
            };
            this.inventoryThings.ForEach(x =>
            {
                Thing thing = ThingMaker.MakeThing(x.thing, x.stuff);
                thing.stackCount = x.count.RandomInRange;
                pawn.inventory.innerContainer.TryAdd(thing);
            });
            this.inventoryCategorys.ForEach(x =>
            {
                Thing thing = ThingMaker.MakeThing(x.category.DescendantThingDefs.RandomElement(), x.stuff);
                thing.stackCount = x.count.RandomInRange;
                pawn.inventory.innerContainer.TryAdd(thing);
            });
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref this.dataName, "QE_PawnData_dataName");
            Scribe_Values.Look(ref this.buffer, "QE_PawnData_buffer");
            Scribe_Values.Look(ref this.bufferMax, "QE_PawnData_bufferMax");
            Scribe_Values.Look(ref this.buffer_time, "QE_PawnData_buffer_time");
            Scribe_Values.Look(ref this.enableLord, "QE_PawnData_isOneOfLord");
            Scribe_Values.Look(ref this.count, "QE_PawnData_count");
            Scribe_Values.Look(ref this.lordDataName, "QE_PawnData_lordDataName");
            Scribe_Values.Look(ref this.timeToSpawn, "QE_PawnData_timeToSpawn");
            Scribe_Values.Look(ref this.routeName, "QE_PawnData_routeName");
            Scribe_Values.Look(ref this.spawnType, "QE_PawnData_spawnType");
            Scribe_Values.Look(ref this.spawnMessage, "QE_PawnData_spawnMessage");
            Scribe_Values.Look(ref this.rotation, "QE_PawnData_rotation");
            Scribe_Values.Look(ref this.buffer_chance, "buffer_chance");
            Scribe_Values.Look(ref this.generationChance, "QE_PawnData_generationChance");
            Scribe_Values.Look(ref this.roundTrip, "QE_PawnData_roundTrip");
            Scribe_Values.Look(ref this.faction, "QE_PawnData_faction");
            Scribe_Defs.Look(ref this.duty, "QE_PawnData_duty");
            Scribe_Defs.Look(ref this.dialogManager, "QE_PawnData_dialogManager");
            Scribe_Defs.Look(ref this.kind, "QE_PawnData_kind");
            Scribe_Deep.Look(ref this.way, "way");
            Scribe_Collections.Look(ref this.extraKinds, "QE_PawnSpawnData_extraKind", LookMode.Def);
            Scribe_Collections.Look(ref this.inventoryThings, "QE_PawnSpawnData_inventoryThings", LookMode.Deep);
            Scribe_Collections.Look(ref this.inventoryCategorys, "QE_PawnSpawnData_inventoryCategorys", LookMode.Deep);
            Scribe_Collections.Look(ref this.hediffs, "QE_PawnSpawnData_hediffs", LookMode.Deep);
            Scribe_Collections.Look(ref this.actions, "QE_PawnSpawnData_actions", LookMode.Deep);
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

            if (Text.CalcSize(label).x > labelWidth)
                TooltipHandler.TipRegion(labelRect, label);
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
            if (icon != null)
                Widgets.DrawTextureFitted(new Rect(row.x + 4f, y + 4f, 24f, 24f), icon, 1f);
            Rect name = new Rect(row.x + 36f, y, Mathf.Max(20f, row.width - 170f), 32f);
            if (CQFUIStyle.ButtonText(name, label ?? "Select".Translate().ToString(), false))
                Find.WindowStack.Add(CQFEditorBridge.Editor.CreateDrawableWindow(item));
            TooltipHandler.TipRegion(name, "CQF_ClickToEdit".Translate());
            int min = item.count.min;
            int max = item.count.max;
            Widgets.TextFieldNumeric(new Rect(row.xMax - 124f, y + 2f, 50f, 28f), ref min, ref item.bufferMin);
            Widgets.Label(new Rect(row.xMax - 70f, y + 4f, 12f, 25f), "~");
            Widgets.TextFieldNumeric(new Rect(row.xMax - 52f, y + 2f, 50f, 28f), ref max, ref item.bufferMax);
            item.count = new IntRange(min, max);
            y += 38f;
        }

        [NoTranslate]
        public string dataName = "undefined";
        public string buffer;
        public string bufferMax;
        public string buffer_time;
        public string buffer_chance;
        public float generationChance = 1f;
        public IntRange count = new IntRange(1, 1);
        public int timeToSpawn = 0;
        public bool enableLord = false;
        public string lordDataName;
        [CQFLocalizableText]
        public string spawnMessage = null;
        public string routeName = null;
        public bool roundTrip = true;
        public Rot4 rotation = Rot4.South;
        public SpawnType spawnType = SpawnType.MapGeneration;
        public string faction = null;
        public ArrivingWay way = new ArrivingWay();
        public DutyDef duty = DutyDefOf.Defend;
        public PawnKindDef kind = null;
        public List<PawnKindDef> extraKinds = new List<PawnKindDef>();
        public DialogManagerDef dialogManager = null;
        public List<HediffInformation> hediffs = new List<HediffInformation>();
        public List<CQFAction> actions = new List<CQFAction>();
        public List<CQFThingDefCount> inventoryThings = new List<CQFThingDefCount>();
        public List<CQFThingCategoryCount> inventoryCategorys = new List<CQFThingCategoryCount>();
    }
}
