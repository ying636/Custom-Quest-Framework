using System.Globalization;
using System.Xml.Linq;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    internal static class CQFAISceneChecks
    {
        public static XElement Read(Map map, string xml)
        {
            XElement request = CQFAIChanges.Parse(xml);
            XElement[] entries = request.Elements().ToArray();
            if (request.Name != "checks" || request.HasAttributes || entries.Length is < 1 or > 24 ||
                request.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                throw new InvalidDataException("CQF_AI_InvalidTool: scene checks");

            XElement[] results = entries.Select(entry => Read(entry, map)).ToArray();
            return new XElement("sceneChecks", new XAttribute("mapId", map.uniqueID),
                new XAttribute("passed", results.All(entry => (bool?)entry.Attribute("passed") == true)), results);
        }

        private static XElement Read(XElement entry, Map map)
        {
            string[] attributes = entry.Name.LocalName switch
            {
                "route" => new[] { "x", "z", "toX", "toZ" },
                "thing" or "terrain" => new[] { "x", "z", "def" },
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: scene check kind")
            };
            if (entry.Name.Namespace != XNamespace.None || entry.HasElements || !string.IsNullOrWhiteSpace(entry.Value)
                || entry.Attributes().Count() != attributes.Length || entry.Attributes().Any(attribute => !attributes.Contains(attribute.Name.ToString())))
                throw new InvalidDataException("CQF_AI_InvalidTool: scene check fields");

            IntVec3 position = Cell(entry, map, "x", "z");
            if (entry.Name == "route")
            {
                IntVec3 destination = Cell(entry, map, "toX", "toZ");
                bool startWalkable = position.Walkable(map), endWalkable = destination.Walkable(map);
                bool reachable = startWalkable && endWalkable &&
                    map.reachability.CanReach(position, destination, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly);
                return Result(entry, reachable, new XAttribute("startWalkable", startWalkable), new XAttribute("endWalkable", endWalkable));
            }

            string name = entry.Attribute("def")!.Value;
            if (name.Length is < 1 or > 150) throw new InvalidDataException("CQF_AI_InvalidTool: scene check def");
            if (entry.Name == "terrain")
            {
                string actual = map.terrainGrid.TerrainAt(position).defName;
                return Result(entry, name == actual, new XAttribute("actual", actual));
            }
            Thing? thing = position.GetThingList(map).FirstOrDefault(value => value.def.defName == name);
            return Result(entry, thing != null, thing == null ? null : new XAttribute("thingId", thing.ThingID));
        }

        private static XElement Result(XElement entry, bool passed, params XAttribute?[] details) =>
            new XElement(entry.Name, entry.Attributes(), new XAttribute("passed", passed), details);

        private static IntVec3 Cell(XElement entry, Map map, string x, string z)
        {
            if (!int.TryParse(entry.Attribute(x)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int xx)
                || !int.TryParse(entry.Attribute(z)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int zz)
                || xx < 0 || zz < 0 || xx >= map.Size.x || zz >= map.Size.z)
                throw new InvalidDataException("CQF_AI_MapBounds");
            return new IntVec3(xx, 0, zz);
        }
    }
}
