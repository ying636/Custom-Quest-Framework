using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicBinding : ISaveable
{
    public CQFCinematicBinding() { }

    public CQFCinematicBinding(string key, TargetInfo target)
    {
        Game game = Current.Game ?? throw new InvalidOperationException("CQF_Cinematic_MapRequired".Translate());
        Map map = target.Map ?? throw new InvalidOperationException("CQF_Cinematic_MapRequired".Translate());
        if (!target.IsValid || map.Disposed || !game.Maps.Contains(map) || target.Thing != null && !map.spawnedThings.Contains(target.Thing))
            throw new InvalidOperationException("CQF_Cinematic_InvalidBinding".Translate());
        this.key = key;
        worldSeed = game.World.info.seedString;
        worldID = game.World.info.persistentRandomValue;
        mapID = map.uniqueID;
        thingID = target.Thing?.GetUniqueLoadID() ?? "";
        cell = target.Cell;
    }

    public bool Matches(TargetInfo target)
    {
        Game? game = Current.Game;
        return game != null && game.World.info.seedString == worldSeed && game.World.info.persistentRandomValue == worldID
            && target.Map?.uniqueID == mapID && (target.Thing != null ? target.Thing.GetUniqueLoadID() == thingID
                : thingID.Length == 0 && target.Cell == cell);
    }

    public TargetInfo Resolve(Map fallbackMap)
    {
        Game game = Current.Game ?? throw new InvalidOperationException("CQF_Cinematic_MapRequired".Translate());
        if (game.World.info.seedString != worldSeed || game.World.info.persistentRandomValue != worldID)
            throw new InvalidOperationException("CQF_Cinematic_BindingWorldMismatch".Translate());
        IEnumerable<Map> maps = game.Maps;
        GameComponent_MapManager? manager = game.GetComponent<GameComponent_MapManager>();
        if (manager != null) maps = maps.Concat(manager.HibernatingMaps);
        Map? map = fallbackMap != null && !fallbackMap.Disposed && fallbackMap.uniqueID == mapID && maps.Contains(fallbackMap)
            ? fallbackMap : maps.FirstOrDefault(candidate => !candidate.Disposed && candidate.uniqueID == mapID);
        if (map == null) throw new InvalidOperationException("CQF_Cinematic_BindingMapMissing".Translate());
        if (!string.IsNullOrEmpty(thingID))
        {
            Thing? thing = map.spawnedThings.FirstOrDefault(candidate => !candidate.Destroyed && candidate.GetUniqueLoadID() == thingID);
            if (thing == null) throw new InvalidOperationException("CQF_Cinematic_BindingThingMissing".Translate());
            return new TargetInfo(thing);
        }
        if (!cell.IsValid || !cell.InBounds(map)) throw new InvalidOperationException("CQF_Cinematic_InvalidBinding".Translate());
        return new TargetInfo(cell, map);
    }

    public XElement SaveToXElement(string nodeName)
    {
        return new XElement(nodeName, new XElement("key", key), new XElement("worldSeed", worldSeed),
            new XElement("worldID", worldID), new XElement("mapID", mapID), new XElement("thingID", thingID),
            CQFSerialization.XElementFromObject(cell, typeof(IntVec3), "cell"));
    }

    public string key = "";
    public string worldSeed = "";
    public int worldID;
    public int mapID = -1;
    public string thingID = "";
    public IntVec3 cell = IntVec3.Invalid;
}
