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
    public static class CQFAction_SentSignalEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SentSignal cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFSignalEditor.DrawBookField(y, "OutSignal".Translate(), cqfReceiver.signal, value => cqfReceiver.signal = value, x, 350f, inRect.width - x - 12f);
            y += 30f;
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.CheckboxLabeled(rect, "SignalOnlyIsValidInPart".Translate(), ref cqfReceiver.signalIsOnlyValidInPart);
            TooltipHandler.TipRegion(rect, "SignalOnlyIsValidInPartTip".Translate());
            y += 30f;
            rect.y += 30f;
            Widgets.CheckboxLabeled(rect, "AddQuestPrefix".Translate(), ref cqfReceiver.addQuestPrefix);
            TooltipHandler.TipRegion(rect, "AddQuestPrefixTip".Translate());
            y += 30f;
        }
    }
}
