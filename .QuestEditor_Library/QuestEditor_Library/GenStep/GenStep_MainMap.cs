using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class GenStep_MainMap : GenStep
    {
        public override int SeedPart => 1150612;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!(map.Parent is MainSite site))
            {
                return;
            }
            CustomMapDataDef def = this.GetMainSiteMap(site);
            if (def == null)
            {
                return;
            }
            MainMapGenerationContext.CurrentSite = site;
            MainMapGenerationContext.CurrentDef = site.mainMapDef;
            try
            {
                GenStep_CustomMap.SpawnCustomMap(map, parms, def, site.quest,
                    site.dev, null,
                    false, false,
                    false, def.destroyAllThing, site.replaceMapGeneration);
            }
            finally
            {
                MainMapGenerationContext.CurrentSite = null;
                MainMapGenerationContext.CurrentDef = null;
            }
        }

        private CustomMapDataDef GetMainSiteMap(MainSite site)
        {
            if (site == null || site.mainMapDef == null || site.mainMapDef.maps.NullOrEmpty())
            {
                return site?.mapDef;
            }
            Quest quest = site.quest;
            foreach (MainMapAndCondition item in site.mainMapDef.maps)
            {
                if (item != null && item.Satisfied(quest))
                {
                    CustomMapDataDef map = item.set?.GetMap();
                    if (map != null)
                    {
                        site.mapDef = map;
                        return map;
                    }
                }
            }
            return site.mapDef;
        }
    }
}
