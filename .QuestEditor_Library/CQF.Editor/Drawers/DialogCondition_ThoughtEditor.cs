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
    public static class DialogCondition_ThoughtEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition_Thought cqfReceiver, ref float y, Rect inRect, float x)
        {
            DialogCondition_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            List<KeyValuePair<ThoughtDef, ThoughtStage>> stagets = new List<KeyValuePair<ThoughtDef, ThoughtStage>>();
            DefDatabase<ThoughtDef>.AllDefsListForReading.ForEach(t =>
            {
                t.stages.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<ThoughtDef, ThoughtStage>(t, s));
                });
            });
            if (Widgets.ButtonText(rect, "CQF_ThoughtDef".Translate(cqfReceiver.thought?.stages.Find(s => s.untranslatedLabel == cqfReceiver.untranslatedLabel)?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<KeyValuePair<ThoughtDef, ThoughtStage>>(new TextSelectDrawer<KeyValuePair<ThoughtDef, ThoughtStage>>(stagets, t => t.Value?.label, t =>
                {
                    cqfReceiver.thought = t.Key;
                    if (t.Key.stages.Contains(t.Value))
                    {
                        cqfReceiver.untranslatedLabel = t.Value.untranslatedLabel;
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
