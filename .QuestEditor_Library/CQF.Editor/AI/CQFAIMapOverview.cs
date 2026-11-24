using System.Globalization;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIMapOverview
    {
        public CQFAIMapOverview(Func<IntVec3, (string Terrain, bool Roofed, bool Walkable)> read)
        {
            this.read = read;
        }

        public XElement Describe(CellRect region, int divisions) => Describe(region, divisions, false);

        public XElement Describe(CellRect region, int divisions, bool compact)
        {
            if (region.Width < 1 || region.Height < 1) throw new InvalidDataException("CQF_AI_MapBounds");
            if (divisions < 1 || divisions > 16) throw new InvalidDataException("CQF_AI_InvalidTool: divisions");
            int columns = Math.Min(divisions, region.Width);
            int rows = Math.Min(divisions, region.Height);
            XElement tiles = compact
                ? new XElement("grid", new XAttribute("fields", "terrain,matching,roofed,blocked,n"),
                    new XAttribute("xEdges", Edges(region.minX, region.Width, columns)),
                    new XAttribute("zEdges", Edges(region.minZ, region.Height, rows)))
                : new XElement("tiles");
            Dictionary<string, int> terrainIds = new Dictionary<string, int>(StringComparer.Ordinal);
            int samples = 0;
            for (int row = 0; row < rows; row++)
            {
                List<string>? rowValues = compact ? new List<string>(columns) : null;
                int minZ = Partition(region.minZ, region.Height, row, rows);
                int endZ = Partition(region.minZ, region.Height, row + 1, rows);
                for (int column = 0; column < columns; column++)
                {
                    int minX = Partition(region.minX, region.Width, column, columns);
                    int endX = Partition(region.minX, region.Width, column + 1, columns);
                    int width = endX - minX;
                    int height = endZ - minZ;
                    int sampleColumns = Math.Min(width, 3);
                    int sampleRows = Math.Min(height, 3);
                    int tileSamples = sampleColumns * sampleRows;
                    int blocked = 0, roofed = 0;
                    Dictionary<string, int> terrainCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                    for (int z = 0; z < sampleRows; z++)
                    {
                        int sampleZ = Partition(minZ, height, z * 2 + 1, sampleRows * 2);
                        for (int x = 0; x < sampleColumns; x++)
                        {
                            int sampleX = Partition(minX, width, x * 2 + 1, sampleColumns * 2);
                            var cell = read(new IntVec3(sampleX, 0, sampleZ));
                            if (!cell.Walkable) blocked++;
                            if (cell.Roofed) roofed++;
                            terrainCounts.TryGetValue(cell.Terrain, out int count);
                            terrainCounts[cell.Terrain] = count + 1;
                        }
                    }
                    var dominant = terrainCounts.OrderByDescending(terrain => terrain.Value).ThenBy(terrain => terrain.Key, StringComparer.Ordinal).First();
                    if (!terrainIds.TryGetValue(dominant.Key, out int id))
                    {
                        id = terrainIds.Count;
                        terrainIds.Add(dominant.Key, id);
                    }
                    if (rowValues != null)
                        rowValues.Add(FormattableString.Invariant($"{id},{dominant.Value},{roofed},{blocked},{tileSamples}"));
                    else tiles.Add(new XElement("tile", new XAttribute("x", minX), new XAttribute("z", minZ),
                        new XAttribute("w", width), new XAttribute("h", height), new XAttribute("terrain", id),
                        new XAttribute("matching", dominant.Value), new XAttribute("roofed", roofed),
                        new XAttribute("blocked", blocked), new XAttribute("n", tileSamples)));
                    samples += tileSamples;
                }
                if (rowValues != null) tiles.Add(new XElement("row", string.Join(" ", rowValues)));
            }

            return new XElement("overview", new XAttribute("sampled", true), new XAttribute("samples", samples),
                new XAttribute("columns", columns), new XAttribute("rows", rows),
                new XElement("terrains", terrainIds.OrderBy(pair => pair.Value).Select(terrain =>
                    new XElement("terrain", new XAttribute("id", terrain.Value), new XAttribute("def", terrain.Key)))),
                tiles);
        }

        private static string Edges(int start, int length, int parts) =>
            string.Join(",", Enumerable.Range(0, parts + 1)
                .Select(part => Partition(start, length, part, parts).ToString(CultureInfo.InvariantCulture)));

        private static int Partition(int start, int length, int part, int count)
        {
            return start + (int)((long)length * part / count);
        }

        private readonly Func<IntVec3, (string Terrain, bool Roofed, bool Walkable)> read;
    }
}
