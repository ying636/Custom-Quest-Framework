using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAILiveMapPlan
    {
        public CQFAILiveMapPlan(CQFAILiveMap backend, CQFAIModel model, string command, bool generateText)
        {
            this.backend = backend;
            map = backend.Map;
            this.model = model;
            this.command = command;
            this.generateText = generateText;
        }
        public IReadOnlyList<CQFAILiveMapEdit> Build(XElement changes)
        {
            foreach (XElement operation in changes.Elements())
            {
                if (operation.Name == "editThing") { EditThing(operation); continue; }
                if (operation.HasElements || !string.IsNullOrWhiteSpace(operation.Value)) throw new InvalidDataException("CQF_AI_InvalidChanges");
                int x = Number(operation, "x"), z = Number(operation, "z");
                if (operation.Name == "place") { Place(operation, new IntVec3(x, 0, z)); continue; }
                int width = Number(operation, "width", 1), height = Number(operation, "height", 1);
                if (width < 1 || height < 1 || x < 0 || z < 0 || (long)x + width > map.Size.x || (long)z + height > map.Size.z || (long)width * height > 4096)
                    throw new InvalidDataException("CQF_AI_MapBounds");
                CellRect region = new CellRect(x, z, width, height);
                if (operation.Name == "erase")
                {
                    Attributes(operation, "x", "z", "width", "height", "category");
                    string category = operation.Attribute("category")?.Value ?? "Building";
                    if (!Enum.TryParse(category, out ThingCategory filter) || filter is not (ThingCategory.Building or ThingCategory.Item or ThingCategory.Plant or ThingCategory.Filth))
                        throw new InvalidDataException("CQF_AI_InvalidValue: erase category");
                    if (planned.Any(thing => GenAdj.OccupiedRect(thing.Position, thing.Rotation, thing.def.size).Overlaps(region)))
                        throw new InvalidDataException("CQF_AI_InvalidChanges: erase before place, or use another tool call");
                    foreach (Thing thing in region.SelectMany(cell => cell.GetThingList(map)).Distinct().Where(thing => thing.def.category == filter).ToArray()) Remove(thing);
                }
                else if (operation.Name == "terrain")
                {
                    Attributes(operation, "def", "x", "z", "width", "height");
                    TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(operation.Attribute("def")?.Value ?? "") ?? throw new InvalidDataException("CQF_AI_MissingResource: terrain");
                    if (terrain.isFoundation || terrain.temporary) throw new InvalidDataException("CQF_AI_InvalidValue: foundation or temporary terrain is not supported");
                    foreach (IntVec3 cell in region) Terrain(cell, terrain);
                }
                else if (operation.Name == "roof")
                {
                    Attributes(operation, "def", "x", "z", "width", "height");
                    string name = operation.Attribute("def")?.Value ?? throw new InvalidDataException("CQF_AI_InvalidValue: roof");
                    RoofDef? roof = name.Length == 0 ? null : DefDatabase<RoofDef>.GetNamedSilentFail(name) ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
                    foreach (IntVec3 cell in region) Roof(cell, roof);
                }
                else throw new InvalidDataException("CQF_AI_LiveMapOperationRequired: " + operation.Name);
            }
            if (edits.Count > 10000) throw new InvalidDataException("CQF_AI_ToolLimit");
            return edits;
        }
        private void Place(XElement operation, IntVec3 cell)
        {
            if (edits.Count >= 10000) throw new InvalidDataException("CQF_AI_ToolLimit");
            Attributes(operation, "def", "stuff", "x", "z", "rotation", "count", "faction");
            string name = operation.Attribute("def")?.Value ?? "";
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name) ?? throw new InvalidDataException("CQF_AI_MissingResource: " + name);
            if (def.category is not (ThingCategory.Building or ThingCategory.Item or ThingCategory.Plant) || def.IsBlueprint || def.IsFrame || def.IsCorpse || def.randomizeRotationOnSpawn)
                throw new InvalidDataException("CQF_AI_InvalidValue: live placement type");
            int rotation = Number(operation, "rotation", 0), count = Number(operation, "count", 1);
            if (rotation < 0 || rotation > 3 || count < 1 || count > def.stackLimit) throw new InvalidDataException("CQF_AI_InvalidValue: placement");
            ThingDef? stuff = operation.Attribute("stuff") is XAttribute material ? DefDatabase<ThingDef>.GetNamedSilentFail(material.Value) : null;
            if (def.MadeFromStuff && (stuff == null || !GenStuff.AllowedStuffsFor(def).Contains(stuff)) || !def.MadeFromStuff && operation.Attribute("stuff") != null)
                throw new InvalidDataException("CQF_AI_InvalidValue: stuff");
            string faction = operation.Attribute("faction")?.Value ?? "player";
            if (faction != "player" && faction != "none") throw new InvalidDataException("CQF_AI_InvalidValue: faction");
            Rot4 rot = new Rot4(rotation);
            CellRect footprint = GenAdj.OccupiedRect(cell, rot, def.size);
            if (!footprint.InBounds(map)) throw new InvalidDataException("CQF_AI_MapBounds");
            foreach (Thing existing in footprint.SelectMany(position => position.GetThingList(map)).Distinct().Where(thing => !removed.Contains(thing)).ToArray())
            {
                if (existing is Pawn && def.passability == Traversability.Impassable) throw new InvalidDataException("CQF_AI_LiveMapBlocked: " + existing.ThingID);
                if (!GenSpawn.SpawningWipes(def, existing.def) && !(def.IsEdifice() && existing.def.IsEdifice())) continue;
                if (existing.def.category is ThingCategory.Plant or ThingCategory.Filth) Remove(existing);
                else throw new InvalidDataException("CQF_AI_LiveMapBlocked: " + existing.ThingID);
            }
            foreach (Thing existing in planned)
                if (GenAdj.OccupiedRect(existing.Position, existing.Rotation, existing.def.size).Overlaps(footprint)
                    && (GenSpawn.SpawningWipes(def, existing.def) || def.category == ThingCategory.Plant && existing.def.category == ThingCategory.Plant
                        || def.category == ThingCategory.Item && existing.def.category == ThingCategory.Item))
                    throw new InvalidDataException("CQF_AI_LiveMapBlocked: overlapping placements");
            if (def.category == ThingCategory.Item && footprint.Any(position => position.GetThingList(map).Any(thing => thing.def.category == ThingCategory.Item && !removed.Contains(thing))))
                throw new InvalidDataException("CQF_AI_LiveMapBlocked: existing items");
            if (def.category == ThingCategory.Plant && footprint.Any(position => position.GetThingList(map).Any(thing => thing.def.category == ThingCategory.Plant && !removed.Contains(thing))))
                throw new InvalidDataException("CQF_AI_LiveMapBlocked: existing plants");
            if (!def.CanSpawnAt(cell, rot, map)) throw new InvalidDataException("CQF_AI_LiveMapBlocked: " + name);
            Thing thing = ThingMaker.MakeThing(def, stuff);
            thing.Position = cell; thing.Rotation = rot; thing.stackCount = count;
            if (def.CanHaveFaction && faction == "player") thing.SetFaction(Faction.OfPlayer);
            planned.Add(thing);
            CQFAILiveMapCellState cells = new CQFAILiveMapCellState(map, thing);
            edits.Add(new CQFAILiveMapEdit("thing:" + thing.ThingID, () =>
            {
                cells.Capture();
                CheckSpawn(thing, cell, rot);
                GenSpawn.Spawn(thing, cell, map, rot, WipeMode.Vanish);
                if (!thing.Spawned || thing.Map != map || thing.Position != cell || thing.stackCount != count) throw new InvalidDataException("CQF_AI_ApplyMismatch: " + thing.ThingID);
            }, () => { if (thing.Spawned) Despawn(thing); cells.Restore(); }, () => ThingState(thing, footprint) + ":" + cells.Current,
                () => new XElement("placed", new XAttribute("thingId", thing.ThingID), new XAttribute("def", def.defName), new XAttribute("x", cell.x), new XAttribute("z", cell.z))));
        }
        private void Remove(Thing thing)
        {
            if (removed.Contains(thing)) return;
            if (edits.Count >= 10000) throw new InvalidDataException("CQF_AI_ToolLimit");
            if (thing is Pawn || thing is CustomMapEntrance entrance && entrance.exit != null || thing is CustomMapExit exit && exit.entrance != null)
                throw new InvalidDataException("CQF_AI_PortalLinked: " + thing.ThingID);
            if (configured.Contains(thing)) throw new InvalidDataException("CQF_AI_InvalidChanges: edit and erase the same thing in separate calls");
            removed.Add(thing);
            IntVec3 position = thing.Position; Rot4 rotation = thing.Rotation; CellRect footprint = thing.OccupiedRect();
            CQFAILiveMapCellState cells = new CQFAILiveMapCellState(map, thing);
            edits.Add(new CQFAILiveMapEdit("thing:" + thing.ThingID, () =>
            {
                cells.Capture();
                if (!thing.Spawned || thing.Map != map || thing.Position != position) throw new InvalidOperationException("CQF_AI_StaleTarget");
                Despawn(thing);
                if (thing.Spawned || thing.Destroyed) throw new InvalidDataException("CQF_AI_ApplyMismatch: " + thing.ThingID);
            }, () =>
            {
                if (thing.Destroyed) throw new InvalidOperationException("CQF_AI_RollbackMismatch: " + thing.ThingID);
                if (!thing.Spawned) { CheckSpawn(thing, position, rotation); GenSpawn.Spawn(thing, position, map, rotation, WipeMode.Vanish); }
                cells.Restore();
            }, () => ThingState(thing, footprint) + ":" + cells.Current, () => new XElement("removed", new XAttribute("thingId", thing.ThingID))));
        }
        private void Terrain(IntVec3 cell, TerrainDef terrain)
        {
            if (edits.Count >= 10000) throw new InvalidDataException("CQF_AI_ToolLimit");
            if (map.terrainGrid.TempTerrainAt(cell) != null || map.terrainGrid.FoundationAt(cell) != null) throw new InvalidDataException("CQF_AI_InvalidValue: layered terrain");
            if (cell.GetThingList(map).Any(thing => thing.def.IsBlueprint || thing.def.IsFrame)) throw new InvalidDataException("CQF_AI_LiveMapBlocked: blueprint or frame");
            foreach (Thing thing in cell.GetThingList(map).Where(thing => !removed.Contains(thing)).ToArray())
                if (thing.def.category == ThingCategory.Plant && (!thing.def.plant.completelyIgnoreFertility && terrain.fertility < thing.def.plant.fertilityMin
                    || thing.def.plant.WildTerrainTags.Count > 0 && !thing.def.plant.WildTerrainTags.Overlaps(terrain.tags ?? new List<string>()))
                    || thing.def.category == ThingCategory.Filth && !FilthMaker.TerrainAcceptsFilth(terrain, thing.def)) Remove(thing);
            TerrainDef? top = null, under = null; ColorDef? color = null; float snow = 0f, sand = 0f;
            List<Designation>? designations = null;
            edits.Add(new CQFAILiveMapEdit("terrain:" + cell.x + ":" + cell.z, () =>
            {
                top = map.terrainGrid.TopTerrainAt(cell); under = map.terrainGrid.UnderTerrainAt(cell); color = map.terrainGrid.ColorAt(cell);
                snow = map.snowGrid.GetDepth(cell); sand = map.sandGrid?.GetDepth(cell) ?? 0f;
                designations = map.designationManager.AllDesignationsAt(cell).Where(designation => !designation.target.HasThing).ToList();
                map.terrainGrid.SetTerrain(cell, terrain);
                if (map.terrainGrid.TopTerrainAt(cell) != terrain) throw new InvalidDataException("CQF_AI_ApplyMismatch: terrain");
            }, () =>
            {
                if (top == null) return;
                map.terrainGrid.SetTerrain(cell, top);
                if (under != null) map.terrainGrid.SetUnderTerrain(cell, under);
                else ((TerrainDef[])UnderGrid.GetValue(map.terrainGrid)!)[map.cellIndices.CellToIndex(cell)] = null!;
                map.terrainGrid.SetTerrainColor(cell, color);
                map.snowGrid.SetDepth(cell, snow); map.sandGrid?.SetDepth(cell, sand);
                foreach (Designation designation in designations!)
                    if (map.designationManager.DesignationAt(cell, designation.def) == null) map.designationManager.AddDesignation(designation);
            }, () => TerrainState(cell), () => new XElement("terrain", new XAttribute("x", cell.x), new XAttribute("z", cell.z), new XAttribute("def", terrain.defName))));
        }
        private void Roof(IntVec3 cell, RoofDef? roof)
        {
            if (edits.Count >= 10000) throw new InvalidDataException("CQF_AI_ToolLimit");
            RoofDef? before = null;
            edits.Add(new CQFAILiveMapEdit("roof:" + cell.x + ":" + cell.z, () =>
            {
                before = map.roofGrid.RoofAt(cell); map.roofGrid.SetRoof(cell, roof);
                if (map.roofGrid.RoofAt(cell) != roof) throw new InvalidDataException("CQF_AI_ApplyMismatch: roof");
            }, () => map.roofGrid.SetRoof(cell, before), () => map.roofGrid.RoofAt(cell)?.defName ?? "",
                () => new XElement("roof", new XAttribute("x", cell.x), new XAttribute("z", cell.z), new XAttribute("def", roof?.defName ?? ""))));
        }
        private void EditThing(XElement operation)
        {
            if (edits.Count >= 10000) throw new InvalidDataException("CQF_AI_ToolLimit");
            Attributes(operation, "id");
            if (operation.Elements().Count() != 1 || operation.Element("changes") == null) throw new InvalidDataException("CQF_AI_InvalidChanges: editThing");
            Thing thing = backend.FindThing(operation.Attribute("id")?.Value ?? "");
            if (thing is Pawn || removed.Contains(thing) || !configured.Add(thing)) throw new InvalidDataException("CQF_AI_InvalidChanges: duplicate or unavailable thing");
            CQFAILiveThingConfiguration before = (CQFAILiveThingConfiguration)model.Copy(CQFAILiveMap.Configuration(thing));
            CQFAILiveThingConfiguration after = (CQFAILiveThingConfiguration)new CQFAIChanges(model).Build(before, operation.Element("changes")!, command, generateText);
            CQFAILiveMap.ValidateConfiguration(thing, after);
            CellRect footprint = thing.OccupiedRect();
            CQFAILiveMapCellState cells = new CQFAILiveMapCellState(map, thing);
            edits.Add(new CQFAILiveMapEdit("thing:" + thing.ThingID, () =>
            {
                CQFAILiveMap.ApplyConfiguration(thing, after);
                if (!XNode.DeepEquals(model.Write(CQFAILiveMap.Configuration(thing)), model.Write(after))) throw new InvalidDataException("CQF_AI_ApplyMismatch: configuration");
            }, () => CQFAILiveMap.ApplyConfiguration(thing, before), () => ThingState(thing, footprint) + ":" + cells.Current,
                () => new XElement("edited", new XAttribute("thingId", thing.ThingID), new CQFAITargetReader(model).Summary(CQFAILiveMap.Configuration(thing)))));
        }
        private void CheckSpawn(Thing thing, IntVec3 cell, Rot4 rotation)
        {
            foreach (Thing existing in GenAdj.OccupiedRect(cell, rotation, thing.def.size).SelectMany(position => position.GetThingList(map)).Distinct())
                if (existing != thing && (GenSpawn.SpawningWipes(thing.def, existing.def) || thing.def.IsEdifice() && existing.def.IsEdifice() || existing is Pawn && thing.def.passability == Traversability.Impassable
                    || thing.def.category == ThingCategory.Item && existing.def.category == ThingCategory.Item
                    || thing.def.category == ThingCategory.Plant && existing.def.category == ThingCategory.Plant))
                    throw new InvalidDataException("CQF_AI_LiveMapBlocked: " + existing.ThingID);
            if (!thing.def.CanSpawnAt(cell, rotation, map)) throw new InvalidDataException("CQF_AI_LiveMapBlocked: " + thing.ThingID);
        }
        private string ThingState(Thing thing, CellRect footprint) => thing.Spawned + ":" + thing.Destroyed + ":" + thing.Position + ":" + thing.Rotation.AsInt + ":"
            + thing.Faction?.GetUniqueLoadID() + ":" + (thing is Pawn ? "" : model.Write(CQFAILiveMap.Configuration(thing)).ToString(SaveOptions.DisableFormatting)) + ":"
            + string.Join(",", footprint.SelectMany(cell => cell.GetThingList(map)).Where(item => item is not Pawn).Distinct().Select(item => item.ThingID).OrderBy(id => id));
        private string TerrainState(IntVec3 cell) => map.terrainGrid.TopTerrainAt(cell).defName + ":" + map.terrainGrid.UnderTerrainAt(cell)?.defName + ":"
            + map.terrainGrid.TempTerrainAt(cell)?.defName + ":" + map.terrainGrid.FoundationAt(cell)?.defName + ":" + map.terrainGrid.ColorAt(cell)?.defName
            + ":" + map.snowGrid.GetDepth(cell).ToString("R", CultureInfo.InvariantCulture) + ":" + (map.sandGrid?.GetDepth(cell) ?? 0f).ToString("R", CultureInfo.InvariantCulture);
        private static void Despawn(Thing thing)
        {
            bool leaveTerrain = thing is Building building && building.canChangeTerrainOnDestroyed;
            try
            {
                if (thing is Building before) before.canChangeTerrainOnDestroyed = false;
                thing.DeSpawn(DestroyMode.WillReplace);
            }
            finally { if (thing is Building after) after.canChangeTerrainOnDestroyed = leaveTerrain; }
        }
        private static int Number(XElement operation, string name, int? fallback = null)
        {
            if (operation.Attribute(name) == null && fallback != null) return fallback.Value;
            return int.TryParse(operation.Attribute(name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : throw new InvalidDataException("CQF_AI_InvalidValue: " + name);
        }
        private static void Attributes(XElement operation, params string[] allowed)
        {
            if (operation.Attributes().Any(attribute => !allowed.Contains(attribute.Name.ToString()))) throw new InvalidDataException("CQF_AI_InvalidChanges: attributes");
        }
        private readonly CQFAILiveMap backend;
        private readonly Map map;
        private readonly CQFAIModel model;
        private readonly string command;
        private readonly bool generateText;
        private readonly List<CQFAILiveMapEdit> edits = new List<CQFAILiveMapEdit>();
        private readonly HashSet<Thing> removed = new HashSet<Thing>();
        private readonly HashSet<Thing> configured = new HashSet<Thing>();
        private readonly List<Thing> planned = new List<Thing>();
        private static readonly FieldInfo UnderGrid = typeof(TerrainGrid).GetField("underGrid", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(TerrainGrid).FullName, "underGrid");
    }
}
