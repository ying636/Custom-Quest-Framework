using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.PathFinderJob;

namespace QuestEditor_Library
{
    public static class ZoneConditionEditor
    {
        public static void Draw_0(QuestEditor_Library.ZoneCondition cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Rect rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect, cqfReceiver.GetType().Name.Translate().Colorize(CQFUIStyle.Accent));
            if ((cqfReceiver.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (cqfReceiver.GetType().Name + "_Tip").Translate());
            }

            y += 30f;
        }
    }
}
