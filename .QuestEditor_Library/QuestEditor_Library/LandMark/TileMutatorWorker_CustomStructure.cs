using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    internal class TileMutatorWorker_CustomStructure : TileMutatorWorker
    {
        public TileMutatorWorker_CustomStructure(TileMutatorDef def) : base(def)
        {
        }

        public override void GenerateCriticalStructures(Map map)
        {
            if (this.def.GetModExtension<ModExtension_LandMark>() is ModExtension_LandMark ex
                && ex.maps != null)
            {
                List<CellRect> orGenerateVar = MapGenerator.GetOrGenerateVar<List<CellRect>>("UsedRects");
                List<IntVec3> poss = map.AllCells.ToList().FindAll(p => !orGenerateVar.
                Exists(c => c.Contains(p)));
                for (int i = 0; i < ex.count.RandomInRange; i++)
                {
                    if (ex.maps.GetMap() is CustomMapDataDef mapDef)
                    {
                        mapDef.Generate(poss.RandomElement(), map, null);
                    }
                }
            }
        }



    }
}
