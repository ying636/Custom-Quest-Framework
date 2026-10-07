using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIRuntimeCatalog
    {
        public static XElement Targets(Map map, string search, string thingId, int offset, int limit)
        {
            ValidatePage(search, offset, limit);
            if (thingId.Length > 120) throw new InvalidDataException("CQF_AI_InvalidTool: thing_id");
            IEnumerable<TargetWithKey> entries = map.GetComponent<MapComponent_CQFTargets>()?.Entries ?? Array.Empty<TargetWithKey>();
            return Page("mapTargets", entries.Where(entry => (thingId.Length == 0 || entry.target.Thing.ThingID == thingId)
                && (search.Length == 0 || entry.key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(entry => entry.key).Select(entry => new XElement("entry", new XAttribute("key", entry.key), Target(entry.target))), offset, limit);
        }
        public static XElement Signals(Map map, string search, int offset, int limit)
            => Signals(CQFSignalCatalog.Build(map), search, offset, limit);
        public static XElement Signals(CQFSignalCatalog catalog, string search, int offset, int limit)
        {
            ValidatePage(search, offset, limit);
            CQFSignalEndpoint[] entries = catalog.Entries.ToArray();
            CQFSignalEndpoint[] matches = entries.Where(entry => search.Length == 0
                || (entry.Signal + " " + entry.SourceLabel + " " + entry.SourceThing?.ThingID).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(entry => entry.Signal).ThenBy(entry => entry.IsReceiver).ToArray();
            return new XElement("signals", new XAttribute("total", matches.Length), new XAttribute("offset", offset), new XAttribute("limit", limit),
                new XAttribute("hasMore", (long)offset + limit < matches.Length),
                new XAttribute("scope", "CQF map object references; not a signal execution trace or all quest receivers"),
                matches.Skip(offset).Take(limit).Select(entry => new XElement("endpoint",
                    new XAttribute("signal", entry.Signal), new XAttribute("display", entry.DisplaySignal), new XAttribute("receiver", entry.IsReceiver),
                    new XAttribute("automatic", entry.IsAutomatic), new XAttribute("template", entry.IsTemplate), new XAttribute("questScoped", entry.QuestScoped),
                    new XAttribute("partScoped", entry.PartScoped), new XAttribute("part", entry.PartName), new XAttribute("ownerType", entry.Owner.GetType().FullName!),
                    new XAttribute("thingId", entry.SourceThing?.ThingID ?? ""), new XAttribute("source", entry.SourceLabel.Length > 300 ? entry.SourceLabel.Substring(0, 300) : entry.SourceLabel),
                    new XAttribute("senders", entries.Count(other => !other.IsReceiver && entry.Matches(other))),
                    new XAttribute("receivers", entries.Count(other => other.IsReceiver && entry.Matches(other))))));
        }
        public static XElement Target(TargetInfo target)
        {
            XElement result = new XElement("target", new XAttribute("valid", target.IsValid));
            if (target.HasThing)
            {
                Thing thing = target.Thing;
                result.Add(new XAttribute("thingId", thing.ThingID), new XAttribute("def", thing.def?.defName ?? ""),
                    new XAttribute("label", thing.def?.label ?? ""), new XAttribute("spawned", thing.Spawned), new XAttribute("destroyed", thing.Destroyed));
            }
            if (target.Map != null) result.Add(new XAttribute("mapId", target.Map.uniqueID));
            if (target.Cell.IsValid) result.Add(new XAttribute("x", target.Cell.x), new XAttribute("z", target.Cell.z));
            return result;
        }
        public static XElement ThingState(Thing thing)
        {
            XElement result = new XElement("runtimeState", new XAttribute("questTagCount", thing.questTags?.Count ?? 0),
                thing.questTags?.Take(20).Select(tag => new XElement("questTag", tag)));
            if (thing is LootBox box) result.Add(new XAttribute("opened", box.opened));
            if (thing is IThingHolder holder && thing is not Pawn)
            {
                ThingOwner? contents = holder.GetDirectlyHeldThings();
                result.Add(new XAttribute("heldCount", contents?.Count ?? 0), contents?.Take(20).Select(held => new XElement("held", Target(new TargetInfo(held)))));
            }
            return result;
        }
        public static XElement Page(string name, IEnumerable<XElement> entries, int offset, int limit)
        {
            XElement result = new XElement(name);
            int total = 0;
            foreach (XElement entry in entries)
            {
                if (total >= offset && (long)total < (long)offset + limit) result.Add(entry);
                total++;
            }
            result.Add(new XAttribute("total", total), new XAttribute("offset", offset), new XAttribute("limit", limit), new XAttribute("hasMore", (long)offset + limit < total));
            return result;
        }
        public static void ValidatePage(string search, int offset, int limit)
        {
            if (search.Length > 100 || offset < 0 || offset > 100000 || limit < 1 || limit > 40) throw new InvalidDataException("CQF_AI_InvalidTool: pagination/search");
        }
    }
}
