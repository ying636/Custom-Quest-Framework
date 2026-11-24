using System.Globalization;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed partial class CQFLargeMapDraft
    {
        public CQFLargeMapDraft(Map map) : this(new CQFAILiveMap(map), new CQFAIModel()) { }

        private CQFLargeMapDraft(CQFAILiveMap backend, CQFAIModel model)
        {
            Map = backend.Map;
            this.backend = backend;
            this.model = model;
        }

        public Map Map { get; }
        public bool IsValid => backend.IsValid;
        public bool Busy => work != null;
        public bool CanUndo => !Busy && applied.Count > 0;
        public int Revision { get; private set; }
        public int Completed { get; private set; }
        public int Total { get; private set; }
        public string Phase { get; private set; } = "";
        public Exception? Error { get; private set; }
        public IReadOnlyDictionary<IntVec3, TerrainDef> Terrain => terrain;
        public IReadOnlyDictionary<IntVec3, RoofDef> Roofs => roofs;
        public IReadOnlyList<ThingData> Placements => placements;
        public IReadOnlyCollection<IntVec3> PlacementConflicts => placementConflicts;
        public IReadOnlyList<IntVec3> BlockedRoad => road.Where(c => !RoadPassable(c)).Take(41).ToArray();

        public XElement Stage(XElement command)
        {
            RequireIdle();
            if (CanUndo) throw new InvalidOperationException("CQF_AI_LargeDraftAlreadyApplied");
            if (command.Name != "draft" || command.HasAttributes || command.Elements().Count() is < 1 or > 64 ||
                command.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value)))
                throw new InvalidDataException("CQF_AI_InvalidChanges");
            Start("CQF_AI_LargeDraftPreparing", StageSteps(new XElement(command)), command.Elements().Count());
            return new XElement("mapDraftQueued", new XAttribute("map", Map.uniqueID), new XAttribute("revision", Revision),
                new XAttribute("statusTool", "cqf_map_draft_status"));
        }

        public XElement Summary()
        {
            var blocked = BlockedRoad;
            return new XElement("mapDraft", new XAttribute("map", Map.uniqueID), new XAttribute("revision", Revision),
                new XAttribute("busy", Busy), new XAttribute("applied", CanUndo), new XAttribute("phase", Phase),
                new XAttribute("completed", Completed), new XAttribute("total", Total),
                Error == null ? null : new XElement("error", Error.Message),
                new XAttribute("placementConflicts", placementConflicts.Count), new XAttribute("terrainCells", terrain.Count), new XAttribute("roofCells", roofs.Count),
                new XAttribute("placements", placements.Count), new XAttribute("roadCells", road.Count),
                new XAttribute("blockedRoadSamples", Math.Min(40, blocked.Count)), new XAttribute("blockedRoadMore", blocked.Count > 40),
                blocked.Take(40).Select(cell => new XElement("blocked", new XAttribute("x", cell.x), new XAttribute("z", cell.z))),
                placementConflicts.Take(40).Select(cell => new XElement("placementConflict", new XAttribute("x", cell.x), new XAttribute("z", cell.z))));
        }

        public void Clear()
        {
            RequireIdle();
            terrain.Clear(); before.Clear(); roofs.Clear(); placements.Clear(); road.Clear(); blockedPlacements.Clear(); placementConflicts.Clear();
            Error = null;
            Revision++;
        }

        public void StartNew()
        {
            RequireIdle();
            applied.Clear();
            Clear();
        }

        private IEnumerable<int> StageSteps(XElement command)
        {
            var next = new CQFLargeMapDraft(backend, model);
            foreach (var entry in terrain) { next.terrain.Add(entry.Key, entry.Value); yield return 0; }
            foreach (var entry in before) { next.before.Add(entry.Key, entry.Value); yield return 0; }
            foreach (var entry in roofs) { next.roofs.Add(entry.Key, entry.Value); yield return 0; }
            next.placements.AddRange(placements);
            next.road.UnionWith(road);
            foreach (XElement op in command.Elements())
            {
                foreach (int step in next.StageOperation(op))
                {
                    if (!IsValid) throw new InvalidOperationException("CQF_AI_StaleTarget");
                    yield return step;
                }
                Completed++;
            }
            next.CheckPlacements();
            terrain = next.terrain;
            before = next.before;
            roofs = next.roofs;
            placements = next.placements;
            road = next.road;
            blockedPlacements = next.blockedPlacements;
            placementConflicts = next.placementConflicts;
            Revision++;
        }

        private IEnumerable<int> StageOperation(XElement op)
        {
            if (op.Name == "blueprint")
            {
                foreach (int step in StageBlueprint(op)) yield return step;
                yield break;
            }

            TerrainDef destination = TerrainNamed(Required(op, "terrain"));
            if (op.Name == "fill" || op.Name == "replace")
            {
                Validate(op, op.Name == "fill" ? new[] { "x", "z", "width", "height", "terrain" } :
                    new[] { "x", "z", "width", "height", "terrain", "from" });
                var rect = Area(Number(op, "x"), Number(op, "z"), Number(op, "width"), Number(op, "height"));
                TerrainDef? from = op.Name == "replace" ? TerrainNamed(Required(op, "from")) : null;
                foreach (IntVec3 cell in rect)
                {
                    if (from == null || EffectiveTerrain(cell) == from) Paint(cell, destination);
                    yield return 0;
                }
                yield break;
            }

            if (op.Name != "road" && op.Name != "waterway") throw new InvalidDataException("CQF_AI_InvalidChanges: " + op.Name);
            Validate(op, new[] { "terrain", "width" }, true);
            if (op.Name == "road" && destination.passability == Traversability.Impassable)
                throw new InvalidDataException("CQF_AI_LargeDraftRoadTerrain");
            if (op.Name == "waterway" && !destination.IsWater)
                throw new InvalidDataException("CQF_AI_LargeDraftWaterTerrain");
            int width = Number(op, "width");
            if (width is < 1 or > 21 || op.Elements().Count() is < 2 or > 64)
                throw new InvalidDataException("CQF_AI_InvalidChanges: route");
            IntVec3[] points = op.Elements().Select(point =>
            {
                if (point.Name != "point") throw new InvalidDataException("CQF_AI_InvalidChanges: point");
                Validate(point, new[] { "x", "z" });
                var cell = new IntVec3(Number(point, "x"), 0, Number(point, "z"));
                if (!cell.InBounds(Map)) throw new InvalidDataException("CQF_AI_MapBounds");
                return cell;
            }).ToArray();
            for (int i = 1; i < points.Length; i++)
                foreach (IntVec3 cell in Route(points[i - 1], points[i]))
                {
                    if (op.Name == "road") road.Add(cell);
                    foreach (IntVec3 part in Area(cell.x - width / 2, cell.z - width / 2, width, width))
                    {
                        Paint(part, destination);
                        yield return 0;
                    }
                }
        }

        private void Paint(IntVec3 cell, TerrainDef value)
        {
            if (!before.TryGetValue(cell, out TerrainDef original))
                original = Map.terrainGrid.TopTerrainAt(cell);
            if (original == value)
            {
                before.Remove(cell);
                terrain.Remove(cell);
                return;
            }
            before[cell] = original;
            terrain[cell] = value;
        }

        private bool RoadPassable(IntVec3 cell)
        {
            return EffectiveTerrain(cell).passability != Traversability.Impassable
                && cell.GetEdifice(Map)?.def.passability != Traversability.Impassable
                && !blockedPlacements.Contains(cell);
        }

        private TerrainDef EffectiveTerrain(IntVec3 cell) => terrain.TryGetValue(cell, out TerrainDef value) ? value : Map.terrainGrid.TopTerrainAt(cell);

        private CellRect Area(int x, int z, int width, int height)
        {
            if (x < 0 || z < 0 || width < 1 || height < 1 || (long)x + width > Map.Size.x || (long)z + height > Map.Size.z)
                throw new InvalidDataException("CQF_AI_MapBounds");
            return new CellRect(x, z, width, height);
        }

        private static TerrainDef TerrainNamed(string name)
        {
            var result = DefDatabase<TerrainDef>.GetNamedSilentFail(name) ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
            if (result.isFoundation || result.temporary) throw new InvalidDataException("CQF_AI_InvalidValue: layered terrain");
            return result;
        }

        private static IEnumerable<IntVec3> Route(IntVec3 from, IntVec3 to)
        {
            int dx = Math.Abs(to.x - from.x), dz = Math.Abs(to.z - from.z);
            int sx = Math.Sign(to.x - from.x), sz = Math.Sign(to.z - from.z);
            int x = 0, z = 0;
            yield return from;
            while (x < dx || z < dz)
            {
                if (x < dx && (z == dz || (2L * x + 1) * dz <= (2L * z + 1) * dx)) x++;
                else z++;
                yield return new IntVec3(from.x + x * sx, 0, from.z + z * sz);
            }
        }

        private static void Validate(XElement element, string[] attributes, bool allowChildren = false)
        {
            if (!allowChildren && element.HasElements || element.Attributes().Any(a => !attributes.Contains(a.Name.ToString()))
                || element.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value)))
                throw new InvalidDataException("CQF_AI_InvalidChanges: " + element.Name);
        }

        private static string Required(XElement element, string field) =>
            (string?)element.Attribute(field) ?? throw new InvalidDataException("CQF_AI_InvalidValue: " + field);

        private static int Number(XElement element, string field, int? fallback = null)
        {
            XAttribute? attribute = element.Attribute(field);
            if (attribute == null && fallback.HasValue) return fallback.Value;
            return int.TryParse(attribute?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result : throw new InvalidDataException("CQF_AI_InvalidValue: " + field);
        }

        private void RequireIdle()
        {
            if (Busy || !IsValid) throw new InvalidOperationException("CQF_AI_StaleTarget");
        }

        private readonly CQFAILiveMap backend;
        private readonly CQFAIModel model;
        private Dictionary<IntVec3, TerrainDef> terrain = new Dictionary<IntVec3, TerrainDef>();
        private Dictionary<IntVec3, TerrainDef> before = new Dictionary<IntVec3, TerrainDef>();
        private Dictionary<IntVec3, RoofDef> roofs = new Dictionary<IntVec3, RoofDef>();
        private List<ThingData> placements = new List<ThingData>();
        private HashSet<IntVec3> road = new HashSet<IntVec3>();
        private HashSet<IntVec3> blockedPlacements = new HashSet<IntVec3>();
    }
}
