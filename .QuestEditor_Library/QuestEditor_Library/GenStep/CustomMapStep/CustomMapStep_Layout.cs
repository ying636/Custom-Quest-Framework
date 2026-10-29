using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CustomMapStep_Layout : CustomMapStep
{
    public CQFLayoutPlan Preview(Map map, string scope = "")
    {
        Validate();
        CQFMapRegionCollection regions = MapComponent_CustomMapData.GetComp(map).Regions;
        HashSet<IntVec3> occupied = new HashSet<IntVec3>();
        foreach (Thing thing in map.listerThings.AllThings)
            if (!(thing is Plant) && !(thing is Filth) && !(thing is Mote) && !(thing is ZoneCore)) occupied.UnionWith(thing.OccupiedRect().Cells);
        bool NaturalCell(IntVec3 cell) => cell.InBounds(map) && cell.GetTerrain(map).passability != Traversability.Impassable && !occupied.Contains(cell);
        CQFLayoutPlanningContext context = new CQFLayoutPlanningContext(map.Size, this, DefDatabase<CustomMapDataDef>.AllDefsListForReading,
            regions.All, NaturalCell, NaturalCell, c => c.Walkable(map));
        context.Blocked.UnionWith(GenStep_CustomMap.disgenerate);
        foreach (Designation designation in map.designationManager.designationsByDef[QEDefOf.QE_Disgenerate]) context.Blocked.Add(designation.target.Cell);
        foreach (Designation designation in map.designationManager.designationsByDef[QEDefOf.QE_Disdestroy]) context.Plan.Protected.Add(designation.target.Cell);
        foreach (Thing thing in map.listerThings.AllThings)
            if (thing.OccupiedRect().Cells.Any(context.Plan.Protected.Contains)) context.Plan.Protected.UnionWith(thing.OccupiedRect().Cells);
        foreach (var count in GenStep_CustomMap.generatedCount) context.Counts[count.Key.Origin] = count.Value;
        List<CQFMapRegion?> references = string.IsNullOrWhiteSpace(referenceKey) && referenceTags.Count == 0
            ? new List<CQFMapRegion?> { null }
            : regions.Select(referenceKey, referenceTags, scope, !eachReference).Select(r => (CQFMapRegion?)r).ToList();
        CQFLayoutPlan plan = new CQFLayoutPlanner().Plan(context, references);
        CQFLayoutExecution.ValidateTargets(plan.Parts.Select(p => p.Data), map.GetComponent<MapComponent_CQFTargets>().Entries.Select(t => t.key));
        return plan;
    }

    public override void Generate(Map map, CustomMapDataDef def, CustomMapGenerationContext context)
    {
        if (def?.preCustomSteps.Contains(this) == true) throw new CQFLayoutException("CQF_Layout_PostStepRequired");
        Generate(map, context);
    }

    public void Generate(Map map, CustomMapGenerationContext context, CQFLayoutPlan? expected = null)
    {
        CQFLayoutPlan plan = Preview(map, context.RegionScope);
        if (expected != null && !expected.Matches(plan)) throw new CQFLayoutException("CQF_Layout_PreviewChanged");
        using CQFLayoutGenerationBatch batch = new CQFLayoutGenerationBatch(context.Quest);
        Rand.PushState(seed);
        try
        {
            CQFMapRegionCollection registry = MapComponent_CustomMapData.GetComp(map).Regions;
            string scope = registry.NextScope(outputKey);
            int index = 0;
            foreach (CQFLayoutPlacement placement in plan.Parts)
            {
                using (new CQFLayoutExecution(map, placement.Bounds.Cells, plan.Protected))
                {
                    List<Thing> generated = placement.Data.GenerateByCore(placement.Origin, map, context.Quest, destroyThings: true);
                    if (generated == null) throw new CQFLayoutException("CQF_Layout_GenerationFailed: " + placement.Source.defName);
                }
                registry.Add(new CQFMapRegion
                {
                    key = "Part_" + ++index, name = placement.Source.LabelCap, tags = new List<string>(outputTags),
                    cells = placement.Bounds.Cells.ToList(), connections = placement.Entrances().Where(c => placement.Bounds.Contains(c.cell)
                        && !placement.Bounds.Contains(c.cell + c.direction.FacingCell)).Select(c => c.Copy()).ToList()
                }, scope, map);
            }
            if (plan.Roads.Count > 0)
            {
                if (roadTerrain == null) throw new CQFLayoutException("CQF_Layout_MissingRoadTerrain");
                foreach (IntVec3 cell in plan.Roads.OrderBy(c => c.x).ThenBy(c => c.z))
                {
                    if (plan.Protected.Contains(cell) || !plan.Allowed.Contains(cell)) throw new CQFLayoutException("CQF_Layout_ProtectedWrite: " + cell);
                    if (cell.GetThingList(map).Any(t => !(t is Plant) && !(t is Filth) && !(t is Mote)))
                        throw new CQFLayoutException("CQF_Layout_RoadBlocked: " + cell);
                    foreach (Thing thing in cell.GetThingList(map).ToList()) if (thing is Plant || thing is Filth) thing.Destroy();
                    map.terrainGrid.SetTerrain(cell, roadTerrain);
                }
            }
            batch.Complete();
            if (connectRoads) new CQFLayoutRoadPlanner().Validate(map, plan, registry.All);
            foreach (string notice in plan.Notices) Log.Warning(new CQFLayoutException(notice).Message);
            if (Current.ProgramState == ProgramState.Playing) map.mapDrawer.RegenerateEverythingNow();
        }
        catch (Exception exception) { Log.Error("CQF_Layout_GenerationFailed: " + layout?.defName + " " + exception); throw; }
        finally { Rand.PopState(); }
    }

    public void Validate()
    {
        if (layout == null) throw new CQFLayoutException("CQF_Layout_MissingDefinition");
        if (spacing < 0 || edgeMargin < 0 || gridSize < 4 || roadWidth < 1 || roadWidth > 9 || gridSize <= roadWidth + 2
            || distance.min < 0 || distance.max < distance.min || maxAttempts < 1 || maxAttempts > 100000 || clusterCount < 1 || clusterCount > 64
            || vacancy < 0f || vacancy >= 1f || string.IsNullOrWhiteSpace(outputKey)) throw new CQFLayoutException("CQF_Layout_InvalidSettings");
        if (relation != "Around" && relation != "Inside" && relation != "Outside" && relation != "Anywhere") throw new CQFLayoutException("CQF_Layout_InvalidRelation");
        if (connectRoads && roadTerrain == null) throw new CQFLayoutException("CQF_Layout_MissingRoadTerrain");
    }

    public override void Draw(ref float y, Rect rect, float x)
    {
        if (!buffersInitialized)
        {
            minBuffer = distance.min.ToString(); maxBuffer = distance.max.ToString();
            vacancyBuffer = vacancy.ToString(System.Globalization.CultureInfo.InvariantCulture); buffersInitialized = true;
        }
        using CQFUIScope scope = new CQFUIScope(rect.width);
        float width = rect.width - x - 12f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), layout?.LabelCap ?? "CQF_Layout_MissingDefinition".Translate()))
            CQFEditorTools.DrawFloatMenu(DefDatabase<CQFMapLayoutDef>.AllDefsListForReading, d => layout = d, d => d.LabelCap);
        y += 38f;
        TextField("CQF_Layout_ReferenceKey", ref referenceKey, ref y, rect, x);
        TagsField("CQF_Layout_ReferenceTags", ref referenceTags, ref y, rect, x);
        Widgets.CheckboxLabeled(new Rect(x, y, width, 28f), "CQF_Layout_EachReference".Translate(), ref eachReference); y += 34f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), ("CQF_Layout_" + relation).Translate()))
            CQFEditorTools.DrawFloatMenu(new List<string> { "Around", "Inside", "Outside", "Anywhere" }, v => relation = v, v => ("CQF_Layout_" + v).Translate());
        y += 38f;
        TextField("CQF_Layout_AllowedRegion", ref allowedRegion, ref y, rect, x);
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), "CQF_Layout_Directions".Translate() + ": " + string.Join(",", directions.Select(d => d.ToStringHuman()))))
            CQFEditorTools.DrawFloatMenu(Enumerable.Range(0, 4).Select(i => new Rot4(i)).ToList(), d => { if (!directions.Remove(d)) directions.Add(d); }, d => d.ToStringHuman());
        y += 38f;
        CQFEditorTools.DrawIntRange(ref y, "CQF_Layout_Distance".Translate(), ref distance, ref minBuffer, ref maxBuffer, x, 60f);
        IntField("CQF_Layout_Seed", ref seed, ref y, rect, x, 0, int.MaxValue);
        IntField("CQF_Layout_Spacing", ref spacing, ref y, rect, x, 0, 100);
        IntField("CQF_Layout_GridSize", ref gridSize, ref y, rect, x, 4, 200);
        IntField("CQF_Layout_EdgeMargin", ref edgeMargin, ref y, rect, x, 0, 100);
        IntField("CQF_Layout_Clusters", ref clusterCount, ref y, rect, x, 1, 64);
        Widgets.Label(new Rect(x, y, width * 0.65f, 28f), "CQF_Layout_Vacancy".Translate());
        Widgets.TextFieldNumeric(new Rect(x + width * 0.65f, y, width * 0.35f, 28f), ref vacancy, ref vacancyBuffer, 0f, 0.95f); y += 34f;
        Widgets.CheckboxLabeled(new Rect(x, y, width, 28f), "CQF_Layout_ConnectRoads".Translate(), ref connectRoads); y += 34f;
        if (connectRoads)
        {
            IntField("CQF_Layout_RoadWidth", ref roadWidth, ref y, rect, x, 1, 9);
            if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), roadTerrain?.LabelCap ?? "CQF_Layout_MissingRoadTerrain".Translate()))
                CQFEditorTools.DrawFloatMenu(DefDatabase<TerrainDef>.AllDefsListForReading, t => roadTerrain = t, t => t.LabelCap);
            y += 38f;
        }
        TextField("CQF_Layout_OutputKey", ref outputKey, ref y, rect, x);
        TagsField("CQF_Layout_OutputTags", ref outputTags, ref y, rect, x);
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), "Add".Translate())) parts.Add(new CQFLayoutPartRule());
        y += 38f;
        foreach (CQFLayoutPartRule rule in parts.ToList())
        {
            if (CQFUIStyle.ButtonText(new Rect(x, y, width - 64f, 30f), rule.role)) Find.WindowStack.Add(CQFEditorBridge.Editor.CreateDrawableWindow(rule));
            if (CQFUIStyle.ButtonText(new Rect(x + width - 60f, y, 60f, 30f), "Delete".Translate())) parts.Remove(rule);
            y += 38f;
        }
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        if (layout != null) result.Add(new XElement("layout", layout.defName));
        if (roadTerrain != null) result.Add(new XElement("roadTerrain", roadTerrain.defName));
        result.Add(new XElement("referenceKey", referenceKey), CQFSerialization.SaveList(referenceTags, "referenceTags"), new XElement("eachReference", eachReference),
            new XElement("relation", relation), new XElement("allowedRegion", allowedRegion), CQFSerialization.SaveList(directions, "directions"), new XElement("distance", distance), new XElement("seed", seed),
            new XElement("spacing", spacing), new XElement("gridSize", gridSize), new XElement("edgeMargin", edgeMargin), new XElement("roadWidth", roadWidth),
            new XElement("connectRoads", connectRoads), new XElement("clusterCount", clusterCount), new XElement("vacancy", vacancy), new XElement("maxAttempts", maxAttempts),
            new XElement("outputKey", outputKey), CQFSerialization.SaveList(outputTags, "outputTags"), CQFSerialization.SaveList_Saveable(parts, "parts"));
        return result;
    }

    private void TextField(string label, ref string value, ref float y, Rect rect, float x)
    {
        Widgets.Label(new Rect(x, y, rect.width - x - 12f, 25f), label.Translate()); y += 26f;
        value = Widgets.TextField(new Rect(x, y, rect.width - x - 12f, 30f), value); y += 36f;
    }

    private void TagsField(string label, ref List<string> values, ref float y, Rect rect, float x)
    {
        string text = string.Join(",", values); TextField(label, ref text, ref y, rect, x);
        values = text.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList();
    }

    private void IntField(string label, ref int value, ref float y, Rect rect, float x, int min, int max)
    {
        float width = rect.width - x - 12f;
        Widgets.Label(new Rect(x, y, width * 0.65f, 28f), label.Translate());
        buffers.TryGetValue(label, out string? buffer);
        Widgets.TextFieldNumeric(new Rect(x + width * 0.65f, y, width * 0.35f, 28f), ref value, ref buffer, min, max); buffers[label] = buffer;
        y += 34f;
    }

    public CQFMapLayoutDef? layout;
    public List<CQFLayoutPartRule> parts = new List<CQFLayoutPartRule>();
    public string referenceKey = "";
    public List<string> referenceTags = new List<string>();
    public bool eachReference;
    public string relation = "Around";
    public string allowedRegion = "";
    public List<Rot4> directions = new List<Rot4>();
    public IntRange distance = new IntRange(12, 45);
    public int seed = 1;
    public int spacing = 6;
    public int gridSize = 24;
    public int edgeMargin = 3;
    public int roadWidth = 3;
    public bool connectRoads;
    public TerrainDef? roadTerrain;
    public int clusterCount = 3;
    public float vacancy = 0.3f;
    public int maxAttempts = 20000;
    public string outputKey = "CQF_Layout";
    public List<string> outputTags = new List<string>();
    private string minBuffer = "";
    private string maxBuffer = "";
    private string vacancyBuffer = "";
    private bool buffersInitialized;
    private readonly Dictionary<string, string?> buffers = new Dictionary<string, string?>();
}
