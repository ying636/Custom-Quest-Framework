using System.Globalization;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAILiveMapCellState
    {
        public CQFAILiveMapCellState(Map map, Thing thing)
        {
            this.map = map;
            footprint = thing.OccupiedRect().ToArray();
            home = new CellRect(thing.Position.x - thing.RotatedSize.x / 2 - 4, thing.Position.z - thing.RotatedSize.z / 2 - 4,
                thing.RotatedSize.x + 8, thing.RotatedSize.z + 8).ClipInsideMap(map).ToArray();
        }
        public string Current => string.Join("", home.Select(cell => map.areaManager.Home[cell] ? "1" : "0")) + ":"
            + string.Join(",", footprint.Select(cell => map.snowGrid.GetDepth(cell).ToString("R", CultureInfo.InvariantCulture) + "/"
                + (map.sandGrid?.GetDepth(cell) ?? 0f).ToString("R", CultureInfo.InvariantCulture)));
        public void Capture()
        {
            homeValues = home.Select(cell => map.areaManager.Home[cell]).ToArray();
            snow = footprint.Select(cell => map.snowGrid.GetDepth(cell)).ToArray();
            sand = footprint.Select(cell => map.sandGrid?.GetDepth(cell) ?? 0f).ToArray();
        }
        public void Restore()
        {
            if (homeValues == null) return;
            for (int index = 0; index < home.Length; index++) map.areaManager.Home[home[index]] = homeValues[index];
            for (int index = 0; index < footprint.Length; index++) { map.snowGrid.SetDepth(footprint[index], snow![index]); map.sandGrid?.SetDepth(footprint[index], sand![index]); }
        }
        private readonly Map map;
        private readonly IntVec3[] footprint;
        private readonly IntVec3[] home;
        private bool[]? homeValues;
        private float[]? snow;
        private float[]? sand;
    }
}
