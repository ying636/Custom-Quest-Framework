using System;
using System.Collections.Generic;
using RimWorld.Planet;
using RimWorld;
using Verse;
using Verse.AI.Group;
using System.Linq;

namespace QuestEditor_Library
{
    public class GenStep_CustomMap : GenStep
    {
        public override int SeedPart => 1114561;
        public override void Generate(Map map, GenStepParams parms)
        {
            if (parms.sitePart.parms is CustomSitePartParams customParams &&
                customParams.mapData is CustomMapDataDef def)
            {
                if (map.Parent is CustomSite site)
                {
                    site.mapDef = def;
                }
                GenStep_CustomMap.SpawnCustomMap(map, parms, def, customParams.quest,
                    customParams.dev, null,
                    false, customParams.isSubMap,
                    false, def.destroyAllThing, customParams.replaceMapGeneration);
            }
            else if (map.Parent is CustomSite site)
            {
                GenStep_CustomMap.SpawnCustomMap(map, parms, site.mapDef, site.quest,
                    site.dev, null,
                    false, false,
                    false, site.mapDef.destroyAllThing, site.replaceMapGeneration);
            }
        }

        public static List<Thing> SpawnCustomMap(Map map, GenStepParams parms,
            CustomMapDataDef def, Quest quest, bool load = false, IntVec3? centerP = null, bool isGenerateByCore = false
            , bool isSubMap = false, bool debug = false, bool destroyThings = false, bool replaceMap = false, Func<Thing, bool> validator = null,bool ignoreDisgenerate = false)
        {
            try
            {
                List<Thing> result = new List<Thing>();
                if (!isGenerateByCore)
                {
                    GameTools.isGeneratingMap = true;
                    GameTools.ResetTemporaryTargets();
                    def.mapPartGenerationLimit.ForEach(l => generatedLimit_Key.SetOrAdd(l.key, l.limit));
                    faction = GameTools.GetFaction(def.faction, map); 
                }
                CustomSitePartParams customParams = new CustomSitePartParams()
                {
                    quest = quest,
                    mapData = def,
                    isSubMap = isSubMap
                };
                def.preCustomSteps.ForEach(s => s.Generate(map, def, customParams));
                quest = customParams.quest;
                string questId = quest == null ? "0" : quest?.id.ToString();
                List<IntVec3> disgenerate2 = new List<IntVec3>();
                CustomMapDataDef origin = def.Origin;
                generatedCount.SetOrAdd(origin, generatedCount.ContainsKey(origin) ? generatedCount[origin] + 1 : 1);
                IntVec3 center = centerP == null ? map.Center : centerP.Value;
                if (def.size.IsValid && center != IntVec3.Zero)
                {
                    center -= new IntVec3(def.size.x / 2, 0, def.size.z / 2);
                }
                if (replaceMap || isSubMap || (load && !debug))
                {
                    center = centerP == null ? IntVec3.Zero : centerP.Value;
                }
                if (def != null && !def.disgenerate.NullOrEmpty())
                {
                    def.disgenerate.ForEach(d => disgenerate2.Add(d + center));
                }
                if (!load)
                {
                    GenStep_CustomMap.Pretreat(map, def, center, isGenerateByCore, disgenerate2, destroyThings, validator);
                }
                GenStep_CustomMap.SetRoofAndTerrain(map, def, center, ignoreDisgenerate);
                disgenerate.AddRange(disgenerate2);
                result.AddRange(GenStep_CustomMap.SpawnThings(map, parms, def, center, load));
                result.AddRange(GenStep_CustomMap.SpawnCustomThing(map, def, quest, parms, center, load));
                if (!load)
                {
                    def.generationActions.ForEach(a =>
                    {
                        IntVec3 pos = a.pos + center;
                        if (pos.InBounds(map))
                        {
                            a.actions?.ForEach(acion => acion.Work(new Dictionary<string, TargetInfo>()
                            { ["Position"] = new TargetInfo(pos, map) }, quest));
                        }
                    });
                    def.lordDatas.ForEach(l =>
                    {
                        Lord lord = LordMaker.MakeNewLord(GameTools.GetFaction(l.faction, map), l.lordJobData.CreateJob(map, quest), map);
                        map.GetComponent<MapComponent_CustomMapData>().Lords.Add(new LordWithName() { name = l.name, lord = lord });
                        GameComponent_Editor.Instance.GetQuestData(quest)?.Lords.SetOrAdd(l.name, lord);
                        if (!lordsWithName.ContainsKey(l.name))
                        {
                            lordsWithName.Add(l.name, lord);
                        }
                        lordsWithData.Add(lord, l);
                    });
                    result.AddRange(GenStep_CustomMap.SpawnZone(map, def, quest, parms, center, load));
                    result.AddRange(GenStep_CustomMap.SpawnPawns(map, def, center, questId, quest));
                }
                else
                {
                    def.generationActions.ForEach(a =>
                   {
                       IntVec3 pos = a.pos + center; 
                       GenerationActionWorker worker = (GenerationActionWorker)GenSpawn.Spawn(QEDefOf.QE_GenerationActionWorker, pos, map);
                       a.actions.ForEach(a2 => worker.actions.Add(a2.Copy()));
                   });
                    result.AddRange(GenStep_CustomMap.SpawnZone(map, def, quest, parms, center, load));
                    GenStep_CustomMap.SpawnSpawners(map, def, center);
                }
                GenStep_CustomMap.AddPawnDataToLord(map, def, center);
                map.GetComponent<MapComponent_CustomMapData>().questTag = "Quest" + questId;
                customParams.quest = quest;
                def.customSteps.ForEach(s => s.Generate(map, def, customParams));
                if (!isGenerateByCore)
                {
                    if (!load && def.reserveThing is ThingData data && !CQFEditorTools.disgenerateByCore)
                    {
                        map.AllCells.ToList().ForEach(c =>
                        {
                            if (!disgenerate.Contains(c) && c.GetFirstBuilding(map) == null)
                            {
                                result.Add(data.Spawn(map, c, (t, b) => GetDef(t, def, b)));
                            }
                        });
                    }
                    PostGenerateMap(quest);
                    if (DebugTools.clearGenerationData)
                    {
                        GameTools.isGeneratingMap = false;
                        GameTools.ClearTemporaryTargets();
                        faction = null;
                        replaceData?.Clear();
                        replaceData = null;
                        disgenerate.Clear();
                        generatedCount.Clear();
                        generatedCount_Key.Clear();
                        generatedLimit_Key.Clear();
                        lordsWithName.Clear();
                        lordsWithData.Clear();

                    }
                    foreach (var item in requests)
                    {
                        item.Execute();
                    }
                    requests.Clear();
                    if (!load)
                    {
                        GenStep_CustomMap.Fog(map, def, center, isGenerateByCore, isSubMap);
                    }
                }
                
                return result;
            }
            catch(Exception e) 
            {
                Log.Error("Generate Custom map error:" + def?.defName + "," + e);
            }

            return null;
        }

        public static List<Thing> SpawnCustomThing(Map map, CustomMapDataDef def, Quest quest
            , GenStepParams parms,IntVec3 centre,bool load = false)
        {
            List<Thing> result = new List<Thing>();

            foreach (CustomThingData data in def.customThings)
            {
                if (data as CustomThingData_ZoneCore == null)
                {
                    IntVec3 pos = data.position + centre;
                    if (!pos.InBounds(map))
                    {
                        continue;
                    }
                    Thing t = data.SpawnThing(map, quest, out List<Thing> ts, centre, load, def, (d, s) =>
                        {
                            return load ? d : GenStep_CustomMap.GetDef(d, def, s);
                        });
                    result.Add(t);
                    result.AddRange(ts);
                    if (t != null && t.def.CanHaveFaction && t.Faction == null && faction != null)
                    {
                        t.SetFaction(faction);
                    }
                }
            }
            return result;
        }
        public static List<Thing> SpawnZone(Map map, CustomMapDataDef def, Quest quest, GenStepParams parms, IntVec3 centre, bool load = false) 
        {
            List<Thing> results = new List<Thing>();
            foreach (CustomThingData data in def.zoneCores)
            {
                IntVec3 pos = data.position + centre;
                if (!pos.InBounds(map))
                {
                    continue;
                }
                Thing t = data.SpawnThing(map, quest, out List<Thing> ts, centre, load, def, (d, s) =>
                {
                    return load ? d : GenStep_CustomMap.GetDef(d, def, s);
                });
                results.Add(t);
                results.AddRange(ts);
                if (t != null && t.def.CanHaveFaction && t.Faction == null && faction != null)
                {
                    t.SetFaction(faction);
                }
            }
            return results;
        }
        private static void AddPawnDataToLord(Map map, CustomMapDataDef def, IntVec3 center)
        {
            foreach (KeyValuePair<IntVec3, List<PawnSpawnData>> content in def.specialSpawnPawns)
            {
                IntVec3 intVec3 = center + content.Key;
                MapComponent_CustomMapData component = map.GetComponent<MapComponent_CustomMapData>();
                List<PawnSpawnData> specialDatas = new List<PawnSpawnData>();
                if (content.Value.Any())
                {
                    foreach (PawnSpawnData data in content.Value)
                    {
                        if (data is PawnSpawnData pawnData && pawnData.spawnType == SpawnType.BuildingTick)
                        {
                            component.pawnSpawnDatas_Tick.Add(new PawnDataWithPosAndTime() { data = pawnData, position = intVec3 });
                        }
                        else
                        {
                            specialDatas.Add(data);
                        }
                    }
                }
                if (specialDatas.Any() && intVec3.GetFirstBuilding(map) is Building building)
                {
                    component.PawnSpawnDatas_Building.Add(building, specialDatas);
                }
            }
        }
        private static void SpawnSpawners(Map map, CustomMapDataDef def, IntVec3 center) 
        {
            foreach (KeyValuePair<IntVec3, List<PawnSpawnData>> content in def.pawns)
            {
                ((Spawner)GenSpawn.Spawn(QEDefOf.QE_Spawner_Editor,center + content.Key,map)).pawns = content.Value;
            }
        }
        private static List<Pawn> SpawnPawns(Map map, CustomMapDataDef def, IntVec3 center,string questId,Quest quest, bool ignoreDisgenerate = false)
        {
            List<Pawn> result= new List<Pawn>();
            Dictionary<Faction, Lord> lords = new Dictionary<Faction, Lord>();
            foreach (KeyValuePair<IntVec3, List<PawnSpawnData>> content in def.pawns)
            {
                Faction faction = null;
                foreach (PawnSpawnData data in content.Value)
                {
                    Lord lord = null;
                    List<Pawn> pawns = new List<Pawn>();
                    IntVec3 pos = center + content.Key;
                    if (!pos.InBounds(map))
                    {
                        continue;
                    }
                    PawnSpawnData pawnData = data as PawnSpawnData;
                    if (pawnData != null && pawnData.lordDataName != null && lordsWithName.ContainsKey(pawnData.lordDataName))
                    {
                        lord = lordsWithName[pawnData.lordDataName];
                    }
                    else if (pawnData != null)
                    {
                        if (faction == null && pawnData.faction != null && GameTools.GetFaction(pawnData.faction, map) != null)
                        {
                            faction = GameTools.GetFaction(pawnData.faction,map);
                        }
                        if (faction != null && pawnData.enableLord)
                        {
                            if (lords.ContainsKey(faction))
                            {
                                lord = lords[faction];
                            }
                            else
                            {
                                lord = LordMaker.MakeNewLord(faction, new LordJob_Custom(), map);
                                QuestUtility.AddQuestTag(ref lord.questTags, "Quest" + questId);
                                lords.Add(faction, lord);
                            }
                        }
                    }
                    List<Pawn> ps = new List<Pawn>();
                    if (lord != null)
                    {
                        Dictionary<string, TargetInfo> ts = data.Spawn(pos, map, "Quest" + questId, quest, lord);
                        ts.ToList().ForEach(p => ps.Add(p.Value.Thing as Pawn));
                        if (lordsWithTarget.TryGetValue(lord, out Dictionary<string, TargetInfo> ts2))
                        {
                            ts.ToList().ForEach(t => 
                            {
                                if (!ts2.ContainsKey(t.Key)) 
                                {
                                    ts2.SetOrAdd(t.Key,t.Value);
                                }
                            });
                        }
                        else 
                        {
                            lordsWithTarget.SetOrAdd(lord,ts);
                        }
                        result.AddRange(ps);
                        pawns.AddRange(ps);
                    }
                    else
                    {
                        data.Spawn(pos, map, "Quest" + questId, quest).ToList().ForEach(p =>
                        ps.Add(p.Value.Thing as Pawn));
                        result.AddRange(ps);
                        pawns.AddRange(ps);
                    }

                    if (pawnData != null && lord != null && lord.LordJob is LordJob_Custom lordJob)
                    {
                        if (pawnData.duty == QEDefOf.QE_Duty_Guard)
                        {
                            if (def.routes.TryGetValue(pawnData.routeName, out List<IntVec3> route))
                            {
                                List<IntVec3> route2 = new List<IntVec3>();
                                route.ForEach((x) => route2.Add(x + center));
                                pawns.ForEach((x) =>
                                {
                                    lordJob.pawnRouteDatas.SetOrAdd(x,new RouteData(route2));
                                });
                            }
                            else
                            {
                                Log.Error("null route");
                            }
                        }
                        if (pawnData.duty == QEDefOf.QE_Duty_Waiter || pawnData.duty == DutyDefOf.Defend) 
                        {
                            pawns.ForEach((x) =>
                            {
                                lordJob.pawnRouteDatas.SetOrAdd(x,new RouteData(new List<IntVec3>() { pos }));
                            });
                        }
                        pawns.ForEach((x) =>
                        {
                            lordJob.pawnDutyDatas.SetOrAdd(x, pawnData.duty);
                        });
                    }
                }
            }
            return result;
        }

        public static void PostGenerateMap(Quest quest) 
        {
            lordsWithName.ToList().ForEach(l => lordsWithData[l.Value].actions?.ForEach(a => a.WorkForLord(lordsWithTarget[l.Value], quest, l.Value)));
        }

        public static List<Thing> SpawnThings(Map map, GenStepParams parms, CustomMapDataDef def, IntVec3 center, bool load = false, bool ignoreDisgenerate = false)
        {
            List<Thing> result = new List<Thing>();
            foreach (ThingData content in def.thingDatas)
            {
                List<IntVec3> poss = new List<IntVec3>();
                bool enablePosition = !content.allPositions.Any() && !content.allRect.Any();
                if (content.allPositions.Any())
                {
                    content.allPositions.ForEach(pos => poss.Add(pos));
                }
                if (content.allRect.Any())
                {
                    content.allRect.ForEach(rect => poss.AddRange(rect.Cells));
                }
                if(enablePosition)
                {
                    poss.Add(content.position);
                }
                poss.ForEach(p =>
                {
                    IntVec3 intVec3 = center + p;
                    if (intVec3.InBounds(map))
                    {
                       Thing t = content.Spawn(map, intVec3, (d,s) =>
      {
          return load ? d : GenStep_CustomMap.GetDef(d,def,s);
      }
      );
                        result.Add(t);
                        if (t != null && t.def.CanHaveFaction && t.Faction == null && faction != null)
                        {
                            t.SetFaction(faction);
                        }
                    }
                });
            }
            return result;
        }
        private static ThingDef GetDef(ThingDef def,CustomMapDataDef map,bool isStuff)
        {
            if (def == null || map ==null) 
            {
                return def;
            }
            if (replaceData == null && map.replaces != null && map.replaces.Any()) 
            {
                replaceData = map.replaces.RandomElement();
                replaceData.Init();
            }
            if (replaceData != null) 
            {
                return isStuff ? replaceData.ReplaceStuff(def) : replaceData.ReplaceThing(def);
            }
            return def;
        }
        private static TerrainDef GetTerrainDef(TerrainDef def, CustomMapDataDef map)
        {
            if (def == null || map == null)
            {
                return def;
            }
            if (replaceData == null && map.replaces != null && map.replaces.Any())
            {
                replaceData = map.replaces.RandomElement();
                replaceData.Init();
            }
            if (replaceData != null)
            {
                return replaceData.ReplaceTerrain(def);
            }
            return def;
        }
        private static void SetTerrainSafely(Map map, IntVec3 cell, TerrainDef terrain)
        {
            if (terrain == null || !cell.InBounds(map))
            {
                return;
            }
            if (terrain.isFoundation && map.terrainGrid.UnderTerrainAt(cell) != null)
            {
                map.terrainGrid.RemoveTopLayer(cell, false);
            }
            map.terrainGrid.SetTerrain(cell, terrain);
        }
        public static void SetRoofAndTerrain(Map map, CustomMapDataDef def, IntVec3 center,bool ignoreDisgenerate = false)
        {
            foreach (KeyValuePair<string, List<IntVec3>> content in def.terrains)
            {
                TerrainDef terrain = GetTerrainDef(TerrainDef.Named(content.Key), def);
                content.Value.ForEach(x =>
                {
                    if ((x + center).InBounds(map))
                    {
                        SetTerrainSafely(map, x + center, terrain);
                    }
                });
            }
            foreach (KeyValuePair<string, List<CellRect>> content in def.terrainsRect)
            {
                TerrainDef terrain = GetTerrainDef(TerrainDef.Named(content.Key), def);
                content.Value.ForEach(x =>
                {
                    foreach (var item in x.Cells.ToList())
                    {
                        if ((item + center).InBounds(map))
                        {
                            SetTerrainSafely(map, item + center, terrain);
                        }
                    }
                });
            }
            foreach (var content in def.terrainsColorRect)
            { 
                content.Value.ForEach(x =>
                {
                    foreach (var item in x.Cells.ToList())
                    {
                        if ((item + center).InBounds(map))
                        {
                            map.terrainGrid.SetTerrainColor(item + center,content.Key);
                        }
                    }
                });
            }
            foreach (KeyValuePair<RoofDef, List<IntVec3>> content in def.roofs)
            {
                content.Value.ForEach(x =>
                {
                    if ((x + center).InBounds(map))
                    {
                        map.roofGrid.SetRoof(x + center, content.Key);
                    }
                });
            }
            foreach (KeyValuePair<RoofDef, List<CellRect>> content in def.roofRects)
            { 
                content.Value.ForEach(x =>
                {
                    foreach (var item in x.Cells.ToList())
                    {
                        if ((item + center).InBounds(map))
                        {
                            map.roofGrid.SetRoof(item + center, content.Key);
                        }
                    }
                });
            }
        }

        private static void Pretreat(Map map, CustomMapDataDef def, IntVec3 center,
            bool isGenerateByCore, List<IntVec3> disgenerate2, 
            bool destroyThings = false, Func<Thing, bool> validator = null)
        {
            CellRect rect = def.GetRect(center);
            List<IntVec3> poss = rect.Cells.ToList();
            HashSet<IntVec3> customMapCells = new HashSet<IntVec3>(def.GetAllPosition().Select(p => p + center));
            List<Thing> things = new List<Thing>();
            if (map.fogGrid == null)
            {
                map.ConstructComponents();
            }
            poss.ForEach(x =>
            {
                if (x.InBounds(map) && customMapCells.Contains(x) && !def.disdestroy.Contains(x - center))
                {
                    if (!isGenerateByCore && destroyThings) 
                    {
                        map.roofGrid.SetRoof(x, null);
                    }
                    things.AddRange(x.GetThingList(map).ListFullCopy().FindAll(t => validator == null || validator(t)));
                    if (isGenerateByCore)
                    {
                        disgenerate2.Add(x);
                    }
                }
            });
            if (!isGenerateByCore || destroyThings)
            {
                while (things.Count != 0)
                {
                    Thing thing = things[0];
                    if (thing.def.destroyable && !thing.Destroyed && !(thing is CustomMapExit)
                        && !(thing is Skyfaller))
                    {
                        thing.Destroy();
                    }
                    if (things.Contains(thing))
                    {
                        things.Remove(thing);
                    }
                }
            }
        }

        private static void Fog(Map map, CustomMapDataDef def, IntVec3 center, bool isGenerateByCore, bool isSubMap = false)
        {
            if (isSubMap)
            {
                return;
            }
            map.fogGrid.Refog(CellRect.WholeMap(map)); 
            List<IntVec3> rootsToUnfog = MapGenerator.rootsToUnfog;
            for (int i = 0; i < rootsToUnfog.Count; i++)
            {
                FloodFillerFog.FloodUnfog(rootsToUnfog[i], map);
                map.fogGrid.Unfog(rootsToUnfog[i]);
            }
            UnfogMapFromEdge(map);
            if (!def.fogged)
            {
                // CellRect.FromLimits 包含末端坐标；地图尺寸是格数，因此末端必须减一。
                var end = center + new IntVec3(def.size.x - 1, def.size.y - 1, def.size.z - 1);
                end.y = 1;
                foreach (var c in CellRect.FromLimits(center,end).Cells)
                {
                    map.fogGrid.Unfog(c);    
                }
            }
            if (Current.ProgramState == ProgramState.Playing)
            {
                map.roofGrid.Drawer.SetDirty();
            } 
        }
        private static void UnfogMapFromEdge(Map map)
        {
            Predicate<IntVec3> validator = (IntVec3 c) => c.Standable(map) && !c.Roofed(map) && 
                                                          map.reachability.CanReachMapEdge(c, 
                                                              TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater, Danger.Deadly, false, false, false, true, false));
            IntVec3 root;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 30, validator, out root, -1) && !CellFinder.TryFindRandomEdgeCellWith(validator, map, 0f, out root) && !CellFinder.TryFindRandomCell(map, validator, out root))
            {
                return;
            }
            FloodFillerFog.FloodUnfog(root, map);
        }
        public static Faction faction;
        public static List<IntVec3> disgenerate = new List<IntVec3>();
        public static ReplaceData replaceData = null;
        public static Dictionary<CustomMapDataDef, int> generatedCount = new Dictionary<CustomMapDataDef, int>();
        public static Dictionary<string, int> generatedLimit_Key = new Dictionary<string, int>();
        public static Dictionary<string, int> generatedCount_Key = new Dictionary<string, int>();
        public static Dictionary<string, Lord> lordsWithName = new Dictionary<string, Lord>();
        public static Dictionary<Lord, LordData> lordsWithData = new Dictionary<Lord, LordData>();
        public static Dictionary<Lord, Dictionary<string, TargetInfo>> lordsWithTarget = new Dictionary<Lord, Dictionary<string, TargetInfo>>();

        public static List<ExecutiveRequest> requests = new List<ExecutiveRequest>();
    }
}



