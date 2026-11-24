using System.Globalization;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed partial class CQFAILiveMap
    {
        public static XElement EncodeRegionPage(int mapId, int total, int offset, IReadOnlyList<IntVec3> cells,
            Func<IntVec3, (string Terrain, string? Roof, IReadOnlyList<Thing> Things)> read, Func<Thing, XElement> describe)
        {
            var terrains = new Dictionary<string, int>(StringComparer.Ordinal);
            var roofs = new Dictionary<string, int>(StringComparer.Ordinal);
            var things = new Dictionary<Thing, int>();
            var tuples = new List<string>(cells.Count);
            foreach (IntVec3 cell in cells)
            {
                var sample = read(cell);
                int terrain = Index(terrains, sample.Terrain);
                string roofIndex = sample.Roof == null ? "-" : Index(roofs, sample.Roof).ToString(CultureInfo.InvariantCulture);
                var contents = sample.Things;
                string references = contents.Count == 0 ? "-" : string.Join(":", contents.Take(20)
                    .Select(thing => Index(things, thing).ToString(CultureInfo.InvariantCulture)));
                tuples.Add(FormattableString.Invariant($"{cell.x},{cell.z},{terrain},{roofIndex},{contents.Count},{references}"));
            }
            return new XElement("region", new XAttribute("map", mapId), new XAttribute("total", total),
                new XAttribute("offset", offset), new XAttribute("encoding", "compact_cells_v1"),
                new XElement("terrains", terrains.OrderBy(entry => entry.Value).Select(entry => new XElement("def", entry.Key))),
                new XElement("roofs", roofs.OrderBy(entry => entry.Value).Select(entry => new XElement("def", entry.Key))),
                new XElement("cells", new XAttribute("fields", "x,z,terrain,roof,count,refs"), string.Join(" ", tuples)),
                new XElement("things", things.OrderBy(entry => entry.Value).Select(entry => describe(entry.Key))));
        }

        private XElement ReadRegionCompact(CellRect region, int offset, IReadOnlyList<IntVec3> cells) =>
            EncodeRegionPage(Map.uniqueID, region.Area, offset, cells,
                cell => (Map.terrainGrid.TerrainAt(cell).defName, Map.roofGrid.RoofAt(cell)?.defName, cell.GetThingList(Map)),
                DescribeRegionThing);

        private static int Index<T>(Dictionary<T, int> entries, T value) where T : notnull
        {
            if (!entries.TryGetValue(value, out int index))
            {
                index = entries.Count;
                entries.Add(value, index);
            }
            return index;
        }
    }
}
