using System.Globalization;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIRuntimeRequest
    {
        public static XElement Parse(string xml, string root)
        {
            XElement value = CQFAIChanges.Parse(xml);
            if (value.Name != root || value.HasAttributes || value.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value))
                || value.Elements().GroupBy(element => element.Name).Any(group => group.Count() > 1)) throw new InvalidDataException("CQF_AI_InvalidTool: " + root);
            return value;
        }
        public static void Fields(XElement request, params string[] fields)
        {
            if (request.HasAttributes || request.Elements().Any(element => element.Name.Namespace != XNamespace.None || !fields.Contains(element.Name.LocalName))
                || request.Elements().GroupBy(element => element.Name).Any(group => group.Count() > 1) || request.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                throw new InvalidDataException("CQF_AI_InvalidTool: unexpected/duplicate field");
        }
        public static string Text(XElement value, string name, string fallback = "")
        {
            XElement? element = value.Element(name);
            if (element?.HasAttributes == true || element?.HasElements == true) throw new InvalidDataException("CQF_AI_InvalidTool: " + name);
            string text = element?.Value ?? fallback;
            if (text.Length > (name.EndsWith("_xml", StringComparison.Ordinal) ? 65536 : 4000)) throw new InvalidDataException("CQF_AI_InvalidTool: " + name);
            return text;
        }
        public static int Int(XElement value, string name, int fallback = -1) => int.TryParse(Text(value, name, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result : throw new InvalidDataException("CQF_AI_InvalidTool: " + name);
        public static float Float(XElement value, string name, float fallback = 0)
        {
            if (!float.TryParse(Text(value, name, fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Float, CultureInfo.InvariantCulture, out float result) || float.IsNaN(result) || float.IsInfinity(result))
                throw new InvalidDataException("CQF_AI_InvalidTool: " + name);
            return result;
        }
        public static bool Bool(XElement value, string name, bool fallback = false) => bool.TryParse(Text(value, name, fallback.ToString()), out bool result)
            ? result : throw new InvalidDataException("CQF_AI_InvalidTool: " + name);
        public static T Def<T>(string name) where T : Def => DefDatabase<T>.AllDefsListForReading.FirstOrDefault(def => def.defName == name)
            ?? throw new InvalidDataException("CQF_AI_MissingResource: " + typeof(T).Name + " " + name);
        public static Map Map(XElement value)
        {
            int id = Int(value, "map_id");
            if (id < 0 && value.Element("map_id") != null) throw new InvalidDataException("CQF_AI_InvalidTool: map_id");
            return id < 0 ? Find.CurrentMap ?? throw new InvalidDataException("CQF_AI_MissingResource: current map")
                : Current.Game?.Maps.FirstOrDefault(map => map.uniqueID == id) ?? throw new InvalidDataException("CQF_AI_MissingResource: map " + id);
        }
        public static Thing Thing(string id) => Current.Game?.Maps.SelectMany(map => map.listerThings.AllThings).FirstOrDefault(thing => thing.ThingID == id && thing.Spawned)
            ?? (Thing?)Pawns().FirstOrDefault(pawn => pawn.ThingID == id) ?? throw new InvalidDataException("CQF_AI_LiveThingMissing: " + id);
        public static Pawn Pawn(XElement value) => Thing(Text(value, "pawn_id")) as Pawn ?? throw new InvalidDataException("CQF_AI_InvalidValue: Pawn required");
        public static IEnumerable<Pawn> Pawns()
        {
            if (Current.Game == null) return Array.Empty<Pawn>();
            IEnumerable<Pawn> maps = Current.Game.Maps.SelectMany(map => map.listerThings.AllThings.OfType<Pawn>().Where(pawn => pawn.Spawned));
            IEnumerable<Pawn> world = Current.Game.World?.worldPawns?.AllPawnsAliveOrDead ?? new List<Pawn>();
            IEnumerable<Pawn> cached = Current.Game.World == null ? Array.Empty<Pawn>() : (MainMapWorldComponent.Component?.GetAllMainSites() ?? new List<MainSite>())
                .SelectMany(site => site.mainPawns?.Values ?? Enumerable.Empty<Pawn>());
            IEnumerable<Pawn> dialogues = Current.Game.components?.OfType<GameComponent_Editor>().FirstOrDefault()?.dialogsWithTargets?.Keys.OfType<Pawn>() ?? Array.Empty<Pawn>();
            return maps.Concat(world).Concat(cached).Concat(dialogues).Where(pawn => pawn != null && !pawn.Destroyed).Distinct();
        }
        public static Quest? Quest(XElement value, bool required = false)
        {
            int id = Int(value, "quest_id");
            if (id < 0 && value.Element("quest_id") != null) throw new InvalidDataException("CQF_AI_InvalidTool: quest_id");
            if (id < 0 && !required) return null;
            return Find.QuestManager?.QuestsListForReading.FirstOrDefault(quest => quest.id == id) ?? throw new InvalidDataException("CQF_AI_MissingResource: quest " + id);
        }
        public static Dictionary<string, TargetInfo> Targets(XElement value)
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            XElement? root = value.Element("targets");
            if (root == null) return result;
            if (root.HasAttributes || root.Elements().Count() > 32 || root.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value))) throw new InvalidDataException("CQF_AI_InvalidTool: targets");
            foreach (XElement entry in root.Elements())
            {
                if (entry.Name != "target" || entry.HasAttributes || entry.Elements().GroupBy(element => element.Name).Any(group => group.Count() > 1) || entry.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value))) throw new InvalidDataException("CQF_AI_InvalidTool: target");
                Fields(entry, "key", "thing_id", "map_id", "x", "z");
                string key = Text(entry, "key");
                if (string.IsNullOrWhiteSpace(key) || key.Length > 200 || result.ContainsKey(key)) throw new InvalidDataException("CQF_AI_InvalidTool: target key");
                if (entry.Element("thing_id") != null)
                {
                    if (entry.Elements().Any(element => element.Name != "thing_id" && element.Name != "key")) throw new InvalidDataException("CQF_AI_InvalidTool: ambiguous target");
                    result.Add(key, new TargetInfo(Thing(Text(entry, "thing_id"))));
                }
                else
                {
                    Map map = Map(entry); IntVec3 cell = new IntVec3(Int(entry, "x"), 0, Int(entry, "z"));
                    if (!cell.InBounds(map)) throw new InvalidDataException("CQF_AI_MapBounds");
                    result.Add(key, new TargetInfo(cell, map));
                }
            }
            return result;
        }
        public static XElement Page(string name, IEnumerable<XElement> values, XElement request)
            => CQFAIRuntimeCatalog.Page(name, values, Offset(request), Limit(request));
        public static int Offset(XElement request) { int value = Int(request, "offset", 0); CQFAIRuntimeCatalog.ValidatePage("", value, Limit(request)); return value; }
        public static int Limit(XElement request) { int value = Int(request, "limit", 20); if (value < 1 || value > 40) throw new InvalidDataException("CQF_AI_InvalidTool: limit"); return value; }
    }
}
