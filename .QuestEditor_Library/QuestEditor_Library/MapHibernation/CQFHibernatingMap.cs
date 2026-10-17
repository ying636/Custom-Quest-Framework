using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFHibernatingMap : IExposable
{
    public CQFHibernatingMap() { }

    public CQFHibernatingMap(bool allowLegacy) { this.allowLegacy = allowLegacy; }

    public CQFHibernatingMap(Map map)
    {
        this.map = map;
        regionUpdaterEnabled = map.regionAndRoomUpdater.Enabled;
    }

    public Map Map => map ?? throw new InvalidOperationException(allowLegacy
        ? "[CQF] Legacy hibernation has an unresolved map reference or missing map data."
        : "[CQF] Saved hibernating map is missing in CQF_ownedHibernatingMaps.");
    public bool RegionUpdaterEnabled => regionUpdaterEnabled;
    public bool OwnsMapData => !legacyReference;

    public void ExposeData()
    {
        Scribe_Values.Look(ref regionUpdaterEnabled, "regionUpdaterEnabled", true);
        if (Scribe.mode != LoadSaveMode.Saving)
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars && allowLegacy)
            {
                System.Xml.XmlNode parent = Scribe.loader.curXmlParent;
                System.Xml.XmlNode? node = parent["map"];
                if (node == null && !parent.ChildNodes.OfType<System.Xml.XmlElement>().Any())
                {
                    System.Xml.XmlElement reference = parent.OwnerDocument!.CreateElement("map");
                    reference.InnerText = parent.InnerText.Trim();
                    parent.AppendChild(reference);
                    node = reference;
                }
                legacyReference = node != null && !node.ChildNodes.OfType<System.Xml.XmlElement>().Any();
            }
            if (legacyReference) Scribe_References.Look(ref map, "map");
            else Scribe_Deep.Look(ref map, "map");
            return;
        }
        List<Map> maps = Current.Game.Maps;
        if (maps.Contains(Map) || maps.Count > sbyte.MaxValue)
            throw new InvalidOperationException("[CQF] Cannot serialize hibernating map: invalid active map registration or index limit.");
        maps.Add(Map);
        try
        {
            SetRegionIndex(Map, (sbyte)(maps.Count - 1));
            Scribe_Deep.Look(ref map, "map");
        }
        finally
        {
            SetRegionIndex(Map, -1);
            maps.Remove(Map);
        }
    }

    internal static void SetMapIndex(Map map, sbyte index)
    {
        foreach (Thing thing in map.spawnedThings) thingMapIndex(thing) = index;
        SetRegionIndex(map, index);
    }

    private static void SetRegionIndex(Map map, sbyte index)
    {
        if (map.regionGrid == null) return;
        foreach (District district in map.regionGrid.allDistricts) district.mapIndex = index;
        foreach (Region region in map.regionGrid.AllRegions_NoRebuild_InvalidAllowed) region.mapIndex = index;
    }

    private Map? map;
    private bool regionUpdaterEnabled;
    private readonly bool allowLegacy;
    private bool legacyReference;
    private static readonly AccessTools.FieldRef<Thing, sbyte> thingMapIndex = AccessTools.FieldRefAccess<Thing, sbyte>("mapIndexOrState");
}
