using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.Tilemaps;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class WorldActionEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldAction cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.Label(rect, cqfReceiver.GetType().Name.Translate().Colorize(CQFUIStyle.Accent));
            if ((cqfReceiver.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (cqfReceiver.GetType().Name + "_Tip").Translate());
            }

            y += 30f;
        }
    }
}
