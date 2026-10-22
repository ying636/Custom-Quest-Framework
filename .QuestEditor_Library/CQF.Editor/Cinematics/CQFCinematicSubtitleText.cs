using System.Xml.Linq;

namespace QuestEditor_Library;

public sealed class CQFCinematicSubtitleText
{
    public CQFCinematicSubtitleText() { }

    public CQFCinematicSubtitleText(string key, string chinese, string english)
    {
        Key = key;
        Chinese = chinese;
        English = english;
    }

    public string Key { get; set; } = "";
    public string Chinese { get; set; } = "";
    public string English { get; set; } = "";

    public CQFCinematicSubtitleText Copy() => new(Key, Chinese, English);

    public XElement SaveToXElement() => new("li", new XElement("key", Key),
        new XElement("chinese", Chinese), new XElement("english", English));
}
