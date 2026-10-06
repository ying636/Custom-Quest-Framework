using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using System.IO;
using System.Xml;

namespace QuestEditor_Library
{
    public class QuestEditor_CustomQuestMap : Page
    {
        public override string PageTitle => "SpawnNewCustomMap".Translate();
        public override Vector2 InitialSize => QuestEditor_CustomQuestMap.size;
        public override void DoWindowContents(Rect inRect)
        {
            base.DrawPageTitle(inRect);
            if (Widgets.CloseButtonFor(inRect))
            {
                this.Close();
            }
            float y = 50f;
            string text = "MapSize".Translate();
            Widgets.Label(new Rect((inRect.width - Text.CalcSize(text).x) / 2, y, 300f, 30f), text);
            y += 25f;
            Widgets.TextFieldNumeric<int>(new Rect((inRect.width / 2) - 75f, y, 70f, 20f), ref this.mapSize.x, ref this.buffer);
            Widgets.Label(new Rect((inRect.width / 2) - 5f, y, 10f, 20f), "x");
            Widgets.TextFieldNumeric<int>(new Rect((inRect.width / 2) + 5f, y, 70f, 20f), ref this.mapSize.z, ref this.bufferz);
            y += 35f;
            Rect backgroundMapRect = new Rect(10f, y, inRect.width - 20f, 25f);
            Widgets.CheckboxLabeled(backgroundMapRect, "CQF_MapBackgroundIsBackgroundMap".Translate(), ref this.enableBackground);
            TooltipHandler.TipRegion(backgroundMapRect, "CQF_MapBackgroundIsBackgroundMapTip".Translate());
            y += 35f;
            if (Widgets.ButtonText(new Rect(10f, y, 100f, 38f), "OK".Translate()))
            {
                GenerateMap(this.mapSize, null, this.enableBackground);
                this.Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 110f, y, 100f, 38f), "Load".Translate()))
            {
                Action<CustomMapDataDef> generateMap = (m) =>
                {
                    GenStep_SetTerrain.customMap = m;
                    GenerateMap(m.size, m);
                };
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                List<CustomMapDataDef> defs = DefDatabase<CustomMapDataDef>.AllDefsListForReading;
                defs.ForEach(x => options.Add(new FloatMenuOption(x.label, () =>
                {
                    generateMap(x);
                    this.Close();
                })));
                if (options.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
        }

        public static void GenerateMap(IntVec3 size,CustomMapDataDef def = null, bool enableTerrainEdges = false) 
        {
            if (Current.Game == null)
            {
                Messages.Message("NoGame".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            if (size.z <= 0 || size.x <= 0)
            {
                Messages.Message("MapSizeCantBeZero".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            MapGenerator.PlayerStartSpot = new IntVec3(size.x / 2, 1, size.z / 2);
            MapParent customMap = (MapParent)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("QE_CustomMap_Editor"));
            customMap.Tile = TileFinder.RandomSettlementTileFor(Faction.OfPlayer);
            customMap.SetFaction(Find.FactionManager.OfPlayer);
            Find.WorldObjects.Add(customMap);
            string seed = Find.World.info.seedString;
            Find.World.info.seedString = Find.TickManager.TicksGame.ToString();
            LongEventHandler.SetCurrentEventText("GeneratingMap".Translate());
            DeepProfiler.Start("Generate new map");
            LongEventHandler.QueueLongEvent(() =>
            {
                size.y = 1;
                Map map = MapGenerator.GenerateMap(size, customMap, customMap.MapGeneratorDef, def == null ? customMap.ExtraGenStepDefs :
                    Gen.YieldSingle(new GenStepWithParams(QEDefOf.QE_CustomSite_GenStep, new GenStepParams()
                    {
                        sitePart = new SitePart(null, QEDefOf.QE_CustomSite, new CustomSitePartParams()
                        {
                            mapData = def,
                            dev = true
                        })
                    })));
                Current.Game.CurrentMap = map;
                if (def == null && enableTerrainEdges)
                {
                    MapComponent_CustomMapData comp = map.GetComponent<MapComponent_CustomMapData>();
                    comp.background = new CustomMapBackgroundData();
                    comp.background.enableTerrainEdges = true;
                }

                def?.disdestroy?.ForEach(d =>
                {
                    if (!map.designationManager.HasMapDesignationAt(d)) 
                    {
                        map.designationManager.AddDesignation(new Designation(d, QEDefOf.QE_Disdestroy, null));
                    }
                }
                );
                def?.disgenerate?.ForEach(d =>
                {
                    if (!map.designationManager.HasMapDesignationAt(d))
                    { 
                        map.designationManager.AddDesignation(new Designation(d, QEDefOf.QE_Disgenerate, null)); 
                    }
                });
                def?.routes?.ToList().ForEach(r => map.GetComponent<MapComponent_CustomMapData>().route.SetOrAdd(r.Key,new Route() {route = r.Value}));
                def?.lordDatas?.ForEach(d => map.GetComponent<MapComponent_CustomMapData>().Lords.Add(new LordWithName() {data = d,name = d.name}));
            }, "GeneratingMap".Translate(), true, (Exception x) => { Log.Message("GenerateMapError:" + x.ToString()); });

            Find.World.info.seedString = seed;
            DeepProfiler.End();
            Find.WindowStack.TryRemove(typeof(Page_QuestEditor));
        }

        public IntVec3 mapSize = IntVec3.Zero;
        public string buffer;
        public string bufferz;
        public bool enableBackground;
        private static readonly Vector2 size = new Vector2(260f, 235f);
    }
}
