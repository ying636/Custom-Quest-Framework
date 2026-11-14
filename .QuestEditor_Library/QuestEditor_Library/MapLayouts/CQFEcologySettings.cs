using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFEcologySettings : ISaveable, IDrawable
{
    public void Validate()
    {
        if (!regenerate) return;
        if (float.IsNaN(plantDensity) || plantDensity < -1f || plantDensity > 1f || float.IsNaN(waterCoverage) || waterCoverage < 0f || waterCoverage > 0.9f
            || float.IsNaN(rockCoverage) || rockCoverage < 0f || rockCoverage > 0.9f || waterCoverage + rockCoverage > 0.9f || patchSize < 4 || patchSize > 128)
            throw new CQFLayoutException("CQF_Ecology_InvalidSettings");
        if (terrains.Any(t => t.terrain == null || t.weight <= 0f || float.IsNaN(t.weight) || float.IsInfinity(t.weight))
            || plants.Any(p => p.plant?.plant == null || p.plant.size.x != 1 || p.plant.size.z != 1 || p.weight <= 0f || float.IsNaN(p.weight) || float.IsInfinity(p.weight))
            || rock != null && (rock.building?.isNaturalRock != true || rock.size.x != 1 || rock.size.z != 1))
            throw new CQFLayoutException("CQF_Ecology_InvalidResource");
    }

    public void Draw(ref float y, Rect rect, float x)
    {
        using CQFUIScope scope = new CQFUIScope(rect.width);
        float width = rect.width - x - 12f;
        Widgets.CheckboxLabeled(new Rect(x, y, width, 28f), "CQF_Ecology_Regenerate".Translate(), ref regenerate); y += 34f;
        if (!regenerate) return;
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), "CQF_Ecology_Biome".Translate() + ": " + (biome?.LabelCap ?? "CQF_Ecology_CurrentBiome".Translate())))
            CQFEditorTools.DrawFloatMenu(DefDatabase<BiomeDef>.AllDefsListForReading, d => biome = d, d => d.LabelCap,
                new List<FloatMenuOption> { new FloatMenuOption("CQF_Ecology_CurrentBiome".Translate(), () => biome = null) });
        y += 38f;
        Number("CQF_Ecology_PlantDensity", ref plantDensity, ref y, rect, x, -1f, 1f);
        Number("CQF_Ecology_WaterCoverage", ref waterCoverage, ref y, rect, x, 0f, 0.9f);
        Number("CQF_Ecology_RockCoverage", ref rockCoverage, ref y, rect, x, 0f, 0.9f);
        Widgets.Label(new Rect(x, y, width * 0.65f, 28f), "CQF_Ecology_PatchSize".Translate());
        Widgets.TextFieldNumeric(new Rect(x + width * 0.65f, y, width * 0.35f, 28f), ref patchSize, ref patchBuffer, 4, 128); y += 34f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), "CQF_Ecology_WaterTerrain".Translate() + ": " + (waterTerrain?.LabelCap ?? "CQF_Ecology_Automatic".Translate())))
            CQFEditorTools.DrawFloatMenu(DefDatabase<TerrainDef>.AllDefsListForReading.Where(d => d.IsWater).ToList(), d => waterTerrain = d, d => d.LabelCap,
                new List<FloatMenuOption> { new FloatMenuOption("CQF_Ecology_Automatic".Translate(), () => waterTerrain = null) });
        y += 38f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), "CQF_Ecology_RockType".Translate() + ": " + (rock?.LabelCap ?? "CQF_Ecology_Automatic".Translate())))
            CQFEditorTools.DrawFloatMenu(DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.building?.isNaturalRock == true && d.size.x == 1 && d.size.z == 1).ToList(), d => rock = d, d => d.LabelCap,
                new List<FloatMenuOption> { new FloatMenuOption("CQF_Ecology_Automatic".Translate(), () => rock = null) });
        y += 38f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), "CQF_Ecology_AddTerrain".Translate())) terrains.Add(new CQFEcologyTerrain()); y += 38f;
        foreach (CQFEcologyTerrain entry in terrains.ToList())
        {
            if (CQFUIStyle.ButtonText(new Rect(x, y, width - 80f, 30f), entry.terrain?.LabelCap ?? "CQF_Ecology_SelectTerrain".Translate())) Find.WindowStack.Add(CQFEditorBridge.Editor.CreateDrawableWindow(entry));
            if (CQFUIStyle.ButtonText(new Rect(x + width - 76f, y, 76f, 30f), "Delete".Translate())) terrains.Remove(entry); y += 38f;
        }
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 30f), "CQF_Ecology_AddPlant".Translate())) plants.Add(new CQFEcologyPlant()); y += 38f;
        foreach (CQFEcologyPlant entry in plants.ToList())
        {
            if (CQFUIStyle.ButtonText(new Rect(x, y, width - 80f, 30f), entry.plant?.LabelCap ?? "CQF_Ecology_SelectPlant".Translate())) Find.WindowStack.Add(CQFEditorBridge.Editor.CreateDrawableWindow(entry));
            if (CQFUIStyle.ButtonText(new Rect(x + width - 76f, y, 76f, 30f), "Delete".Translate())) plants.Remove(entry); y += 38f;
        }
    }

    public XElement SaveToXElement(string nodeName) => new XElement(nodeName, new XElement("regenerate", regenerate), biome == null ? null : new XElement("biome", biome.defName),
        new XElement("plantDensity", plantDensity), new XElement("waterCoverage", waterCoverage), new XElement("rockCoverage", rockCoverage), new XElement("patchSize", patchSize),
        waterTerrain == null ? null : new XElement("waterTerrain", waterTerrain.defName), rock == null ? null : new XElement("rock", rock.defName),
        CQFSerialization.SaveList_Saveable(terrains, "terrains"), CQFSerialization.SaveList_Saveable(plants, "plants"));

    private void Number(string key, ref float value, ref float y, Rect rect, float x, float min, float max)
    {
        float width = rect.width - x - 12f;
        Rect label = new Rect(x, y, width * 0.65f, 28f);
        Widgets.Label(label, key.Translate());
        if (key == "CQF_Ecology_PlantDensity") TooltipHandler.TipRegion(label, "CQF_Ecology_PlantDensityTip".Translate());
        buffers.TryGetValue(key, out string? buffer);
        Widgets.TextFieldNumeric(new Rect(x + width * 0.65f, y, width * 0.35f, 28f), ref value, ref buffer, min, max); buffers[key] = buffer; y += 34f;
    }

    public bool regenerate = true;
    public BiomeDef? biome;
    public float plantDensity = -1f;
    public float waterCoverage = 0.08f;
    public float rockCoverage = 0.12f;
    public int patchSize = 24;
    public TerrainDef? waterTerrain;
    public ThingDef? rock;
    public List<CQFEcologyTerrain> terrains = new List<CQFEcologyTerrain>();
    public List<CQFEcologyPlant> plants = new List<CQFEcologyPlant>();
    private string? patchBuffer;
    private readonly Dictionary<string, string?> buffers = new Dictionary<string, string?>();
}
