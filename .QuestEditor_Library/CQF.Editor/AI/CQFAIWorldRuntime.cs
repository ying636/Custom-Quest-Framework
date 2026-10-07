using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public sealed class CQFAIWorldRuntime
    {
        public CQFAIWorldRuntime(CQFAIModel model, CQFAIRuntimeJournal journal, bool text = true, string command = "") { this.model = model; this.journal = journal; this.text = text; this.command = command; }
        public XElement Help(string group) => group == "diagnostics" ? new XElement("diagnostics",
            new XElement("queries", "cqf_inspect_map kinds: cell(map_id,x,z) returns roof/support/walkability/room, reachability(pawn_id,x,z,optional end_mode=Touch|OnCell|InteractionCell and danger=None|Some|Deadly), conditions(map_id,offset,limit). Exact cell bounds required; reachability uses the Pawn's own current map. Room and roof diagnostics observe current native game calculations."))
            : new XElement("world",
                new XElement("queries", "maps or main_sites,offset,limit; main_site_cache requires site_id and supports offset/limit; world_map requires map_id; spawn_queues/map_routes require map_id,optional path=/,offset,limit. Queue/configuration schema is CQFAISpawnQueueConfiguration; building dictionary keys are exact spawned Building Thing IDs. Root is a summary; use narrower paths for full data."),
                new XElement("operation", new XAttribute("kind", "generate_submap"), "thing_id=spawned CustomMapEntrance. Requires valid configured MapDef, no generated destination and no existing linked exit. Generates through the actual CQF map pipeline; does not move Pawns. Returns actual destination ID; not undoable."),
                new XElement("operation", new XAttribute("kind", "link_portals"), "entrance_id,exit_id exact spawned CQF portal IDs. Links existing maps bidirectionally; rejects conflicting partners/destinations. Supports undo; does not generate maps or move Pawns."),
                new XElement("operation", new XAttribute("kind", "world_map_configure"), "map_id,optional custom_name,permanent,level>=0. Requires MapParent_Custom; updates only supplied metadata with undo."),
                new XElement("operation", new XAttribute("kind", "map_queues_configure"), "map_id,changes_xml=generic changes for CQFAISpawnQueueConfiguration. Replaces routes and queued Pawn recipes with validation/undo. Does not spawn immediately; timers count elapsed ticks upward and spawn repeatedly after timeToSpawn, while building conditions can trigger normally on later game ticks."),
                new XElement("operation", new XAttribute("kind", "main_site_cache"), "site_id from main_sites,key,pawn_id or remove=true. Updates existing MainSite NPC cache with undo; does not spawn or destroy cached Pawns."),
                new XElement("operation", new XAttribute("kind", "main_site_visits"), "site_id,count>=0. Updates visit count with undo. Site creation/destruction, quest-linked generation and teleports are available through actual CQF actions and their schemas."));
        public XElement Read(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "map_id", "site_id", "path", "offset", "limit");
            string kind = CQFAIRuntimeRequest.Text(request, "kind");
            if (kind is "maps" or "main_sites") CQFAIRuntimeRequest.Fields(request, "kind", "offset", "limit");
            if (kind == "maps") return CQFAIRuntimeRequest.Page("maps", Current.Game.Maps.Select(MapSummary), request);
            if (kind == "main_sites") return CQFAIRuntimeRequest.Page("mainSites", Sites().Select(site => new XElement("site", new XAttribute("id", site.ID), new XAttribute("definition", site.mainMapDef?.defName ?? ""),
                new XAttribute("mapId", site.Map?.uniqueID ?? -1), new XAttribute("visits", site.visitCount), new XAttribute("lastLeaveTick", site.lastLeaveTick), new XAttribute("killed", site.killed),
                new XAttribute("cachedPawnCount", site.mainPawns?.Count ?? 0))), request);
            if (kind == "main_site_cache")
            {
                CQFAIRuntimeRequest.Fields(request, "kind", "site_id", "offset", "limit");
                MainSite site = Sites().FirstOrDefault(value => value.ID == CQFAIRuntimeRequest.Int(request, "site_id")) ?? throw new InvalidDataException("CQF_AI_MissingResource: MainSite");
                return CQFAIRuntimeRequest.Page("cachedPawns", (site.mainPawns ?? new Dictionary<string, Pawn>()).OrderBy(pair => pair.Key).Select(pair => new XElement("pawn", new XAttribute("key", pair.Key), new XAttribute("id", pair.Value?.ThingID ?? ""), new XAttribute("spawned", pair.Value?.Spawned == true))), request);
            }
            Map map = CQFAIRuntimeRequest.Map(request);
            if (kind == "world_map") return new XElement("worldMap", MapSummary(map), CQFAIRuntimeRequest.Page("portals", map.listerThings.AllThings.OfType<CQFMapPortal>().Select(PortalSummary), request));
            if (kind is not ("spawn_queues" or "map_routes")) throw new InvalidDataException("CQF_AI_InvalidTool: world query");
            CQFAISpawnQueueConfiguration value = Queues(map);
            string path = CQFAIRuntimeRequest.Text(request, "path", kind == "map_routes" ? "/routes" : "/");
            return path == "/" ? new CQFAITargetReader(model).Summary(value) : new CQFAITargetReader(model).Read(value, path, CQFAIRuntimeRequest.Offset(request), CQFAIRuntimeRequest.Limit(request));
        }
        public XElement Inspect(XElement request)
        {
            if (Current.Game == null) throw new InvalidDataException("CQF_AI_MissingResource: no active game");
            CQFAIRuntimeRequest.Fields(request, "kind", "map_id", "x", "z", "pawn_id", "end_mode", "danger", "offset", "limit");
            string kind = CQFAIRuntimeRequest.Text(request, "kind");
            if (kind == "reachability")
            {
                Pawn pawn = CQFAIRuntimeRequest.Pawn(request);
                if (!pawn.Spawned || pawn.Map == null) throw new InvalidDataException("CQF_AI_InvalidValue: reachability requires a spawned Pawn");
                IntVec3 cell = Cell(request, pawn.Map);
                if (!Enum.TryParse(CQFAIRuntimeRequest.Text(request, "end_mode", "Touch"), out PathEndMode mode) || !Enum.IsDefined(typeof(PathEndMode), mode)
                    || !Enum.TryParse(CQFAIRuntimeRequest.Text(request, "danger", "Deadly"), out Danger danger) || !Enum.IsDefined(typeof(Danger), danger)) throw new InvalidDataException("CQF_AI_InvalidValue: reachability parameters");
                return new XElement("reachability", new XAttribute("pawnId", pawn.ThingID), new XAttribute("x", cell.x), new XAttribute("z", cell.z), new XAttribute("reachable", pawn.CanReach(cell, mode, danger)), new XAttribute("endMode", mode), new XAttribute("danger", danger));
            }
            Map map = CQFAIRuntimeRequest.Map(request);
            if (kind == "conditions") return CQFAIRuntimeRequest.Page("conditions", map.GameConditionManager.ActiveConditions.Select(condition => new XElement("condition", new XAttribute("def", condition.def.defName), new XAttribute("type", condition.GetType().FullName!), new XAttribute("ticksLeft", condition.TicksLeft), new XAttribute("permanent", condition.Permanent))), request);
            if (kind != "cell") throw new InvalidDataException("CQF_AI_InvalidTool: diagnostics kind");
            IntVec3 position = Cell(request, map); Room? room = position.GetRoom(map); RoofDef? roof = map.roofGrid.RoofAt(position);
            return new XElement("cell", new XAttribute("x", position.x), new XAttribute("z", position.z), new XAttribute("walkable", position.Walkable(map)), new XAttribute("roof", roof?.defName ?? ""),
                new XAttribute("roofSupported", roof == null || !roof.canCollapse || RoofCollapseUtility.WithinRangeOfRoofHolder(position, map)), room == null ? null : new XElement("room", new XAttribute("id", room.ID), new XAttribute("cells", room.CellCount), new XAttribute("openRoofCount", room.OpenRoofCount), new XAttribute("outdoors", room.PsychologicallyOutdoors), new XAttribute("role", room.Role?.defName ?? "")));
        }
        public void AddInstruction(string text) { command += "\n" + text; }
        public XElement Operate(XElement request)
        {
            string kind = CQFAIRuntimeRequest.Text(request, "kind");
            if (kind == "generate_submap")
            {
                CQFAIRuntimeRequest.Fields(request, "kind", "thing_id");
                CustomMapEntrance entrance = CQFAIRuntimeRequest.Thing(CQFAIRuntimeRequest.Text(request, "thing_id")) as CustomMapEntrance ?? throw new InvalidDataException("CQF_AI_InvalidValue: map entrance required");
                if (entrance.CustomMap != null) return new XElement("submap", new XAttribute("existing", true), new XAttribute("mapId", entrance.CustomMap.uniqueID));
                if (entrance.exit != null || entrance.MapDef == null || entrance.MapDef.isPart || entrance.MapDef.size.x < 1 || entrance.MapDef.size.z < 1) throw new InvalidDataException("CQF_AI_InvalidValue: configured complete map definition required");
                CQFAIChanges.Validate(entrance.MapDef);
                string seed = Find.World.info.seedString;
                journal.MarkIrreversible();
                try { entrance.GenerateCustomMap(entrance.Map, null!); } finally { Find.World.info.seedString = seed; }
                if (entrance.CustomMap == null || !Current.Game.Maps.Contains(entrance.CustomMap)) throw new InvalidDataException("CQF_AI_ApplyMismatch: map generation");
                return new XElement("submap", new XAttribute("undoSupported", false), MapSummary(entrance.CustomMap));
            }
            if (kind == "link_portals") return Link(request);
            if (kind is "main_site_cache" or "main_site_visits") return SiteEdit(request);
            if (kind == "map_queues_configure") return QueueEdit(request);
            if (kind != "world_map_configure") throw new InvalidDataException("CQF_AI_InvalidTool: world operation");
            CQFAIRuntimeRequest.Fields(request, "kind", "map_id", "custom_name", "permanent", "level");
            Map map = CQFAIRuntimeRequest.Map(request);
            MapParent_Custom parent = map.Parent as MapParent_Custom ?? throw new InvalidDataException("CQF_AI_InvalidValue: custom map parent required");
            string? oldName = parent.customName, name = request.Element("custom_name") == null ? oldName : CQFAIRuntimeRequest.Text(request, "custom_name");
            if (!text && name != oldName && !string.IsNullOrEmpty(name) && !name.CanTranslate() && !command.Contains(name) && !System.Text.RegularExpressions.Regex.IsMatch(name, "^CQF_[A-Za-z0-9_]+$")) throw new InvalidDataException("CQF_AI_TextNotAllowed: custom_name");
            bool oldPermanent = parent.permanent, permanent = CQFAIRuntimeRequest.Bool(request, "permanent", oldPermanent);
            int oldLevel = parent.level, level = CQFAIRuntimeRequest.Int(request, "level", oldLevel);
            if (level < 0 || level > 100000) throw new InvalidDataException("CQF_AI_InvalidValue: map level");
            return journal.Edit("mapParent:" + map.uniqueID, () => { parent.customName = name!; parent.permanent = permanent; parent.level = level; },
                () => { parent.customName = oldName!; parent.permanent = oldPermanent; parent.level = oldLevel; }, () => MapSummary(map));
        }
        public static CQFAISpawnQueueConfiguration Queues(Map map)
        {
            MapComponent_CustomMapData component = map.GetComponent<MapComponent_CustomMapData>() ?? throw new InvalidDataException("CQF_AI_MissingResource: map CQF component");
            return new CQFAISpawnQueueConfiguration
            {
                routes = (component.route ?? new Dictionary<string, Route>()).ToDictionary(pair => pair.Key, pair => pair.Value.route == null ? new List<IntVec3>() : new List<IntVec3>(pair.Value.route)),
                timed = component.pawnSpawnDatas_Tick ?? new List<PawnDataWithPosAndTime>(),
                building = (component.pawnSpawnDatas_Building ?? new Dictionary<Building, List<PawnSpawnData>>()).Where(pair => pair.Key != null && pair.Key.Spawned).ToDictionary(pair => pair.Key.ThingID, pair => pair.Value)
            };
        }
        private XElement QueueEdit(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "map_id", "changes_xml");
            Map map = CQFAIRuntimeRequest.Map(request); MapComponent_CustomMapData component = map.GetComponent<MapComponent_CustomMapData>();
            CQFAISpawnQueueConfiguration after = (CQFAISpawnQueueConfiguration)new CQFAIChanges(model).Build(Queues(map), CQFAIChanges.Parse(CQFAIRuntimeRequest.Text(request, "changes_xml")), command, text);
            if (after.routes == null || after.timed == null || after.building == null || after.routes.Count > 256 || after.timed.Count > 256 || after.building.Count > 256
                || after.routes.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null || pair.Value.Count < 1 || pair.Value.Count > 1024 || pair.Value.Any(cell => !cell.InBounds(map)))
                || after.timed.Any(data => data == null || data.time < 0 || !data.position.InBounds(map) || data.data == null)
                || after.building.Values.Any(values => values == null || values.Count > 256 || values.Any(data => data == null))) throw new InvalidDataException("CQF_AI_InvalidValue: queues/routes");
            Dictionary<Building, List<PawnSpawnData>> building = after.building.ToDictionary(pair => CQFAIRuntimeRequest.Thing(pair.Key) is Building target && target.Map == map ? target : throw new InvalidDataException("CQF_AI_InvalidValue: queued Building map/type"), pair => pair.Value);
            CQFAILiveFeatureValidation.Validate(new CQFAILiveThingConfiguration { spawner = new CQFAISpawnerConfiguration { pawns = after.timed.Select(data => data.data).Concat(after.building.Values.SelectMany(values => values)).ToList() } });
            foreach (PawnSpawnData recipe in after.timed.Select(data => data.data).Concat(after.building.Values.SelectMany(values => values))) CQFAIPawnRuntime.ValidateSpawnData(recipe);
            var oldRoutes = component.route; var oldTimed = component.pawnSpawnDatas_Tick; var oldBuilding = component.pawnSpawnDatas_Building;
            Dictionary<string, Route> routes = after.routes.ToDictionary(pair => pair.Key, pair => new Route { route = pair.Value });
            return journal.Edit("mapQueues:" + map.uniqueID, () => { component.route = routes; component.pawnSpawnDatas_Tick = after.timed; component.pawnSpawnDatas_Building = building; },
                () => { component.route = oldRoutes; component.pawnSpawnDatas_Tick = oldTimed; component.pawnSpawnDatas_Building = oldBuilding; }, () => model.Write(Queues(map), root: true));
        }
        private XElement Link(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "entrance_id", "exit_id");
            CustomMapEntrance entrance = CQFAIRuntimeRequest.Thing(CQFAIRuntimeRequest.Text(request, "entrance_id")) as CustomMapEntrance ?? throw new InvalidDataException("CQF_AI_InvalidValue: entrance");
            CustomMapExit exit = CQFAIRuntimeRequest.Thing(CQFAIRuntimeRequest.Text(request, "exit_id")) as CustomMapExit ?? throw new InvalidDataException("CQF_AI_InvalidValue: exit");
            if (entrance.Map == exit.Map || entrance.exit != null && entrance.exit != exit || exit.entrance != null && exit.entrance != entrance || entrance.CustomMap != null && entrance.CustomMap != exit.Map) throw new InvalidDataException("CQF_AI_InvalidValue: portal link conflict");
            MapParent_Custom? parent = exit.Map.Parent as MapParent_Custom;
            if (parent?.entrance != null && parent.entrance != entrance || parent?.exit != null && parent.exit != exit || parent?.sourceMap != null && parent.sourceMap != entrance.Map) throw new InvalidDataException("CQF_AI_InvalidValue: map parent link conflict");
            CustomMapExit? oldExit = entrance.exit; CustomMapEntrance? oldEntrance = exit.entrance; Map? oldMap = entrance.CustomMap;
            string? oldName = entrance.exitName; CustomMapEntrance? oldParentEntrance = parent?.entrance; CustomMapExit? oldParentExit = parent?.exit;
            Map? oldSource = parent?.sourceMap; IntVec3 oldEnterSpot = parent?.enterSpot ?? IntVec3.Invalid;
            return journal.Edit("portal:" + entrance.ThingID + ":" + exit.ThingID,
                () => { entrance.exit = exit; entrance.CustomMap = exit.Map; entrance.exitName = exit.exitName; exit.entrance = entrance; if (parent != null) { parent.entrance = entrance; parent.exit = exit; parent.sourceMap = entrance.Map; parent.enterSpot = exit.Position; } },
                () => { entrance.exit = oldExit!; entrance.CustomMap = oldMap!; entrance.exitName = oldName!; exit.entrance = oldEntrance!; if (parent != null) { parent.entrance = oldParentEntrance!; parent.exit = oldParentExit!; parent.sourceMap = oldSource!; parent.enterSpot = oldEnterSpot; } },
                () => new XElement("portalLink", PortalSummary(entrance), PortalSummary(exit), new XAttribute("parentEntrance", parent?.entrance?.ThingID ?? ""), new XAttribute("parentExit", parent?.exit?.ThingID ?? ""), new XAttribute("sourceMapId", parent?.sourceMap?.uniqueID ?? -1), new XAttribute("enterSpot", parent?.enterSpot.ToString() ?? "")));
        }
        private XElement SiteEdit(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "site_id", "key", "pawn_id", "remove", "count");
            MainSite site = Sites().FirstOrDefault(site => site.ID == CQFAIRuntimeRequest.Int(request, "site_id")) ?? throw new InvalidDataException("CQF_AI_MissingResource: MainSite");
            if (CQFAIRuntimeRequest.Text(request, "kind") == "main_site_visits")
            {
                int count = CQFAIRuntimeRequest.Int(request, "count"), before = site.visitCount;
                if (count < 0 || count > 1000000) throw new InvalidDataException("CQF_AI_InvalidValue: visit count");
                return journal.Edit("mainSiteVisits:" + site.ID, () => site.visitCount = count, () => site.visitCount = before, () => new XElement("visits", site.visitCount));
            }
            string key = CQFAIRuntimeRequest.Text(request, "key");
            if (string.IsNullOrWhiteSpace(key) || key.Length > 200) throw new InvalidDataException("CQF_AI_InvalidValue: NPC cache key");
            Pawn? pawn = CQFAIRuntimeRequest.Bool(request, "remove") ? null : CQFAIRuntimeRequest.Pawn(request);
            var old = site.mainPawns; var next = old == null ? new Dictionary<string, Pawn>() : new Dictionary<string, Pawn>(old);
            if (pawn == null) next.Remove(key); else next[key] = pawn;
            return journal.Edit("mainSiteCache:" + site.ID, () => site.mainPawns = next, () => site.mainPawns = old!,
                () => new XElement("cache", (site.mainPawns ?? new Dictionary<string, Pawn>()).OrderBy(pair => pair.Key).Select(pair => new XElement("pawn", new XAttribute("key", pair.Key), new XAttribute("id", pair.Value?.ThingID ?? "")))));
        }
        private static IEnumerable<MainSite> Sites() => MainMapWorldComponent.Component?.GetAllMainSites() ?? new List<MainSite>();
        private static IntVec3 Cell(XElement request, Map map) { IntVec3 cell = new IntVec3(CQFAIRuntimeRequest.Int(request, "x"), 0, CQFAIRuntimeRequest.Int(request, "z")); return cell.InBounds(map) ? cell : throw new InvalidDataException("CQF_AI_MapBounds"); }
        private static XElement MapSummary(Map map) => new XElement("map", new XAttribute("id", map.uniqueID), new XAttribute("width", map.Size.x), new XAttribute("height", map.Size.z), new XAttribute("parentType", map.Parent?.GetType().FullName ?? ""),
            new XAttribute("sourceMapId", (map.Parent as PocketMapParent)?.sourceMap?.uniqueID ?? -1), new XAttribute("definition", (map.Parent as MapParent_Custom)?.mapDataDef?.defName ?? ""), new XAttribute("customName", (map.Parent as MapParent_Custom)?.customName ?? ""),
            new XAttribute("level", (map.Parent as MapParent_Custom)?.level ?? 0), new XAttribute("permanent", (map.Parent as MapParent_Custom)?.permanent ?? false), new XAttribute("questId", (map.Parent as MapParent_Custom)?.quest?.id ?? -1), new XAttribute("rootSiteId", (map.Parent as MapParent_Custom)?.rootSite?.ID ?? -1));
        private static XElement PortalSummary(CQFMapPortal portal) => new XElement("portal", new XAttribute("id", portal.ThingID), new XAttribute("type", portal.GetType().FullName!), new XAttribute("mapId", portal.Map?.uniqueID ?? -1),
            new XAttribute("destinationMapId", portal is CustomMapEntrance entrance ? entrance.CustomMap?.uniqueID ?? -1 : portal is CustomMapExit exit ? exit.entrance?.Map?.uniqueID ?? -1 : -1),
            new XAttribute("partnerId", portal is CustomMapEntrance input ? input.exit?.ThingID ?? "" : portal is CustomMapExit output ? output.entrance?.ThingID ?? "" : ""),
            new XAttribute("exitName", portal is CustomMapEntrance namedEntrance ? namedEntrance.exitName ?? "" : portal is CustomMapExit namedExit ? namedExit.exitName ?? "" : ""));
        private readonly CQFAIModel model;
        private readonly CQFAIRuntimeJournal journal;
        private readonly bool text;
        private string command;
    }
}
