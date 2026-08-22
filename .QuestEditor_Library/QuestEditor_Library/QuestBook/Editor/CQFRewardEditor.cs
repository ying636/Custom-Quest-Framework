using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFRewardEditor
    {
        public static void OpenThingSelector(Action<ThingDef> selected)
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => (def.category == ThingCategory.Item && !def.IsCorpse)
                    || (def.category == ThingCategory.Building && def.Minifiable))
                .Where(def => def.uiIcon != null
                    && !def.uiIcon.NullOrBad()
                    && def.uiIcon != BaseContent.PlaceholderImage
                    && def.category != ThingCategory.Mote
                    && def.mote == null
                    && def.thingClass != null
                    && !typeof(Mote).IsAssignableFrom(def.thingClass))
                .OrderBy(def => def.label)
                .ToList();
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(
                defs, def => def.uiIcon, def => def.label, selected, null,
                (def, rect) => Widgets.DefIcon(rect, def)), "CQF_QuestBook_SelectRewardThing".Translate()));
        }

        public static void OpenRewardSelector(Action<CQFThingData> selected)
        {
            List<Type> types = typeof(CQFThingData).AllSubclassesNonAbstract()
                .OrderBy(type => type.Name.Translate().ToString())
                .ToList();
            CQFEditorTools.DrawFloatMenu(types, type =>
            {
                if (type == typeof(CQFThingDefCount))
                {
                    OpenThingSelector(definition => selected(new CQFThingDefCount { thing = definition }));
                    return;
                }
                selected((CQFThingData)Activator.CreateInstance(type));
            }, type => type.Name.Translate());
        }
    }
}
