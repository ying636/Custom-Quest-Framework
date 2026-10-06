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
    public static class CQFAction_TraitEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Trait cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t =>
            {
                t.degreeDatas.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s));
                });
            });
            if (Widgets.ButtonText(rect, "RequiredTrait".Translate(cqfReceiver.trait?.degreeDatas.Find(d => d.degree == cqfReceiver.degree)?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(new TextSelectDrawer<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, t => t.Value.label, t =>
                {
                    cqfReceiver.trait = t.Key;
                    cqfReceiver.degree = t.Value.degree;
                }, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
        }
    }
}
