using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CustomMapStep_GenStepDef : CustomMapStep
{
    public override void Draw(ref float y, Rect inRect, float x)
    {
        if (Widgets.ButtonText(new Rect(x,y,520f,30f), "CQF_GenStepDef".Translate(step?.label ?? step?.defName), false))
        {
            CQFEditorTools.DrawFloatMenu(DefDatabase<GenStepDef>.AllDefsListForReading, g => step = g, g => g.label ?? g.defName);
        }
        y += 35f;
    }

    public override void Generate(Map map, CustomMapDataDef def, CustomSitePartParams param)
    {
        this.step.genStep.Generate(map,new GenStepParams());
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        result.Add(new XElement("step", this.step.defName));
        return result;
    }

    public GenStepDef step;
}