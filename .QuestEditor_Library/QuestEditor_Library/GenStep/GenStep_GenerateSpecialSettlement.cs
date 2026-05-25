using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class GenStep_GenerateSpecialSettlement : GenStep
    {
        public override int SeedPart => 710;
        public override void Generate(Map map, GenStepParams parms)
        {
            if (map.ParentFaction is Faction faction) 
            {
                Dictionary<CustomMapDataDef, float> datas = new Dictionary<CustomMapDataDef, float>();
                DefDatabase<SpecialMapGenerationDef>.AllDefsListForReading.ForEach(d =>
                {
                    if (d.factionOfReplacedSettlement == faction.def) 
                    {
                        d.customMapDataTagsToReplace.ForEach(t => DefDatabase<CustomMapDataDef>.AllDefsListForReading.ForEach(mapData =>
                        {
                            if (mapData.tags.Contains(t.tag)) 
                            {
                                datas.SetOrAdd(mapData,t.weight);
                            }
                        }));
                        d.customMapDatasToReplace.ForEach(data => datas.SetOrAdd(data.data, data.weight));
                    }
                });
                
               GenStep_CustomMap.SpawnCustomMap(map, parms,datas.RandomElementByWeight(d => d.Value).Key,null,false, null, false,false,false,true);;
            }
        }
    }
}
