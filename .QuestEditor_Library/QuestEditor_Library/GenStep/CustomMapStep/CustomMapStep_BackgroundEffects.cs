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
        this.backgroundEffects ??= new List<CustomMapBackgroundEffectDef>();
        Widgets.Label(new Rect(x, y, inRect.width - 40f, 30f), "CustomMapStep_BackgroundEffects".Translate().Colorize(ColorLibrary.SkyBlue));
        y += 35f;
        CQFEditorTools.DrawButtonForList_UseIcon(y, this.backgroundEffects, def => def.LabelCap, () =>
        {
            Find.WindowStack.Add(new Dialog_Select<CustomMapBackgroundEffectDef>(
                DefDatabase<CustomMapBackgroundEffectDef>.AllDefsListForReading,
                null,
                def => def.LabelCap,
                "CQF_MapBackgroundSelectDynamicEffect".Translate(),
                def => this.backgroundEffects.Add(def),
                null,
                null,
                def => def.description));
        }, x + 235f);
        y += 35f;
        foreach (CustomMapBackgroundEffectDef effect in this.backgroundEffects.Where(effect => effect != null))
        {
            Widgets.Label(new Rect(x + 12f, y, 418f, 25f), effect.LabelCap);
            y += 28f;
        }
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
