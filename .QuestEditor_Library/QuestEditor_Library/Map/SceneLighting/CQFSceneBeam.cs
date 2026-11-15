#if RIMWORLD_1_7
using UnityEngine;
using Verse;
using System.Xml.Linq;

namespace QuestEditor_Library;

public sealed class CQFSceneBeam : IExposable, ISaveable
{
    public CQFSceneBeam Copy() => (CQFSceneBeam)this.MemberwiseClone();

    public XElement SaveToXElement(string nodeName)
    {
        XElement result = new XElement(nodeName);
        foreach (var field in typeof(CQFSceneBeam).GetFields())
            result.Add(CQFSerialization.XElementFromObject(field.GetValue(this)!, field.FieldType, field.Name, field, true));
        return result;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref this.lightIndex, "lightIndex", 2);
        Scribe_Values.Look(ref this.offset, "offset", 28f);
        Scribe_Values.Look(ref this.slope, "slope", .85f);
        Scribe_Values.Look(ref this.heightSlope, "heightSlope", .45f);
        Scribe_Values.Look(ref this.innerWidth, "innerWidth", 8f);
        Scribe_Values.Look(ref this.outerWidth, "outerWidth", 16f);
        Scribe_Values.Look(ref this.intensity, "intensity", 1f);
        Scribe_Values.Look(ref this.color, "color", new Color(1f, 1f, .98f));
        Scribe_Values.Look(ref this.drift, "drift", 3f);
        Scribe_Values.Look(ref this.sway, "sway", .035f);
        Scribe_Values.Look(ref this.period, "period", 180f);
        Scribe_Values.Look(ref this.skyResponse, "skyResponse", .3f);
        Scribe_Values.Look(ref this.density, "density", .055f);
        Scribe_Values.Look(ref this.dust, "dust", .018f);
        Scribe_Values.Look(ref this.scatterHeight, "scatterHeight", 6f);
        Scribe_Values.Look(ref this.enabled, "enabled", true);
        Scribe_Defs.Look(ref this.scatteringShader, "scatteringShader");
        Scribe_Defs.Look(ref this.compositeShader, "compositeShader");
    }

    public int lightIndex = 2;
    public float offset = 28f;
    public float slope = .85f;
    public float heightSlope = .45f;
    public float innerWidth = 8f;
    public float outerWidth = 16f;
    public float intensity = 1f;
    public Color color = new Color(1f, 1f, .98f);
    public float drift = 3f;
    public float sway = .035f;
    public float period = 180f;
    public float skyResponse = .3f;
    public float density = .055f;
    public float dust = .018f;
    public float scatterHeight = 6f;
    public bool enabled = true;
    public ShaderDef? scatteringShader;
    public ShaderDef? compositeShader;
}
#endif
