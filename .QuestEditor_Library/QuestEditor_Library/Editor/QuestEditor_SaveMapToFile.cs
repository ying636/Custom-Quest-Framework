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
            float y = 50f;
            float x2 = 10f;
            Widgets.BeginScrollView(new Rect(0f, y,inRect.width - 10f,inRect.height - 50f), ref this.pos, new Rect(0f, y, inRect.width - 30f, this.y));
            Widgets.Label(new Rect(x2, y, 300f, 25f), "MapDataDefName".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            QuestEditor_SaveMapToFile.def.defName = Widgets.TextField(new Rect(x2, y, 230f, 25f), QuestEditor_SaveMapToFile.def.defName);
            y += 30f;
            Widgets.Label(new Rect(x2, y, 300f,25f), "MapName".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            QuestEditor_SaveMapToFile.def.label = Widgets.TextField(new Rect(x2, y, 230f, 25f), QuestEditor_SaveMapToFile.def.label);
            y += 35f;
            if (!def.isPart)
            {
                Widgets.Label(new Rect(x2, y, 300f, 25f), "MapDesc".Translate().Colorize(ColorLibrary.SkyBlue));
                y += 30f;
                QuestEditor_SaveMapToFile.def.description = Widgets.TextArea(new Rect(x2, y, 230f, 50f), QuestEditor_SaveMapToFile.def.description);
                y += 55f;
            }
            Widgets.CheckboxLabeled(new Rect(x2, y, 250f, 25f), "FoggedWhenPlayerEnter".Translate(), ref QuestEditor_SaveMapToFile.def.fogged);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "GenerationChance".Translate(),ref def.commonality,ref this.buffer,x2,150f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "GenerationLimit".Translate(), ref def.generationLimit, ref buffer2, x2, 100f);
            TooltipHandler.TipRegion(new Rect(x2, y, 100f, 25f), "GenerationLimitTip".Translate());
            y += 30f;       
            Widgets.CheckboxLabeled(new Rect(x2, y, 250f, 25f), "CustomMapIsPart".Translate(), ref QuestEditor_SaveMapToFile.def.isPart);
            TooltipHandler.TipRegion(new Rect(x2, y, 250f, 25f), "CustomMapIsPartTip".Translate());  
            y += 30f;
            if (!def.isPart)
            {
                Rect reserveRect = new Rect(x2, y, 300f, 25f);
                if (!def.isPart && Widgets.ButtonText(reserveRect, "ReserveGenerationThing".Translate(def.reserveThing == null ? "NoGenerate".Translate().ToString() : def.reserveThing?.stuff?.label + def.reserveThing?.def?.label), false))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    options.Add(new FloatMenuOption("NoGenerate".Translate(), () => def.reserveThing = null));
                    options.Add(new FloatMenuOption("Select".Translate(), () =>
                    {
                        def.reserveThing = new ThingData();
                        Find.WindowStack.Add(new Dialog_Select<ThingDef>(Designator_SpawnThing.Bespawnable,
               t => t.uiIcon, t => t.label, "Select".Translate(),
               t =>
               {
                   def.reserveThing.def = t;
                   def.reserveThing.hitPoint = t.BaseMaxHitPoints;
                   if (t.MadeFromStuff)
                   {
                       Find.WindowStack.Add(new Dialog_Select<ThingDef>(GenStuff.AllowedStuffsFor(t).ToList(), s => s.uiIcon, s => s.label, "SelectStuff".Translate(), s =>
                       {
                           def.reserveThing.stuff = s;
                           def.reserveThing.hitPoint = (int)(t.BaseMaxHitPoints * (s.stuffProps.statFactors.Find(s2 => s2.stat == StatDefOf.MaxHitPoints) is StatModifier stat ? stat.value : 1f));
                       }, t2 => t2.graphic?.Color ?? Color.white));
                   }
               }, t => t.graphic?.Color ?? Color.white));
                    }));
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                TooltipHandler.TipRegion(reserveRect, "ReserveGenerationThing_MapDef_Tip".Translate());
                y += 30f;
            }
            if (Widgets.ButtonText(new Rect(x2, y, 100f, 25f), "ReplaceDatas".Translate(), false))
            {
                Find.WindowStack.Add(new Window_ReplaceData(def));
            }
            Rect tip = new Rect(110f + x2, y, 25f, 25f);
            Widgets.DrawTextureFitted(tip, Page_QuestEditor.tipIcon, 1f);
            TooltipHandler.TipRegion(tip, "ReplaceDefTip".Translate());
            y += 30f;
            if (Widgets.ButtonText(new Rect(x2,y,180f,30f), "AdvancedSetting".Translate(),false))
            {
                Find.WindowStack.Add(new Dialog_MapMisc(def));
            }
            y += 30f;
            Rect rect3 = new Rect(x2, y, 150f, 38f);
            if (Widgets.ButtonText(rect3, "OK".Translate()))
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
            rect3.x += 160f;
            if (Widgets.ButtonText(rect3, "Load".Translate()))
            {
                List<CustomMapDataDef> list = new List<CustomMapDataDef>();
                list.AddRange(DefDatabase<CustomMapDataDef>.AllDefsListForReading.ToList());
                CQFEditorTools.DrawFloatMenu<CustomMapDataDef>(list, (x) => 
                {
                    QuestEditor_SaveMapToFile.def = x.Copy(null);
                    this.buffer = (100f * x.commonality).ToString();
                    QuestEditor_SaveMapToFile.def.generationLimit = x.generationLimit;
                    QuestEditor_SaveMapToFile.def.thingDatas.Clear();
                    QuestEditor_SaveMapToFile.def.customThings.Clear();
                    QuestEditor_SaveMapToFile.def.roofRects.Clear();
                    QuestEditor_SaveMapToFile.def.terrainsRect.Clear();
                    QuestEditor_SaveMapToFile.def.roofs.Clear();
                    QuestEditor_SaveMapToFile.def.terrains.Clear();
                    QuestEditor_SaveMapToFile.def.pawns.Clear();
                    QuestEditor_SaveMapToFile.def.specialSpawnPawns.Clear();
                    QuestEditor_SaveMapToFile.def.roofs.Clear();
                    QuestEditor_SaveMapToFile.def.lordDatas.Clear();
                    QuestEditor_SaveMapToFile.def.zoneCores.Clear();
                    QuestEditor_SaveMapToFile.def.generationActions.Clear();
                    this.buffer2 =  x.generationLimit.ToString();
                }, (x) => x.label);
            }
            Widgets.EndScrollView();
            this.y = y;
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
            string path = Page_QuestEditor.Path + @"\Map\" + QuestEditor_SaveMapToFile.def.defName + ".xml";
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
                def = new CustomMapDataDef() {isPart = def.isPart};
            }), "SaveToFile".Translate(), true, (Exception x) => { Log.Message("SaveError:" + x.ToString()); });
            this.saveMode = SaveMode.None;   
            this.Close();
        }
        List<IntVec3> poss;
        IntVec3 centre;
        IntVec3 mapSize;
        public SaveMode saveMode = SaveMode.None;

        string buffer;
        string buffer2;
        public float y = 0f;
        public static CustomMapDataDef def = new CustomMapDataDef();
        private Vector2 pos = Vector2.zero;
        private static readonly Vector2 size = new Vector2(400f,480f);
        public static readonly Texture2D arrowIcon = ContentFinder<Texture2D>.Get("UI/Icon_Arrow", true);
    }
}
