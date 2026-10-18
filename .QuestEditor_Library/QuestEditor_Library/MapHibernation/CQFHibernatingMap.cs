using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFHibernatingMap : IExposable
{
    public CQFHibernatingMap() { }

    public CQFHibernatingMap(Map map)
    {
        this.map = map;
        regionUpdaterEnabled = map.regionAndRoomUpdater.Enabled;
    }

    public Map Map => map ?? throw new InvalidOperationException("[CQF] Saved hibernating map is missing in CQF_ownedHibernatingMaps.");
    public bool RegionUpdaterEnabled => regionUpdaterEnabled;

    public void ExposeData()
    {
        Scribe_Values.Look(ref regionUpdaterEnabled, "regionUpdaterEnabled", true);
        if (Scribe.mode != LoadSaveMode.Saving)
        {
            Scribe_Deep.Look(ref map, "map");
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
    private static readonly AccessTools.FieldRef<Thing, sbyte> thingMapIndex = AccessTools.FieldRefAccess<Thing, sbyte>("mapIndexOrState");
}
