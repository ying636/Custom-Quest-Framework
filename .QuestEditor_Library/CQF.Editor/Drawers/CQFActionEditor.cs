using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Xml;
using System.Xml.Linq;
using RimWorld.QuestGen;
using Verse.Grammar;
using System.Reflection;
using UnityEngine;
using System.Collections;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using Unity.Collections;
using RimWorld.Planet;
using System.Net.NetworkInformation;
using System.Text;

namespace QuestEditor_Library
{
    public static class CQFActionEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction cqfReceiver, ref float y, Rect inRect, float x)
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
