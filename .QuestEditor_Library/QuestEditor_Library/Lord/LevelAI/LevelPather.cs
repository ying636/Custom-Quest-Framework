using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public static class LevelPather
    {
        public static List<MapPortal> GetPathPortal(Map root, Map destination)
        {
            List<MapPortal> result = new List<MapPortal>();
            List<Map> destinationToRoot = GetAllParentMaps(destination);
            if (destinationToRoot.Contains(root))
            {
                result.AddRange(GetPathLine(root, destination));
            }
            else if (root.Parent is MapParent_Custom custom)
            {
                List<Map> rootParent = GetAllParentMaps(destination);
                Map BranchMap = destination;
                while (BranchMap.Parent is MapParent_Custom parent)
                {
                    if (!rootParent.Contains(BranchMap) && parent.entrance != null)
                    {
                        BranchMap = parent.entrance.Map;
                    }
                    else
                    {
                        break;
                    }
                }
                result.AddRange(GetPathLineReverse(custom.Map, BranchMap));
            }
            else
            {
                StringBuilder log = new StringBuilder("?There is a strange bug in Ancient Market Mod");
                log.AppendLine(destination.ToString());
                Log.Error(log.ToString().Trim());

            }
            return result;
        }
        public static List<MapPortal> GetPathLineReverse(Map root, Map destination)
        {
            List<MapPortal> result = new List<MapPortal>();
            Map curMap = root;
            while (curMap.Parent as MapParent_Custom != null)
            {
                if (curMap.Parent is MapParent_Custom map)
                {
                    result.Add(map.Exit);
                    if (curMap == destination)
                    {
                        break;
                    }
                    curMap = map.sourceMap;
                }
            }
            return result;
        }
        public static List<CQFMapPortal> GetPathLine(Map root, Map destination)
        {
            List<CQFMapPortal> result = new List<CQFMapPortal>();

            Map curMap = destination;
            while (curMap.Parent as MapParent_Custom != null)
            {
                if (curMap.Parent is MapParent_Custom map)
                {
                    result.Add(map.entrance);
                    curMap = map.sourceMap;
                    if (curMap == root)
                    {
                        break;
                    }
                }
            }
            return result;
        }

        public static List<Map> GetAllParentMaps(Map target)
        {
            List<Map> result = new List<Map>();
            if (target.Parent is MapParent_Custom map)
            {
                result.AddRange(GetParent(map));
            }
            else
            {
                result.Add(target);
            }
            return result;
        }

        public static List<Map> GetParent(MapParent_Custom map)
        {
            List<Map> result = new List<Map>() { map.Map };
            if (map.sourceMap.Parent is MapParent_Custom map2)
            {
                result.AddRange(GetParent(map2));
            }
            else
            {
                result.Add(map.sourceMap);
            }
            return result;
        }
    }
}
