using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Noise;

namespace QuestEditor_Library
{
    public class CustomMapDataDef : Def, ISaveable
    {
        public CustomMapDataDef() { }
        public CustomMapDataDef Origin => this.origin == null ? this : this.origin;
        public List<Thing> Generate(IntVec3 center, Map map, Quest quest, bool load = false, bool debug = false, bool destroyThings = false, bool ignoreDisgenerate = false)
        {
            try
            {
                return GenStep_CustomMap.SpawnCustomMap(map, new GenStepParams(), this, quest, load, center, false, true, debug,
                    destroyThings: destroyThings || this.destroyAllThing,
                    ignoreDisgenerate: ignoreDisgenerate);
            }
            catch (Exception e)
            {
                Log.Error($"Generate map part error:Mappart={this.defName},Center={center.ToString()},{e.Message}");
            }
            //if (Prefs.DevMode) 
            //{
            //    StringBuilder test = new StringBuilder();
            //    test.AppendLine(this.defName);
            //    test.AppendLine(this.rot.ToStringHuman());
            //    this.thingDatas.ForEach(t => test.AppendLine(t.def.label + t.position));
            //    this.customThings.ForEach(t => test.AppendLine(t.def.label + t.position));
            //    Log.Message(test.ToString().Trim());
            //}

            return null;
        }
        public List<Thing> GenerateByCore(IntVec3 center, Map map, Quest quest, bool load = false, bool debug = false, bool destroyThings = false, bool ignoreDisgenerate = false)
        {
            try
            {
                return GenStep_CustomMap.SpawnCustomMap(map, new GenStepParams(), this, quest, load, center, true, true, debug,
                    destroyThings: destroyThings,
                    ignoreDisgenerate: ignoreDisgenerate);
            }
            catch (Exception e)
            {
                Log.Error($"Generate map part error:Mappart={this.defName},Center={center.ToString()},{e.Message}");
            }
            //if (Prefs.DevMode) 
            //{
            //    StringBuilder test = new StringBuilder();
            //    test.AppendLine(this.defName);
            //    test.AppendLine(this.rot.ToStringHuman());
            //    this.thingDatas.ForEach(t => test.AppendLine(t.def.label + t.position));
            //    this.customThings.ForEach(t => test.AppendLine(t.def.label + t.position));
            //    Log.Message(test.ToString().Trim());
            //}

            return null;
        }
        public void GenerateAsSubmap(Map parent,IntVec3 pos,string questID,CustomMapEntrance entrance)
        {
            MapGenerator.PlayerStartSpot = pos;
            MapParent_Custom custom
                = (MapParent_Custom)WorldObjectMaker.MakeWorldObject(parent.Tile.Layer.Def.isSpace
                ? QEDefOf.QE_CustomMap_SpaceSubMap
                : QEDefOf.QE_CustomMap_SubMap);
            custom.mapDataDef = this;
            custom.quest = Find.QuestManager.QuestsListForReading.Find(q => q.id.ToString() == questID);
            custom.level = 1;
            if (parent.Parent is MapParent_Custom mapParent)
            {
                custom.level += mapParent.level;
                custom.rootSite = mapParent.rootSite;
            }
            custom.SetFaction(Find.FactionManager.OfPlayer);
            custom.entrance = entrance;
            if (parent.Parent is CustomSite site)
            {
                custom.rootSite = site;
            }
            if (custom.rootSite != null)
            {
                custom.rootSite.allSubMaps.Add(custom);
            }
            parent.GetComponent<MapComponent_CustomMapData>().AddSubMap(custom);
            custom.Tile = parent.Tile;
            string seed = Find.World.info.seedString;
            Find.World.info.seedString = Find.TickManager.TicksGame.ToString();
            LongEventHandler.SetCurrentEventText("GenerateSubMap".Translate());
            DeepProfiler.Start("Generate map");
            Map customMap = GameTools.GenerateSubMap(this.size, custom,
                this.generator ?? custom.def.mapGenerator, this.GetSteps(questID,pos), parent);
            QuestUtility.AddQuestTag(ref customMap.Parent.questTags, "Quest" + questID + "." + this.defName);
            QuestUtility.AddQuestTag(ref customMap.Parent.questTags, "Quest" + questID + "." + custom.level);
            Find.World.info.seedString = seed;
            DeepProfiler.End();
        }
        public IEnumerable<GenStepWithParams> GetSteps(string questID,IntVec3 pos)
        {
            yield return new GenStepWithParams(QEDefOf.QE_CustomSite_GenStep, new GenStepParams()
            {
                sitePart = new SitePart(null, QEDefOf.QE_CustomSite, new CustomSitePartParams
                {
                    mapData = this,
                    quest = questID != null ? Find.QuestManager.QuestsListForReading.Find(q => q.id.ToString() == questID) : null,
                    spot = pos,
                    isSubMap = true
                })
            });
            // yield return new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("Fog"), new GenStepParams());
            yield break;
        }
        public CustomMapDataDef GetDataByCore(CustomThingData_ZoneCore coreData)
        {
            CustomMapDataDef resolved = this.GetNewDataUseNewOrigih(coreData.position, coreData.coreRotation);
            resolved = resolved.GetRotated(coreData.coreRotation.Opposite);
            return resolved;
        }
        public CellRect GetRect(IntVec3 pos)
        {
            List<IntVec3> cells = this.GetAllPosition();
            int? minX = null;
            int? maxX = null;
            int? minZ = null;
            int? maxZ = null;
            cells.ToList().ForEach((c) =>
            {
                maxX = maxX != null && c.x < maxX ? maxX : c.x;
                minX = minX != null && c.x > minX ? minX : c.x;
                minZ = minZ != null && c.z > minZ ? minZ : c.z;
                maxZ = maxZ != null && c.z < maxZ ? maxZ : c.z;
            });
            CellRect result = CellRect.FromLimits(pos.x + minX.Value, pos.z + minZ.Value, pos.x + maxX.Value, pos.z + maxZ.Value);
            return result;
        }
        public List<IntVec3> GetAllPosition()
        {
            List<IntVec3> result = new List<IntVec3>();
            Action<IntVec3> add = p =>
            {
                if (!result.Contains(p))
                {
                    result.Add(p);
                }
            };
            this.customThings.ForEach(t => add(t.position));
            this.thingDatas.ForEach(d =>
            {
                add(d.position);
                d.allPositions.ForEach(add);
                d.allRect.ForEach(d2 => d2.Cells.ToList().ForEach(d3 => add(d3)));
            });
            this.zoneCores.ForEach(d => add(d.position));
            this.pawns.Keys.ToList().ForEach(p => add(p));
            this.specialSpawnPawns.Keys.ToList().ForEach(p => add(p));
            this.enterSpots.ForEach(add);
            this.routes.Values.ToList().ForEach(p => p.ForEach(p2 => add(p2)));
            this.terrains.Values.ToList().ForEach(p => p.ForEach(p2 => add(p2)));
            this.terrainsRect.Values.ToList().ForEach(p => p.ForEach(p2 => p2.Cells.ToList().ForEach(p3 => 
            add(p3))));
            this.terrainsColorRect.Values.ToList().ForEach(p => p.ForEach(p2 => p2.Cells.ToList().ForEach(p3 =>
add(p3))));
            this.roofs.Values.ToList().ForEach(p => p.ForEach(p2 => add(p2)));
            this.roofRects.Values.ToList().ForEach(p => p.ForEach(p2 => p2.Cells.ToList().ForEach(p3 =>
add(p3))));
            return result;
        }
        public CustomMapDataDef GetRotated(Rot4 rot)
        {
            if (!rot.IsValid || !this.rot.IsValid)
            {
                Log.Message("Invalid rotation:" + this.ToString());
                return null;
            }
            if (rot == this.rot)
            {
                return this;
            }
            if (this.extraDataByDirection.ContainsKey(rot))
            {
                return this.extraDataByDirection[rot];
            }
            CustomMapDataDef result = this.Copy(rot.ToStringWord());
            RotationDirection direction = Rot4.GetRelativeRotation(result.rot, rot);
            result.thingDatas.ForEach(d =>
            {
                if (d.def.rotatable)
                {
                    d.rotation.Rotate(direction);
                }
            });
            if (result.enterDirection.IsValid)
            {
                result.enterDirection.Rotate(direction);
            }

            result.customThings.ForEach(t =>
            {
                if (t.def.rotatable)
                {
                    t.rotation.Rotate(direction);
                }
            });
            result.zoneCores.ForEach(c => 
            {
                if (c is CustomThingData_ZoneCore core)
                {
                    //if (Prefs.DevMode)
                    //{
                    //    Log.Message($"{core.size.ToStringHuman()}");
                    //    Log.Message($"核心方向：{core.coreRotation.ToStringHuman()}");
                    //}
                    core.coreRotation.Rotate(direction);
                    core.size.Rotate(this.rot, direction);

                    //if (Prefs.DevMode)
                    //{
                    //    Log.Message($"{core.size.ToStringHuman()}");
                    //    Log.Message($"转换至{core.coreRotation.ToStringHuman()}，源于{result.rot.ToStringHuman()}至{rot.ToStringHuman()}");
                    //}
                }
            });
            switch (direction)
            {
                case RotationDirection.Opposite: result.OppositeRotate(result.rot); break;
                case RotationDirection.Counterclockwise: result.CounterclockwiseRotate(result.rot); break;
                case RotationDirection.Clockwise: result.ClockwiseRotate(result.rot); break;
                default:; break;
            }
            result.rot = rot;
            this.extraDataByDirection.Add(rot, result);
            return result;
        }
        public void CounterclockwiseRotate(Rot4 rot)
        {
            this.ChangePosition(new IntVec3(-1, 1, 1), (v, p) => new IntVec3(-p.z, p.y, p.x));
            this.size = new IntVec3(this.size.z, 1, this.size.x);
        }
        public void ClockwiseRotate(Rot4 rot)
        {
            this.ChangePosition(new IntVec3(-1, 1, 1), (v, p) => new IntVec3(p.z, p.y, -p.x));
            this.size = new IntVec3(this.size.z, 1, this.size.x);
        }
        public void OppositeRotate(Rot4 rot)
        {
            this.ChangePosition(new IntVec3(-1, 1, -1),
                (v, p) => new IntVec3(-p.x, p.y, -p.z));
        }
        public CustomMapDataDef GetNewDataUseNewOrigih(IntVec3 posInMap, Rot4 rot)
        {
            if (this.extraDataByOrigin.ContainsKey(posInMap))
            {
                return this.extraDataByOrigin[posInMap];
            }
            CustomMapDataDef result = this.Copy(posInMap.ToString());
            result.zoneCores.Remove(result.zoneCores.Find(t => t is CustomThingData_ZoneCore && t.position == posInMap));

            result.ChangePosition(posInMap, (variable, position) => position - variable);
            result.rot = rot;
            this.extraDataByOrigin.Add(posInMap, result);
            return result;
        }
        private void ChangePosition(IntVec3 variable, Func<IntVec3, IntVec3, IntVec3> ChangingAction)
        {
            this.thingDatas.ForEach(d =>
            {
                d.position = ChangingAction(variable, d.position);
                List<IntVec3> thingPoss = new List<IntVec3>();
                List<IntVec3> thingPoss_Changed = new List<IntVec3>();
                d.allRect.ForEach(rect => thingPoss.AddRange(rect.Cells));
                thingPoss.AddRange(d.allPositions);
                d.allRect.Clear();
                d.allPositions.Clear();
                thingPoss.ForEach(c => thingPoss_Changed.Add(ChangingAction(variable, c)));
                d.allRect = this.GetRect(thingPoss_Changed);
            });
            this.generationActions.ForEach(d =>
            {
                d.pos = ChangingAction(variable, d.pos);
            });
            this.customThings.ForEach(t => t.position = ChangingAction(variable, t.position));
            this.zoneCores.ForEach(t => t.position = ChangingAction(variable, t.position));
            List<IntVec3> cells = new List<IntVec3>();
            this.disdestroy.ForEach(t => cells.Add(ChangingAction(variable, t)));
            this.disdestroy = cells;
            foreach (KeyValuePair<string, List<IntVec3>> terrain in this.terrains.ToList().ListFullCopy())
            {
                List<IntVec3> poss = new List<IntVec3>();
                terrain.Value.ForEach(t => poss.Add(ChangingAction(variable, t)));
                this.terrains[terrain.Key] = poss;
            }
            foreach (KeyValuePair<string, List<CellRect>> terrain in this.terrainsRect.ToList().ListFullCopy())
            {
                List<IntVec3> poss = new List<IntVec3>();

                terrain.Value.ForEach(t => t.Cells.ToList().ForEach(t2 =>
                poss.Add(ChangingAction(variable, t2))));
                this.terrainsRect[terrain.Key] = this.GetRect(poss);
            }
            foreach (var terrain in this.terrainsColorRect.ToList().ListFullCopy())
            {
                List<IntVec3> poss = new List<IntVec3>();

                terrain.Value.ForEach(t => t.Cells.ToList().ForEach(t2 =>
                poss.Add(ChangingAction(variable, t2))));
                this.terrainsColorRect[terrain.Key] = this.GetRect(poss);
            }
            foreach (KeyValuePair<RoofDef, List<IntVec3>> roof in this.roofs.ToList().ListFullCopy())
            {
                List<IntVec3> poss = new List<IntVec3>();
                roof.Value.ForEach(t => poss.Add(ChangingAction(variable, t)));
                this.roofs[roof.Key] = poss;
            }
            foreach (KeyValuePair<RoofDef, List<CellRect>> roof in this.roofRects.ToList().ListFullCopy())
            {
                List<IntVec3> poss = new List<IntVec3>();

                roof.Value.ForEach(t => t.Cells.ToList().ForEach(t2 =>
                poss.Add(ChangingAction(variable, t2))));
                this.roofRects[roof.Key] = this.GetRect(poss);
            }
            foreach (KeyValuePair<string, List<IntVec3>> route in this.routes.ToList().ListFullCopy())
            {
                List<IntVec3> poss = new List<IntVec3>();
                route.Value.ForEach(t => poss.Add(ChangingAction(variable, t)));
                this.routes[route.Key] = poss;
            }
            List<KeyValuePair<IntVec3, List<PawnSpawnData>>> pawns = this.pawns.ToList().ListFullCopy();
            this.pawns.Clear();
            foreach (KeyValuePair<IntVec3, List<PawnSpawnData>> pawn in pawns)
            {
                this.pawns.Add(ChangingAction(variable, pawn.Key), pawn.Value);
            }
            List<KeyValuePair<IntVec3, List<PawnSpawnData>>> specialSpawnPawns = this.specialSpawnPawns.ToList().ListFullCopy();
            this.specialSpawnPawns.Clear();
            foreach (KeyValuePair<IntVec3, List<PawnSpawnData>> pawn in specialSpawnPawns)
            {
                this.specialSpawnPawns.Add(ChangingAction(variable, pawn.Key), pawn.Value);
            }
            List<IntVec3> enterSpots = new List<IntVec3>();
            this.enterSpots.ForEach(t => enterSpots.Add(ChangingAction(variable, t)));
            this.enterSpots = enterSpots;
        }
        public CustomMapDataDef Copy(string name)
        {
            CustomMapDataDef result = new CustomMapDataDef();
            result.defName = this.defName + name;
            result.label = this.label;
            result.description = this.description;
            result.size = this.size;
            result.isPart = this.isPart;
            result.destroyAllThing = this.destroyAllThing;
            result.fogged = this.fogged;
            result.tags = this.tags.ListFullCopy();
            result.rot = this.rot;
            result.customThings.Clear();
            result.faction = this.faction;
            result.commonality = this.commonality;
            this.customThings.ListFullCopy().ForEach(t => result.customThings.Add(t.Copy()));
            this.zoneCores.ListFullCopy().ForEach(t => result.zoneCores.Add(t.Copy()));
            this.thingDatas.ForEach(d => result.thingDatas.Add(d.Copy()));
            result.terrains = new Dictionary<string, List<IntVec3>>();
            this.terrains.ToList().ForEach(t => result.terrains.Add(t.Key, t.Value));
            result.terrainsRect = new Dictionary<string, List<CellRect>>();
            this.terrainsRect.ToList().ForEach(t => result.terrainsRect.Add(t.Key, t.Value));
            result.terrainsColorRect = new Dictionary<ColorDef, List<CellRect>>();
            this.terrainsColorRect.ToList().ForEach(t => result.terrainsColorRect.Add(t.Key, t.Value));
            result.roofs = new Dictionary<RoofDef, List<IntVec3>>();
            this.roofs.ToList().ForEach(t => result.roofs.Add(t.Key, t.Value));
            result.roofRects = new Dictionary<RoofDef, List<CellRect>>();
            this.roofRects.ToList().ForEach(t => result.roofRects.Add(t.Key, t.Value));
            result.pawns = new Dictionary<IntVec3, List<PawnSpawnData>>();
            this.pawns.ToList().ForEach(t => result.pawns.Add(t.Key, t.Value));
            result.specialSpawnPawns = new Dictionary<IntVec3, List<PawnSpawnData>>();
            this.specialSpawnPawns.ToList().ForEach(t => result.specialSpawnPawns.Add(t.Key, t.Value));
            result.enterSpots = this.enterSpots.ListFullCopy();
            result.enterDirection = this.enterDirection;
            result.routes = new Dictionary<string, List<IntVec3>>();
            this.routes.ToList().ForEach(t => result.routes.Add(t.Key, t.Value));
            result.replaces = new List<ReplaceData>();
            this.replaces.ToList().ForEach(t => result.replaces.Add(t));
            result.generationActions = new List<GenerationAction>();
            this.generationActions.ToList().ForEach(t => result.generationActions.Add(t));
            result.preCustomSteps = new List<CustomMapStep>();
            this.preCustomSteps.ForEach(t => result.preCustomSteps.Add(t));
            result.customSteps = new List<CustomMapStep>();
            this.customSteps.ForEach(t => result.customSteps.Add(t));
            result.origin = this.Origin;
            return result;
        }
        public void LoadData(Map map)
        {
            if (map == null)
            {
                return;
            }
            IntVec3 centre = IntVec3.Zero;
            this.size = map.Size;
            if (map.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData component && component.route != null)
            {
                foreach (KeyValuePair<string, Route> route in component.route)
                {
                    List<IntVec3> routePoss = new List<IntVec3>();
                    foreach (IntVec3 pos in route.Value.route)
                    {
                        routePoss.Add(pos - centre);
                    }
                    this.routes.Add(route.Key, routePoss);
                }
            }
            this.SaveThings(centre, map.listerThings.AllThings, map.AllCells.ToList());
            SavePoss(map, map.AllCells.ToList(), centre);
            map.designationManager.designationsByDef[QEDefOf.QE_Disgenerate].ForEach(d => this.disgenerate.Add(d.target.Cell - centre));
            map.designationManager.designationsByDef[QEDefOf.QE_Disdestroy].ForEach(d => this.disdestroy.Add(d.target.Cell - centre));
        }
        public void LoadData(Map map, List<IntVec3> poss, IntVec3 size)
        {
            int? minX = null;
            int? minZ = null;
            poss.ToList().ForEach((c) =>
            {
                minX = minX != null && c.x > minX ? minX : c.x;
                minZ = minZ != null && c.z > minZ ? minZ : c.z;
            });
            IntVec3 centre = new IntVec3(minX.Value, 0, minZ.Value);
            this.size = size;
            List<Thing> things = new List<Thing>();
            poss.ForEach(p =>
            {
                if (p.InBounds(map))
                {
                    p.GetThingList(map).ForEach(t =>
                    {
                        if (!things.Contains(t))
                        {
                            things.Add(t);
                        }
                    });
                }
            });
            this.SaveThings(centre, things, poss);
            this.SavePoss(map, poss, centre);
            if (map.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData component && component.route != null)
            {
                foreach (KeyValuePair<string, Route> route in component.route)
                {
                    List<IntVec3> routePoss = new List<IntVec3>();
                    foreach (IntVec3 pos in route.Value.route)
                    {
                        routePoss.Add(pos - centre);
                    }
                    this.routes.Add(route.Key, routePoss);
                }
            }
            map.designationManager.designationsByDef[QEDefOf.QE_Disgenerate].ForEach(d => this.disgenerate.Add(d.target.Cell - centre));
            map.designationManager.designationsByDef[QEDefOf.QE_Disdestroy].ForEach(d => this.disdestroy.Add(d.target.Cell - centre));
        }
        private void SavePoss(Map map, List<IntVec3> poss, IntVec3 centre)
        {
            var colors = new Dictionary<ColorDef,List<IntVec3>>();
            foreach (IntVec3 pos in poss)
            {
                IntVec3 savePos = pos - centre;
                if (map.roofGrid.Roofed(pos))
                {
                    RoofDef roofDef = map.roofGrid.RoofAt(pos);
                    if (this.roofs.TryGetValue(roofDef, out List<IntVec3> list))
                    {
                        list.Add(savePos);

                    }
                    else
                    {
                        this.roofs.Add(roofDef, new List<IntVec3>() { savePos });
                    }
                }
                if (pos.GetTerrain(map) is TerrainDef def && def.defName != "QE_Null")
                {
                    if (this.terrains.TryGetValue(def.defName, out List<IntVec3> list))
                    {
                        list.Add(savePos);
                    }
                    else
                    {
                        this.terrains.Add(def.defName, new List<IntVec3>() { savePos });
                    }
                } 
                if (map.terrainGrid.ColorAt(pos) is ColorDef color) 
                { 
                    if (colors.TryGetValue(color, out List<IntVec3> list))
                    {
                        list.Add(savePos);
                    }
                    else
                    {
                        colors.Add(color, new List<IntVec3>() { savePos });
                    }
                }
            }
            this.terrains.ToList().ForEach(t => 
            {
                List<CellRect> rect = this.GetRect(t.Value.ListFullCopy());
                this.terrainsRect.SetOrAdd(t.Key,rect);
            });
            colors.ToList().ForEach(t =>
            { 
                List<CellRect> rect = this.GetRect(t.Value.ListFullCopy());
                this.terrainsColorRect.SetOrAdd(t.Key, rect);
            });
            this.terrains.Clear();
            this.roofs.ToList().ForEach(t =>
            {
                List<CellRect> rect = this.GetRect(t.Value.ListFullCopy());
                this.roofRects.SetOrAdd(t.Key, rect);
            });
            this.roofs.Clear();
            map.designationManager.designationsByDef[QEDefOf.QE_Disdestroy].ForEach(d =>
            {
                if (poss.Contains(d.target.Cell))
                {
                    this.disdestroy.Add(d.target.Cell - centre);
                }
            });
        }
        private void SaveThings(IntVec3 centre, List<Thing> things, List<IntVec3> rect)
        {
            List<ThingData> datas = new List<ThingData>();
            foreach (Thing thing in things)
            {
                if (thing is Pawn || thing is Gas)
                {
                    continue;
                }
                IntVec3 savePos = thing.Position - centre;

                if (thing is ZoneCore core)
                {
                    CustomThingData_ZoneCore dataCore = new CustomThingData_ZoneCore(core, savePos);
                    int? minX = null;
                    int? maxX = null;
                    int? minZ = null;
                    int? maxZ = null;
                    rect.ForEach((c) =>
                    {
                        IntVec3 curPos = c - centre;
                        minX = minX != null && savePos.x - curPos.x < minX ? minX : savePos.x - curPos.x;
                        maxX = maxX != null && curPos.x - savePos.x < maxX ? maxX : curPos.x - savePos.x;
                        minZ = minZ != null && savePos.z - curPos.z < minZ ? minZ : savePos.z - curPos.z;
                        maxZ = maxZ != null && curPos.z - savePos.z < maxZ ? maxZ : curPos.z - savePos.z;
                    });
                    dataCore.size = new CoreSize(minX.Value, minZ.Value, maxX.Value, maxZ.Value);
                    this.zoneCores.Add(dataCore);
                    continue;
                }
                if (thing is Spawner spawner)
                {
                    List<PawnSpawnData> pawns = new List<PawnSpawnData>();
                    List<PawnSpawnData> specialPawns = new List<PawnSpawnData>();
                    foreach (PawnSpawnData pawnKinds in spawner.pawns)
                    {
                        if (pawnKinds == null || !pawnKinds.CanSaveToMap())
                        {
                            continue;
                        }
                        if (pawnKinds.spawnType == SpawnType.MapGeneration)
                        {
                            pawns.Add(pawnKinds);
                        }
                        else
                        {
                            specialPawns.Add(pawnKinds);
                        }
                    }
                    if (specialPawns.Any())
                    {
                        if (this.specialSpawnPawns.TryGetValue(savePos, out List<PawnSpawnData> pawnDatas))
                        {
                            pawnDatas.AddRange(pawns);
                            continue;
                        }
                        this.specialSpawnPawns.Add(savePos, specialPawns);
                    }
                    if (pawns.Any())
                    {
                        if (this.pawns.TryGetValue(savePos, out List<PawnSpawnData> pawnDatas))
                        {
                            pawnDatas.AddRange(pawns);
                            continue;
                        }
                        this.pawns.Add(savePos, pawns);
                    }
                    continue;
                }
                if (thing is CustomMapEnterSpot)
                {
                    this.enterSpots.Add(savePos);
                    continue;
                }
                if (thing is GenerationActionWorker worker) 
                {
                    this.generationActions.Add(new GenerationAction(savePos,worker.actions));
                    continue;
                }
                if (thing is ICustomThing customThing)
                {
                    this.customThings.Add(customThing.GetData(savePos));
                    continue;
                }
                if (thing.TryGetComp <CompActionWorker >() != null || thing.TryGetComp<CompCustomText>() !=null)
                {
                    this.customThings.Add(new CustomThingData(thing,savePos));
                    continue;
                }
                ThingData data = new ThingData(thing, savePos);
                if (datas.Find(d => d.Equals_Def(data)) is ThingData dataEqualed)
                {
                    dataEqualed.allPositions.Add(savePos);
                }
                else
                {
                    datas.Add(new ThingData(thing, savePos));
                }
            }
            datas.ForEach(d =>
            {
                if (d.allPositions.Any())
                {
                    d.allPositions.Add(d.position);
                    d.position = IntVec3.Zero;
                    d.allRect = this.GetRect(d.allPositions);
                    d.allPositions.Clear();
                    //用于标记是否启用allPositions
                }
                this.thingDatas.Add(d);
            });
        }
        public List<CellRect> GetRect(List<IntVec3> allPos) 
        {
            List<CellRect> result = new List<CellRect>();
            allPos.SortBy(p => p.DistanceTo(IntVec3.Zero));
            while (allPos.Any()) 
            {
                IntVec3 position = allPos.First();
                int x = position.x;
                int z = position.z;
                int curX = position.x;
                int curZ = position.z;
                while (true)
                {
                    if (allPos.Contains(position + IntVec3.East))
                    {
                        position.x++;
                        curX++;
                    }
                    else 
                    {
                        break;
                    }
                }
                bool unended = true;
                while (unended)
                {
                    for (int x1 = x; x1 <= curX; x1++)
                    {
                        if (!allPos.Contains(new IntVec3(x1, 0, curZ + 1)))
                        {
                            unended = false;
                            break;
                        }
                    }
                    if (unended)
                    {
                        curZ++; 
                    }
                }
                CellRect rect = new CellRect(x, z, curX - x + 1, curZ - z + 1);
                rect.Cells.ToList().ForEach(c => allPos.Remove(c));

                result.Add(rect);
            }
            return result;
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName", this.defName));
            result.Add(new XElement("label", this.label));
            if (!this.description.NullOrEmpty())
            {
                result.Add(new XElement("description", this.description));
            }
            if (this.fogged)
            {
                result.Add(new XElement("fogged", this.fogged));
            }
            result.Add(new XElement("size", this.size));
            if (this.isPart)
            {
                result.Add(new XElement("isPart", this.isPart));
            }
            if (!this.destroyAllThing)
            {
                result.Add(new XElement("destroyAllThing", this.destroyAllThing));
            }
            result.Add(new XElement("commonality", this.commonality));
            if (!this.faction.NullOrEmpty())
            {
                result.Add(new XElement("faction", this.faction));
            }
            if (this.generationLimit != 0)
            {
                result.Add(new XElement("generationLimit", this.generationLimit));
            }
            if (this.generator != null && this.generator != QEDefOf.QE_CustomMap_Editor_Generator)
            {
                result.Add(new XElement("generator", this.generator.defName));
            }
            if (this.replaces.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.replaces, "replaces"));
            }
            if (this.customThings.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.customThings, "customThings"));
            }
            if (this.specialSpawnPawns.Any())
            {
                result.Add(CQFEditorTools.SaveDictionary_Saveable_List(this.specialSpawnPawns, "specialSpawnPawns"));
            }
            if (this.pawns.Any())
            {
                result.Add(CQFEditorTools.SaveDictionary_Saveable_List(this.pawns, "pawns"));
            }
            if (this.enterSpots.Any())
            {
                result.Add(CQFEditorTools.SaveList(this.enterSpots, "enterSpots"));
            }
            if (this.enterDirection.IsValid)
            {
                result.Add(new XElement("enterDirection", this.enterDirection));
            }
            if (this.routes.Any())
            {
                result.Add(CQFEditorTools.SaveDictionary_List(this.routes, "routes"));
            }
            if (this.roofRects.Any())
            {
                result.Add(CQFEditorTools.SaveDictionary_List(this.roofRects, "roofRects"));
            }
            if (this.terrainsRect.Any())
            {
                result.Add(CQFEditorTools.SaveDictionary_List(this.terrainsRect, "terrainsRect"));
            }
            if (!this.terrainsColorRect.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveDictionary_List(this.terrainsColorRect, "terrainsColorRect"));
            }
            if (this.thingDatas.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.thingDatas, "thingDatas"));
            }
            if (this.zoneCores.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.zoneCores, "zoneCores"));
            }
            if (this.preCustomSteps.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.preCustomSteps, "preCustomSteps"));
            }
            if (this.customSteps.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.customSteps, "customSteps"));
            }
            if (this.tags.Any())
            {
                result.Add(CQFEditorTools.SaveList(this.tags, "tags"));
            }
            if (this.disgenerate.Any())
            {
                result.Add(CQFEditorTools.SaveList(this.disgenerate, "disgenerate"));
            }
            if (this.disdestroy.Any())
            {
                result.Add(CQFEditorTools.SaveList(this.disdestroy, "disdestroy"));
            }
            if (this.reserveThing != null && this.reserveThing.def != null)
            {
                result.Add(this.reserveThing.SaveToXElement("reserveThing"));
            }
            if (this.lordDatas != null && this.lordDatas.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.lordDatas, "lordDatas"));
            }
            if (this.generationActions != null && this.generationActions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.generationActions, "generationActions"));
            }
            if (this.mapPartGenerationLimit != null && this.mapPartGenerationLimit.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.mapPartGenerationLimit, "mapPartGenerationLimit"));
            }
            return result;
        }
        public string GetInformation()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("---");
            this.customThings.ForEach(t =>
            {
                if (t is CustomThingData_ZoneCore core)
                {
                    result.AppendLine(core.coreRotation.ToStringHuman());
                    result.AppendLine("核心大小" + core.size.ToStringHuman());
                    result.AppendLine(core.position.ToString());
                    result.AppendLine("---");
                }
            });
            result.AppendLine("---");
            this.thingDatas.ForEach(d => result.AppendLine(d.ToString()));
            result.AppendLine("---");
            return result.ToString().Trim();
        }

        public override void PostLoad()
        {
            base.PostLoad();
            if (!this.zoneCores.Any()) 
            {
                this.zoneCores.AddRange(this.customThings.FindAll(t => t is CustomThingData_ZoneCore));
            }
        }

        public void EnterCaravan(Caravan caravan, Map map, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop, bool draftColonists = false)
        {
            if (this.TryGetEnterSpot(map, out IntVec3 enterSpot))
            {
                CaravanEnterMapUtility.Enter(caravan, map, pawn => CellFinder.RandomSpawnCellForPawnNear(enterSpot, map), dropInventoryMode, draftColonists);
                return;
            }
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, dropInventoryMode, draftColonists);
        }

        public bool TryGetEnterSpot(Map map, out IntVec3 enterSpot)
        {
            if (map != null && !this.enterSpots.NullOrEmpty())
            {
                IntVec3 offset = IntVec3.Zero;
                if (!this.size.IsValid || map.Size != this.size)
                {
                    offset = map.Center - new IntVec3(this.size.x / 2, 0, this.size.z / 2);
                }
                List<IntVec3> localSpots = this.FilterEnterSpotsByDirection(this.enterSpots);
                List<IntVec3> spots = localSpots.Select(p => p + offset).Where(p => p.InBounds(map)).ToList();
                if (spots.Any())
                {
                    enterSpot = spots.RandomElement();
                    return true;
                }
            }
            enterSpot = IntVec3.Invalid;
            return false;
        }

        private List<IntVec3> FilterEnterSpotsByDirection(List<IntVec3> spots)
        {
            if (!this.enterDirection.IsValid || spots.NullOrEmpty())
            {
                return spots;
            }
            int targetValue;
            switch (this.enterDirection.AsInt)
            {
                case 0:
                    targetValue = spots.Max(p => p.z);
                    return spots.FindAll(p => p.z == targetValue);
                case 1:
                    targetValue = spots.Max(p => p.x);
                    return spots.FindAll(p => p.x == targetValue);
                case 2:
                    targetValue = spots.Min(p => p.z);
                    return spots.FindAll(p => p.z == targetValue);
                case 3:
                    targetValue = spots.Min(p => p.x);
                    return spots.FindAll(p => p.x == targetValue);
                default:
                    return spots;
            }
        }

        public bool fogged = false;
        public IntVec3 size;
        public bool isPart = false;
        public bool destroyAllThing = true;
        public float commonality = 0.8f;
        public int generationLimit = 0;
        public string faction = null;
        public List<ReplaceData> replaces = new List<ReplaceData>();
        public List<CustomThingData> customThings = new List<CustomThingData>();
        public List<CustomThingData> zoneCores = new List<CustomThingData>();
        public Dictionary<IntVec3, List<PawnSpawnData>> specialSpawnPawns = new Dictionary<IntVec3, List<PawnSpawnData>>();
        public Dictionary<IntVec3, List<PawnSpawnData>> pawns = new Dictionary<IntVec3, List<PawnSpawnData>>();
        public List<IntVec3> enterSpots = new List<IntVec3>();
        public Rot4 enterDirection = Rot4.Invalid;
        public Dictionary<string, List<IntVec3>> routes = new Dictionary<string, List<IntVec3>>();
        public Dictionary<RoofDef, List<IntVec3>> roofs = new Dictionary<RoofDef, List<IntVec3>>();
        public Dictionary<RoofDef, List<CellRect>> roofRects = new Dictionary<RoofDef, List<CellRect>>();
        public Dictionary<string, List<IntVec3>> terrains = new Dictionary<string, List<IntVec3>>();
        public Dictionary<string, List<CellRect>> terrainsRect = new Dictionary<string, List<CellRect>>();
        public Dictionary<ColorDef, List<CellRect>> terrainsColorRect = new Dictionary<ColorDef, List<CellRect>>();
        public List<ThingData> thingDatas = new List<ThingData>();
        public List<GenerationAction> generationActions = new List<GenerationAction>();
        public List<CustomMapStep> preCustomSteps = new List<CustomMapStep>();
        public List<CustomMapStep> customSteps = new List<CustomMapStep>();
        public List<string> tags = new List<string>();
        public ThingData reserveThing = null;
        public List<IntVec3> disgenerate = new List<IntVec3>();
        public List<IntVec3> disdestroy = new List<IntVec3>();
        public List<LordData> lordDatas = new List<LordData>();
        public List<GenerationKeyWithLimit> mapPartGenerationLimit = new List<GenerationKeyWithLimit>();
        public MapGeneratorDef generator = QEDefOf.QE_CustomMap_Editor_Generator;

        public Rot4 rot = Rot4.Invalid;
        public CustomMapDataDef origin;
        public Dictionary<Rot4, CustomMapDataDef> extraDataByDirection = new Dictionary<Rot4, CustomMapDataDef>();
        public Dictionary<IntVec3, CustomMapDataDef> extraDataByOrigin = new Dictionary<IntVec3, CustomMapDataDef>();
    }
    public class GenerationKeyWithLimit : ISaveable
    {
        public string key;
        public int limit;
        public string buffer;

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("key", key));
            result.Add(new XElement("limit", limit));
            result.Add(new XElement("buffer", buffer));
            return result;
        }
    }
    public class GenerationAction : ISaveable
    {
        public GenerationAction() { }
        public GenerationAction(IntVec3 pos,List<CQFAction> actions) 
        {
            this.pos = pos;
            this.actions = actions;
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("pos", this.pos.ToString()));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions,"actions"));
            return result;
        }

        public IntVec3 pos;
        public List<CQFAction> actions = new List<CQFAction>();
    }
    public class ReplaceData : ISaveable,IDrawable
    {
        public virtual string DataName => this.dataName;
        public virtual Dictionary<string, string> ReplaceThings => this.replaceThings;
        public virtual Dictionary<string, string> ReplaceTerrains => this.replaceTerrains;
        public virtual Dictionary<string, string> ReplaceStuffs => this.replaceStuffs;
        public virtual void Init() 
        {
 
        }

        public virtual void Clear() 
        {
        
        }
        public virtual TerrainDef ReplaceTerrain(TerrainDef def) 
        {
            if (this.ReplaceTerrains != null && this.ReplaceTerrains.TryGetValue(def.defName, out string result))
            {
                return TerrainDef.Named(result);
            }
            return def;
        }
        public virtual ThingDef ReplaceThing(ThingDef def) 
        {
            if (this.ReplaceThings.TryGetValue(def.defName, out string result))
            {
                return ThingDef.Named(result);
            }
            return def;
        }
        public virtual ThingDef ReplaceStuff(ThingDef def)
        {
            if (this.ReplaceStuffs.TryGetValue(def.defName, out string result))
            {
                return ThingDef.Named(result);
            }
            return def;
        }  
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            float width = inRect.width - x - 12f;
            Rect nameSection = new Rect(x, y, width, 78f);
            Widgets.DrawMenuSection(nameSection);
            Widgets.Label(new Rect(x + 12f, y + 8f, width - 24f, 25f),
                "DataName".Translate().Colorize(ColorLibrary.PaleBlue));
            this.dataName = Widgets.TextField(new Rect(x + 12f, y + 39f, width - 24f, 27f), this.dataName);
            y = nameSection.yMax + 10f;

            this.DrawThingReplacementSection(ref y, x, width, "ThingReplacement".Translate(),
                this.replaceThings, this.OpenThingReplacementSelector);
            this.DrawThingReplacementSection(ref y, x, width, "StuffReplacement".Translate(),
                this.replaceStuffs, this.OpenStuffReplacementSelector);
            this.DrawTerrainReplacementSection(ref y, x, width);
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            if (this.GetType() != typeof(ReplaceData))
            {
                result.SetAttributeValue("Class", this.GetType().FullName);
            }
            if (this.dataName != null) 
            {
                result.Add(new XElement("dataName", dataName));
            }
            if (this.replaceThings.Any())
            {
                result.Add(CQFEditorTools.SaveDictionary(this.replaceThings, "replaceThings"));
            }
            if (this.replaceTerrains.Any())
            {
                result.Add(CQFEditorTools.SaveDictionary(this.replaceTerrains, "replaceTerrains"));
            }
            if (this.replaceStuffs.Any())
            {
                result.Add(CQFEditorTools.SaveDictionary(this.replaceStuffs, "replaceStuffs"));
            }
            return result;
        }
        public override string ToString()
        {
            return this.dataName;
        }
        public virtual void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (object obj in xmlRoot.ChildNodes)
            {
                if (obj is XmlNode n)
                {
                    if (n.Name == "dataName")
                    {
                        this.dataName = n.InnerText;
                    }
                    else
                    {
                        this.LoadReplacement(n);
                    }
                }
            }
        }

        public void LoadReplacement(XmlNode xmlRoot) 
        {
            switch (xmlRoot.Name) 
            {
                case "replaceThings":
                    foreach (object obj in xmlRoot.ChildNodes)
                    {
                        if (obj is XmlNode n)
                        {
                            this.replaceThings.Add(n.Name, n.InnerText);
                        }
                    }
                    ; break;
                case "replaceStuffs":
                    foreach (object obj in xmlRoot.ChildNodes)
                    {
                        if (obj is XmlNode n)
                        {
                            this.replaceStuffs.Add(n.Name, n.InnerText);
                        }
                    }
                    ; break;
                case "replaceTerrains":
                    foreach (object obj in xmlRoot.ChildNodes)
                    {
                        if (obj is XmlNode n)
                        {
                            this.replaceTerrains.Add(n.Name, n.InnerText);
                        }
                    }
                    ; break;
            }
        }

        private void DrawThingReplacementSection(ref float y, float x, float width, string title,
            Dictionary<string, string> replacements, Action addAction)
        {
            float sectionHeight = 50f + Math.Max(1, replacements.Count) * 42f;
            Rect sectionRect = new Rect(x, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            this.DrawReplacementHeader(y, x, width, title, addAction,
                () => CQFEditorTools.DrawFloatMenu(replacements.ToList(),
                    pair => replacements.Remove(pair.Key), this.GetThingReplacementLabel));

            float rowY = y + 42f;
            if (!replacements.Any())
            {
                Widgets.Label(new Rect(x + 14f, rowY + 5f, width - 28f, 25f), "-");
            }
            foreach (KeyValuePair<string, string> pair in replacements)
            {
                ThingDef source = DefDatabase<ThingDef>.GetNamedSilentFail(pair.Key);
                ThingDef target = DefDatabase<ThingDef>.GetNamedSilentFail(pair.Value);
                this.DrawReplacementRow(new Rect(x + 10f, rowY, width - 20f, 36f),
                    source, source?.label ?? pair.Key, target, target?.label ?? pair.Value);
                rowY += 42f;
            }
            y = sectionRect.yMax + 10f;
        }

        private void DrawTerrainReplacementSection(ref float y, float x, float width)
        {
            float sectionHeight = 50f + Math.Max(1, this.replaceTerrains.Count) * 42f;
            Rect sectionRect = new Rect(x, y, width, sectionHeight);
            Widgets.DrawMenuSection(sectionRect);
            this.DrawReplacementHeader(y, x, width, "TerrainReplacement".Translate(),
                this.OpenTerrainReplacementSelector,
                () => CQFEditorTools.DrawFloatMenu(this.replaceTerrains.ToList(),
                    pair => this.replaceTerrains.Remove(pair.Key), this.GetTerrainReplacementLabel));

            float rowY = y + 42f;
            if (!this.replaceTerrains.Any())
            {
                Widgets.Label(new Rect(x + 14f, rowY + 5f, width - 28f, 25f), "-");
            }
            foreach (KeyValuePair<string, string> pair in this.replaceTerrains)
            {
                TerrainDef source = DefDatabase<TerrainDef>.GetNamedSilentFail(pair.Key);
                TerrainDef target = DefDatabase<TerrainDef>.GetNamedSilentFail(pair.Value);
                this.DrawReplacementRow(new Rect(x + 10f, rowY, width - 20f, 36f),
                    source, source?.label ?? pair.Key, target, target?.label ?? pair.Value);
                rowY += 42f;
            }
            y = sectionRect.yMax + 10f;
        }

        private void DrawReplacementHeader(float y, float x, float width, string title,
            Action addAction, Action removeAction)
        {
            Widgets.Label(new Rect(x + 12f, y + 9f, width - 96f, 25f), title.Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(x + width - 72f, y + 6f, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                addAction();
            }
            TooltipHandler.TipRegion(addRect, "Add".Translate());
            Rect removeRect = new Rect(x + width - 36f, y + 6f, 28f, 28f);
            if (Widgets.ButtonImage(removeRect, TexButton.Delete))
            {
                removeAction();
            }
            TooltipHandler.TipRegion(removeRect, "Remove".Translate());
        }

        private void DrawReplacementRow(Rect rect, Def source, string sourceLabel, Def target, string targetLabel)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            float sideWidth = (rect.width - 58f) / 2f;
            Rect sourceIconRect = new Rect(rect.x + 4f, rect.y + 3f, 30f, 30f);
            if (source != null)
            {
                Widgets.DefIcon(sourceIconRect, source);
            }
            Widgets.Label(new Rect(sourceIconRect.xMax + 6f, rect.y + 6f, sideWidth - 40f, 25f), sourceLabel);

            Rect arrowRect = new Rect(rect.center.x - 14f, rect.y + 4f, 28f, 28f);
            Widgets.DrawTextureFitted(arrowRect, QuestEditor_SaveMapToFile.arrowIcon, 1f);

            Rect targetIconRect = new Rect(rect.center.x + 20f, rect.y + 3f, 30f, 30f);
            if (target != null)
            {
                Widgets.DefIcon(targetIconRect, target);
            }
            Widgets.Label(new Rect(targetIconRect.xMax + 6f, rect.y + 6f, sideWidth - 40f, 25f), targetLabel);
            TooltipHandler.TipRegion(rect, sourceLabel + " -> " + targetLabel);
        }

        private string GetThingReplacementLabel(KeyValuePair<string, string> pair)
        {
            return (DefDatabase<ThingDef>.GetNamedSilentFail(pair.Key)?.label ?? pair.Key) + " -> " +
                (DefDatabase<ThingDef>.GetNamedSilentFail(pair.Value)?.label ?? pair.Value);
        }

        private string GetTerrainReplacementLabel(KeyValuePair<string, string> pair)
        {
            return (DefDatabase<TerrainDef>.GetNamedSilentFail(pair.Key)?.label ?? pair.Key) + " -> " +
                (DefDatabase<TerrainDef>.GetNamedSilentFail(pair.Value)?.label ?? pair.Value);
        }

        private void OpenThingReplacementSelector()
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(t => !t.IsCorpse &&
                t.category != ThingCategory.Mote && t.category != ThingCategory.Projectile &&
                t.category != ThingCategory.Pawn && t.category != ThingCategory.Ethereal &&
                t.category != ThingCategory.Attachment);
            this.OpenThingReplacementSelector(defs, this.replaceThings);
        }

        private void OpenStuffReplacementSelector()
        {
            this.OpenThingReplacementSelector(
                DefDatabase<ThingDef>.AllDefsListForReading.FindAll(def => def.IsStuff), this.replaceStuffs);
        }

        private void OpenThingReplacementSelector(List<ThingDef> defs, Dictionary<string, string> replacements)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(defs,
                def => def.uiIcon, def => def.label,
                source => Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(defs,
                    def => def.uiIcon, def => def.label,
                    target => replacements[source.defName] = target.defName,
                    def => def.graphicData == null ? Color.white : def.graphicData.color),
                    "SelectThingDefToReplace".Translate())),
                def => def.graphicData == null ? Color.white : def.graphicData.color),
                "SelectReplacedThing".Translate()));
        }

        private void OpenTerrainReplacementSelector()
        {
            List<TerrainDef> terrains = DefDatabase<TerrainDef>.AllDefsListForReading;
            Find.WindowStack.Add(new Dialog_Select<TerrainDef>(new TextureSelectDrawer<TerrainDef>(terrains,
                def => def.uiIcon, def => def.label,
                source => Find.WindowStack.Add(new Dialog_Select<TerrainDef>(new TextureSelectDrawer<TerrainDef>(terrains,
                    def => def.uiIcon, def => def.label,
                    target => this.replaceTerrains[source.defName] = target.defName,
                    def => def.DrawColor,
                    (def, rect) => Widgets.DefIcon(rect, def, null, 1f, null, false, def.DrawColor)),
                    "SelectTerrainDefToReplace".Translate())),
                def => def.DrawColor,
                (def, rect) => Widgets.DefIcon(rect, def, null, 1f, null, false, def.DrawColor)),
                "SelectReplacedTerrain".Translate()));
        }

        public string dataName;
        public Dictionary<string, string> replaceThings = new Dictionary<string, string>();
        public Dictionary<string, string> replaceTerrains = new Dictionary<string, string>();
        public Dictionary<string, string> replaceStuffs = new Dictionary<string, string>();
    }
    public class ReplaceData_Def : ReplaceData
    {
        public override string DataName => this.def?.defName ?? "ReplaceData_Def".Translate();
        public override Dictionary<string, string> ReplaceThings => this.data?.replaceThings;
        public override Dictionary<string, string> ReplaceTerrains => this.data?.replaceTerrains;
        public override Dictionary<string, string> ReplaceStuffs => this.data?.replaceStuffs;
        public override void Init()
        {
            this.data = this.def?.datas.RandomElement();
             
        }
        public override void Clear()
        {
            this.data = null;
        }
        public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (object obj in xmlRoot.ChildNodes)
            {
                if (obj is XmlNode n)
                {
                    if (n.Name == "def")
                    {
                        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this,"def",n.InnerText);
                    }
                }
            }
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("def", this.def.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            if (Widgets.ButtonText(new Rect(x,y,250f,25f),"ReplacementDef".Translate(this.def?.defName),false)) 
            {
                CQFEditorTools.DrawFloatMenu<ReplacementDataDef>(DefDatabase<ReplacementDataDef>.AllDefsListForReading,d => this.def = d,d => d.defName);
            }
            y += 30f;
        }

        public ReplacementDataDef def;
        public ReplaceData data;
    }
}


