using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library
{
    public static class LevelPather
    {
        public static Map GetRoot(Map map)
        {
            List<Map> parents = GetAllParentMaps(map);
            return parents.Count == 0 ? null : parents[parents.Count - 1];
        }

        public static List<MapPortal> GetPathPortal(Map source, Map destination)
        {
            if (source == null || destination == null || source.Disposed || destination.Disposed)
                throw new ArgumentException("[CQF] MetaLord route requires valid source and destination maps.");
            List<Map> sourceParents = GetAllParentMaps(source);
            List<Map> targetParents = GetAllParentMaps(destination);
            HashSet<Map> targetSet = new HashSet<Map>(targetParents);
            Map common = sourceParents.Find(targetSet.Contains);
            if (common == null)
                throw new InvalidOperationException("[CQF] MetaLord maps have no common ancestor.");
            List<MapPortal> route = new List<MapPortal>();
            for (int i = 0; sourceParents[i] != common; i++)
            {
                if (!(sourceParents[i].Parent is MapParent_Custom parent) || parent.Exit == null)
                    throw new InvalidOperationException("[CQF] MetaLord route has no CQF exit: " + sourceParents[i]);
                route.Add(parent.Exit);
            }
            for (int i = targetParents.IndexOf(common) - 1; i >= 0; i--)
            {
                if (!(targetParents[i].Parent is MapParent_Custom parent) || parent.entrance == null)
                    throw new InvalidOperationException("[CQF] MetaLord route has no CQF entrance: " + targetParents[i]);
                route.Add(parent.entrance);
            }
            return route;
        }

        public static List<CQFMapPortal> GetPathLine(Map source, Map destination)
        {
            List<CQFMapPortal> result = new List<CQFMapPortal>();
            foreach (MapPortal portal in GetPathPortal(source, destination))
                result.Add((CQFMapPortal)portal);
            return result;
        }

        public static List<MapPortal> GetPathLineReverse(Map source, Map destination)
        {
            return GetPathPortal(source, destination);
        }

        public static List<Map> GetAllParentMaps(Map map)
        {
            List<Map> result = new List<Map>();
            HashSet<Map> visited = new HashSet<Map>();
            while (map != null)
            {
                if (!visited.Add(map))
                    throw new InvalidOperationException("[CQF] Cyclic pocket map parent chain: " + map);
                result.Add(map);
                map = (map.Parent as PocketMapParent)?.sourceMap;
            }
            return result;
        }

        public static List<Map> GetParent(MapParent_Custom parent)
        {
            return GetAllParentMaps(parent?.Map);
        }
    }
}
