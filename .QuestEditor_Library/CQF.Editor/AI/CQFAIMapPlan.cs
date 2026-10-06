using System.Xml.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace QuestEditor_Library
{
    public static class CQFAIMapPlan
    {
        public static void Apply(CustomMapDataDef map, XElement operation)
        {
            map.terrains ??= new Dictionary<string, List<IntVec3>>();
            map.terrainsRect ??= new Dictionary<string, List<CellRect>>();
            map.roofs ??= new Dictionary<RoofDef, List<IntVec3>>();
            map.roofRects ??= new Dictionary<RoofDef, List<CellRect>>();
            map.thingDatas ??= new List<ThingData>();
            map.customThings ??= new List<CustomThingData>();
            map.zoneCores ??= new List<CustomThingData>();
            if (operation.HasElements) throw new InvalidDataException("CQF_AI_InvalidChanges");
            int Number(string name, int defaultValue = 0) => operation.Attribute(name) == null ? defaultValue : int.TryParse(operation.Attribute(name)!.Value, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int value) ? value : throw new InvalidDataException("CQF_AI_InvalidValue: " + name);
            if (operation.Name == "mapResize")
            {
                CheckAttributes(operation, "x", "z");
                map.size = new IntVec3(Number("x"), 1, Number("z"));
                CheckSize(map);
                return;
            }
            int x = Number("x"), z = Number("z"), width = Number("width", 1), height = Number("height", 1);
            if (width < 1 || height < 1 || x < 0 || z < 0 || x + (long)width > map.size.x || z + (long)height > map.size.z) throw new InvalidDataException("CQF_AI_MapBounds");
            CellRect rect = new CellRect(x, z, width, height);
            string name = operation.Attribute("def")?.Value ?? string.Empty;
            if (operation.Name == "terrain")
            {
                CheckAttributes(operation, "def", "x", "z", "width", "height");
                TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(name) ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
                foreach (var pair in map.terrains) pair.Value.RemoveAll(cell => rect.Contains(cell));
                foreach (string key in map.terrainsRect.Keys.ToList()) map.terrainsRect[key] = map.terrainsRect[key].SelectMany(area => Subtract(area, rect)).ToList();
                if (!map.terrainsRect.TryGetValue(terrain.defName, out List<CellRect> areas)) map.terrainsRect.Add(terrain.defName, areas = new List<CellRect>());
                areas.Add(rect);
            }
            else if (operation.Name == "roof")
            {
                CheckAttributes(operation, "def", "x", "z", "width", "height");
                RoofDef roof = DefDatabase<RoofDef>.GetNamedSilentFail(name) ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
                foreach (var pair in map.roofs) pair.Value.RemoveAll(cell => rect.Contains(cell));
                foreach (RoofDef key in map.roofRects.Keys.ToList()) map.roofRects[key] = map.roofRects[key].SelectMany(area => Subtract(area, rect)).ToList();
                if (!map.roofRects.TryGetValue(roof, out List<CellRect> areas)) map.roofRects.Add(roof, areas = new List<CellRect>());
                areas.Add(rect);
            }
            else if (operation.Name == "erase")
            {
                CheckAttributes(operation, "x", "z", "width", "height");
                foreach (ThingData thing in map.thingDatas.ToList())
                {
                    List<IntVec3> remaining = Positions(thing).Where(position => !Intersects(GenAdj.OccupiedRect(position, thing.rotation, thing.def.size), rect)).ToList();
                    if (remaining.Count == 0) map.thingDatas.Remove(thing);
                    else if (remaining.Count == 1) { thing.position = remaining[0]; thing.allPositions.Clear(); thing.allRect.Clear(); }
                    else { thing.position = IntVec3.Zero; thing.allPositions.Clear(); thing.allRect = remaining.Select(cell => new CellRect(cell.x, cell.z, 1, 1)).ToList(); }
                }
                map.customThings.RemoveAll(thing => Intersects(GenAdj.OccupiedRect(thing.position, thing.rotation, thing.def.size), rect));
                map.zoneCores.RemoveAll(thing => Intersects(GenAdj.OccupiedRect(thing.position, thing.rotation, thing.def.size), rect));
            }
            else if (operation.Name == "place")
            {
                CheckAttributes(operation, "def", "stuff", "x", "z", "rotation", "count", "Class");
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name) ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
                int rotation = Number("rotation"), count = Number("count", 1);
                if (rotation < 0 || rotation > 3 || count < 1 || count > def.stackLimit) throw new InvalidDataException("CQF_AI_InvalidValue: placement");
                ThingDef? stuff = operation.Attribute("stuff") == null ? null : DefDatabase<ThingDef>.GetNamedSilentFail(operation.Attribute("stuff")!.Value);
                if (def.MadeFromStuff && (stuff == null || !GenStuff.AllowedStuffsFor(def).Contains(stuff)) || !def.MadeFromStuff && operation.Attribute("stuff") != null) throw new InvalidDataException("CQF_AI_InvalidValue: stuff");
                string? className = operation.Attribute("Class")?.Value;
                IntVec3 position = new IntVec3(x, 0, z);
                if (className != null)
                {
                    Type type = typeof(CustomThingData).Assembly.GetType(className) ?? throw new InvalidDataException("CQF_AI_UnknownType");
                    if (type.IsAbstract || !typeof(CustomThingData).IsAssignableFrom(type)) throw new InvalidDataException("CQF_AI_UnknownType");
                    CustomThingData custom = (CustomThingData)Activator.CreateInstance(type)!;
                    custom.def = def; custom.stuff = stuff!; custom.position = position; custom.rotation = new Rot4(rotation); custom.count = count;
                    CheckDataType(def, type);
                    if (custom is CustomThingData_ZoneCore) map.zoneCores.Add(custom);
                    else map.customThings.Add(custom);
                }
                else
                {
                    CheckDataType(def, null);
                    map.thingDatas.Add(new ThingData { def = def, stuff = stuff!, position = position, rotation = new Rot4(rotation), count = count });
                }
            }
            else throw new InvalidDataException("CQF_AI_InvalidChanges");
        }
        public static void Validate(CustomMapDataDef map)
        {
            CheckSize(map);
            bool InBounds(IntVec3 cell) => cell.x >= 0 && cell.z >= 0 && cell.x < map.size.x && cell.z < map.size.z;
            foreach (ThingData thing in map.thingDatas ?? new List<ThingData>())
            {
                if (thing.def == null || thing.count < 1 || thing.count > thing.def.stackLimit) throw new InvalidDataException("CQF_AI_MapBounds");
                if (thing.def.MadeFromStuff && (thing.stuff == null || !GenStuff.AllowedStuffsFor(thing.def).Contains(thing.stuff))) throw new InvalidDataException("CQF_AI_InvalidValue: stuff");
                CheckDataType(thing.def, null);
                foreach (CellRect area in thing.allRect)
                    if (area.Width <= 0 || area.Height <= 0 || !InBounds(new IntVec3(area.minX, 0, area.minZ)) || !InBounds(new IntVec3(area.maxX, 0, area.maxZ))) throw new InvalidDataException("CQF_AI_MapBounds");
                foreach (IntVec3 cell in Positions(thing)) CheckFootprint(thing.def, cell, thing.rotation, map.size);
            }
            foreach (CustomThingData thing in (map.customThings ?? new List<CustomThingData>()).Concat(map.zoneCores ?? new List<CustomThingData>()))
            {
                if (thing.def == null || thing.count < 1 || thing.count > thing.def.stackLimit || !InBounds(thing.position)) throw new InvalidDataException("CQF_AI_MapBounds");
                if (thing.def.MadeFromStuff && (thing.stuff == null || !GenStuff.AllowedStuffsFor(thing.def).Contains(thing.stuff))) throw new InvalidDataException("CQF_AI_InvalidValue: stuff");
                CheckDataType(thing.def, thing.GetType());
                CheckFootprint(thing.def, thing.position, thing.rotation, map.size);
            }
            foreach (var pair in map.terrains ?? new Dictionary<string, List<IntVec3>>())
                if (DefDatabase<TerrainDef>.GetNamedSilentFail(pair.Key) == null || pair.Value.Any(cell => !InBounds(cell))) throw new InvalidDataException("CQF_AI_MissingResource: " + pair.Key);
            foreach (var pair in map.terrainsRect ?? new Dictionary<string, List<CellRect>>())
                if (DefDatabase<TerrainDef>.GetNamedSilentFail(pair.Key) == null || pair.Value.Any(rect => !InBounds(new IntVec3(rect.minX, 0, rect.minZ)) || !InBounds(new IntVec3(rect.maxX, 0, rect.maxZ)))) throw new InvalidDataException("CQF_AI_MapBounds");
            foreach (var pair in map.roofs ?? new Dictionary<RoofDef, List<IntVec3>>())
                if (pair.Key == null || pair.Value.Any(cell => !InBounds(cell))) throw new InvalidDataException("CQF_AI_MapBounds");
            foreach (var pair in map.roofRects ?? new Dictionary<RoofDef, List<CellRect>>())
                if (pair.Key == null || pair.Value.Any(rect => rect.Width <= 0 || rect.Height <= 0 || !InBounds(new IntVec3(rect.minX, 0, rect.minZ)) || !InBounds(new IntVec3(rect.maxX, 0, rect.maxZ)))) throw new InvalidDataException("CQF_AI_MapBounds");
            IEnumerable<IntVec3> points = (map.enterSpots ?? new List<IntVec3>()).Concat(map.disgenerate ?? new List<IntVec3>()).Concat(map.disdestroy ?? new List<IntVec3>())
                .Concat((map.routes ?? new Dictionary<string, List<IntVec3>>()).Values.SelectMany(cells => cells))
                .Concat((map.pawns ?? new Dictionary<IntVec3, List<PawnSpawnData>>()).Keys)
                .Concat((map.specialSpawnPawns ?? new Dictionary<IntVec3, List<PawnSpawnData>>()).Keys)
                .Concat((map.generationActions ?? new List<GenerationAction>()).Select(action => action.pos));
            if (points.Any(cell => !InBounds(cell))) throw new InvalidDataException("CQF_AI_MapBounds");
        }
        public static IEnumerable<IntVec3> Positions(ThingData thing)
        {
            if (thing.allPositions.Count == 0 && thing.allRect.Count == 0) { yield return thing.position; yield break; }
            foreach (IntVec3 cell in thing.allPositions) yield return cell;
            foreach (CellRect area in thing.allRect)
                foreach (IntVec3 cell in area.Cells) yield return cell;
        }
        private static bool Intersects(CellRect first, CellRect second)
        {
            return first.minX <= second.maxX && first.maxX >= second.minX && first.minZ <= second.maxZ && first.maxZ >= second.minZ;
        }
        private static void CheckDataType(ThingDef definition, Type? dataType)
        {
            Type? thingClass = definition.thingClass;
            bool Matches(Type type) => thingClass != null && type.GetConstructors().Any(constructor => constructor.GetParameters().Length == 2
                && constructor.GetParameters()[1].ParameterType == typeof(IntVec3) && constructor.GetParameters()[0].ParameterType.IsAssignableFrom(thingClass));
            if (dataType != null && dataType != typeof(CustomThingData) && !Matches(dataType)) throw new InvalidDataException("CQF_AI_InvalidValue: CustomThingData/ThingDef");
            if ((dataType == null || dataType == typeof(CustomThingData)) && typeof(CustomThingData).Assembly.GetTypes().Any(type => !type.IsAbstract && type != typeof(CustomThingData) && typeof(CustomThingData).IsAssignableFrom(type) && Matches(type)))
                throw new InvalidDataException("CQF_AI_InvalidValue: CustomThingData required");
        }
        private static void CheckFootprint(ThingDef definition, IntVec3 position, Rot4 rotation, IntVec3 size)
        {
            if (!rotation.IsValid) throw new InvalidDataException("CQF_AI_InvalidValue: rotation");
            CellRect rect = GenAdj.OccupiedRect(position, rotation, definition.size);
            if (rect.minX < 0 || rect.minZ < 0 || rect.maxX >= size.x || rect.maxZ >= size.z) throw new InvalidDataException("CQF_AI_MapBounds");
        }
        private static void CheckSize(CustomMapDataDef map)
        {
            if (map.size.x < 1 || map.size.z < 1 || map.size.x > 512 || map.size.z > 512) throw new InvalidDataException("CQF_AI_MapBounds");
        }
        private static IEnumerable<CellRect> Subtract(CellRect source, CellRect paint)
        {
            int minX = Math.Max(source.minX, paint.minX), minZ = Math.Max(source.minZ, paint.minZ);
            int maxX = Math.Min(source.maxX, paint.maxX), maxZ = Math.Min(source.maxZ, paint.maxZ);
            if (minX > maxX || minZ > maxZ) { yield return source; yield break; }
            if (source.minZ < minZ) yield return new CellRect(source.minX, source.minZ, source.Width, minZ - source.minZ);
            if (maxZ < source.maxZ) yield return new CellRect(source.minX, maxZ + 1, source.Width, source.maxZ - maxZ);
            if (source.minX < minX) yield return new CellRect(source.minX, minZ, minX - source.minX, maxZ - minZ + 1);
            if (maxX < source.maxX) yield return new CellRect(maxX + 1, minZ, source.maxX - maxX, maxZ - minZ + 1);
        }
        private static void CheckAttributes(XElement operation, params string[] names)
        {
            if (operation.Attributes().Any(attribute => !names.Contains(attribute.Name.LocalName))) throw new InvalidDataException("CQF_AI_InvalidChanges");
        }
    }
}
