using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class CustomDutyTriggerEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomDutyTrigger cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            string key = "CQF_" + cqfReceiver.GetType().Name;
            string tipKey = key + "_Tip";
            Widgets.Label(new Rect(x, y, 260f, 25f), (key.CanTranslate() ? key.Translate().ToString() : cqfReceiver.GetType().Name.Translate().ToString()).Colorize(CQFUIStyle.Accent));
            if (tipKey.CanTranslate())
            {
                TooltipHandler.TipRegion(new Rect(x, y, 260f, 25f), tipKey.Translate());
            }

            y += 30f;
        }
    }
}
