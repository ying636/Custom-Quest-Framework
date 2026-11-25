using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library;

internal static class CQFOrganizationRuleBook
{
    internal static MetaLordEventDef Copy(MetaLordEventDef source)
    {
        if (source.events.Any(entry => entry?.trigger?.GetType() != typeof(MetaLordEventTrigger)))
            throw new InvalidOperationException("CQF_Organization_UnsupportedTrigger".Translate());
        var xml = new XmlDocument();
        xml.LoadXml(Serialize(source).ToString());
        MetaLordEventDef draft = DirectXmlToObject.ObjectFromXml<MetaLordEventDef>(xml.DocumentElement, false);
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
        return draft;
    }

    internal static MetaLordEventDef Save(MetaLordEventDef draft)
    {
        if (!Regex.IsMatch(draft.defName ?? string.Empty, @"\A[A-Za-z][A-Za-z0-9]*_[A-Za-z0-9_]+\z"))
            throw new InvalidOperationException("CQF_Organization_InvalidDefName".Translate());
        string[] errors = draft.ConfigErrors().ToArray();
        if (errors.Length > 0)
            throw new InvalidOperationException(string.Join("\n", errors));
        MetaLordEventDef? existing = DefDatabase<MetaLordEventDef>.GetNamedSilentFail(draft.defName);
        if (existing?.modContentPack != null)
            throw new InvalidOperationException("CQF_Organization_ModOwned".Translate(draft.defName));
        MetaLordEventDef saved = Copy(draft);
        string directory = Path.Combine(CQFContentPaths.Quests, "Organization");
        string output = Path.Combine(directory, draft.defName + ".xml");
        string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
        Directory.CreateDirectory(directory);
        try
        {
            new XElement("Defs", Serialize(saved)).Save(temporary);
            if (File.Exists(output)) File.Replace(temporary, output, null);
            else File.Move(temporary, output);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        if (existing == null)
        {
            DefDatabase<MetaLordEventDef>.Add(saved);
            return saved;
        }
        existing.label = saved.label;
        existing.description = saved.description;
        existing.tickInterval = saved.tickInterval;
        existing.events = saved.events;
        return existing;
    }

    private static XElement Serialize(MetaLordEventDef definition)
    {
        return new XElement(typeof(MetaLordEventDef).FullName!,
            new XElement("defName", definition.defName),
            definition.label.NullOrEmpty() ? null : new XElement("label", definition.label),
            definition.description.NullOrEmpty() ? null : new XElement("description", definition.description),
            new XElement("tickInterval", definition.tickInterval),
            new XElement("events", definition.events.Select(entry =>
            {
                MetaLordEventTrigger trigger = entry.trigger;
                return new XElement("li", new XElement("key", entry.key),
                    new XElement("once", entry.once), new XElement("cooldownTicks", entry.cooldownTicks),
                    new XElement("trigger", new XElement("type", trigger.type),
                        new XElement("signalTag", trigger.signalTag), new XElement("groupTag", trigger.groupTag),
                        new XElement("minimumMembers", trigger.minimumMembers), new XElement("maximumMembers", trigger.maximumMembers),
                        trigger.sourceSquad?.SaveToXElement("sourceSquad")),
                    new XElement("actions", entry.actions.Select(action => action.SaveToXElement("li"))),
                    new XElement("orders", entry.orders.Select(order => order.SaveToXElement())));
            })));
    }
}
