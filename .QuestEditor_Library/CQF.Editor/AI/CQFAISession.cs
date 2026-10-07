using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAISession
    {
        public CQFAISession(string? id = null)
        {
            Id = id ?? Guid.NewGuid().ToString("N");
            if (!Guid.TryParseExact(Id, "N", out _)) throw new InvalidDataException("CQF_AI_InvalidHistory");
        }
        public string Id { get; }
        public string Title { get; set; } = string.Empty;
        public DateTime Updated { get; set; } = DateTime.UtcNow;
        public string Draft { get; set; } = string.Empty;
        public List<CQFAIMessage> Messages { get; } = new List<CQFAIMessage>();
        public List<CQFAIActivity> Activities { get; } = new List<CQFAIActivity>();
        public CQFAITokenTotals TaskUsage { get; } = new CQFAITokenTotals();
        public CQFAITokenTotals SessionUsage { get; } = new CQFAITokenTotals();
        public string Preview => Messages.LastOrDefault(message => message.Role is "user" or "assistant")?.DisplayContent.Replace('\n', ' ') ?? Draft.Replace('\n', ' ');
        public XElement Save() => new XElement("CQF_AI_Session", new XAttribute("version", 1), new XAttribute("id", Id), new XAttribute("updated", Updated.ToUniversalTime()),
            new XElement("title", Title), new XElement("preview", Preview.Length > 120 ? Preview.Substring(0, 120) : Preview), new XElement("draft", Draft), new XElement("messages", Messages.Where(message => message.IsVisible).Select(message => new XElement("message",
                new XAttribute("role", message.Role), message.DisplayContent))), new XElement("activities", Activities.Select(activity => activity.Save())), TaskUsage.Save("taskUsage"), SessionUsage.Save("sessionUsage"));
        public static CQFAISession Restore(XElement value)
        {
            if (value.Name != "CQF_AI_Session" || (int?)value.Attribute("version") != 1) throw new InvalidDataException("CQF_AI_InvalidHistory");
            CQFAISession session = new CQFAISession((string?)value.Attribute("id") ?? "")
            { Title = value.Element("title")?.Value ?? "", Updated = ((DateTime)value.Attribute("updated")!).ToUniversalTime(), Draft = value.Element("draft")?.Value ?? "" };
            if (session.Title.Length > 100 || session.Draft.Length > 600000) throw new InvalidDataException("CQF_AI_InvalidHistory");
            foreach (XElement message in value.Element("messages")?.Elements("message") ?? Enumerable.Empty<XElement>())
            {
                string role = (string?)message.Attribute("role") ?? "";
                if (role is not ("user" or "assistant" or "system")) throw new InvalidDataException("CQF_AI_InvalidHistory");
                session.Messages.Add(new CQFAIMessage(role, message.Value, visible: true));
            }
            if (session.Messages.Sum(message => (long)message.Content.Length) > 600000 || session.Messages.Count > 10000) throw new InvalidDataException("CQF_AI_InvalidHistory");
            session.Activities.AddRange((value.Element("activities")?.Elements("activity") ?? Enumerable.Empty<XElement>()).Select(CQFAIActivity.Restore));
            if (session.Activities.Count > 10000 || session.Activities.Any(activity => activity.MessageIndex > session.Messages.Count)) throw new InvalidDataException("CQF_AI_InvalidHistory");
            session.TaskUsage.Restore(value.Element("taskUsage") ?? throw new InvalidDataException("CQF_AI_InvalidHistory"));
            session.SessionUsage.Restore(value.Element("sessionUsage") ?? throw new InvalidDataException("CQF_AI_InvalidHistory"));
            return session;
        }
    }
}
