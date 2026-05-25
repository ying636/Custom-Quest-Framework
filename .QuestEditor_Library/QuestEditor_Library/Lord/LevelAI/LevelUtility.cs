using LudeonTK;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public static class LevelUtility
    {
        public static List<MapParent_Custom> GetSubMaps(Map map)
        {
            if (map == null)
            {
                return new List<MapParent_Custom>();
            }
            List<MapParent_Custom> result = new List<MapParent_Custom>();
            if (map.Parent is MapParent_Custom custom)
            {
                result.Add(custom);
            }
            if (map.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData comp)
            {
                comp.Submaps.ForEach(m =>
                {
                    if (!m.Destroyed && m.Map != null)
                    {
                        result.AddRange(LevelUtility.GetSubMaps(m.Map));
                    }
                });
            }
            return result;
        }
        public static Map GetRootMap(Map map)
        {
            if (map?.Parent is PocketMapParent custom)
            {
                return LevelUtility.GetRootMap(custom.sourceMap);
            }

            return map;
        }
    }
}
