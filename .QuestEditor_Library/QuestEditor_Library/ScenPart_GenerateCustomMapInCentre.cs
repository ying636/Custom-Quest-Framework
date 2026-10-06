using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace QuestEditor_Library
{
    public class ScenPart_GenerateCustomMapInCentre : ScenPart
    {
        public override void DoEditInterface(Listing_ScenEdit listing)

        {
            if (!CQFEditorBridge.IsLoaded)
            {
                Widgets.Label(listing.GetScenPartRect(this, ScenPart.RowHeight * 3f), "CQF_Editor_NotLoaded".Translate());
                return;
            }

            object[] arguments = new object[]
            {
                listing
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.ScenPart_GenerateCustomMapInCentre.DoEditInterface(None:Verse.Listing_ScenEdit)", this, arguments);
        }

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            if (Find.TickManager.TicksGame < 5f && map != null)
            {
                CustomMapDataDef data = this.maps.RandomElement();
                GenStep_CustomMap.SpawnCustomMap(map, new GenStepParams(), data, null, false, map.Center - new IntVec3(data.size.x / 2, 0, data.size.z / 2), false, false, false, data.destroyAllThing, false, t => t is Building);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.maps, "maps",LookMode.Def);
        }


        public List<CustomMapDataDef> maps = new List<CustomMapDataDef>();
    }
}
