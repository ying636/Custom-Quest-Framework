using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QuestEditor_Library;

public sealed class CQFCinematicDraftStore
{
    public CQFCinematicDraftStore(string directory)
    {
        this.directory = Path.GetFullPath(directory);
    }

    public string? Read(string worldIdentity)
    {
        string path = DraftPath(worldIdentity);
        if (!File.Exists(path)) return null;
        using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2097152
        });
        XElement root = XElement.Load(reader);
        if (root.Name != "CQFCinematicAutosave" || (string?)root.Attribute("world") != worldIdentity || root.Elements().Count() != 1)
            throw new InvalidDataException("CQF_Cinematic_InvalidScene");
        return root.Elements().Single().ToString(SaveOptions.DisableFormatting);
    }

    public string Write(string worldIdentity, string snapshot)
    {
        string destination = DraftPath(worldIdentity);
        XElement draft;
        using (StringReader source = new(snapshot))
        using (XmlReader reader = XmlReader.Create(source, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2097152
        })) draft = XElement.Load(reader);
        if (draft.Name != "CQFCinematicDraft") throw new InvalidDataException("CQF_Cinematic_InvalidScene");
        Directory.CreateDirectory(directory);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string value = new XElement("CQFCinematicAutosave", new XAttribute("world", worldIdentity), draft).ToString();
            File.WriteAllText(temporary, value, new UTF8Encoding(false));
            if (File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return destination;
    }

    public string DraftPath(string worldIdentity)
    {
        if (string.IsNullOrEmpty(worldIdentity)) throw new ArgumentException("CQF_Cinematic_MapRequired", nameof(worldIdentity));
        using SHA256 hash = SHA256.Create();
        string id = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(worldIdentity))).Replace("-", "");
        return Path.Combine(directory, "CQF_" + id + ".cqfdraft");
    }

    public string PreserveInvalidDraft(string worldIdentity)
    {
        string source = DraftPath(worldIdentity);
        string recovery = source + ".recovery-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)
            + "-" + Guid.NewGuid().ToString("N");
        File.Copy(source, recovery, false);
        return recovery;
    }

    private readonly string directory;
}
