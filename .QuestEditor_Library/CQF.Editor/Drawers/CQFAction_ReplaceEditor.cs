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
    public static class CQFAction_ReplaceEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Replace cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 250f, 25f), "CQFReplaceThing".Translate(cqfReceiver.data.stuff?.label + " " + cqfReceiver.data.def?.label), false))
            {
                cqfReceiver.data.OpenSelectDialog();
            }

            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 150f, 25f), "UseSameStuff".Translate(), ref cqfReceiver.useSameStuff);
            y += 30f;
        }
    }
}
