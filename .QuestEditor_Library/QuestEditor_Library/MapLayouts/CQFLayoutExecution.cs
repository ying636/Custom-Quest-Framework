using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutExecution : IDisposable
{
    public CQFLayoutExecution(Map map, IEnumerable<IntVec3> allowed, IEnumerable<IntVec3> protectedCells)
    {
        this.map = map; this.allowed = new HashSet<IntVec3>(allowed); this.protectedCells = new HashSet<IntVec3>(protectedCells);
        previous = current;
        if (previous != null)
        {
            if (previous.map != map) throw new CQFLayoutException("CQF_Layout_NestedMap");
            this.allowed.IntersectWith(previous.allowed); this.protectedCells.UnionWith(previous.protectedCells);
        }
        current = this;
    }

    public static CQFLayoutExecution? Current => current;

    public bool Allows(Map map, IEnumerable<IntVec3> cells) => this.map == map && cells.All(c => allowed.Contains(c) && !protectedCells.Contains(c));

    public void Validate(Map map, CustomMapDataDef definition, IntVec3 origin)
    {
        ValidateTemplate(definition);
        List<IntVec3> cells = definition.GetAllPosition().Select(c => c + origin).ToList();
        foreach (ThingData thing in definition.thingDatas)
        {
            IEnumerable<IntVec3> positions = thing.allPositions.Concat(thing.allRect.SelectMany(r => r.Cells));
            if (!positions.Any()) positions = new[] { thing.position };
            foreach (IntVec3 position in positions) cells.AddRange(GenAdj.OccupiedRect(position + origin, thing.rotation, thing.def.size).Cells);
        }
        foreach (CustomThingData thing in definition.customThings) cells.AddRange(GenAdj.OccupiedRect(thing.position + origin, thing.rotation, thing.def.size).Cells);
        if (!Allows(map, cells)) throw new CQFLayoutException("CQF_Layout_ProtectedWrite: " + definition.defName);
        foreach (Thing thing in cells.SelectMany(c => c.GetThingList(map)).Distinct())
            if (thing.OccupiedRect().Cells.Any(protectedCells.Contains)) throw new CQFLayoutException("CQF_Layout_ProtectedThing: " + thing.ThingID);
    }

    public static void ValidateTemplate(CustomMapDataDef definition)
    {
        if (definition.generationActions.Any(a => a.actions?.Count > 0) || definition.preCustomSteps.Count > 0 || definition.customSteps.Count > 0)
            throw new CQFLayoutException("CQF_Layout_UnsupportedStep: " + definition.defName);
    }

    public static void ValidateTargets(IEnumerable<CustomMapDataDef> definitions, IEnumerable<string> reserved)
    {
        HashSet<string> keys = new HashSet<string>(reserved, StringComparer.Ordinal);
        foreach (CustomMapDataDef definition in definitions)
            foreach (string key in definition.thingDatas.SelectMany(t => t.targetKeys ?? new List<string>())
                .Concat(definition.customThings.Where(t => !(t is CustomThingData_ZoneCore)).SelectMany(t => t.targetKeys ?? new List<string>()))
                .Concat(definition.zoneCores.SelectMany(t => t.targetKeys ?? new List<string>())))
            {
                if (string.IsNullOrWhiteSpace(key) || key != key.Trim() || CQFTargetNames.Reserved.Contains(key))
                    throw new CQFLayoutException("CQF_TargetKeyInvalid: " + definition.defName + " " + key);
                if (!keys.Add(key)) throw new CQFLayoutException("CQF_TargetKeyConflict: " + definition.defName + " " + key);
            }
    }

    public void Dispose()
    {
        if (current != this) throw new CQFLayoutException("CQF_Layout_InvalidScope");
        current = previous;
    }

    private readonly Map map;
    private readonly HashSet<IntVec3> allowed;
    private readonly HashSet<IntVec3> protectedCells;
    private readonly CQFLayoutExecution? previous;
    [ThreadStatic] private static CQFLayoutExecution? current;
}
