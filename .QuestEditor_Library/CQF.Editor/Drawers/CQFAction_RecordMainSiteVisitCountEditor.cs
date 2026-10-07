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
    public static class CQFAction_RecordMainSiteVisitCountEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_RecordMainSiteVisitCount cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFTargetKeyEditor.DrawBookField(y, "MainSiteKey".Translate(), cqfReceiver.key, value => cqfReceiver.key = value, x, 150f, inRect.width - x - 20f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref cqfReceiver.recordKey, x, 150f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 350f, 25f), "RecordToTemporaryBase".Translate(), ref cqfReceiver.recordToTemporaryBase);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 350f, 25f), "RecordToQuestBase".Translate(), ref cqfReceiver.recordToQuestBase);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 350f, 25f), "RecordToGlobalBase".Translate(), ref cqfReceiver.recordToGlobalBase);
            y += 30f;
        }
    }
}
