using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed partial class CQFLargeMapDraft
    {
        public string Export(string name)
        {
            RequireIdle();
            XmlConvert.VerifyNCName(name);
            if (!name.StartsWith("CQF_", StringComparison.Ordinal)) throw new InvalidDataException("CQF_AI_InvalidName");
            if (!CanUndo && (BlockedRoad.Count > 0 || placementConflicts.Count > 0))
                throw new InvalidDataException("CQF_AI_LargeDraftExportConflict");
            CustomMapDataDef definition = new CustomMapDataDef { defName = name, label = name };
            definition.LoadData(Map, captureCells: false);
            if (Map.GetComponent<MapComponent_CustomMapData>() is { } component)
                definition.lordDatas = component.Lords.Select(lord => lord.data).ToList();
            bool overlay = !CanUndo;
            var bounds = CellRect.WholeMap(Map);
            WriteRuns(bounds, cell => overlay ? EffectiveTerrain(cell) : Map.terrainGrid.TopTerrainAt(cell),
                (def, rect) => { if (def.defName != "QE_Null") Add(definition.terrainsRect, def.defName, rect); });
            WriteRuns(bounds, cell => overlay && roofs.TryGetValue(cell, out RoofDef draftRoof) ? draftRoof : Map.roofGrid.RoofAt(cell),
                (def, rect) => Add(definition.roofRects, def, rect));
            WriteRuns(bounds, cell => Map.terrainGrid.ColorAt(cell), (color, rect) => Add(definition.terrainsColorRect, color, rect));
            if (overlay) definition.thingDatas.AddRange(placements);
            string path = CQFAIDefDocument.PathFor(definition);
            string directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);
            if (File.Exists(path)) throw new IOException("CQF_AI_LargeDraftExportExists");
            string temporary = Path.Combine(directory, "." + name + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                new XDocument(new XElement("Defs", definition.SaveToXElement(typeof(CustomMapDataDef).FullName))).Save(temporary);
                File.Move(temporary, path);
                return path;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private static void WriteRuns<T>(CellRect bounds, Func<IntVec3, T?> read, Action<T, CellRect> write) where T : class
        {
            var previous = new Dictionary<(int x, int width, T value), CellRect>();
            var current = new Dictionary<(int x, int width, T value), CellRect>();
            for (int z = bounds.minZ; z <= bounds.maxZ; z++)
            {
                int start = bounds.minX;
                T? value = read(new IntVec3(start, 0, z));
                for (int x = start + 1; x <= bounds.maxX + 1; x++)
                {
                    T? next = x <= bounds.maxX ? read(new IntVec3(x, 0, z)) : null;
                    if (ReferenceEquals(value, next) && x <= bounds.maxX) continue;
                    if (value != null)
                    {
                        var key = (start, x - start, value);
                        current.Add(key, previous.TryGetValue(key, out CellRect above)
                            ? new CellRect(above.minX, above.minZ, above.Width, above.Height + 1)
                            : new CellRect(start, z, x - start, 1));
                        previous.Remove(key);
                    }
                    start = x;
                    value = next;
                }
                foreach (var item in previous) write(item.Key.value, item.Value);
                previous.Clear();
                (previous, current) = (current, previous);
            }
            foreach (var item in previous) write(item.Key.value, item.Value);
        }

        private static void Add<T>(Dictionary<T, List<CellRect>> destination, T key, CellRect rect) where T : notnull
        {
            if (!destination.TryGetValue(key, out List<CellRect> list))
                destination.Add(key, list = new List<CellRect>());
            list.Add(rect);
        }
    }
}
