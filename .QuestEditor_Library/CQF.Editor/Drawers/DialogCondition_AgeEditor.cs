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
    public static class DialogCondition_AgeEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition_Age cqfReceiver, ref float y, Rect inRect, float x)
        {
            DialogCondition_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFAge".Translate(), ref cqfReceiver.age, ref cqfReceiver.buffer, x);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 325f, 25f), "NeedToBeGreater".Translate(), ref cqfReceiver.needToBeGreater);
            y += 30f;
        }
    }
}
