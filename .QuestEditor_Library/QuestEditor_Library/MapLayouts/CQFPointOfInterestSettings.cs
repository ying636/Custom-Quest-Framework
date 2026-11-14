using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFPointOfInterestSettings : ISaveable, IDrawable
{
    public void Validate()
    {
        if (!new[] { "Any", "Shore", "RockEdge", "ForestClearing", "Open" }.Contains(habitat) || featureDistance.min < 0 || featureDistance.max < featureDistance.min || featureDistance.max > 64)
            throw new CQFLayoutException("CQF_Ecology_InvalidSite");
    }

    public void Draw(ref float y, Rect rect, float x)
    {
        using CQFUIScope scope = new CQFUIScope(rect.width);
        float width = rect.width - x - 12f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), ("CQF_Ecology_Habitat_" + habitat).Translate()))
            CQFEditorTools.DrawFloatMenu(new List<string> { "Any", "Shore", "RockEdge", "ForestClearing", "Open" }, v => habitat = v, v => ("CQF_Ecology_Habitat_" + v).Translate());
        y += 38f;
        if (!initialized) { minBuffer = featureDistance.min.ToString(); maxBuffer = featureDistance.max.ToString(); initialized = true; }
        CQFEditorTools.DrawIntRange(ref y, "CQF_Ecology_FeatureDistance".Translate(), ref featureDistance, ref minBuffer, ref maxBuffer, x, 70f);
        Widgets.CheckboxLabeled(new Rect(x, y, width, 28f), "CQF_Ecology_Reachable".Translate(), ref requireReachable); y += 34f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), "CQF_Ecology_AllowedTerrain".Translate()))
            CQFEditorTools.DrawFloatMenu(DefDatabase<TerrainDef>.AllDefsListForReading, d => { if (!allowedTerrains.Contains(d)) allowedTerrains.Add(d); }, d => d.LabelCap);
        y += 38f;
        foreach (TerrainDef terrain in allowedTerrains.ToList())
        {
            Widgets.Label(new Rect(x, y, width - 80f, 30f), terrain.LabelCap);
            if (CQFUIStyle.ButtonText(new Rect(x + width - 76f, y, 76f, 30f), "Delete".Translate())) allowedTerrains.Remove(terrain); y += 38f;
        }
    }

    public XElement SaveToXElement(string nodeName) => new XElement(nodeName, new XElement("habitat", habitat), new XElement("featureDistance", featureDistance),
        new XElement("requireReachable", requireReachable), new XElement("allowedTerrains", allowedTerrains.Select(t => new XElement("li", t.defName))));

    public string habitat = "Any";
    public IntRange featureDistance = new IntRange(1, 8);
    public bool requireReachable = true;
    public List<TerrainDef> allowedTerrains = new List<TerrainDef>();
    private string minBuffer = "";
    private string maxBuffer = "";
    private bool initialized;
}
