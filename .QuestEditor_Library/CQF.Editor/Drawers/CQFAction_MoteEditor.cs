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
    public static class CQFAction_MoteEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Mote cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "CQF_MoteDef".Translate(cqfReceiver.mote?.defName), false))
            {
                Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextSelectDrawer<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll(d => d.category == ThingCategory.Mote), d => d.defName, d => cqfReceiver.mote = d, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
            CQFEditorTools.DrawVector(ref y, "MoteOffset".Translate(), ref cqfReceiver.off, ref cqfReceiver.buffer, ref cqfReceiver.buffer2, ref cqfReceiver.buffer3, x, 40f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "MoteScale".Translate(), ref cqfReceiver.scale, ref cqfReceiver.bufferS, x, 80f);
            y += 30f;
        }
    }
}
