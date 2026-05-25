using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class GenStep_GenerateSpecialMap : GenStep
    {
        public override int SeedPart => 710;
        public override void Generate(Map map, GenStepParams parms)
        {
            Dictionary<CustomMapDataDef, float> datas = new Dictionary<CustomMapDataDef, float>();
            this.customMapDataTags.ForEach(t =>
            DefDatabase<CustomMapDataDef>.AllDefsListForReading.ForEach(mapData =>
            {
                if (mapData.tags.Contains(t.tag))
                {
                    datas.SetOrAdd(mapData, t.weight);
                }
            }));
            this.customMapDatas.ForEach(data => datas.SetOrAdd(data.data, data.weight));
            GenStep_CustomMap.SpawnCustomMap(map, parms, datas.RandomElementByWeight(d => d.Value).Key, null, false, null, false, false, false, true); ;
        }

        public List<CustomMapDataTagWithWeight> customMapDataTags = new List<CustomMapDataTagWithWeight>();
        public List<CustomMapDataWithWeight> customMapDatas = new List<CustomMapDataWithWeight>();
    }
}
