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
            Rect scenPartRect = listing.GetScenPartRect(this, this.maps.Count * 30f + ScenPart.RowHeight);
            CQFEditorTools.DrawButtonWithIcon(scenPartRect.y,() => CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading,d => this.maps.Add(d),d => d.label),() =>
            CQFEditorTools.DrawFloatMenu(this.maps, d => this.maps.Remove(d), d => d.label),scenPartRect.width + 20f,20);
            scenPartRect.y += 30f;
            this.maps.ForEach(m =>
            {
                Widgets.Label(scenPartRect, m.label);
                scenPartRect.y += 30f;
            });
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
