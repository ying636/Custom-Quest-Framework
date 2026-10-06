using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

    public class CustomMapStep_GenStepDef : CustomMapStep
{
    public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapStep_GenStepDef.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
