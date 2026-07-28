using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public class QuestEditor_SaveMapToFile : Page
    {
        public QuestEditor_SaveMapToFile() { this.saveMode = SaveMode.None; def.isPart = false; }
        public QuestEditor_SaveMapToFile(List<IntVec3> poss, IntVec3 centre, IntVec3 size, SaveMode saveMode)
        {
            poss.RemoveAll(p => !p.InBounds(Find.CurrentMap));
            this.poss = poss;
            this.centre = centre;
            this.mapSize = size + new IntVec3(1,0,1);
            this.saveMode = saveMode;
            def.isPart = true;
        }
        public override Vector2 InitialSize => QuestEditor_SaveMapToFile.size;
        public override string PageTitle => "SaveMapToFile".Translate();

        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }

            const float titleHeight = 52f;
            const float footerHeight = 48f;
            Rect outRect = new Rect(0f, titleHeight, inRect.width, inRect.height - titleHeight - footerHeight - 10f);
            float contentWidth = outRect.width - 20f;
            Rect viewRect = new Rect(0f, 0f, contentWidth, Mathf.Max(this.y, outRect.height));
            Widgets.BeginScrollView(outRect, ref this.pos, viewRect);

            float y = 0f;
            this.DrawBasicSection(ref y, contentWidth);
            this.DrawGenerationSection(ref y, contentWidth);
            this.DrawMapContentSection(ref y, contentWidth);
            this.DrawDataToolsSection(ref y, contentWidth);
            Widgets.EndScrollView();
            this.y = y + 4f;

            float buttonWidth = (inRect.width - 12f) / 2f;
            float footerY = inRect.height - footerHeight;
            if (Widgets.ButtonText(new Rect(0f, footerY, buttonWidth, 40f), "Load".Translate()))
            {
                this.OpenLoadMenu();
            }
            if (Widgets.ButtonText(new Rect(buttonWidth + 12f, footerY, buttonWidth, 40f), "OK".Translate()))
            {
                if (this.saveMode == SaveMode.None)
                {
                    Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().Lords.ForEach(l =>
                    QuestEditor_SaveMapToFile.def.lordDatas.Add(l.data));
                    Save(() => QuestEditor_SaveMapToFile.def.LoadData(Find.CurrentMap));
                }
                else 
                {
                    Save(() => QuestEditor_SaveMapToFile.def.LoadData(Find.CurrentMap,this.poss,this.mapSize));
                }
            }
        }

        public virtual void Save(Action saveAction) 
        {
            if (QuestEditor_SaveMapToFile.def.label == null || QuestEditor_SaveMapToFile.def.defName == null || def.defName == "UnnamedDef")
            {
                Messages.Message("NoName".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            if (Find.CurrentMap == null)
            {
                Messages.Message("SaveInNonEditorMap".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            string path = Path.Combine(Page_QuestEditor.Path, "Map", QuestEditor_SaveMapToFile.def.defName + ".xml");
            LongEventHandler.QueueLongEvent((Action)(() =>
            {
                saveAction();
                XDocument mapXml = new XDocument();
                XElement defXml = QuestEditor_SaveMapToFile.def.SaveToXElement("QuestEditor_Library.CustomMapDataDef");
                XElement defs = new XElement("Defs", defXml);
                mapXml.Add(defs);
                mapXml.Save(path);
                Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                if (!DefDatabase<CustomMapDataDef>.AllDefsListForReading.Exists(d => d.defName == def.defName))
                {
                    DefDatabase<CustomMapDataDef>.Add(def);
                }
                def = new CustomMapDataDef() {isPart = def.isPart, destroyAllThing = def.destroyAllThing};
            }), "SaveToFile".Translate(), true, (Exception x) => { Log.Message("SaveError:" + x.ToString()); });
            this.saveMode = SaveMode.None;   
            this.Close();
        }

        private void DrawBasicSection(ref float y, float width)
        {
            float sectionHeight = def.isPart ? 114f : 184f;
            Rect sectionRect = new Rect(0f, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            Widgets.Label(new Rect(12f, y + 8f, width - 24f, 25f),
                "CQF_MapSave_BasicInformation".Translate().Colorize(ColorLibrary.PaleBlue));
            y += 38f;
            this.DrawTextRow(ref y, width, "MapDataDefName".Translate(), ref def.defName);
            this.DrawTextRow(ref y, width, "MapName".Translate(), ref def.label);
            if (!def.isPart)
            {
                Widgets.Label(new Rect(12f, y + 3f, 130f, 25f), "MapDesc".Translate());
                def.description = Widgets.TextArea(new Rect(146f, y, width - 158f, 58f), def.description);
                y += 66f;
            }
            y = sectionRect.yMax + 10f;
        }

        private void DrawGenerationSection(ref float y, float width)
        {
            Rect sectionRect = new Rect(0f, y, width, 146f);
            Widgets.DrawMenuSection(sectionRect);
            Widgets.Label(new Rect(12f, y + 8f, width - 24f, 25f),
                "CQF_MapSave_Generation".Translate().Colorize(ColorLibrary.PaleBlue));
            y += 38f;
            this.DrawCheckboxRow(ref y, width, "FoggedWhenPlayerEnter".Translate(), ref def.fogged);

            Rect chanceRect = new Rect(12f, y, width - 24f, 30f);
            Widgets.DrawHighlightIfMouseover(chanceRect);
            Widgets.Label(new Rect(chanceRect.x + 6f, y + 3f, 130f, 25f), "GenerationChance".Translate());
            Widgets.TextFieldPercent(new Rect(chanceRect.x + 140f, y + 2f, chanceRect.width - 146f, 26f),
                ref def.commonality, ref this.buffer);
            y += 32f;

            Rect limitRect = new Rect(12f, y, width - 24f, 30f);
            Widgets.DrawHighlightIfMouseover(limitRect);
            Widgets.Label(new Rect(limitRect.x + 6f, y + 3f, 130f, 25f), "GenerationLimit".Translate());
            Widgets.TextFieldNumeric(new Rect(limitRect.x + 140f, y + 2f, limitRect.width - 146f, 26f),
                ref def.generationLimit, ref this.buffer2, 0);
            TooltipHandler.TipRegion(limitRect, "GenerationLimitTip".Translate());
            y = sectionRect.yMax + 10f;
        }

        private void DrawMapContentSection(ref float y, float width)
        {
            bool drawFullMapOptions = !def.isPart;
            float sectionHeight = drawFullMapOptions ? 148f : 82f;
            Rect sectionRect = new Rect(0f, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            Widgets.Label(new Rect(12f, y + 8f, width - 24f, 25f),
                "CQF_MapSave_MapContent".Translate().Colorize(ColorLibrary.PaleBlue));
            y += 38f;
            this.DrawCheckboxRow(ref y, width, "CustomMapIsPart".Translate(), ref def.isPart,
                "CustomMapIsPartTip".Translate());
            if (drawFullMapOptions)
            {
                this.DrawCheckboxRow(ref y, width, "CustomMapDestroyAllThing".Translate(), ref def.destroyAllThing,
                    "CustomMapDestroyAllThingTip".Translate());
                Rect reserveRect = new Rect(12f, y, width - 24f, 30f);
                if (Widgets.ButtonText(reserveRect,
                        "ReserveGenerationThing".Translate(def.reserveThing == null
                            ? "NoGenerate".Translate().ToString()
                            : def.reserveThing?.stuff?.label + def.reserveThing?.def?.label), false))
                {
                    this.OpenReserveThingMenu();
                }
                TooltipHandler.TipRegion(reserveRect, "ReserveGenerationThing_MapDef_Tip".Translate());
            }
            y = sectionRect.yMax + 10f;
        }

        private void DrawDataToolsSection(ref float y, float width)
        {
            Rect sectionRect = new Rect(0f, y, width, 88f);
            Widgets.DrawMenuSection(sectionRect);
            Widgets.Label(new Rect(12f, y + 8f, width - 24f, 25f),
                "CQF_MapSave_DataTools".Translate().Colorize(ColorLibrary.PaleBlue));
            y += 40f;
            float buttonWidth = (width - 36f) / 2f;
            Rect replaceRect = new Rect(12f, y, buttonWidth, 32f);
            if (Widgets.ButtonText(replaceRect, "ReplaceDatas".Translate(), false))
            {
                Find.WindowStack.Add(new Window_ReplaceData(def));
            }
            TooltipHandler.TipRegion(replaceRect, "ReplaceDefTip".Translate());
            if (Widgets.ButtonText(new Rect(replaceRect.xMax + 12f, y, buttonWidth, 32f),
                    "AdvancedSetting".Translate(), false))
            {
                Find.WindowStack.Add(new Dialog_MapMisc(def));
            }
            y = sectionRect.yMax + 10f;
        }

        private void DrawCheckboxRow(ref float y, float width, string label, ref bool value, string tip = "")
        {
            Rect rowRect = new Rect(12f, y, width - 24f, 30f);
            Widgets.DrawHighlightIfMouseover(rowRect);
            Widgets.CheckboxLabeled(rowRect.ContractedBy(6f, 2f), label, ref value);
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rowRect, tip);
            }
            y += 32f;
        }

        private void DrawTextRow(ref float y, float width, string label, ref string value)
        {
            Rect rowRect = new Rect(12f, y, width - 24f, 30f);
            Widgets.DrawHighlightIfMouseover(rowRect);
            Widgets.Label(new Rect(rowRect.x + 6f, y + 3f, 130f, 25f), label);
            value = Widgets.TextField(new Rect(rowRect.x + 140f, y + 2f, rowRect.width - 146f, 26f), value);
            y += 32f;
        }

        private void OpenLoadMenu()
        {
            CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading.ToList(), x =>
            {
                def = x.Copy(null);
                this.buffer = (100f * x.commonality).ToString();
                def.generationLimit = x.generationLimit;
                def.thingDatas.Clear();
                def.customThings.Clear();
                def.roofRects.Clear();
                def.terrainsRect.Clear();
                def.roofs.Clear();
                def.terrains.Clear();
                def.pawns.Clear();
                def.specialSpawnPawns.Clear();
                def.lordDatas.Clear();
                def.zoneCores.Clear();
                def.generationActions.Clear();
                this.buffer2 = x.generationLimit.ToString();
            }, x => x.label);
        }

        private void OpenReserveThingMenu()
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("NoGenerate".Translate(), () => def.reserveThing = null),
                new FloatMenuOption("Select".Translate(), () =>
                {
                    def.reserveThing = new ThingData();
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(
                        Designator_SpawnThing.Bespawnable, t => t.uiIcon, t => t.label, t =>
                        {
                            def.reserveThing.def = t;
                            def.reserveThing.hitPoint = t.BaseMaxHitPoints;
                            if (t.MadeFromStuff)
                            {
                                Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(
                                    GenStuff.AllowedStuffsFor(t).ToList(), s => s.uiIcon, s => s.label, s =>
                                    {
                                        def.reserveThing.stuff = s;
                                        def.reserveThing.hitPoint = (int)(t.BaseMaxHitPoints *
                                            (s.stuffProps.statFactors.Find(s2 => s2.stat == StatDefOf.MaxHitPoints)
                                                is StatModifier stat ? stat.value : 1f));
                                    }, t2 => t2.graphic?.Color ?? Color.white), "SelectStuff".Translate()));
                            }
                        }, t => t.graphic?.Color ?? Color.white), "Select".Translate()));
                })
            }));
        }

        public SaveMode saveMode = SaveMode.None;
        public float y = 0f;
        public static CustomMapDataDef def = new CustomMapDataDef();
        public static readonly Texture2D arrowIcon = ContentFinder<Texture2D>.Get("UI/Icon_Arrow", true);

        private List<IntVec3> poss;
        private IntVec3 centre;
        private IntVec3 mapSize;
        private string buffer;
        private string buffer2;
        private Vector2 pos = Vector2.zero;
        private static readonly Vector2 size = new Vector2(480f, 580f);
    }
}
