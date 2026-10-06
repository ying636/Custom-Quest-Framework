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
    public static class DialogConditionEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition cqfReceiver, ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.Label(rect, cqfReceiver.GetType().Name.Translate().Colorize(ColorLibrary.SkyBlue));
            if ((cqfReceiver.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (cqfReceiver.GetType().Name + "_Tip").Translate());
            }

            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFFailReason".Translate(), ref cqfReceiver.failReason, x, 100f);
            y += 30f;
        }
    }
}
