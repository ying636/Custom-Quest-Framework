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
        float width = inRect.width - x - 12f;
        Widgets.Label(new Rect(x, y, width - 72f, 30f), "CustomMapStep_BackgroundEffects".Translate().Colorize(ColorLibrary.SkyBlue));

        Rect addRect = new Rect(x + width - 64f, y, 28f, 28f);
        if (Widgets.ButtonImage(addRect, TexButton.Plus))
        {
            Find.WindowStack.Add(new Dialog_Select<CustomMapBackgroundEffectDef>(new TextSelectDrawer<CustomMapBackgroundEffectDef>(
                DefDatabase<CustomMapBackgroundEffectDef>.AllDefsListForReading, def => def.LabelCap,
                def => this.backgroundEffects.Add(def), null, def => def.description, null, null, null, null),
                "CQF_MapBackgroundSelectDynamicEffect".Translate()));
        }
        TooltipHandler.TipRegion(addRect, "Add".Translate());

        Rect removeRect = new Rect(x + width - 28f, y, 28f, 28f);
        if (Widgets.ButtonImage(removeRect, TexButton.Delete))
        {
            CQFEditorTools.DrawFloatMenu(this.backgroundEffects.Where(effect => effect != null).ToList(),
                effect => this.backgroundEffects.Remove(effect), effect => effect.LabelCap);
        }
        TooltipHandler.TipRegion(removeRect, "Remove".Translate());
        y += 35f;

        if (!this.backgroundEffects.Any(effect => effect != null))
        {
            Widgets.Label(new Rect(x + 8f, y, width - 16f, 25f), "CQF_None".Translate());
            y += 30f;
        }
        foreach (CustomMapBackgroundEffectDef effect in this.backgroundEffects.Where(effect => effect != null))
        {
            Rect rowRect = new Rect(x, y, width, 30f);
            Widgets.DrawHighlightIfMouseover(rowRect);
            Widgets.Label(rowRect.ContractedBy(8f, 3f), effect.LabelCap);
            TooltipHandler.TipRegion(rowRect, effect.description);
            y += 32f;
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
