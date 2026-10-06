using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

    public class CustomMapStep_BackgroundEffects : CustomMapStep
{
    public override void Generate(Map map, CustomMapDataDef def, CustomSitePartParams param)
    {
        if (MapComponent_CustomMapData.GetComp(map) is { } comp)
        {
            comp.background ??= new CustomMapBackgroundData();
            comp.background.backgroundEffects = this.backgroundEffects.Where(effect => effect != null).ToList();
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
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapStep_BackgroundEffects.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        if (!this.backgroundEffects.NullOrEmpty())
        {
            XElement effects = new XElement("backgroundEffects");
            foreach (CustomMapBackgroundEffectDef effect in this.backgroundEffects.Where(effect => effect != null))
            {
                effects.Add(new XElement("li", effect.defName));
            }
            result.Add(effects);
        }
        return result;
    }

    public List<CustomMapBackgroundEffectDef> backgroundEffects = new List<CustomMapBackgroundEffectDef>();
}
