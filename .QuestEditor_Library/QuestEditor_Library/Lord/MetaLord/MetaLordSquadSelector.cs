using RimWorld;
using System.Xml.Linq;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed class MetaLordSquadSelector
{
    public bool Matches(Lord squad)
    {
        if (squad?.LordJob is not MultLevelLordJobBase job) return false;
        if (!this.name.NullOrEmpty() && job.groupName != this.name) return false;
        if (!this.tag.NullOrEmpty() && !job.groupTags.Contains(this.tag)) return false;
        if (this.mapKey.NullOrEmpty()) return true;

        Map map = squad.Map;
        if (map == null || map.Disposed) return false;
        if (this.mapLookup == CQFCompositeMapLookup.MapTemplate)
        {
            CustomMapDataDef template = (map.Parent as MapParent_Custom)?.mapDataDef ?? (map.Parent as CustomSite)?.mapDef;
            return template?.Origin.defName == this.mapKey;
        }
#if RIMWORLD_1_7
        var instance = GameComponent_MapManager.Instance?.GetCompositeMap(map);
        return instance?.MapDatabase.Find(this.mapKey, this.mapLookup) == map;
#else
        return false;
#endif
    }

    public XElement SaveToXElement(string element)
    {
        var result = new XElement(element);
        if (!this.name.NullOrEmpty()) result.Add(new XElement("name", this.name));
        if (!this.tag.NullOrEmpty()) result.Add(new XElement("tag", this.tag));
        if (!this.mapKey.NullOrEmpty())
        {
            result.Add(new XElement("mapKey", this.mapKey));
            result.Add(new XElement("mapLookup", this.mapLookup));
        }
        return result;
    }

    [NoTranslate] public string name;
    [NoTranslate] public string tag;
    [NoTranslate] public string mapKey;
    public CQFCompositeMapLookup mapLookup = CQFCompositeMapLookup.MapTemplate;
}
