using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;
public class CustomMapStep_MapBackground : CustomMapStep
{
    public override void Generate(Map map, CustomMapDataDef def, CustomMapGenerationContext context)
    {
        if (MapComponent_CustomMapData.GetComp(map)is { } comp)
        {
#if RIMWORLD_1_7
            CustomMapBackgroundData placed = this.background?.Copy();
            if (placed?.HasStructures == true && context.Origin != IntVec3.Zero)
                placed.TransformScene(cell => cell + context.Origin);
            if (this.sceneOnly && placed != null)
            {
                comp.background ??= new CustomMapBackgroundData();
                comp.background.cliffs.AddRange(placed.cliffs);
                comp.background.structures.AddRange(placed.structures);
                comp.RefreshBackgroundDrawing();
                return;
            }
#endif
            List<CustomMapBackgroundEffectDef> backgroundEffects = comp.background?.backgroundEffects;
#if RIMWORLD_1_7
            comp.background = placed;
#else
            comp.background = this.background?.Copy();
#endif
            if (comp.background != null && backgroundEffects != null)
            {
#if RIMWORLD_1_7
                foreach (var effect in backgroundEffects)
                    if (!comp.background.backgroundEffects.Contains(effect)) comp.background.backgroundEffects.Add(effect);
#else
                comp.background.backgroundEffects = backgroundEffects;
#endif
            }

            if (Current.ProgramState == ProgramState.Playing)
            {
                map.mapDrawer.RegenerateEverythingNow();
            }
        }
    }

    public override void Draw(ref float y, Rect inRect, float x)
    {
        using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
        this.background ??= new CustomMapBackgroundData();
        this.background.Draw(ref y, inRect, x);
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        if (this.background != null)
        {
            result.Add(this.background.SaveToXElement("background"));
        }
#if RIMWORLD_1_7
        if (this.sceneOnly) result.Add(new XElement("sceneOnly", true));
#endif

        return result;
    }

    public CustomMapBackgroundData background = new CustomMapBackgroundData();
#if RIMWORLD_1_7
    public bool sceneOnly;
#endif
}
