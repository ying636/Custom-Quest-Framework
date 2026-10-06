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
    public static class CQFAction_GainMoodEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_GainMood cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            List<KeyValuePair<ThoughtDef, ThoughtStage>> stagets = new List<KeyValuePair<ThoughtDef, ThoughtStage>>();
            DefDatabase<ThoughtDef>.AllDefsListForReading.ForEach(t =>
            {
                if (t.IsMemory)
                {
                    t.stages.ForEach(s =>
                    {
                        stagets.Add(new KeyValuePair<ThoughtDef, ThoughtStage>(t, s));
                    });
                }
            });
            if (Widgets.ButtonText(rect, "CQF_ThoughtDef".Translate(cqfReceiver.thought?.stages.Count > cqfReceiver.stage ? cqfReceiver.thought?.stages[cqfReceiver.stage].label : ""), false))
            {
                Find.WindowStack.Add(new Dialog_Select<KeyValuePair<ThoughtDef, ThoughtStage>>(new TextSelectDrawer<KeyValuePair<ThoughtDef, ThoughtStage>>(stagets, t => t.Value?.label, t =>
                {
                    cqfReceiver.thought = t.Key;
                    if (t.Key.stages.Contains(t.Value))
                    {
                        cqfReceiver.stage = t.Key.stages.IndexOf(t.Value);
                    }
                    else
                    {
                        Log.Message("CQF Action Gain Mood Error:A thoughtstage without thought");
                    }
                }, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
        }
    }
}
