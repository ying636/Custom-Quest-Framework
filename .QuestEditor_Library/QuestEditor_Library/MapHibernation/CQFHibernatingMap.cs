using System.Collections.Generic;
using System.Linq;
using Verse;

namespace QuestEditor_Library
{
    internal sealed class CQFHibernatingMap
    {
        public CQFHibernatingMap(Map map)
        {
            Map = map;
            RegionUpdaterEnabled = map.regionAndRoomUpdater.Enabled;
            Things = map.spawnedThings.ToList();
            Regions = map.regionGrid.AllRegions_NoRebuild_InvalidAllowed.ToList();
            Districts = map.regionGrid.allDistricts
                .Concat(map.regionGrid.AllRooms.SelectMany(room => room.Districts))
                .Concat(Regions.Select(region => region.District).Where(district => district != null))
                .Distinct().ToList();
        }

        public Map Map { get; }
        public bool RegionUpdaterEnabled { get; }
        public List<Thing> Things { get; }
        public List<Region> Regions { get; }
        public List<District> Districts { get; }
    }
}
