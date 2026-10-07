using System.Xml;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAISessionStore
    {
        public CQFAISessionStore(string directory) { this.directory = Path.GetFullPath(directory); }
        public IReadOnlyList<CQFAISessionEntry> List(Action<Exception> report)
        {
            List<CQFAISessionEntry> sessions = new List<CQFAISessionEntry>();
            if (!Directory.Exists(directory)) return sessions;
            foreach (string path in Directory.EnumerateFiles(directory, "CQF_*.xml"))
            {
                try
                {
                    CQFAISessionEntry session = ReadEntry(path);
                    if (Path.GetFileName(path) != "CQF_" + session.Id + ".xml") throw new InvalidDataException("CQF_AI_InvalidHistory");
                    sessions.Add(session);
                }
                catch (Exception error) { report(new InvalidDataException("CQF_AI_InvalidHistory: " + Path.GetFileName(path), error)); }
            }
            return sessions.OrderByDescending(session => session.Updated).ToArray();
        }
        public CQFAISession? Recent()
        {
            string path = Path.Combine(directory, "recent.xml");
            if (!File.Exists(path)) return null;
            XElement value = ReadXml(path);
            if (value.Name != "CQF_AI_Recent") throw new InvalidDataException("CQF_AI_InvalidHistory");
            string id = (string?)value.Attribute("id") ?? "";
            return File.Exists(SessionPath(id)) ? Load(id) : null;
        }
        public CQFAISession Load(string id)
        {
            CQFAISession session = Read(SessionPath(id));
            if (session.Id != id) throw new InvalidDataException("CQF_AI_InvalidHistory");
            return session;
        }
        public void Save(CQFAISession session, bool recent = true)
        {
            if (session.Title.Length > 100 || session.Draft.Length > 600000 || session.Messages.Sum(message => (long)message.DisplayContent.Length) > 600000)
                throw new InvalidDataException("CQF_AI_HistoryTooLarge");
            XElement value = session.Save();
            if (value.ToString(SaveOptions.DisableFormatting).Length > 12582912) throw new InvalidDataException("CQF_AI_HistoryTooLarge");
            Write(SessionPath(session.Id), value);
            if (recent) Write(Path.Combine(directory, "recent.xml"), new XElement("CQF_AI_Recent", new XAttribute("id", session.Id)));
        }
        public void Delete(string id)
        {
            File.Delete(SessionPath(id));
            string path = Path.Combine(directory, "recent.xml");
            if (File.Exists(path) && (string?)ReadXml(path).Attribute("id") == id) File.Delete(path);
        }
        private string SessionPath(string id)
        {
            if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidDataException("CQF_AI_InvalidHistory");
            return Path.Combine(directory, "CQF_" + id + ".xml");
        }
        private static CQFAISession Read(string path) => CQFAISession.Restore(ReadXml(path));
        private static CQFAISessionEntry ReadEntry(string path)
        {
            using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 12582912 });
            reader.MoveToContent();
            if (reader.Name != "CQF_AI_Session" || reader.GetAttribute("version") != "1") throw new InvalidDataException("CQF_AI_InvalidHistory");
            string id = reader.GetAttribute("id") ?? "";
            DateTime updated = XmlConvert.ToDateTime(reader.GetAttribute("updated") ?? "", XmlDateTimeSerializationMode.Utc);
            reader.ReadStartElement(); reader.MoveToContent();
            string title = reader.ReadElementContentAsString("title", ""); reader.MoveToContent();
            string preview = reader.ReadElementContentAsString("preview", "");
            return new CQFAISessionEntry(id, title, preview, updated);
        }
        private static XElement ReadXml(string path)
        {
            using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 12582912 });
            return XElement.Load(reader);
        }
        private void Write(string path, XElement value)
        {
            Directory.CreateDirectory(directory);
            string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                value.Save(temporary);
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private readonly string directory;
    }
}
