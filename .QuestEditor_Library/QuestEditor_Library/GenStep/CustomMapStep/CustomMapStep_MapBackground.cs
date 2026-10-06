using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

    public class CustomMapStep_MapBackground : CustomMapStep
{
    public override void Generate(Map map, CustomMapDataDef def, CustomSitePartParams param)
    {
        if (MapComponent_CustomMapData.GetComp(map) is { } comp)
        {
            List<CustomMapBackgroundEffectDef> backgroundEffects = comp.background?.backgroundEffects;
            comp.background = this.background?.Copy();
            if (comp.background != null && backgroundEffects != null)
            {
                comp.background.backgroundEffects = backgroundEffects;
            }
            if (Current.ProgramState == ProgramState.Playing)
            {
                map.mapDrawer.RegenerateEverythingNow();
            }
        }
    }

    public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapStep_MapBackground.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        if (this.background != null)
        {
            result.Add(this.background.SaveToXElement("background"));
        }
        return result;
    }

    public CustomMapBackgroundData background = new CustomMapBackgroundData();
}
