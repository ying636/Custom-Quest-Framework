using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CustomMapStep_GenStepDef : CustomMapStep
{
    public override void Draw(ref float y, Rect inRect, float x)
    {
        float width = inRect.width - x - 12f;
        Widgets.Label(new Rect(x, y, width, 30f), "CustomMapStep_GenStepDef".Translate().Colorize(ColorLibrary.SkyBlue));
        y += 35f;
        if (Widgets.ButtonText(new Rect(x, y, width, 30f),
                this.step?.label ?? this.step?.defName ?? "CQF_NotSelected".Translate(), false))
        {
            CQFEditorTools.DrawFloatMenu(DefDatabase<GenStepDef>.AllDefsListForReading,
                g => this.step = g, g => g.label ?? g.defName);
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
