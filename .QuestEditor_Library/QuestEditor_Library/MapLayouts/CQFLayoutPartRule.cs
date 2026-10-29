using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutPartRule : ISaveable, IDrawable
{
    public IEnumerable<(CustomMapDataDef Data, float Weight)> Candidates(IEnumerable<CustomMapDataDef> definitions)
    {
        Dictionary<CustomMapDataDef, float> candidates = new Dictionary<CustomMapDataDef, float>();
        if (pool.tags.Any(t => string.IsNullOrWhiteSpace(t.tag) || t.weight <= 0f || float.IsNaN(t.weight) || float.IsInfinity(t.weight)))
            throw new CQFLayoutException("CQF_Layout_InvalidWeight: " + role);
        foreach (CustomMapDataWithWeight entry in pool.datas)
        {
            if (entry.data == null || entry.weight <= 0f || float.IsNaN(entry.weight) || float.IsInfinity(entry.weight))
                throw new CQFLayoutException("CQF_Layout_InvalidWeight: " + role);
            if (candidates.ContainsKey(entry.data)) throw new CQFLayoutException("CQF_Layout_DuplicatePart: " + entry.data.defName);
            candidates.Add(entry.data, entry.weight);
        }
        foreach (CustomMapDataDef definition in definitions.OrderBy(d => d.defName, StringComparer.Ordinal))
        {
            if (candidates.ContainsKey(definition)) continue;
            CustomMapDataTagWithWeight? tag = pool.tags.FirstOrDefault(t => definition.tags.Contains(t.tag));
            if (tag != null && tag.weight > 0f) candidates.Add(definition, tag.weight);
        }
        foreach (KeyValuePair<CustomMapDataDef, float> entry in candidates.OrderBy(p => p.Key.defName, StringComparer.Ordinal))
            if (entry.Key.isPart && requiredTags.All(entry.Key.tags.Contains) && (anyTags.Count == 0 || anyTags.Any(entry.Key.tags.Contains)) && !excludedTags.Any(entry.Key.tags.Contains))
                yield return (entry.Key, entry.Value);
    }

    public void Draw(ref float y, Rect rect, float x)
    {
        if (!buffersInitialized) { minBuffer = count.min.ToString(); maxBuffer = count.max.ToString(); buffersInitialized = true; }
        using CQFUIScope scope = new CQFUIScope(rect.width);
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_Layout_Role".Translate(), ref role, x, 150f); y += 34f;
        CQFEditorTools.DrawIntRange(ref y, "GenerationCount".Translate(), ref count, ref minBuffer, ref maxBuffer, x, 60f);
        Widgets.CheckboxLabeled(new Rect(x, y, rect.width - x - 12f, 28f), "CQF_Layout_Optional".Translate(), ref optional); y += 34f;
        Widgets.CheckboxLabeled(new Rect(x, y, rect.width - x - 12f, 28f), "CQF_Layout_Rotate".Translate(), ref rotate); y += 34f;
        DrawTags("CQF_Layout_RequiredTags", ref requiredTags, ref y, rect, x);
        DrawTags("CQF_Layout_AnyTags", ref anyTags, ref y, rect, x);
        DrawTags("CQF_Layout_ExcludedTags", ref excludedTags, ref y, rect, x);
        pool.Draw(ref y, rect, x);
    }

    public XElement SaveToXElement(string nodeName) => new XElement(nodeName,
        new XElement("role", role), new XElement("count", count), new XElement("optional", optional), new XElement("rotate", rotate),
        new XElement("maxPerDefinition", maxPerDefinition), pool.SaveToXElement("pool"),
        CQFSerialization.SaveList(requiredTags, "requiredTags"), CQFSerialization.SaveList(anyTags, "anyTags"), CQFSerialization.SaveList(excludedTags, "excludedTags"));

    private void DrawTags(string label, ref List<string> values, ref float y, Rect rect, float x)
    {
        Widgets.Label(new Rect(x, y, rect.width - x - 12f, 25f), label.Translate()); y += 26f;
        string value = Widgets.TextField(new Rect(x, y, rect.width - x - 12f, 30f), string.Join(",", values));
        values = value.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList(); y += 36f;
    }

    public string role = "Content";
    public IntRange count = new IntRange(3, 5);
    public bool optional;
    public bool rotate = true;
    public int maxPerDefinition;
    public CustomMapGenerationSet pool = new CustomMapGenerationSet();
    public List<string> requiredTags = new List<string>();
    public List<string> anyTags = new List<string>();
    public List<string> excludedTags = new List<string>();
    private string minBuffer = "";
    private string maxBuffer = "";
    private bool buffersInitialized;
}
