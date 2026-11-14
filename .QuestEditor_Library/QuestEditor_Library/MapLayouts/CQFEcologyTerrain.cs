using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFEcologyTerrain : ISaveable, IDrawable
{
    public void Draw(ref float y, Rect rect, float x)
    {
        if (CQFUIStyle.ButtonText(new Rect(x, y, rect.width - x - 12f, 30f), terrain?.LabelCap ?? "CQF_Ecology_SelectTerrain".Translate()))
            CQFEditorTools.DrawFloatMenu(DefDatabase<TerrainDef>.AllDefsListForReading, d => terrain = d, d => d.LabelCap);
        y += 38f;
        Widgets.Label(new Rect(x, y, 160f, 28f), "Chance".Translate());
        Widgets.TextFieldNumeric(new Rect(x + 170f, y, 100f, 28f), ref weight, ref buffer, 0.001f, 10000f); y += 34f;
    }

    public XElement SaveToXElement(string nodeName) => new XElement(nodeName, terrain == null ? null : new XElement("terrain", terrain.defName), new XElement("weight", weight));

    public TerrainDef? terrain;
    public float weight = 1f;
    private string? buffer;
}
