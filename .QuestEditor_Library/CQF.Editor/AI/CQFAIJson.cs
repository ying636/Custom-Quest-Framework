using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public static class CQFAIJson
    {
        public static XElement Read(string text)
        {
            using XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(text),
                new XmlDictionaryReaderQuotas { MaxDepth = 64, MaxStringContentLength = 2097152, MaxArrayLength = 2097152 });
            return XElement.Load(reader);
        }
        public static string Write(XElement value)
        {
            using MemoryStream stream = new MemoryStream();
            using XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false);
            value.WriteTo(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
